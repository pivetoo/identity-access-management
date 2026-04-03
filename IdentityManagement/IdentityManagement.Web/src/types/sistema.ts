export interface Sistema {
  id: number;
  name: string;
  description: string;
  redirectUris: string;
  isActive: boolean;
  audience: string;
  criadoEm: string;
  ultimaAlteracao: string;
}

export interface CreateSistemaRequest {
  name: string;
  description?: string;
  redirectUris: string;
  audience: string;
}

export interface UpdateSistemaRequest {
  id: number;
  name: string;
  description?: string;
  redirectUris: string;
  isActive: boolean;
  audience: string;
}
