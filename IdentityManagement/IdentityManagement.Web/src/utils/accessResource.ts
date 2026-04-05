export function getHttpMethodClassName(httpMethod: string): string {
  switch (httpMethod.toUpperCase()) {
    case 'GET':
      return 'border-emerald-200 bg-emerald-50 text-emerald-700'
    case 'POST':
      return 'border-amber-200 bg-amber-50 text-amber-700'
    case 'PUT':
      return 'border-orange-200 bg-orange-50 text-orange-700'
    case 'DELETE':
      return 'border-red-200 bg-red-50 text-red-700'
    default:
      return ''
  }
}
