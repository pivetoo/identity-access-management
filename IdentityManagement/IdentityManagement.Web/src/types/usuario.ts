export interface Usuario {
  id: number;
  username: string;
  email: string;
  name: string;
  isActive: boolean;
  lastLoginAt?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateUsuarioRequest {
  username: string;
  email: string;
  password: string;
  name: string;
}

export interface UpdateUsuarioRequest {
  id: number;
  username: string;
  email: string;
  password?: string;
  name: string;
  isActive: boolean;
}
