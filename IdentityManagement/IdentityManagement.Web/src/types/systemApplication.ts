export interface SystemApplication {
  id: number
  name: string
  description: string
  redirectUris: string
  isActive: boolean
  audience: string
  type?: number | string
  createdAt?: string
  updatedAt?: string
}

export interface CreateSystemApplicationRequest {
  name: string
  description?: string
  redirectUris: string
  audience: string
  type?: number | string
}

export interface UpdateSystemApplicationRequest {
  id: number
  name: string
  description?: string
  redirectUris: string
  isActive: boolean
  audience: string
  type?: number | string
}
