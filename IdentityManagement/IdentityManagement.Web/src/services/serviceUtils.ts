import type { PaginationParams, PaginatedResult } from 'd-rts'

function getValue(record: object, field: string): unknown {
  return field.split('.').reduce<unknown>((current, part) => {
    if (current && typeof current === 'object' && part in current) {
      return (current as Record<string, unknown>)[part]
    }

    return undefined
  }, record)
}

function compareValues(left: unknown, right: unknown): number {
  if (left == null && right == null) {
    return 0
  }

  if (left == null) {
    return -1
  }

  if (right == null) {
    return 1
  }

  if (typeof left === 'number' && typeof right === 'number') {
    return left - right
  }

  return String(left).localeCompare(String(right), 'pt-BR', { sensitivity: 'base' })
}

export function queryCollection<T extends object>(
  items: T[],
  params?: PaginationParams,
  searchFields: string[] = []
): PaginatedResult<T> {
  const page = params?.page && params.page > 0 ? params.page : 1
  const pageSize = params?.pageSize && params.pageSize > 0 ? params.pageSize : items.length || 1
  const search = params?.search?.trim().toLowerCase()
  const orderBy = params?.orderBy?.trim()

  let filteredItems = [...items]

  if (search && searchFields.length > 0) {
    filteredItems = filteredItems.filter((item) =>
      searchFields.some((field) => {
        const value = getValue(item, field)
        return String(value ?? '').toLowerCase().includes(search)
      })
    )
  }

  if (orderBy) {
    const [field, direction] = orderBy.split(/\s+/)
    const isDescending = direction?.toLowerCase() === 'desc'

    filteredItems.sort((left, right) => {
      const result = compareValues(getValue(left, field), getValue(right, field))
      return isDescending ? result * -1 : result
    })
  }

  const totalCount = filteredItems.length
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const normalizedPage = Math.min(page, totalPages)
  const startIndex = (normalizedPage - 1) * pageSize
  const data = filteredItems.slice(startIndex, startIndex + pageSize)

  return {
    data,
    totalCount,
    page: normalizedPage,
    pageSize,
    totalPages,
    hasPreviousPage: normalizedPage > 1,
    hasNextPage: normalizedPage < totalPages,
  }
}
