export interface KPIs {
  activeUsers: number;
  activeContratos: number;
  empresas: number;
  sistemas: number;
}

export interface UsersByEmpresa {
  name: string;
  value: number;
}

export interface TopSistema {
  name: string;
  logins: number;
}

export interface ActiveSession {
  sessionId: string;
  userId: number;
  userName: string;
  userEmail: string;
  companyName: string;
  systemApplicationName: string;
  ipAddress: string;
  userAgent: string;
  createdAt: string;
  expiresAt: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
