export const DatabaseProviderValue = {
  PostgreSql: 1,
  SqlServer: 2,
  MySql: 3,
} as const

export type DatabaseProvider = typeof DatabaseProviderValue[keyof typeof DatabaseProviderValue]

export interface TenantDatabase {
  id: number
  contractId: number
  tenantId: string
  companyName: string
  systemApplicationName: string
  applicationId: string
  connectionString: string
  databaseProvider: DatabaseProvider
  schemaName: string
  apiKey: string
  isActive: boolean
  createdAt?: string
  updatedAt?: string
}

export interface CreateTenantDatabaseRequest {
  contractId: number
  connectionString: string
  databaseProvider: DatabaseProvider
  schemaName?: string
  apiKey: string
}

export interface UpdateTenantDatabaseRequest {
  id: number
  connectionString: string
  databaseProvider: DatabaseProvider
  schemaName?: string
  apiKey: string
  isActive: boolean
}
