using Archon.Core.ValueObjects;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.Tenants;
using IdentityManagement.Application.Responses.Tenants;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class TenantDatabaseService : ITenantDatabaseService
    {
        private readonly DbContext dbContext;
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public TenantDatabaseService(DbContext dbContext, IStringLocalizer<IdentityManagementResource> Localizer)
        {
            this.dbContext = dbContext;
            this.Localizer = Localizer;
        }

        public async Task<IReadOnlyCollection<TenantDatabaseResponse>> GetAll(CancellationToken cancellationToken = default)
        {
            return await BuildBaseQuery()
                .OrderBy(item => item.CompanyName)
                .ThenBy(item => item.SystemApplicationName)
                .ToListAsync(cancellationToken);
        }

        public async Task<TenantDatabaseResponse?> GetById(long id, CancellationToken cancellationToken = default)
        {
            return await BuildBaseQuery()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<TenantDatabaseResponse> Create(CreateTenantDatabaseRequest request, CancellationToken cancellationToken = default)
        {
            ValidateRequiredFields(request.ConnectionString, request.ApiKey);

            Contract? contract = await dbContext.Set<Contract>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == request.ContractId, cancellationToken);

            if (contract is null)
            {
                throw new InvalidOperationException("contract.notFound");
            }

            await EnsureContractAvailable(request.ContractId, null, cancellationToken);
            await EnsureUniqueApiKey(request.ApiKey, null, cancellationToken);

            TenantDatabase tenantDatabase = new TenantDatabase(
                request.ContractId,
                request.ConnectionString,
                ResolveDatabaseProvider(request.DatabaseProvider),
                request.ApiKey,
                request.SchemaName);

            dbContext.Set<TenantDatabase>().Add(tenantDatabase);
            await dbContext.SaveChangesAsync(cancellationToken);

            TenantDatabaseResponse? response = await GetById(tenantDatabase.Id, cancellationToken);
            return response ?? throw new InvalidOperationException("tenantDatabase.loadAfterCreate.failed");
        }

        public async Task<TenantDatabaseResponse> Update(long id, UpdateTenantDatabaseRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("request.route.idMismatch");
            }

            ValidateRequiredFields(request.ConnectionString, request.ApiKey);

            TenantDatabase? tenantDatabase = await dbContext.Set<TenantDatabase>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (tenantDatabase is null)
            {
                throw new InvalidOperationException("tenantDatabase.notFound");
            }

            await EnsureUniqueApiKey(request.ApiKey, id, cancellationToken);

            tenantDatabase.Update(
                request.ConnectionString,
                ResolveDatabaseProvider(request.DatabaseProvider),
                request.ApiKey,
                request.SchemaName,
                request.IsActive);

            await dbContext.SaveChangesAsync(cancellationToken);

            TenantDatabaseResponse? response = await GetById(id, cancellationToken);
            return response ?? throw new InvalidOperationException("tenantDatabase.notFound");
        }

        public async Task<TenantDatabaseResponse> SetActive(long id, bool isActive, CancellationToken cancellationToken = default)
        {
            TenantDatabase? tenantDatabase = await dbContext.Set<TenantDatabase>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (tenantDatabase is null)
            {
                throw new InvalidOperationException("tenantDatabase.notFound");
            }

            if (isActive)
            {
                tenantDatabase.Activate();
            }
            else
            {
                tenantDatabase.Deactivate();
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            TenantDatabaseResponse? response = await GetById(id, cancellationToken);
            return response ?? throw new InvalidOperationException("tenantDatabase.notFound");
        }

        private IQueryable<TenantDatabaseResponse> BuildBaseQuery()
        {
            return
                from td in dbContext.Set<TenantDatabase>().AsNoTracking()
                join contract in dbContext.Set<Contract>().AsNoTracking() on td.ContractId equals contract.Id
                join company in dbContext.Set<Company>().AsNoTracking() on contract.CompanyId equals company.Id
                join systemApplication in dbContext.Set<SystemApplication>().AsNoTracking() on contract.SystemApplicationId equals systemApplication.Id
                select new TenantDatabaseResponse
                {
                    Id = td.Id,
                    ContractId = td.ContractId,
                    TenantId = company.TenantId,
                    CompanyName = company.LegalName,
                    SystemApplicationName = systemApplication.Name,
                    ApplicationId = systemApplication.Audience,
                    ConnectionString = td.ConnectionString,
                    DatabaseProvider = (int)td.DatabaseProvider,
                    SchemaName = td.SchemaName,
                    ApiKey = td.ApiKey,
                    IsActive = td.IsActive,
                    CreatedAt = td.CreatedAt,
                    UpdatedAt = td.UpdatedAt
                };
        }

        private void ValidateRequiredFields(string connectionString, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("tenantDatabase.connectionString.required");
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("tenantDatabase.apiKey.required");
            }
        }

        private async Task EnsureContractAvailable(long contractId, long? currentId, CancellationToken cancellationToken)
        {
            bool exists = await dbContext.Set<TenantDatabase>()
                .AsNoTracking()
                .AnyAsync(item => item.ContractId == contractId && (!currentId.HasValue || item.Id != currentId.Value), cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException("tenantDatabase.contract.alreadyConfigured");
            }
        }

        private async Task EnsureUniqueApiKey(string apiKey, long? currentId, CancellationToken cancellationToken)
        {
            string normalized = apiKey.Trim();
            bool exists = await dbContext.Set<TenantDatabase>()
                .AsNoTracking()
                .AnyAsync(item => item.ApiKey == normalized && (!currentId.HasValue || item.Id != currentId.Value), cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException("tenantDatabase.apiKey.alreadyExists");
            }
        }

        private static DatabaseProvider ResolveDatabaseProvider(int value)
        {
            return Enum.IsDefined(typeof(DatabaseProvider), value)
                ? (DatabaseProvider)value
                : DatabaseProvider.PostgreSql;
        }
    }
}
