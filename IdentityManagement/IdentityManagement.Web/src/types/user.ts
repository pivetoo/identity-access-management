export interface User {
  id: number;
  username: string;
  email: string;
  name: string;
  avatarUrl?: string;
  isActive: boolean;
  lastLoginAt?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateUserRequest {
  username: string;
  email: string;
  password: string;
  name: string;
}

export interface UpdateUserRequest {
  id: number;
  username: string;
  email: string;
  password?: string;
  name: string;
  isActive: boolean;
}
