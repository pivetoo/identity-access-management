const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL || '/api').replace(/\/+$/, '');

export interface AdminInvitationInfo {
  companyName: string;
  systemApplicationName: string;
  companyEmail: string;
}

async function validateInvitation(token: string): Promise<AdminInvitationInfo> {
  const response = await fetch(`${apiBaseUrl}/Auth/GetAdminSetup/${encodeURIComponent(token)}`, {
    method: 'GET',
    headers: { 'Content-Type': 'application/json' },
  });

  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    throw new Error(body?.message || 'Link inválido ou expirado.');
  }

  const body = await response.json();
  return body.data as AdminInvitationInfo;
}

async function setupAdmin(token: string, name: string, username: string, email: string, password: string): Promise<void> {
  const response = await fetch(`${apiBaseUrl}/Auth/SetupAdmin`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ token, name, username, email, password }),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    throw new Error(body?.message || 'Erro ao configurar o administrador.');
  }
}

export const adminSetupService = { validateInvitation, setupAdmin };
