using IdentityManagement.Application.Responses.Tenants;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class TenantResolutionService : ITenantResolutionService
    {
        private readonly DbContext dbContext;

        public TenantResolutionService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<TenantResolutionResponse?> ResolveByTenantAndApplication(Guid tenantId, string applicationId, CancellationToken cancellationToken = default)
        {
            if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(applicationId))
            {
                return null;
            }

            string normalizedApplicationId = applicationId.Trim();

            return await BuildBaseQuery()
                .Where(item =>
                    item.Company.TenantId == tenantId &&
                    item.SystemApplication.Audience == normalizedApplicationId)
                .Select(MapToResponse())
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<TenantResolutionResponse>> ListByApplication(string applicationId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(applicationId))
            {
                return Array.Empty<TenantResolutionResponse>();
            }

            string normalized = applicationId.Trim();

            return await BuildBaseQuery()
                .Where(item => item.SystemApplication.Audience == normalized)
                .Select(MapToResponse())
                .ToListAsync(cancellationToken);
        }

        public async Task<TenantResolutionResponse?> ResolveByApiKey(string apiKey, string? applicationId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return null;
            }

            string normalizedApiKey = apiKey.Trim();
            string? normalizedApplicationId = string.IsNullOrWhiteSpace(applicationId) ? null : applicationId.Trim();

            return await BuildBaseQuery()
                .Where(item =>
                    item.TenantDatabase.ApiKey == normalizedApiKey &&
                    (normalizedApplicationId == null || item.SystemApplication.Audience == normalizedApplicationId))
                .Select(MapToResponse())
                .FirstOrDefaultAsync(cancellationToken);
        }

        private IQueryable<TenantResolutionRecord> BuildBaseQuery()
        {
            return
                from td in dbContext.Set<TenantDatabase>().AsNoTracking()
                join contract in dbContext.Set<Contract>().AsNoTracking() on td.ContractId equals contract.Id
                join company in dbContext.Set<Company>().AsNoTracking() on contract.CompanyId equals company.Id
                join systemApplication in dbContext.Set<SystemApplication>().AsNoTracking() on contract.SystemApplicationId equals systemApplication.Id
                where td.IsActive && contract.IsActive && company.IsActive && systemApplication.IsActive
                select new TenantResolutionRecord
                {
                    TenantDatabase = td,
                    Contract = contract,
                    Company = company,
                    SystemApplication = systemApplication
                };
        }

        private static System.Linq.Expressions.Expression<Func<TenantResolutionRecord, TenantResolutionResponse>> MapToResponse()
        {
            return record => new TenantResolutionResponse
            {
                TenantId = record.Company.TenantId.ToString(),
                CompanyName = record.Company.LegalName,
                ApplicationId = record.SystemApplication.Audience,
                ConnectionString = record.TenantDatabase.ConnectionString,
                DatabaseProvider = (int)record.TenantDatabase.DatabaseProvider,
                Schema = record.TenantDatabase.SchemaName,
                ApiKey = record.TenantDatabase.ApiKey
            };
        }

        private sealed class TenantResolutionRecord
        {
            public TenantDatabase TenantDatabase { get; set; } = null!;

            public Contract Contract { get; set; } = null!;

            public Company Company { get; set; } = null!;

            public SystemApplication SystemApplication { get; set; } = null!;
        }
    }
}
