import { httpClient } from 'archon-ui'
import type { UserRole, AssignUserToRoleRequest, RevokeUserFromRoleRequest } from '../types/userRole'

export class UserRoleService {
  private static baseUrl = '/userroles'

  static async getByContract(contractId: number): Promise<UserRole[]> {
    const response = await httpClient.get<UserRole[]>(`${this.baseUrl}/getbycontract/${contractId}`)
    return response.data ?? []
  }

  static async getByUser(userId: number): Promise<UserRole[]> {
    const response = await httpClient.get<UserRole[]>(`${this.baseUrl}/getbyuser/${userId}`)
    return response.data ?? []
  }

  static async getActiveByUser(userId: number): Promise<UserRole[]> {
    const response = await httpClient.get<UserRole[]>(`${this.baseUrl}/getactivebyuser/${userId}`)
    return response.data ?? []
  }

  static async getByRole(roleId: number): Promise<UserRole[]> {
    const response = await httpClient.get<UserRole[]>(`${this.baseUrl}/getbyrole/${roleId}`)
    return response.data ?? []
  }

  static async checkAccess(userId: number, roleId: number): Promise<{ userId: number; roleId: number; hasAccess: boolean }> {
    const response = await httpClient.get<{ hasAccess: boolean }>(`${this.baseUrl}/hasaccess/${userId}/${roleId}`)
    return {
      userId,
      roleId,
      hasAccess: response.data?.hasAccess ?? false,
    }
  }

  static async getById(id: number): Promise<UserRole> {
    const userRoles = await this.getByUser(id)
    const userRole = userRoles.find((item) => item.id === id)

    if (!userRole) {
      throw new Error('Vinculação não encontrada.')
    }

    return userRole
  }

  static async assign(request: AssignUserToRoleRequest): Promise<{ message: string; userId: number; roleId: number }> {
    const response = await httpClient.post<{ message?: string }>(`${this.baseUrl}/assign`, {
      userId: request.userId,
      roleId: request.roleId,
    })

    return {
      message: response.message,
      userId: request.userId,
      roleId: request.roleId,
    }
  }

  static async revoke(userId: number, roleId: number): Promise<{ message: string; userId: number; roleId: number }> {
    const response = await httpClient.delete(`${this.baseUrl}/revoke`, {
      data: {
        userId: userId,
        roleId: roleId,
      } as RevokeUserRoleRequestBackend,
    })

    return {
      message: response.message,
      userId,
      roleId,
    }
  }

  static async reactivate(userId: number, roleId: number): Promise<{ message: string; userId: number; roleId: number }> {
    const response = await httpClient.post(`${this.baseUrl}/reactivate`, {
      userId: userId,
      roleId: roleId,
    } as RevokeUserRoleRequestBackend)

    return {
      message: response.message,
      userId,
      roleId,
    }
  }

  static async delete(id: number): Promise<{ message: string }> {
    const userRole = await this.getById(id)
    const result = await this.revoke(userRole.userId, userRole.roleId)
    return { message: result.message }
  }

  static async getByContrato(contractId: number): Promise<UserRole[]> {
    return this.getByContract(contractId)
  }

  static async getByUsuario(userId: number): Promise<UserRole[]> {
    return this.getByUser(userId)
  }

  static async getActiveByUsuario(userId: number): Promise<UserRole[]> {
    return this.getActiveByUser(userId)
  }

  static async getByPerfil(roleId: number): Promise<UserRole[]> {
    return this.getByRole(roleId)
  }
}

type RevokeUserRoleRequestBackend = Partial<RevokeUserFromRoleRequest> & {
  userId?: number
  roleId?: number
}
