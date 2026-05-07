using Archon.Core.Pagination;
using IdentityManagement.Application.Responses.Dashboard;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class DashboardService : IDashboardService
    {
        private readonly DbContext dbContext;

        public DashboardService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<DashboardKpiResponse> GetKpis(CancellationToken cancellationToken = default)
        {
            int activeUsers = await (
                from user in dbContext.Set<User>().AsNoTracking()
                where user.IsActive
                select user.Id)
                .CountAsync(cancellationToken);

            int activeContracts = await (
                from contract in dbContext.Set<Contract>().AsNoTracking()
                where contract.IsActive
                select contract.Id)
                .CountAsync(cancellationToken);

            int companies = await (
                from company in dbContext.Set<Company>().AsNoTracking()
                where company.IsActive
                select company.Id)
                .CountAsync(cancellationToken);

            int systems = await (
                from systemApplication in dbContext.Set<SystemApplication>().AsNoTracking()
                where systemApplication.IsActive
                select systemApplication.Id)
                .CountAsync(cancellationToken);

            return new DashboardKpiResponse
            {
                ActiveUsers = activeUsers,
                ActiveContratos = activeContracts,
                Empresas = companies,
                Sistemas = systems
            };
        }

        public async Task<IReadOnlyCollection<UsersByCompanyResponse>> GetUsersByCompany(CancellationToken cancellationToken = default)
        {
            List<UsersByCompanyResponse> companies = await (
                from userRole in dbContext.Set<UserRole>().AsNoTracking()
                join role in dbContext.Set<Role>().AsNoTracking() on userRole.RoleId equals role.Id
                join contract in dbContext.Set<Contract>().AsNoTracking() on role.ContractId equals contract.Id
                join company in dbContext.Set<Company>().AsNoTracking() on contract.CompanyId equals company.Id
                where userRole.IsActive && contract.IsActive && company.IsActive
                group userRole by new
                {
                    company.Id,
                    company.LegalName
                }
                into grouped
                orderby grouped.Count() descending, grouped.Key.LegalName
                select new UsersByCompanyResponse
                {
                    Name = grouped.Key.LegalName,
                    Value = grouped
                        .Select(item => item.UserId)
                        .Distinct()
                        .Count()
                })
                .ToListAsync(cancellationToken);

            return companies;
        }

        public async Task<IReadOnlyCollection<TopSystemApplicationResponse>> GetTopSystems(int limit, CancellationToken cancellationToken = default)
        {
            int normalizedLimit = limit <= 0 ? 5 : limit;
            DateTimeOffset now = DateTimeOffset.UtcNow;

            List<TopSystemApplicationResponse> systems = await (
                from session in dbContext.Set<LoginSession>().AsNoTracking()
                join contract in dbContext.Set<Contract>().AsNoTracking() on session.ContractId equals contract.Id
                join systemApplication in dbContext.Set<SystemApplication>().AsNoTracking() on contract.SystemApplicationId equals systemApplication.Id
                where session.IsActive && !session.RevokedAt.HasValue && now < session.ExpiresAt
                group session by new
                {
                    systemApplication.Id,
                    systemApplication.Name
                }
                into grouped
                orderby grouped.Count() descending, grouped.Key.Name
                select new TopSystemApplicationResponse
                {
                    Name = grouped.Key.Name,
                    Logins = grouped.Count()
                })
                .Take(normalizedLimit)
                .ToListAsync(cancellationToken);

            return systems;
        }

        public async Task<DashboardOverviewResponse> GetOverview(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset start = GetUtcDateStart(from ?? now.AddDays(-6));
            DateTimeOffset end = NormalizeRangeEnd(to ?? now);

            if (end < start)
            {
                (start, end) = (GetUtcDateStart(end), NormalizeRangeEnd(start));
            }

            DateTimeOffset expiringLimit = now.AddDays(30);

            List<DashboardLoginTrendResponse> loginTrend = await GetLoginTrend(start, end, cancellationToken);
            DashboardContractHealthResponse contractHealth = await GetContractHealth(now, expiringLimit, cancellationToken);
            List<DashboardSessionsByHourResponse> sessionsByHour = await GetSessionsByHour(start, end, cancellationToken);
            List<DashboardTopSystemResponse> topSystems = await GetTopSystemsForOverview(start, end, 4, cancellationToken);
            DashboardSecurityPulseResponse securityPulse = await GetSecurityPulse(now, cancellationToken);

            return new DashboardOverviewResponse
            {
                LoginTrend = loginTrend,
                ContractHealth = contractHealth,
                SessionsByHour = sessionsByHour,
                TopSystems = topSystems,
                SecurityPulse = securityPulse
            };
        }

        public async Task<PagedResult<ActiveSessionResponse>> GetActiveSessions(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            int normalizedPage = page <= 0 ? 1 : page;
            int normalizedPageSize = pageSize <= 0 ? 20 : pageSize;
            DateTimeOffset now = DateTimeOffset.UtcNow;

            IQueryable<ActiveSessionResponse> query =
                from session in dbContext.Set<LoginSession>().AsNoTracking()
                join user in dbContext.Set<User>().AsNoTracking() on session.UserId equals user.Id
                join contract in dbContext.Set<Contract>().AsNoTracking() on session.ContractId equals contract.Id
                join company in dbContext.Set<Company>().AsNoTracking() on contract.CompanyId equals company.Id
                join systemApplication in dbContext.Set<SystemApplication>().AsNoTracking() on contract.SystemApplicationId equals systemApplication.Id
                where session.IsActive && !session.RevokedAt.HasValue && now < session.ExpiresAt
                orderby session.CreatedAt descending
                select new ActiveSessionResponse
                {
                    SessionId = session.SessionId,
                    UserId = user.Id,
                    UserName = user.Name,
                    UserEmail = user.Email,
                    EmpresaName = company.LegalName,
                    SistemaName = systemApplication.Name,
                    IpAddress = session.IpAddress,
                    UserAgent = session.UserAgent,
                    CreatedAt = session.CreatedAt,
                    ExpiresAt = session.ExpiresAt
                };

            int totalCount = await query.CountAsync(cancellationToken);
            List<ActiveSessionResponse> items = await query
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .ToListAsync(cancellationToken);

            int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)normalizedPageSize));

            return new PagedResult<ActiveSessionResponse>
            {
                Items = items,
                Pagination = new PaginationMetadata
                {
                    Page = normalizedPage,
                    PageSize = normalizedPageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages
                }
            };
        }

        private async Task<List<DashboardLoginTrendResponse>> GetLoginTrend(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
        {
            List<DashboardLoginTrendResponse> trend = [];
            DateTimeOffset cursor = start.Date;
            DateTimeOffset lastDay = end.Date;

            List<DateTimeOffset> loginDates = await dbContext.Set<LoginSession>()
                .AsNoTracking()
                .Where(session => session.CreatedAt >= start && session.CreatedAt <= end)
                .Select(session => session.CreatedAt)
                .ToListAsync(cancellationToken);

            while (cursor <= lastDay)
            {
                DateTimeOffset nextDay = cursor.AddDays(1);
                int logins = loginDates.Count(createdAt => createdAt >= cursor && createdAt < nextDay);

                trend.Add(new DashboardLoginTrendResponse
                {
                    Label = cursor.ToString("dd/MM"),
                    Logins = logins,
                    Failures = 0
                });

                cursor = nextDay;
            }

            return trend;
        }

        private async Task<DashboardContractHealthResponse> GetContractHealth(DateTimeOffset now, DateTimeOffset expiringLimit, CancellationToken cancellationToken)
        {
            List<Contract> contracts = await dbContext.Set<Contract>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            List<long> systemsWithOAuthClient = await dbContext.Set<OAuthClient>()
                .AsNoTracking()
                .Where(client => client.IsActive)
                .Select(client => client.SystemApplicationId)
                .Distinct()
                .ToListAsync(cancellationToken);

            HashSet<long> systemsWithOAuthClientSet = systemsWithOAuthClient.ToHashSet();

            return new DashboardContractHealthResponse
            {
                Active = contracts.Count(contract => contract.IsActive && now >= contract.StartDate && (!contract.EndDate.HasValue || now <= contract.EndDate.Value)),
                ExpiringSoon = contracts.Count(contract => contract.IsActive && contract.EndDate.HasValue && contract.EndDate.Value >= now && contract.EndDate.Value <= expiringLimit),
                Suspended = contracts.Count(contract => !contract.IsActive),
                WithoutOAuthClient = contracts.Count(contract => !systemsWithOAuthClientSet.Contains(contract.SystemApplicationId))
            };
        }

        private async Task<List<DashboardSessionsByHourResponse>> GetSessionsByHour(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
        {
            List<DateTimeOffset> sessionDates = await dbContext.Set<LoginSession>()
                .AsNoTracking()
                .Where(session => session.CreatedAt >= start && session.CreatedAt <= end)
                .Select(session => session.CreatedAt)
                .ToListAsync(cancellationToken);

            int[] hours = [0, 4, 8, 12, 16, 20];
            List<DashboardSessionsByHourResponse> response = [];

            foreach (int hour in hours)
            {
                int nextHour = hour + 4;
                int sessions = sessionDates.Count(createdAt => createdAt.Hour >= hour && createdAt.Hour < nextHour);

                response.Add(new DashboardSessionsByHourResponse
                {
                    Label = $"{hour:00}h",
                    Sessions = sessions
                });
            }

            return response;
        }

        private async Task<List<DashboardTopSystemResponse>> GetTopSystemsForOverview(DateTimeOffset start, DateTimeOffset end, int limit, CancellationToken cancellationToken)
        {
            return await (
                from session in dbContext.Set<LoginSession>().AsNoTracking()
                join contract in dbContext.Set<Contract>().AsNoTracking() on session.ContractId equals contract.Id
                join systemApplication in dbContext.Set<SystemApplication>().AsNoTracking() on contract.SystemApplicationId equals systemApplication.Id
                where session.CreatedAt >= start && session.CreatedAt <= end
                group session by new
                {
                    systemApplication.Id,
                    systemApplication.Name
                }
                into grouped
                orderby grouped.Count() descending, grouped.Key.Name
                select new DashboardTopSystemResponse
                {
                    SystemApplicationId = grouped.Key.Id,
                    Name = grouped.Key.Name,
                    Accesses = grouped.Count()
                })
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        private async Task<DashboardSecurityPulseResponse> GetSecurityPulse(DateTimeOffset now, CancellationToken cancellationToken)
        {
            int totalUsers = await dbContext.Set<User>().AsNoTracking().CountAsync(cancellationToken);
            int activeUsers = await dbContext.Set<User>().AsNoTracking().CountAsync(user => user.IsActive, cancellationToken);
            int totalSessions = await dbContext.Set<LoginSession>().AsNoTracking().CountAsync(cancellationToken);
            int validSessions = await dbContext.Set<LoginSession>().AsNoTracking().CountAsync(session => session.IsActive && !session.RevokedAt.HasValue && now < session.ExpiresAt, cancellationToken);
            int revokedSessions = await dbContext.Set<LoginSession>().AsNoTracking().CountAsync(session => session.RevokedAt.HasValue, cancellationToken);
            int totalOAuthClients = await dbContext.Set<OAuthClient>().AsNoTracking().CountAsync(cancellationToken);
            int activeOAuthClients = await dbContext.Set<OAuthClient>().AsNoTracking().CountAsync(client => client.IsActive, cancellationToken);

            int activeUsersPercentage = CalculatePercentage(activeUsers, totalUsers);
            int revokedSessionsPenalty = CalculatePercentage(revokedSessions, Math.Max(totalSessions, 1));

            return new DashboardSecurityPulseResponse
            {
                MfaCoverage = 0,
                ValidSessions = CalculatePercentage(validSessions, totalSessions),
                RotatedTokens = CalculatePercentage(activeOAuthClients, totalOAuthClients),
                ReviewedAccesses = Math.Clamp(activeUsersPercentage - revokedSessionsPenalty, 0, 100)
            };
        }

        private static DateTimeOffset GetUtcDateStart(DateTimeOffset value)
        {
            DateTime utcDate = value.UtcDateTime.Date;
            return new DateTimeOffset(utcDate, TimeSpan.Zero);
        }

        private static DateTimeOffset NormalizeRangeEnd(DateTimeOffset value)
        {
            DateTime utcDateTime = value.UtcDateTime;

            if (utcDateTime.TimeOfDay == TimeSpan.Zero)
            {
                utcDateTime = utcDateTime.Date.AddDays(1).AddTicks(-1);
            }

            return new DateTimeOffset(utcDateTime, TimeSpan.Zero);
        }

        private static int CalculatePercentage(int value, int total)
        {
            if (total <= 0)
            {
                return 0;
            }

            int percentage = (int)Math.Round(value * 100.0 / total);
            return Math.Clamp(percentage, 0, 100);
        }
    }
}
