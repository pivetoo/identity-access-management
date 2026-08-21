const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL || '/api').replace(/\/+$/, '');

export interface SignupPayload {
  legalName: string;
  tradeName: string;
  document: string;
  email: string;
  phoneNumber?: string;
  annual: boolean;
  acceptedTerms: boolean;
  /** Isca do honeypot. Sempre vazio para gente; robo preenche e o backend descarta em silencio. */
  website?: string;
}

/** Etapa 1. Nada foi provisionado ainda: so existe um cadastro pendente e um e-mail a caminho. */
export interface SignupResult {
  email: string;
  companyName: string;
  planName: string;
  verificationExpiresAt: string;
}

/** Etapa 2. Aqui o ambiente ja existe, e o setupToken emenda direto na definicao de senha. */
export interface SignupConfirmResult {
  setupToken: string;
  companyName: string;
  planName: string;
  trialEndsAt: string | null;
  subscriptionActive: boolean;
}

async function signup(payload: SignupPayload): Promise<SignupResult> {
  const response = await fetch(`${apiBaseUrl}/Signup/Create`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });

  // 429 vem do rate limiter, antes do controller, entao nao tem envelope nem mensagem traduzida.
  if (response.status === 429) {
    throw new Error('Muitas tentativas de cadastro deste endereço. Aguarde alguns minutos e tente de novo.');
  }

  const body = await response.json().catch(() => ({}));

  if (!response.ok) {
    // O backend ja devolve mensagem traduzida no envelope; o fallback cobre 5xx sem corpo.
    throw new Error(body?.message || 'Não foi possível concluir o cadastro. Tente novamente.');
  }

  return body.data as SignupResult;
}

/** Mascara de telefone BR: (00) 0000-0000 (fixo) ou (00) 00000-0000 (celular). */
export function formatPhone(value: string): string {
  const d = (value || '').replace(/\D/g, '').slice(0, 11);
  if (d.length === 0) return '';
  if (d.length <= 2) return `(${d}`;
  const resto = d.slice(2);
  if (resto.length <= 4) return `(${d.slice(0, 2)}) ${resto}`;
  if (resto.length <= 8) return `(${d.slice(0, 2)}) ${resto.slice(0, 4)}-${resto.slice(4)}`;
  return `(${d.slice(0, 2)}) ${resto.slice(0, 5)}-${resto.slice(5)}`;
}

/** Mesmas regras do backend (Phone.IsValid): DDD a partir de 11, celular comecando em 9. */
export function isValidPhone(value: string): boolean {
  const d = (value || '').replace(/\D/g, '');
  if (d.length !== 10 && d.length !== 11) return false;
  if (Number(d.slice(0, 2)) < 11) return false;
  if (d.length === 11 && d[2] !== '9') return false;
  if (d.length === 10 && (d[2] === '0' || d[2] === '1')) return false;
  return !d.slice(2).split('').every((c) => c === d[2]);
}

/**
 * Normaliza para [A-Z0-9] em maiusculo, max 14. O CNPJ alfanumerico da Receita tem letras nas
 * posicoes 1-12, entao descartar nao-digito (como era antes) mutilava o documento e reprovava
 * empresa valida.
 */
export function normalizeDocument(value: string): string {
  return (value || '').replace(/[^0-9A-Za-z]/g, '').toUpperCase().slice(0, 14);
}

/** Mascara de CNPJ para exibicao. O backend normaliza de novo, entao mandar com mascara e seguro. */
export function formatDocument(value: string): string {
  const d = normalizeDocument(value);

  let out = d.slice(0, 2);
  if (d.length > 2) out += '.' + d.slice(2, 5);
  if (d.length > 5) out += '.' + d.slice(5, 8);
  if (d.length > 8) out += '/' + d.slice(8, 12);
  if (d.length > 12) out += '-' + d.slice(12, 14);
  return out;
}

/**
 * Mesmo calculo do backend (Cnpj.IsValid): evita ida ao servidor para erro obvio de digitacao.
 *
 * Aceita o formato alfanumerico: posicoes 1-12 podem ter letra, os verificadores (13-14) seguem
 * numericos, e o valor de cada caractere no modulo 11 e (ASCII - 48) — 'A' vale 17. O CNPJ numerico
 * atual passa pela mesma conta, sem caminho separado.
 */
export function isValidDocument(value: string): boolean {
  const d = normalizeDocument(value);
  if (d.length !== 14 || /^(.)\1+$/.test(d) || !/\d/.test(d[12]) || !/\d/.test(d[13])) {
    return false;
  }

  const charValue = (character: string): number => character.charCodeAt(0) - 48;

  const check = (weights: number[]): number => {
    const sum = weights.reduce((acc, weight, index) => acc + charValue(d[index]) * weight, 0);
    const remainder = sum % 11;
    return remainder < 2 ? 0 : 11 - remainder;
  };

  return check([5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]) === Number(d[12])
    && check([6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]) === Number(d[13]);
}

async function confirm(token: string): Promise<SignupConfirmResult> {
  const response = await fetch(`${apiBaseUrl}/Signup/Confirm`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ token }),
  });

  if (response.status === 429) {
    throw new Error('Muitas tentativas a partir deste endereço. Aguarde um instante e recarregue a página.');
  }

  const body = await response.json().catch(() => ({}));

  if (!response.ok) {
    throw new Error(body?.message || 'Não foi possível confirmar seu e-mail. Tente novamente.');
  }

  return body.data as SignupConfirmResult;
}

export const signupService = { signup, confirm };
