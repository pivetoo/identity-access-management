export function getNameInitials(name: string): string {
  const parts = name.split(' ').filter(Boolean)

  if (parts.length >= 2) {
    return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase()
  }

  return name.substring(0, 2).toUpperCase()
}

export function resolveAvatarUrl(avatarUrl: string | undefined, apiBaseUrl?: string): string | null {
  if (!avatarUrl) {
    return null
  }

  if (avatarUrl.startsWith('http')) {
    return avatarUrl
  }

  if (!apiBaseUrl) {
    return avatarUrl
  }

  return `${apiBaseUrl}${avatarUrl}`
}
