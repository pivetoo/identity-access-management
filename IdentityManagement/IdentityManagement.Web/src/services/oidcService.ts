import { httpClient } from 'archon-ui';

interface AuthorizeWithCredentialsRequest {
  username: string;
  password: string;
  contractId: number;
  authorizeUrl: string;
}

interface AuthorizeWithCredentialsResponse {
  redirectUrl: string;
}

export class OidcService {
  static async authorizeWithCredentials(request: AuthorizeWithCredentialsRequest): Promise<AuthorizeWithCredentialsResponse> {
    const response = await httpClient.post<AuthorizeWithCredentialsResponse>('/oidc/complete-authorize', request);

    if (!response.data?.redirectUrl) {
      throw new Error('OIDC authorize response is empty.');
    }

    return response.data;
  }
}
