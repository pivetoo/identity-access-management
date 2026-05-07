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

export interface DashboardLoginTrend {
  label: string;
  logins: number;
  failures: number;
}

export interface DashboardContractHealth {
  active: number;
  expiringSoon: number;
  suspended: number;
  withoutOAuthClient: number;
}

export interface DashboardSessionsByHour {
  label: string;
  sessions: number;
}

export interface DashboardTopSystem {
  systemApplicationId: number;
  name: string;
  accesses: number;
}

export interface DashboardSecurityPulse {
  mfaCoverage: number;
  validSessions: number;
  rotatedTokens: number;
  reviewedAccesses: number;
}

export interface DashboardOverview {
  loginTrend: DashboardLoginTrend[];
  contractHealth: DashboardContractHealth;
  sessionsByHour: DashboardSessionsByHour[];
  topSystems: DashboardTopSystem[];
  securityPulse: DashboardSecurityPulse;
}
