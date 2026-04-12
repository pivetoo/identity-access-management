import { httpClient, translate } from 'archon-ui'
import type { KPIs, UsersByEmpresa, TopSistema, ActiveSession, PagedResult } from '../types/dashboard'

const dashboardService = {
  getKPIs: async (): Promise<KPIs> => {
    const response = await httpClient.get<KPIs>('/dashboard/getkpis')
    if (!response.data) {
      throw new Error(translate('dashboard.service.kpis.emptyResponse'))
    }

    return response.data
  },

  getUsersByEmpresa: async (): Promise<UsersByEmpresa[]> => {
    const response = await httpClient.get<UsersByEmpresa[]>('/dashboard/getusersbycompany')
    return response.data ?? []
  },

  getTopSistemas: async (limit: number = 4): Promise<TopSistema[]> => {
    const response = await httpClient.get<TopSistema[]>(`/dashboard/gettopsystems?limit=${limit}`)
    return response.data ?? []
  },

  getActiveSessions: async (page: number = 1, pageSize: number = 20): Promise<PagedResult<ActiveSession>> => {
    const response = await httpClient.get<ActiveSession[]>(`/dashboard/getactivesessions?page=${page}&pageSize=${pageSize}`)

    return {
      items: response.data ?? [],
      page: response.pagination?.page ?? page,
      pageSize: response.pagination?.pageSize ?? pageSize,
      totalCount: response.pagination?.totalCount ?? (response.data?.length ?? 0),
      totalPages: response.pagination?.totalPages ?? 1,
      hasPreviousPage: response.pagination?.hasPreviousPage ?? false,
      hasNextPage: response.pagination?.hasNextPage ?? false,
    }
  },

  revokeSession: async (sessionId: string): Promise<void> => {
    await httpClient.post(`/auth/revokesession/${sessionId}`)
  },

  revokeAllSessions: async (): Promise<void> => {
    await httpClient.post('/auth/revokeallsessions')
  }
}

export default dashboardService
