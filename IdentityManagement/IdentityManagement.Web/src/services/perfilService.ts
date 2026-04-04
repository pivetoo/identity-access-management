import { httpClient } from 'archon-ui'
import type { Perfil, CreatePerfilRequest, UpdatePerfilRequest, PerfilDetailViewModel, PerfilSummaryViewModel } from '../types/perfil'
import type { PaginationParams, PaginatedResult } from '../types/pagination'
import { queryCollection } from './serviceUtils'

interface RoleApiResponse {
  id: number
  name: string
  description: string
  contractId: number
  isRoot: boolean
  isDefault: boolean
  accessResourceIds?: number[]
  createdAt?: string
  updatedAt?: string
}

function mapRole(role: RoleApiResponse): Perfil {
  return {
    id: role.id,
    name: role.name,
    description: role.description,
    contratoId: role.contractId,
    isSuperUser: role.isRoot,
    isDefault: role.isDefault,
    accessResourceIds: role.accessResourceIds ?? [],
    criadoEm: role.createdAt ?? '',
    ultimaAlteracao: role.updatedAt,
  }
}

function mapRoleDetail(role: RoleApiResponse): PerfilDetailViewModel {
  return {
    id: role.id,
    name: role.name,
    description: role.description,
    contratoId: role.contractId,
    contratoName: '',
    empresaName: '',
    isSuperUser: role.isRoot,
    isDefault: role.isDefault,
    accessResourceIds: role.accessResourceIds ?? [],
    criadoEm: role.createdAt ?? '',
    ultimaAlteracao: role.updatedAt,
  }
}

function mapRoleSummary(role: RoleApiResponse): PerfilSummaryViewModel {
  return {
    id: role.id,
    name: role.name,
    description: role.description,
    contratoId: role.contractId,
    isSuperUser: role.isRoot,
    isDefault: role.isDefault,
    userCount: 0,
  }
}

export class PerfilService {
  private static baseUrl = '/roles'

  static async getAll(params?: PaginationParams): Promise<PaginatedResult<Perfil>> {
    const response = await httpClient.get<RoleApiResponse[]>(this.baseUrl)
    const perfis = (response.data ?? []).map(mapRole)

    return queryCollection(perfis, params, ['name', 'description'])
  }

  static async getById(id: number): Promise<Perfil> {
    const response = await httpClient.get<RoleApiResponse[]>(this.baseUrl)
    const perfil = (response.data ?? []).map(mapRole).find((item) => item.id === id)

    if (!perfil) {
      throw new Error('Perfil não encontrado.')
    }

    return perfil
  }

  static async getActive(): Promise<PerfilSummaryViewModel[]> {
    const response = await httpClient.get<RoleApiResponse[]>(this.baseUrl)
    return (response.data ?? []).map(mapRoleSummary)
  }

  static async getByContrato(contratoId: number, params?: PaginationParams): Promise<PaginatedResult<Perfil>> {
    const response = await httpClient.get<RoleApiResponse[]>(`${this.baseUrl}/contract/${contratoId}`)
    const perfis = (response.data ?? []).map(mapRole)

    return queryCollection(perfis, params, ['name', 'description'])
  }

  static async getByContratoSummary(contratoId: number): Promise<PerfilSummaryViewModel[]> {
    const response = await httpClient.get<RoleApiResponse[]>(`${this.baseUrl}/contract/${contratoId}`)
    return (response.data ?? []).map(mapRoleSummary)
  }

  static async getDefaultPerfil(contratoId: number): Promise<PerfilDetailViewModel> {
    const response = await httpClient.get<RoleApiResponse>(`${this.baseUrl}/contract/${contratoId}/default`)

    if (!response.data) {
      throw new Error('Perfil padrão não encontrado.')
    }

    return mapRoleDetail(response.data)
  }

  static async create(perfil: CreatePerfilRequest): Promise<PerfilDetailViewModel> {
    const response = await httpClient.post<RoleApiResponse>(this.baseUrl, {
      name: perfil.name,
      description: perfil.description ?? '',
      contractId: perfil.contratoId,
      isRoot: perfil.isSuperUser,
      isDefault: perfil.isDefault,
      accessResourceIds: perfil.accessResourceIds ?? [],
    })

    if (!response.data) {
      throw new Error('Resposta vazia ao criar perfil.')
    }

    return mapRoleDetail(response.data)
  }

  static async update(id: number, perfil: UpdatePerfilRequest): Promise<PerfilDetailViewModel> {
    const response = await httpClient.put<RoleApiResponse>(`${this.baseUrl}/${id}`, {
      name: perfil.name,
      description: perfil.description ?? '',
      isRoot: perfil.isSuperUser,
      isDefault: perfil.isDefault,
      accessResourceIds: perfil.accessResourceIds ?? [],
    })

    if (!response.data) {
      throw new Error('Resposta vazia ao atualizar perfil.')
    }

    return mapRoleDetail(response.data)
  }

  static async updatePermissions(): Promise<void> {
    throw new Error('O gerenciamento de permissões por recurso ainda não foi exposto no backend.')
  }

  static async setAsDefault(): Promise<void> {
    throw new Error('A definição de perfil padrão ainda não foi exposta no backend.')
  }

  static async checkPermission(): Promise<boolean> {
    throw new Error('A validação direta de permissão ainda não foi exposta no backend.')
  }

  static async delete(_id?: number): Promise<void> {
    throw new Error('A exclusão de perfis ainda não foi exposta no backend.')
  }
}
