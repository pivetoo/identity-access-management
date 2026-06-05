export interface SystemApplication {
  id: number
  name: string
  description: string
  isActive: boolean
  audience: string
  type?: number | string
  catalogApiKey?: string
  baseUrl?: string
  createdAt?: string
  updatedAt?: string
}

export interface CreateSystemApplicationRequest {
  name: string
  description?: string
  audience: string
  type?: number | string
  baseUrl?: string
}

export interface UpdateSystemApplicationRequest {
  id: number
  name: string
  description?: string
  isActive: boolean
  audience: string
  type?: number | string
  baseUrl?: string
}
