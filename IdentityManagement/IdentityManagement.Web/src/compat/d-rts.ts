export * from 'archon-ui'

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

export interface ODataParams {
  $select?: string
  $filter?: string
  $expand?: string
  $orderby?: string
  $skip?: number
  $top?: number
  $count?: boolean
}

export interface ODataResponse<T> {
  value: T[]
  '@odata.count'?: number
}
