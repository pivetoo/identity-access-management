const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL || '/api').replace(/\/+$/, '');

export interface SignupPayload {
  legalName: string;
  tradeName: string;
  document: string;
  email: string;
  phoneNumber?: string;
  annual: boolean;
  acceptedTerms: boolean;
}

export interface SignupResult {
  email: string;
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

export const signupService = { signup };
