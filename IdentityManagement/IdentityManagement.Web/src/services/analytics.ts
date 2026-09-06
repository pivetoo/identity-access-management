// Medicao first-party via Umami (script /m.js, carregado no index.html so em producao pelo
// data-domains). Fora de producao window.umami nao existe e as chamadas viram no-op.
type UmamiPayload = Record<string, string | number>;

interface UmamiGlobal {
  track: (name: string, data?: UmamiPayload) => void;
}

export function trackEvent(name: string, data?: UmamiPayload): void {
  const umami = (window as { umami?: UmamiGlobal }).umami;
  umami?.track(name, data);
}
