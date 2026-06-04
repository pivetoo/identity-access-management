#!/usr/bin/env python3
"""Receiver de deploy do Identity Management. Escuta apenas em 127.0.0.1; o nginx publica em
https://auth.mainstay.com.br/deploy-hook. Autentica por token (constant-time),
recebe o docker-compose no corpo, valida, grava e roda o deploy. Sem SSH de entrada."""
import hmac, os, subprocess, threading
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

TOKEN_FILE = "/opt/identity-management-deploy/token"
COMPOSE_PATH = "/var/www/identity-management/docker-compose.prod.yml"
DEPLOY_SCRIPT = "/opt/identity-management-deploy/deploy.sh"
LOG_PATH = "/opt/identity-management-deploy/last-deploy.log"
HOOK_PATH = "/deploy-hook"
MAX_BODY = 1_000_000
LISTEN = ("127.0.0.1", 9878)

with open(TOKEN_FILE) as fh:
    TOKEN = fh.read().strip()

deploy_lock = threading.Lock()


class Handler(BaseHTTPRequestHandler):
    def _reply(self, code, msg):
        data = msg.encode("utf-8", "replace")
        self.send_response(code)
        self.send_header("Content-Type", "text/plain; charset=utf-8")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def do_GET(self):
        if self.path.rstrip("/") == HOOK_PATH:
            self._reply(200, "ok\n")
        else:
            self._reply(404, "not found\n")

    def do_POST(self):
        if self.path.rstrip("/") != HOOK_PATH:
            self._reply(404, "not found\n")
            return
        provided = self.headers.get("X-Deploy-Token", "")
        if not provided or not hmac.compare_digest(provided, TOKEN):
            self._reply(401, "unauthorized\n")
            return
        length = int(self.headers.get("Content-Length") or 0)
        if length > MAX_BODY:
            self._reply(413, "payload too large\n")
            return
        body = self.rfile.read(length) if length > 0 else b""
        if body:
            text = body.decode("utf-8", "replace")
            if "services:" not in text:
                self._reply(400, "invalid compose (missing 'services:')\n")
                return
            tmp = COMPOSE_PATH + ".tmp"
            with open(tmp, "w") as fh:
                fh.write(text)
            os.replace(tmp, COMPOSE_PATH)
        if not deploy_lock.acquire(blocking=False):
            self._reply(409, "deploy already running\n")
            return
        try:
            proc = subprocess.run(
                ["/usr/bin/env", "bash", DEPLOY_SCRIPT],
                capture_output=True, text=True, timeout=900,
            )
            out = (proc.stdout + proc.stderr)[-4000:]
            with open(LOG_PATH, "w") as fh:
                fh.write(out)
            if proc.returncode == 0:
                self._reply(200, "deploy ok\n" + out)
            else:
                self._reply(500, "deploy failed rc=%d\n%s" % (proc.returncode, out))
        except subprocess.TimeoutExpired:
            self._reply(504, "deploy timeout\n")
        finally:
            deploy_lock.release()

    def log_message(self, *args):
        pass


if __name__ == "__main__":
    ThreadingHTTPServer(LISTEN, Handler).serve_forever()
