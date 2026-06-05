import { httpClient, translate } from 'archon-ui'

export interface OnboardClientSystemItem {
  systemApplicationId: number
  startDate: string
  endDate?: string | null
}

export interface OnboardClientRequest {
  legalName: string
  tradeName: string
  document: string
  email: string
  phoneNumber?: string
  systems: OnboardClientSystemItem[]
}

export interface OnboardClientResponse {
  companyId: number
  contractIds: number[]
  databaseNames: string[]
}

export class ClientService {
  private static baseUrl = '/clients'

  static async onboard(request: OnboardClientRequest): Promise<OnboardClientResponse> {
    const response = await httpClient.post<OnboardClientResponse>(`${this.baseUrl}/onboard`, request)

    if (!response.data) {
      throw new Error(translate('client.service.onboard.emptyResponse'))
    }

    return response.data
  }
}
