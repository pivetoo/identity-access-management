#!/usr/bin/env bash
# Aplica a versao mais recente das imagens. Chamado pelo webhook (instantaneo, quando
# o IP do runner nao esta bloqueado) E pelo timer de poll (garantia, a cada 30s).
# flock serializa as duas fontes; so faz prune quando algo realmente mudou.
set -euo pipefail
exec 9>/var/lock/identity-management-deploy.lock
flock -n 9 || { echo "deploy already running, skipping"; exit 0; }
cd /var/www/identity-management
before=$(docker compose -f docker-compose.prod.yml images -q 2>/dev/null | sort | tr '\n' ' ')
docker compose -f docker-compose.prod.yml pull -q
docker compose -f docker-compose.prod.yml up -d --remove-orphans
after=$(docker compose -f docker-compose.prod.yml images -q 2>/dev/null | sort | tr '\n' ' ')
if [ "$before" != "$after" ]; then
  docker image prune -f >/dev/null 2>&1 || true
  echo "[deploy] $(date -u '+%Y-%m-%d %H:%M:%S UTC') updated"
else
  echo "[deploy] $(date -u '+%Y-%m-%d %H:%M:%S UTC') no change"
fi
