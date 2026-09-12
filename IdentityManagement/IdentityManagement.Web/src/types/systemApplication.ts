export interface SystemApplication {
  id: number
  name: string
  description: string
  isActive: boolean
  audience: string
  type?: number | string
  catalogApiKey?: string
  baseUrl?: string
  grantsAdminOnSetup?: boolean
  createdAt?: string
  updatedAt?: string
}

export interface CreateSystemApplicationRequest {
  name: string
  description?: string
  audience: string
  type?: number | string
  baseUrl?: string
  grantsAdminOnSetup?: boolean
}

export interface UpdateSystemApplicationRequest {
  id: number
  name: string
  description?: string
  isActive: boolean
  audience: string
  type?: number | string
  baseUrl?: string
  // Ausente mantem o valor atual no servidor. A desativacao manda corpo parcial de proposito.
  grantsAdminOnSetup?: boolean
}
