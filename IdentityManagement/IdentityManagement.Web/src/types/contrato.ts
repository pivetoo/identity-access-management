export interface Contrato {
  id: number;
  empresa?: {
    id: number;
    nome: string;
  };
  empresaName?: string;
  sistema?: {
    id: number;
    name: string;
  };
  sistemaName?: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
  isValid?: boolean;
  clientId?: string;
  accessTokenLifetime: number;
  refreshTokenLifetime: number;
  criadoEm?: string;
  ultimaAlteracao?: string;
}

export interface CreateContratoRequest {
  empresaId: number;
  sistemaId: number;
  startDate: string;
  endDate?: string;
  accessTokenLifetime?: number;
  refreshTokenLifetime?: number;
}

export interface UpdateContratoRequest {
  id: number;
  empresaId: number;
  sistemaId: number;
  startDate: string;
  endDate?: string;
  isActive: boolean;
  accessTokenLifetime: number;
  refreshTokenLifetime: number;
}

export interface ContratoSecrets {
  clientId: string;
  clientSecret: string;
  jwtSecretKey: string;
}
