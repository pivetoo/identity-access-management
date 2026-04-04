import { httpClient } from 'archon-ui'
import type { UsuarioPerfil, AssignUsuarioToPerfilRequest, RevokeUsuarioFromPerfilRequest } from '../types/usuarioPerfil'

interface UserRoleApiResponse {
  id: number
  userId: number
  username: string
  userEmail: string
  roleId: number
  roleName: string
  contractId: number
  isRoot: boolean
  companyName: string
  assignedAt: string
  revokedAt?: string
  isActive: boolean
}

function mapUserRole(userRole: UserRoleApiResponse): UsuarioPerfil {
  return {
    id: userRole.id,
    usuarioId: userRole.userId,
    username: userRole.username,
    userEmail: userRole.userEmail,
    perfilId: userRole.roleId,
    perfilName: userRole.roleName,
    empresaName: userRole.companyName,
    assignedAt: userRole.assignedAt,
    revokedAt: userRole.revokedAt,
    isActive: userRole.isActive,
  }
}

export class UsuarioPerfilService {
  private static baseUrl = '/userroles'

  static async getByContrato(contratoId: number): Promise<UsuarioPerfil[]> {
    const response = await httpClient.get<UserRoleApiResponse[]>(`${this.baseUrl}/contract/${contratoId}`)
    return (response.data ?? []).map(mapUserRole)
  }

  static async getByUsuario(usuarioId: number): Promise<UsuarioPerfil[]> {
    const response = await httpClient.get<UserRoleApiResponse[]>(`${this.baseUrl}/user/${usuarioId}`)
    return (response.data ?? []).map(mapUserRole)
  }

  static async getActiveByUsuario(usuarioId: number): Promise<UsuarioPerfil[]> {
    const response = await httpClient.get<UserRoleApiResponse[]>(`${this.baseUrl}/user/${usuarioId}/active`)
    return (response.data ?? []).map(mapUserRole)
  }

  static async getByPerfil(perfilId: number): Promise<UsuarioPerfil[]> {
    const response = await httpClient.get<UserRoleApiResponse[]>(`${this.baseUrl}/role/${perfilId}`)
    return (response.data ?? []).map(mapUserRole)
  }

  static async checkAccess(usuarioId: number, perfilId: number): Promise<{ usuarioId: number; perfilId: number; hasAccess: boolean }> {
    const response = await httpClient.get<{ hasAccess: boolean }>(`${this.baseUrl}/user/${usuarioId}/role/${perfilId}/has-access`)
    return {
      usuarioId,
      perfilId,
      hasAccess: response.data?.hasAccess ?? false,
    }
  }

  static async getById(id: number): Promise<UsuarioPerfil> {
    const userRoles = await this.getByUsuario(id)
    const userRole = userRoles.find((item) => item.id === id)

    if (!userRole) {
      throw new Error('Vinculação não encontrada.')
    }

    return userRole
  }

  static async assign(request: AssignUsuarioToPerfilRequest): Promise<{ message: string; usuarioId: number; perfilId: number }> {
    const response = await httpClient.post<{ message?: string }>(this.baseUrl, {
      userId: request.usuarioId,
      roleId: request.perfilId,
    })

    return {
      message: response.message,
      usuarioId: request.usuarioId,
      perfilId: request.perfilId,
    }
  }

  static async revoke(usuarioId: number, perfilId: number): Promise<{ message: string; usuarioId: number; perfilId: number }> {
    const response = await httpClient.delete(`${this.baseUrl}`, {
      data: {
        userId: usuarioId,
        roleId: perfilId,
      } as RevokeUserRoleRequestBackend,
    })

    return {
      message: response.message,
      usuarioId,
      perfilId,
    }
  }

  static async reactivate(usuarioId: number, perfilId: number): Promise<{ message: string; usuarioId: number; perfilId: number }> {
    const response = await httpClient.post(`${this.baseUrl}/reactivate`, {
      userId: usuarioId,
      roleId: perfilId,
    } as RevokeUserRoleRequestBackend)

    return {
      message: response.message,
      usuarioId,
      perfilId,
    }
  }

  static async delete(id: number): Promise<{ message: string }> {
    const userRole = await this.getById(id)
    const result = await this.revoke(userRole.usuarioId, userRole.perfilId)
    return { message: result.message }
  }
}

interface RevokeUserRoleRequestBackend extends RevokeUsuarioFromPerfilRequest {
  userId?: number
  roleId?: number
}
