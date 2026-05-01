import { httpClient } from 'archon-ui';

interface CompleteAuthorizeRequest {
  authorizationSessionToken: string;
  contractId: number;
  authorizeUrl: string;
}

interface AuthorizeWithCredentialsResponse {
  redirectUrl: string;
}

export class OidcService {
  static async completeAuthorize(request: CompleteAuthorizeRequest): Promise<AuthorizeWithCredentialsResponse> {
    const response = await httpClient.post<AuthorizeWithCredentialsResponse>('/oidc/complete-authorize', request);

    if (!response.data?.redirectUrl) {
      throw new Error('OIDC authorize response is empty.');
    }

    return response.data;
  }
}
