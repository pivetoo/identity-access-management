import { httpClient, queryCollection, translate } from 'archon-ui'
import type { PaginationParams, PaginatedResult } from 'archon-ui'
import type { TenantDatabase, CreateTenantDatabaseRequest, UpdateTenantDatabaseRequest } from '../types/tenantDatabase'

export class TenantDatabaseService {
  private static baseUrl = '/tenantdatabases'

  static async getAll(params?: PaginationParams): Promise<PaginatedResult<TenantDatabase>> {
    const response = await httpClient.get<TenantDatabase[]>(`${this.baseUrl}/getall`)
    const items = response.data ?? []

    return queryCollection(items, params, ['companyName', 'systemApplicationName', 'applicationId'])
  }

  static async getActive(): Promise<TenantDatabase[]> {
    const response = await httpClient.get<TenantDatabase[]>(`${this.baseUrl}/getall`)
    return response.data ?? []
  }

  static async create(request: CreateTenantDatabaseRequest): Promise<TenantDatabase> {
    const response = await httpClient.post<TenantDatabase>(`${this.baseUrl}/create`, request)

    if (!response.data) {
      throw new Error(translate('tenantDatabase.loadAfterCreate.failed'))
    }

    return response.data
  }

  static async update(id: number, request: UpdateTenantDatabaseRequest): Promise<TenantDatabase> {
    const response = await httpClient.put<TenantDatabase>(`${this.baseUrl}/update/${id}`, request)

    if (!response.data) {
      throw new Error(translate('tenantDatabase.notFound'))
    }

    return response.data
  }

  static async toggleActive(item: TenantDatabase): Promise<TenantDatabase> {
    return this.update(item.id, {
      id: item.id,
      connectionString: item.connectionString,
      databaseProvider: item.databaseProvider,
      schemaName: item.schemaName,
      apiKey: item.apiKey,
      isActive: !item.isActive,
    })
  }
}
