export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  tokenType: string;
  expiresIn: number;
  redirectUrl?: string;
  user: UsuarioViewModel;
  contrato?: ContratoViewModel;
}

export interface UsuarioViewModel {
  id: number;
  username: string;
  email: string;
  name: string;
  isActive: boolean;
  lastLoginAt?: string;
}

export interface ContratoSelectionResponse {
  userId: number;
  userName: string;
  userEmail: string;
  temporaryToken: string;
  availableContratos: ContratoViewModel[];
}

export interface ContratoViewModel {
  contratoId: number;
  sistemaName: string;
  empresaName: string;
  redirectUris: string[];
  perfilName?: string;
}

export interface LoginWithContratoRequest {
  userId: number;
  contratoId: number;
  temporaryToken: string;
}

export interface ApiError {
  message: string;
  status: number;
  errors?: Record<string, string[]>;
}
