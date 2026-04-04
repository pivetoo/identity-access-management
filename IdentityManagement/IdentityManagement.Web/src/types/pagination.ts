export interface PaginationParams {
  page?: number
  pageSize?: number
  search?: string
  orderBy?: string
}

export interface PaginatedResult<T> {
  data: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}
