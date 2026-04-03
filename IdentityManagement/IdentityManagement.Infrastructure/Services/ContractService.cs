using ApplicationEntity = IdentityManagement.Domain.Entities.Application;
using Archon.Infrastructure.Services;
using IdentityManagement.Application.Requests.Contracts;
using IdentityManagement.Application.Responses.Contracts;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class ContractService : CrudService<Contract>, IContractService
    {
        public ContractService(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<ContractSummaryResponse> CreateContract(CreateContractRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureDependencies(request.CompanyId, request.ApplicationId, cancellationToken);

            Contract? existingContract = await GetByCompanyAndApplication(request.CompanyId, request.ApplicationId, cancellationToken);
            if (existingContract is not null && existingContract.IsActive)
            {
                throw new InvalidOperationException("An active contract already exists for this company and application.");
            }

            Contract contract = new Contract(
                request.CompanyId,
                request.ApplicationId,
                GenerateClientId(),
                GenerateClientSecret(),
                GenerateRandomString(64));

            contract.Update(
                request.CompanyId,
                request.ApplicationId,
                request.StartDate,
                request.EndDate,
                true,
                request.AccessTokenLifetime > 0 ? request.AccessTokenLifetime : 3600,
                request.RefreshTokenLifetime > 0 ? request.RefreshTokenLifetime : 2592000);

            bool success = await Insert(cancellationToken, contract);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            Contract hydratedContract = await GetByIdWithRelations(contract.Id, cancellationToken)
                ?? throw new InvalidOperationException("Contract could not be loaded after creation.");

            return ToSummaryResponse(hydratedContract);
        }

        public async Task<ContractSummaryResponse> UpdateContract(long id, UpdateContractRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("Route id does not match body id.");
            }

            Contract? contract = await (
                from item in DbContext.Set<Contract>().AsTracking()
                where item.Id == id
                select item)
                .FirstOrDefaultAsync(cancellationToken);

            if (contract is null)
            {
                throw new InvalidOperationException("Contract not found.");
            }

            await EnsureDependencies(request.CompanyId, request.ApplicationId, cancellationToken);

            Contract? existingContract = await GetByCompanyAndApplication(request.CompanyId, request.ApplicationId, cancellationToken);
            if (existingContract is not null && existingContract.Id != id && existingContract.IsActive)
            {
                throw new InvalidOperationException("An active contract already exists for this company and application.");
            }

            contract.Update(
                request.CompanyId,
                request.ApplicationId,
                request.StartDate,
                request.EndDate,
                request.IsActive,
                request.AccessTokenLifetime,
                request.RefreshTokenLifetime);

            Contract? result = await Update(contract, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            Contract hydratedContract = await GetByIdWithRelations(result.Id, cancellationToken)
                ?? throw new InvalidOperationException("Contract could not be loaded after update.");

            return ToSummaryResponse(hydratedContract);
        }

        public async Task<IReadOnlyCollection<ContractSummaryResponse>> GetByCompanyId(long companyId, CancellationToken cancellationToken = default)
        {
            List<ContractSummaryResponse> contracts = await (
                from contract in DbContext.Set<Contract>().AsNoTracking()
                join company in DbContext.Set<Company>().AsNoTracking() on contract.CompanyId equals company.Id
                join application in DbContext.Set<ApplicationEntity>().AsNoTracking() on contract.ApplicationId equals application.Id
                where contract.CompanyId == companyId
                orderby company.LegalName, application.Name
                select new ContractSummaryResponse
                {
                    Id = contract.Id,
                    CompanyName = company.LegalName,
                    ApplicationName = application.Name,
                    StartDate = contract.StartDate,
                    EndDate = contract.EndDate,
                    IsActive = contract.IsActive,
                    IsValid = contract.IsActive && DateTimeOffset.UtcNow >= contract.StartDate && (!contract.EndDate.HasValue || DateTimeOffset.UtcNow <= contract.EndDate.Value)
                })
                .ToListAsync(cancellationToken);

            return contracts;
        }

        public async Task<IReadOnlyCollection<ContractSummaryResponse>> GetByApplicationId(long applicationId, CancellationToken cancellationToken = default)
        {
            List<ContractSummaryResponse> contracts = await (
                from contract in DbContext.Set<Contract>().AsNoTracking()
                join company in DbContext.Set<Company>().AsNoTracking() on contract.CompanyId equals company.Id
                join application in DbContext.Set<ApplicationEntity>().AsNoTracking() on contract.ApplicationId equals application.Id
                where contract.ApplicationId == applicationId
                orderby company.LegalName, application.Name
                select new ContractSummaryResponse
                {
                    Id = contract.Id,
                    CompanyName = company.LegalName,
                    ApplicationName = application.Name,
                    StartDate = contract.StartDate,
                    EndDate = contract.EndDate,
                    IsActive = contract.IsActive,
                    IsValid = contract.IsActive && DateTimeOffset.UtcNow >= contract.StartDate && (!contract.EndDate.HasValue || DateTimeOffset.UtcNow <= contract.EndDate.Value)
                })
                .ToListAsync(cancellationToken);

            return contracts;
        }

        public async Task<IReadOnlyCollection<ContractSummaryResponse>> GetActive(CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            List<ContractSummaryResponse> contracts = await (
                from contract in DbContext.Set<Contract>().AsNoTracking()
                join company in DbContext.Set<Company>().AsNoTracking() on contract.CompanyId equals company.Id
                join application in DbContext.Set<ApplicationEntity>().AsNoTracking() on contract.ApplicationId equals application.Id
                where contract.IsActive && now >= contract.StartDate && (!contract.EndDate.HasValue || now <= contract.EndDate.Value)
                orderby company.LegalName, application.Name
                select new ContractSummaryResponse
                {
                    Id = contract.Id,
                    CompanyName = company.LegalName,
                    ApplicationName = application.Name,
                    StartDate = contract.StartDate,
                    EndDate = contract.EndDate,
                    IsActive = contract.IsActive,
                    IsValid = true
                })
                .ToListAsync(cancellationToken);

            return contracts;
        }

        public Task<Contract?> GetByCompanyAndApplication(long companyId, long applicationId, CancellationToken cancellationToken = default)
        {
            return (
                from contract in DbContext.Set<Contract>().AsNoTracking()
                where contract.CompanyId == companyId && contract.ApplicationId == applicationId
                select contract)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<string?> GetUserRoleNameForContract(long userId, long contractId, CancellationToken cancellationToken = default)
        {
            return (
                from userRole in DbContext.Set<UserRole>().AsNoTracking()
                join role in DbContext.Set<Role>().AsNoTracking() on userRole.RoleId equals role.Id
                where userRole.UserId == userId && role.ContractId == contractId && userRole.IsActive && !userRole.RevokedAt.HasValue
                select role.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<ContractSelectionResponseItem>> GetActiveContractSelectionsByUserId(long userId, CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            List<ContractSelectionResponseItem> contracts = await (
                from userRole in DbContext.Set<UserRole>().AsNoTracking()
                join role in DbContext.Set<Role>().AsNoTracking() on userRole.RoleId equals role.Id
                join contract in DbContext.Set<Contract>().AsNoTracking() on role.ContractId equals contract.Id
                join company in DbContext.Set<Company>().AsNoTracking() on contract.CompanyId equals company.Id
                join application in DbContext.Set<ApplicationEntity>().AsNoTracking() on contract.ApplicationId equals application.Id
                where userRole.UserId == userId &&
                      userRole.IsActive &&
                      !userRole.RevokedAt.HasValue &&
                      contract.IsActive &&
                      now >= contract.StartDate &&
                      (!contract.EndDate.HasValue || now <= contract.EndDate.Value)
                orderby company.LegalName, application.Name, role.Name
                select new ContractSelectionResponseItem
                {
                    ContractId = contract.Id,
                    ApplicationName = application.Name,
                    CompanyName = company.LegalName,
                    RedirectUris = application.RedirectUris,
                    RoleName = role.Name
                })
                .ToListAsync(cancellationToken);

            return contracts;
        }

        public Task<Contract?> GetByIdWithRelations(long id, CancellationToken cancellationToken = default)
        {
            return DbContext.Set<Contract>()
                .AsNoTracking()
                .Include(item => item.Company)
                .Include(item => item.Application)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyCollection<Contract>> GetActiveContractsByUserId(long userId, CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            List<long> contractIds = await (
                from userRole in DbContext.Set<UserRole>().AsNoTracking()
                join role in DbContext.Set<Role>().AsNoTracking() on userRole.RoleId equals role.Id
                where userRole.UserId == userId && userRole.IsActive && !userRole.RevokedAt.HasValue
                select role.ContractId)
                .Distinct()
                .ToListAsync(cancellationToken);

            List<Contract> contracts = await (
                from contract in DbContext.Set<Contract>().AsNoTracking()
                where contractIds.Contains(contract.Id) &&
                      contract.IsActive &&
                      now >= contract.StartDate &&
                      (!contract.EndDate.HasValue || now <= contract.EndDate.Value)
                select contract)
                .ToListAsync(cancellationToken);

            return contracts;
        }

        public Task<Contract?> GetByClientId(string clientId, CancellationToken cancellationToken = default)
        {
            return DbContext.Set<Contract>()
                .AsNoTracking()
                .Include(item => item.Company)
                .Include(item => item.Application)
                .FirstOrDefaultAsync(item => item.ClientId == clientId, cancellationToken);
        }

        public Task<ContractSecretsResponse?> GetContractSecrets(long id, CancellationToken cancellationToken = default)
        {
            return (
                from contract in DbContext.Set<Contract>().AsNoTracking()
                where contract.Id == id
                select new ContractSecretsResponse
                {
                    ClientId = contract.ClientId,
                    ClientSecret = contract.ClientSecret,
                    JwtSecretKey = contract.JwtSecretKey
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public string GenerateClientId()
        {
            return $"client_{Guid.NewGuid():N}";
        }

        public string GenerateClientSecret()
        {
            return GenerateRandomString(64);
        }

        private static ContractSummaryResponse ToSummaryResponse(Contract contract)
        {
            return new ContractSummaryResponse
            {
                Id = contract.Id,
                CompanyName = contract.Company.LegalName,
                ApplicationName = contract.Application.Name,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                IsActive = contract.IsActive,
                IsValid = contract.IsValid()
            };
        }

        private static string GenerateRandomString(int length)
        {
            byte[] buffer = RandomNumberGenerator.GetBytes(length);
            return Convert.ToBase64String(buffer)
                .Replace("/", string.Empty, StringComparison.Ordinal)
                .Replace("+", string.Empty, StringComparison.Ordinal)
                .Replace("=", string.Empty, StringComparison.Ordinal)[..length];
        }

        private async Task EnsureDependencies(long companyId, long applicationId, CancellationToken cancellationToken)
        {
            bool companyExists = await DbContext.Set<Company>().AnyAsync(item => item.Id == companyId && item.IsActive, cancellationToken);
            if (!companyExists)
            {
                throw new InvalidOperationException("Company not found or inactive.");
            }

            bool applicationExists = await DbContext.Set<ApplicationEntity>().AnyAsync(item => item.Id == applicationId && item.IsActive, cancellationToken);
            if (!applicationExists)
            {
                throw new InvalidOperationException("Application not found or inactive.");
            }
        }
    }
}
