export interface Empresa {
  id: number;
  nome: string;
  nomeFantasia: string;
  documento: string;
  email: string;
  telefone: string;
  isActive: boolean;
  criadoEm: string;
  ultimaAlteracao: string;
}

export interface CreateEmpresaRequest {
  nome: string;
  nomeFantasia: string;
  documento: string;
  email: string;
  telefone: string;
}

export interface UpdateEmpresaRequest {
  id: number;
  nome: string;
  nomeFantasia: string;
  documento: string;
  email: string;
  telefone: string;
  isActive: boolean;
}
