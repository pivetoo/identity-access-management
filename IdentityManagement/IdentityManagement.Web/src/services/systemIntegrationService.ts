import { httpClient } from 'archon-ui'
import type { SystemIntegration, UpsertSystemIntegrationRequest } from '../types/systemIntegration'

export class SystemIntegrationService {
  private static baseUrl = '/systemintegrations'

  static async getBySystem(systemApplicationId: number): Promise<SystemIntegration[]> {
    const response = await httpClient.get<SystemIntegration[]>(`${this.baseUrl}/bysystem/${systemApplicationId}`)
    return response.data ?? []
  }

  static async create(request: UpsertSystemIntegrationRequest): Promise<SystemIntegration> {
    const response = await httpClient.post<SystemIntegration>(`${this.baseUrl}`, request)
    if (!response.data) {
      throw new Error('Resposta vazia ao criar integração.')
    }
    return response.data
  }

  static async update(id: number, request: UpsertSystemIntegrationRequest): Promise<SystemIntegration> {
    const response = await httpClient.put<SystemIntegration>(`${this.baseUrl}/update/${id}`, request)
    if (!response.data) {
      throw new Error('Resposta vazia ao atualizar integração.')
    }
    return response.data
  }

  static async remove(id: number): Promise<void> {
    await httpClient.delete(`${this.baseUrl}/delete/${id}`)
  }
}
