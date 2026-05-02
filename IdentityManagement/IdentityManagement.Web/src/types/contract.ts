export interface Contract {
  id: number
  companyId: number
  systemApplicationId: number
  tenantId: string
  companyName: string
  systemApplicationName: string
  startDate: string
  endDate?: string
  isActive: boolean
  isValid?: boolean
  createdAt?: string
  updatedAt?: string
}

export interface CreateContractRequest {
  companyId: number
  systemApplicationId: number
  startDate: string
  endDate?: string
}

export interface UpdateContractRequest {
  id: number
  companyId: number
  systemApplicationId: number
  startDate: string
  endDate?: string
  isActive: boolean
}
