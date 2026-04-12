import { httpClient, queryCollection, translate } from 'archon-ui'
import type { PaginationParams, PaginatedResult } from 'archon-ui'
import type { Role, CreateRoleRequest, UpdateRoleRequest } from '../types/role'
import { sortByPriority } from '../utils/sort'

export class RoleService {
  private static baseUrl = '/roles'

  static async getAll(params?: PaginationParams): Promise<PaginatedResult<Role>> {
    const response = await httpClient.get<Role[]>(`${this.baseUrl}/getactive`)
    const roles = sortByPriority(response.data ?? [])

    return queryCollection(roles, params, ['name', 'description'])
  }

  static async getById(id: number): Promise<Role> {
    const response = await httpClient.get<Role[]>(`${this.baseUrl}/getactive`)
    const role = (response.data ?? []).find((item) => item.id === id)

    if (!role) {
      throw new Error(translate('role.notFound'))
    }

    return role
  }

  static async getActive(): Promise<Role[]> {
    const response = await httpClient.get<Role[]>(`${this.baseUrl}/getactive`)
    return sortByPriority(response.data ?? [])
  }

  static async getByContract(contractId: number, params?: PaginationParams): Promise<PaginatedResult<Role>> {
    const response = await httpClient.get<Role[]>(`${this.baseUrl}/getbycontract/${contractId}`)
    const roles = sortByPriority(response.data ?? [])

    return queryCollection(roles, params, ['name', 'description'])
  }

  static async getByContractSummary(contractId: number): Promise<Role[]> {
    const response = await httpClient.get<Role[]>(`${this.baseUrl}/getbycontract/${contractId}`)
    return sortByPriority(response.data ?? [])
  }

  static async getDefaultRole(contractId: number): Promise<Role> {
    const response = await httpClient.get<Role>(`${this.baseUrl}/getdefaultbycontract/${contractId}`)

    if (!response.data) {
      throw new Error(translate('role.default.notFound'))
    }

    return response.data
  }

  static async create(role: CreateRoleRequest): Promise<Role> {
    const response = await httpClient.post<Role>(`${this.baseUrl}/create`, {
      name: role.name,
      description: role.description ?? '',
      contractId: role.contractId,
      isRoot: role.isRoot,
      isDefault: role.isDefault,
      accessResourceIds: role.accessResourceIds ?? [],
    })

    if (!response.data) {
      throw new Error(translate('role.service.create.emptyResponse'))
    }

    return response.data
  }

  static async update(id: number, role: UpdateRoleRequest): Promise<Role> {
    const response = await httpClient.put<Role>(`${this.baseUrl}/update/${id}`, {
      name: role.name,
      description: role.description ?? '',
      isRoot: role.isRoot,
      isDefault: role.isDefault,
      accessResourceIds: role.accessResourceIds ?? [],
    })

    if (!response.data) {
      throw new Error(translate('role.service.update.emptyResponse'))
    }

    return response.data
  }

  static async updatePermissions(): Promise<void> {
    throw new Error(translate('role.service.permissions.notAvailable'))
  }

  static async setAsDefault(): Promise<void> {
    throw new Error(translate('role.service.default.notAvailable'))
  }

  static async checkPermission(): Promise<boolean> {
    throw new Error(translate('role.service.permissionCheck.notAvailable'))
  }

  static async delete(_id?: number): Promise<void> {
    throw new Error(translate('role.service.delete.notAvailable'))
  }

  static async getByContrato(contractId: number, params?: PaginationParams): Promise<PaginatedResult<Role>> {
    return this.getByContract(contractId, params)
  }

  static async getByContratoSummary(contractId: number): Promise<Role[]> {
    return this.getByContractSummary(contractId)
  }

  static async getDefaultPerfil(contractId: number): Promise<Role> {
    return this.getDefaultRole(contractId)
  }
}
