import { httpClient } from 'archon-ui'
import type { Payment, WebhookEvent, PaymentStatus } from '../types/payment'

export class PaymentService {
  private static baseUrl = '/payments'

  static async list(params?: { status?: PaymentStatus; take?: number }): Promise<Payment[]> {
    const query = new URLSearchParams()
    if (params?.status !== undefined) {
      query.set('status', String(params.status))
    }
    if (params?.take !== undefined) {
      query.set('take', String(params.take))
    }
    const qs = query.toString()
    const url = qs ? `${this.baseUrl}/list?${qs}` : `${this.baseUrl}/list`
    const response = await httpClient.get<Payment[]>(url)
    return response.data ?? []
  }

  static async getByCompany(companyId: number): Promise<Payment[]> {
    const response = await httpClient.get<Payment[]>(`${this.baseUrl}/company/${companyId}`)
    return response.data ?? []
  }

  static async listWebhookEvents(params?: { take?: number }): Promise<WebhookEvent[]> {
    const query = new URLSearchParams()
    if (params?.take !== undefined) {
      query.set('take', String(params.take))
    }
    const qs = query.toString()
    const url = qs ? `${this.baseUrl}/webhook-events?${qs}` : `${this.baseUrl}/webhook-events`
    const response = await httpClient.get<WebhookEvent[]>(url)
    return response.data ?? []
  }
}
