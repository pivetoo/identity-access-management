export interface Role {
  id: number
  name: string
  description: string
  contractId: number
  contractName?: string
  companyName?: string
  isRoot: boolean
  isDefault: boolean
  accessResourceIds: number[]
  userCount?: number
  createdAt?: string
  updatedAt?: string
}

export interface CreateRoleRequest {
  name: string
  description?: string
  contractId: number
  isRoot: boolean
  isDefault: boolean
  accessResourceIds?: number[]
}

export interface UpdateRoleRequest {
  name: string
  description?: string
  isRoot: boolean
  isDefault: boolean
  accessResourceIds?: number[]
}

export interface UpdateRolePermissionsRequest {
  accessResourceIds: number[]
}

export interface SetDefaultRoleRequest {
  contractId: number
}
