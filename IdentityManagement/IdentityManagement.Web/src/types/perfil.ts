export interface Perfil {
  id: number;
  name: string;
  description: string;
  contratoId: number;
  contratoName?: string;
  empresaName?: string;
  isSuperUser: boolean;
  isDefault: boolean;
  permissions: string;
  userCount?: number;
  criadoEm: string;
  ultimaAlteracao?: string;
}

export interface CreatePerfilRequest {
  name: string;
  description?: string;
  contratoId: number;
  isSuperUser: boolean;
  isDefault: boolean;
  permissions?: string;
}

export interface UpdatePerfilRequest {
  name: string;
  description?: string;
  isSuperUser: boolean;
  isDefault: boolean;
  permissions?: string;
}

export interface UpdatePerfilPermissionsRequest {
  permissions: string;
}

export interface SetDefaultPerfilRequest {
  contratoId: number;
}

export interface PerfilSummaryViewModel {
  id: number;
  name: string;
  description: string;
  contratoId: number;
  isSuperUser: boolean;
  isDefault: boolean;
  userCount: number;
}

export interface PerfilDetailViewModel {
  id: number;
  name: string;
  description: string;
  contratoId: number;
  contratoName: string;
  empresaName: string;
  isSuperUser: boolean;
  isDefault: boolean;
  permissions: string;
  criadoEm: string;
  ultimaAlteracao?: string;
}
