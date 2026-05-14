export interface Company {
  id: number
  legalName: string
  tradeName: string
  document: string
  email: string
  phoneNumber: string
  tenantId?: string
  isActive: boolean
  createdAt?: string
  updatedAt?: string
}

export interface CreateCompanyRequest {
  legalName: string
  tradeName: string
  document: string
  email: string
  phoneNumber: string
}

export interface UpdateCompanyRequest {
  id: number
  legalName: string
  tradeName: string
  document: string
  email: string
  phoneNumber: string
  isActive: boolean
}
