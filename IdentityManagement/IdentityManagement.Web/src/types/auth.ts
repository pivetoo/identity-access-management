export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string
  refreshToken: string
  tokenType: string
  expiresIn: number
  redirectUrl?: string
  user: UserViewModel
  contract?: ContractViewModel
}

export interface UserViewModel {
  id: number
  username: string
  email: string
  name: string
  isActive: boolean
  lastLoginAt?: string
}

export interface ContractSelectionResponse {
  userId: number
  userName: string
  userEmail: string
  temporaryToken: string
  availableContracts: ContractViewModel[]
}

export interface ContractViewModel {
  contractId: number
  systemApplicationName: string
  companyName: string
  redirectUris: string
  roleName?: string
}

export interface LoginWithContractRequest {
  userId: number
  contractId: number
  temporaryToken: string
}

export interface ApiError {
  message: string
  status: number
  errors?: Record<string, string[]>
}
