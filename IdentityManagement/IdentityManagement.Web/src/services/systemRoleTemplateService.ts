import { httpClient, queryCollection, translate } from 'archon-ui'
import type { PaginationParams, PaginatedResult } from 'archon-ui'
import type { CreateSystemRoleTemplateRequest, SystemRoleTemplate, UpdateSystemRoleTemplateRequest } from '../types/systemRoleTemplate'

export class SystemRoleTemplateService {
  private static baseUrl = '/systemroletemplates'

  static async getBySystemApplicationId(
    systemApplicationId: number,
    params?: PaginationParams
  ): Promise<PaginatedResult<SystemRoleTemplate>> {
    const response = await httpClient.get<SystemRoleTemplate[]>(
      `${this.baseUrl}/getbysystemapplicationid/${systemApplicationId}`
    )

    return queryCollection(response.data ?? [], params, ['name', 'description'])
  }

  static async getById(id: number): Promise<SystemRoleTemplate> {
    const response = await httpClient.get<SystemRoleTemplate>(`${this.baseUrl}/getbyid/${id}`)

    if (!response.data) {
      throw new Error(translate('systemRoleTemplate.notFound'))
    }

    return response.data
  }

  static async create(request: CreateSystemRoleTemplateRequest): Promise<SystemRoleTemplate> {
    const response = await httpClient.post<SystemRoleTemplate>(`${this.baseUrl}/create`, request)

    if (!response.data) {
      throw new Error(translate('systemRoleTemplate.service.create.emptyResponse'))
    }

    return response.data
  }

  static async update(id: number, request: UpdateSystemRoleTemplateRequest): Promise<SystemRoleTemplate> {
    const response = await httpClient.put<SystemRoleTemplate>(`${this.baseUrl}/update/${id}`, request)

    if (!response.data) {
      throw new Error(translate('systemRoleTemplate.service.update.emptyResponse'))
    }

    return response.data
  }
}
