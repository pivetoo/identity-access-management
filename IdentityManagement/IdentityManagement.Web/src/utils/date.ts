export function formatDate(dateString?: string, locale = 'pt-BR'): string {
  if (!dateString) {
    return '-'
  }

  return new Date(dateString).toLocaleDateString(locale)
}

export function formatDateTime(
  dateString?: string,
  locale = 'pt-BR',
  options: Intl.DateTimeFormatOptions = {
    day: '2-digit',
    month: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }
): string {
  if (!dateString) {
    return '-'
  }

  return new Date(dateString).toLocaleString(locale, options)
}
