import { httpClient, queryCollection } from 'archon-ui'
import type { PaginationParams, PaginatedResult } from 'archon-ui'
import type { Plan, CreatePlanRequest, UpdatePlanRequest } from '../types/plan'

export class PlanService {
  private static baseUrl = '/plans'

  static async getAll(params?: PaginationParams): Promise<PaginatedResult<Plan>> {
    const response = await httpClient.get<Plan[]>(`${this.baseUrl}/all`)
    const plans = response.data ?? []

    return queryCollection(plans, params, ['name', 'description'])
  }

  static async getActive(): Promise<Plan[]> {
    const response = await httpClient.get<Plan[]>(`${this.baseUrl}/getactive`)
    return response.data ?? []
  }

  static async getById(id: number): Promise<Plan> {
    const response = await httpClient.get<Plan>(`${this.baseUrl}/getbyid/${id}`)
    if (!response.data) {
      throw new Error('Plano não encontrado.')
    }

    return response.data
  }

  static async create(request: CreatePlanRequest): Promise<Plan> {
    const response = await httpClient.post<Plan>(`${this.baseUrl}/create`, request)
    if (!response.data) {
      throw new Error('Resposta vazia ao criar plano.')
    }

    return response.data
  }

  static async update(id: number, request: UpdatePlanRequest): Promise<Plan> {
    const response = await httpClient.put<Plan>(`${this.baseUrl}/update/${id}`, request)
    if (!response.data) {
      throw new Error('Resposta vazia ao atualizar plano.')
    }

    return response.data
  }

  static async activate(id: number): Promise<void> {
    await httpClient.post(`${this.baseUrl}/${id}/activate`, {})
  }

  static async deactivate(id: number): Promise<void> {
    await httpClient.post(`${this.baseUrl}/${id}/deactivate`, {})
  }
}
