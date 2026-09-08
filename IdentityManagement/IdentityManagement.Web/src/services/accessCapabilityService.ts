import { httpClient } from 'archon-ui'
import type { AccessCapability } from '../types/accessCapability'

export class AccessCapabilityService {
  private static baseUrl = '/accesscapabilities'

  static async getBySystemApplicationId(systemApplicationId: number): Promise<AccessCapability[]> {
    const response = await httpClient.get<AccessCapability[]>(
      `${this.baseUrl}/getbysystemapplication/${systemApplicationId}`
    )
    return response.data ?? []
  }
}
