export interface UserRole {
  id: number
  userId: number
  username: string
  userEmail: string
  roleId: number
  roleName: string
  contractId?: number
  isRoot?: boolean
  companyName: string
  assignedAt: string
  revokedAt?: string
  isActive: boolean
}

export interface AssignUserToRoleRequest {
  userId: number
  roleId: number
}

export interface RevokeUserFromRoleRequest {
  userId: number
  roleId: number
}

export interface UserRoleSummary {
  id: number
  userId: number
  userName: string
  userEmail: string
  roleId: number
  roleName: string
  isActive: boolean
}
