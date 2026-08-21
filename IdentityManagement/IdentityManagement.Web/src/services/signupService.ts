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

/** Mascara de CNPJ para exibicao. O backend normaliza de novo, entao mandar com mascara e seguro. */
export function formatDocument(value: string): string {
  const digits = value.replace(/\D/g, '').slice(0, 14);
  return digits
    .replace(/^(\d{2})(\d)/, '$1.$2')
    .replace(/^(\d{2})\.(\d{3})(\d)/, '$1.$2.$3')
    .replace(/\.(\d{3})(\d)/, '.$1/$2')
    .replace(/(\d{4})(\d)/, '$1-$2');
}

/** Mesmo calculo do backend (Cnpj.IsValid): evita ida ao servidor para erro obvio de digitacao. */
export function isValidDocument(value: string): boolean {
  const digits = value.replace(/\D/g, '');
  if (digits.length !== 14 || new Set(digits).size === 1) {
    return false;
  }

  const check = (weights: number[]): number => {
    const sum = weights.reduce((acc, weight, index) => acc + Number(digits[index]) * weight, 0);
    const remainder = sum % 11;
    return remainder < 2 ? 0 : 11 - remainder;
  };

  return check([5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]) === Number(digits[12])
    && check([6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]) === Number(digits[13]);
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
