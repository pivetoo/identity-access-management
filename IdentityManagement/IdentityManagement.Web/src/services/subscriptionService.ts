import { httpClient } from 'archon-ui'
import type { Subscription } from '../types/subscription'

export class SubscriptionService {
  private static baseUrl = '/subscriptions'

  static async getByCompany(companyId: number): Promise<Subscription | null> {
    try {
      const response = await httpClient.get<Subscription>(`${this.baseUrl}/getbycompany/company/${companyId}`)
      return response.data ?? null
    } catch {
      return null
    }
  }

  static async assign(companyId: number, planId: number): Promise<Subscription> {
    const response = await httpClient.post<Subscription>(`${this.baseUrl}/assign`, { companyId, planId })
    if (!response.data) {
      throw new Error('Resposta vazia ao atribuir assinatura.')
    }

    return response.data
  }

  static async changePlan(companyId: number, planId: number): Promise<Subscription> {
    const response = await httpClient.put<Subscription>(`${this.baseUrl}/changeplan/${companyId}`, { planId })
    if (!response.data) {
      throw new Error('Resposta vazia ao trocar plano.')
    }

    return response.data
  }

  static async cancel(companyId: number): Promise<void> {
    await httpClient.post(`${this.baseUrl}/cancel/${companyId}`, {})
  }

  static async suspend(companyId: number): Promise<void> {
    await httpClient.post(`${this.baseUrl}/suspend/${companyId}`, {})
  }

  static async activate(companyId: number): Promise<void> {
    await httpClient.post(`${this.baseUrl}/activate/${companyId}`, {})
  }
}
