import { httpClient } from 'archon-ui'
import type { AccessResource } from '../types/accessResource'

export class AccessResourceService {
  private static baseUrl = '/accessresources'

  static async getAll(): Promise<AccessResource[]> {
    const response = await httpClient.get<AccessResource[]>(`${this.baseUrl}/get`)
    return response.data ?? []
  }
}
