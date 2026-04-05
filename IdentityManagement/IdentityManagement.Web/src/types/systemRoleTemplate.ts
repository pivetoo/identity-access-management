export interface SystemRoleTemplate {
  id: number
  systemApplicationId: number
  name: string
  description: string
  isRoot: boolean
  isDefault: boolean
  isActive: boolean
  accessResourceIds: number[]
  createdAt?: string
  updatedAt?: string
}

export interface CreateSystemRoleTemplateRequest {
  systemApplicationId: number
  name: string
  description: string
  isRoot: boolean
  isDefault: boolean
  accessResourceIds: number[]
}

export interface UpdateSystemRoleTemplateRequest {
  name: string
  description: string
  isRoot: boolean
  isDefault: boolean
  isActive: boolean
  accessResourceIds: number[]
}
