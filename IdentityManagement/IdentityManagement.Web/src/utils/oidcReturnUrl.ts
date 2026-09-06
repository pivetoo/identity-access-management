// Devolve o authorize do IdentityManagement recebido em returnUrl, ou undefined quando nao ha
// returnUrl ou ele aponta para outra origem/rota.
export function getOidcAuthorizeUrl(): string | undefined {
  if (typeof window === 'undefined') {
    return undefined;
  }

  const rawReturnUrl = new URLSearchParams(window.location.search).get('returnUrl');
  if (!rawReturnUrl) {
    return undefined;
  }

  try {
    const identityManagementUrl = import.meta.env.VITE_IDENTITY_MANAGEMENT_URL;
    if (!identityManagementUrl) {
      return undefined;
    }

    const parsedUrl = new URL(rawReturnUrl);
    const identityUrl = new URL(identityManagementUrl);
    if (parsedUrl.origin !== identityUrl.origin || parsedUrl.pathname !== '/connect/authorize') {
      return undefined;
    }

    return parsedUrl.toString();
  } catch {
    return undefined;
  }
}
