import { httpClient } from 'd-rts';
import type { KPIs, UsersByEmpresa, TopSistema, ActiveSession, PagedResult } from '../types/dashboard';

const dashboardService = {
  getKPIs: async (): Promise<KPIs> => {
    const response = await httpClient.get<KPIs>('/dashboard/kpis');
    return response.data;
  },

  getUsersByEmpresa: async (): Promise<UsersByEmpresa[]> => {
    const response = await httpClient.get<UsersByEmpresa[]>('/dashboard/users-by-company');
    return response.data;
  },

  getTopSistemas: async (limit: number = 4): Promise<TopSistema[]> => {
    const response = await httpClient.get<TopSistema[]>(`/dashboard/top-systems?limit=${limit}`);
    return response.data;
  },

  getActiveSessions: async (page: number = 1, pageSize: number = 20): Promise<PagedResult<ActiveSession>> => {
    const response = await httpClient.get<PagedResult<ActiveSession>>(`/dashboard/active-sessions?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },

  revokeSession: async (sessionId: string): Promise<void> => {
    await httpClient.post(`/auth/revoke-session/${sessionId}`);
  },

  revokeAllSessions: async (): Promise<void> => {
    await httpClient.post('/auth/revoke-all-sessions');
  }
};

export default dashboardService;
