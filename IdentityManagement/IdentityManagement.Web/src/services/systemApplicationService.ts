import { httpClient, queryCollection, translate } from 'archon-ui'
import type { PaginationParams, PaginatedResult } from 'archon-ui'
import type { SystemApplication, CreateSystemApplicationRequest, UpdateSystemApplicationRequest } from '../types/systemApplication'

export class SystemApplicationService {
  private static baseUrl = '/systemapplications'

  static async getAll(params?: PaginationParams): Promise<PaginatedResult<SystemApplication>> {
    const response = await httpClient.get<SystemApplication[]>(`${this.baseUrl}/getactive`)
    const sistemas = response.data ?? []

    return queryCollection(sistemas, params, ['name', 'description', 'audience'])
  }

  static async getById(id: number): Promise<SystemApplication> {
    const sistemas = await this.getActive()
    const sistema = sistemas.find((item) => item.id === id)

    if (!sistema) {
      throw new Error(translate('systemApplication.notFound'))
    }

    return sistema
  }

  static async getActive(): Promise<SystemApplication[]> {
    const response = await httpClient.get<SystemApplication[]>(`${this.baseUrl}/getactive`)
    return response.data ?? []
  }

  static async create(sistema: CreateSystemApplicationRequest): Promise<SystemApplication> {
    const response = await httpClient.post<SystemApplication>(`${this.baseUrl}/create`, {
      name: sistema.name,
      description: sistema.description ?? '',
      audience: sistema.audience,
      type: sistema.type ?? 2,
    })

    if (!response.data) {
      throw new Error(translate('systemApplication.service.create.emptyResponse'))
    }

    return response.data
  }

  static async update(id: number, sistema: UpdateSystemApplicationRequest): Promise<SystemApplication> {
    const response = await httpClient.put<SystemApplication>(`${this.baseUrl}/update/${id}`, {
      id,
      name: sistema.name,
      description: sistema.description ?? '',
      audience: sistema.audience,
      isActive: sistema.isActive,
      type: sistema.type ?? 2,
    })

    if (!response.data) {
      throw new Error(translate('systemApplication.service.update.emptyResponse'))
    }

    return response.data
  }

  static async delete(id: number): Promise<void> {
    const sistema = await this.getById(id)

    await this.update(id, {
      id,
      name: sistema.name,
      description: sistema.description,
      isActive: false,
      audience: sistema.audience,
    })
  }
}
