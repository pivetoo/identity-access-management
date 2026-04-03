export interface UsuarioPerfil {
  id: number;
  usuarioId: number;
  username: string;
  userEmail: string;
  perfilId: number;
  perfilName: string;
  empresaName: string;
  assignedAt: string;
  revokedAt?: string;
  isActive: boolean;
}

export interface AssignUsuarioToPerfilRequest {
  usuarioId: number;
  perfilId: number;
}

export interface RevokeUsuarioFromPerfilRequest {
  usuarioId: number;
  perfilId: number;
}

export interface UsuarioPerfilSummary {
  id: number;
  usuarioId: number;
  userName: string;
  userEmail: string;
  perfilId: number;
  perfilName: string;
  isActive: boolean;
}
