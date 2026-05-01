export interface Contract {
  id: number
  companyId: number
  systemApplicationId: number
  companyName: string
  systemApplicationName: string
  startDate: string
  endDate?: string
  isActive: boolean
  isValid?: boolean
  accessTokenLifetime: number
  refreshTokenLifetime: number
  createdAt?: string
  updatedAt?: string
}

export interface CreateContractRequest {
  companyId: number
  systemApplicationId: number
  startDate: string
  endDate?: string
  accessTokenLifetime?: number
  refreshTokenLifetime?: number
}

export interface UpdateContractRequest {
  id: number
  companyId: number
  systemApplicationId: number
  startDate: string
  endDate?: string
  isActive: boolean
  accessTokenLifetime: number
  refreshTokenLifetime: number
}
