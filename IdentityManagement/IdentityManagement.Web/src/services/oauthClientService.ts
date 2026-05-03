import { httpClient, queryCollection } from 'archon-ui'
import type { PaginationParams, PaginatedResult } from 'archon-ui'
import type { CreateOAuthClientRequest, OAuthClient, UpdateOAuthClientRequest } from '../types/oauthClient'

export class OAuthClientService {
  private static baseUrl = '/oauthclients'

  static async getAll(params?: PaginationParams): Promise<PaginatedResult<OAuthClient>> {
    const response = await httpClient.get<OAuthClient[]>(`${this.baseUrl}/get`)
    const clients = response.data ?? []

    return queryCollection(clients, params, ['clientId', 'clientName', 'systemApplicationName'])
  }

  static async getById(id: number): Promise<OAuthClient | undefined> {
    const response = await httpClient.get<OAuthClient>(`${this.baseUrl}/getbyid/${id}`)
    return response.data
  }

  static async create(request: CreateOAuthClientRequest): Promise<OAuthClient> {
    const response = await httpClient.post<OAuthClient>(`${this.baseUrl}/create`, request)
    if (!response.data) {
      throw new Error('Resposta vazia ao criar OAuth client.')
    }

    return response.data
  }

  static async update(id: number, request: UpdateOAuthClientRequest): Promise<OAuthClient> {
    const response = await httpClient.put<OAuthClient>(`${this.baseUrl}/update/${id}`, request)
    if (!response.data) {
      throw new Error('Resposta vazia ao atualizar OAuth client.')
    }

    return response.data
  }

  static async disable(client: OAuthClient): Promise<OAuthClient> {
    return this.update(client.id, {
      id: client.id,
      clientName: client.clientName,
      clientType: client.clientType,
      rotateClientSecret: false,
      requirePkce: client.requirePkce,
      requireConsent: client.requireConsent,
      allowOfflineAccess: client.allowOfflineAccess,
      isActive: false,
      isDefault: client.isDefault,
      accessTokenLifetime: client.accessTokenLifetime,
      identityTokenLifetime: client.identityTokenLifetime,
      refreshTokenLifetime: client.refreshTokenLifetime,
      refreshTokenRotationEnabled: client.refreshTokenRotationEnabled,
      redirectUris: client.redirectUris.map((item) => ({ uri: item.uri, type: item.type })),
      scopes: client.scopes,
    })
  }
}
