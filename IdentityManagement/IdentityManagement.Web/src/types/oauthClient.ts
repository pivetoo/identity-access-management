export type OAuthClientType = 1 | 2 | 3

export type OAuthRedirectUriType = 1 | 2

export interface OAuthClientRedirectUri {
  id?: number
  uri: string
  type: OAuthRedirectUriType
  isActive?: boolean
}

export interface OAuthClient {
  id: number
  systemApplicationId: number
  systemApplicationName: string
  clientId: string
  clientName: string
  clientType: OAuthClientType
  requirePkce: boolean
  requireConsent: boolean
  allowOfflineAccess: boolean
  isActive: boolean
  accessTokenLifetime: number
  identityTokenLifetime: number
  refreshTokenLifetime: number
  refreshTokenRotationEnabled: boolean
  redirectUris: OAuthClientRedirectUri[]
  scopes: string[]
}

export interface CreateOAuthClientRequest {
  systemApplicationId: number
  clientId: string
  clientName: string
  clientType: OAuthClientType
  clientSecret?: string
  requirePkce: boolean
  requireConsent: boolean
  allowOfflineAccess: boolean
  accessTokenLifetime: number
  identityTokenLifetime: number
  refreshTokenLifetime: number
  refreshTokenRotationEnabled: boolean
  redirectUris: OAuthClientRedirectUri[]
  scopes: string[]
}

export interface UpdateOAuthClientRequest {
  id: number
  clientName: string
  clientType: OAuthClientType
  clientSecret?: string
  rotateClientSecret: boolean
  requirePkce: boolean
  requireConsent: boolean
  allowOfflineAccess: boolean
  isActive: boolean
  accessTokenLifetime: number
  identityTokenLifetime: number
  refreshTokenLifetime: number
  refreshTokenRotationEnabled: boolean
  redirectUris: OAuthClientRedirectUri[]
  scopes: string[]
}
