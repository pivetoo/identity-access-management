import { httpClient, queryCollection } from 'archon-ui'
import type { PaginationParams, PaginatedResult } from 'archon-ui'
import type { CreateSystemRoleTemplateRequest, SystemRoleTemplate, UpdateSystemRoleTemplateRequest } from '../types/systemRoleTemplate'
import { sortByPriority } from '../utils/sort'

export class SystemRoleTemplateService {
  private static baseUrl = '/systemroletemplates'

  static async getBySystemApplicationId(
    systemApplicationId: number,
    params?: PaginationParams
  ): Promise<PaginatedResult<SystemRoleTemplate>> {
    const response = await httpClient.get<SystemRoleTemplate[]>(
      `${this.baseUrl}/getbysystemapplicationid/${systemApplicationId}`
    )

    const templates = sortByPriority(response.data ?? [])
    return queryCollection(templates, params, ['name', 'description'])
  }

  static async getById(id: number): Promise<SystemRoleTemplate> {
    const response = await httpClient.get<SystemRoleTemplate>(`${this.baseUrl}/getbyid/${id}`)

    if (!response.data) {
      throw new Error('Role padrão não encontrado.')
    }

    return response.data
  }

  static async create(request: CreateSystemRoleTemplateRequest): Promise<SystemRoleTemplate> {
    const response = await httpClient.post<SystemRoleTemplate>(`${this.baseUrl}/create`, request)

    if (!response.data) {
      throw new Error('Resposta vazia ao criar perfil padrão.')
    }

    return response.data
  }

  static async update(id: number, request: UpdateSystemRoleTemplateRequest): Promise<SystemRoleTemplate> {
    const response = await httpClient.put<SystemRoleTemplate>(`${this.baseUrl}/update/${id}`, request)

    if (!response.data) {
      throw new Error('Resposta vazia ao atualizar perfil padrão.')
    }

    return response.data
  }
}
