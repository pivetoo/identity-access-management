export interface Perfil {
  id: number;
  name: string;
  description: string;
  contratoId: number;
  contratoName?: string;
  empresaName?: string;
  isSuperUser: boolean;
  isDefault: boolean;
  accessResourceIds: number[];
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
  accessResourceIds?: number[];
}

export interface UpdatePerfilRequest {
  name: string;
  description?: string;
  isSuperUser: boolean;
  isDefault: boolean;
  accessResourceIds?: number[];
}

export interface UpdatePerfilPermissionsRequest {
  accessResourceIds: number[];
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
  accessResourceIds: number[];
  criadoEm: string;
  ultimaAlteracao?: string;
}
