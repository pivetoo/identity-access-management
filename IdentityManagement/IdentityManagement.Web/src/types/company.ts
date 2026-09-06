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
  attribution?: SignupAttribution | null
}

// Origem do cadastro publico (utm_*, gclid, fbclid, pagina de entrada, referrer). Nulo para
// empresas cadastradas pelo admin.
export interface SignupAttribution {
  source?: string | null
  medium?: string | null
  campaign?: string | null
  content?: string | null
  term?: string | null
  gclid?: string | null
  fbclid?: string | null
  landingPage?: string | null
  referrer?: string | null
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
