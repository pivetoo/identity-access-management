const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL as string || '').replace(/\/+$/, '')

export const passwordResetService = {
  async forgotPassword(email: string): Promise<void> {
    const response = await fetch(`${apiBaseUrl}/Auth/ForgotPassword`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email }),
    })
    if (!response.ok) {
      const data = await response.json().catch(() => ({}))
      throw new Error(data?.message ?? 'Erro ao solicitar recuperação de senha.')
    }
  },

  async resetPassword(token: string, newPassword: string): Promise<void> {
    const response = await fetch(`${apiBaseUrl}/Auth/ResetPassword`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ token, newPassword }),
    })
    if (!response.ok) {
      const data = await response.json().catch(() => ({}))
      throw new Error(data?.message ?? 'Erro ao redefinir senha.')
    }
  },
}
