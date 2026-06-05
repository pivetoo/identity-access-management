export const SystemIntegrationParameterSource = {
  Static: 0,
  TenantApiKey: 1,
} as const

export type SystemIntegrationParameterSource = (typeof SystemIntegrationParameterSource)[keyof typeof SystemIntegrationParameterSource]

export interface SystemIntegrationParameter {
  key: string
  value?: string | null
  isSecret: boolean
  valueSource: SystemIntegrationParameterSource
  sourceAudience?: string | null
}

export interface SystemIntegration {
  id: number
  systemApplicationId: number
  name: string
  baseUrl: string
  isActive: boolean
  parameters: SystemIntegrationParameter[]
}

export interface UpsertSystemIntegrationRequest {
  systemApplicationId: number
  name: string
  baseUrl: string
  isActive: boolean
  parameters: SystemIntegrationParameter[]
}
