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
    }
}
