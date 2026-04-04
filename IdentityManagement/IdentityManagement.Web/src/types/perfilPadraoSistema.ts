export interface PerfilPadraoSistema {
  id: number;
  systemApplicationId: number;
  name: string;
  description: string;
  isRoot: boolean;
  isDefault: boolean;
  isActive: boolean;
  accessResourceIds: number[];
  criadoEm?: string;
  ultimaAlteracao?: string;
}

export interface CreatePerfilPadraoSistemaRequest {
  systemApplicationId: number;
  name: string;
  description: string;
  isRoot: boolean;
  isDefault: boolean;
  accessResourceIds: number[];
}

export interface UpdatePerfilPadraoSistemaRequest {
  name: string;
  description: string;
  isRoot: boolean;
  isDefault: boolean;
  isActive: boolean;
  accessResourceIds: number[];
}
