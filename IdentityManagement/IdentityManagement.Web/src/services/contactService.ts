import { httpClient } from 'archon-ui'
import type { ContactRequest } from '../types/contact'

export class ContactService {
  private static baseUrl = '/contact'

  static async list(params?: { pending?: boolean; take?: number }): Promise<ContactRequest[]> {
    const query = new URLSearchParams()
    if (params?.pending !== undefined) {
      query.set('pending', String(params.pending))
    }
    if (params?.take !== undefined) {
      query.set('take', String(params.take))
    }
    const qs = query.toString()
    const response = await httpClient.get<ContactRequest[]>(qs ? `${this.baseUrl}/list?${qs}` : `${this.baseUrl}/list`)
    return response.data ?? []
  }

  // A rota do Archon prefixa o nome da acao quando o template comeca com parametro.
  static async setHandled(id: number, handled: boolean): Promise<ContactRequest | null> {
    const response = await httpClient.put<ContactRequest>(`${this.baseUrl}/Handled/${id}/handled?handled=${handled}`, {})
    return response.data ?? null
  }
}
