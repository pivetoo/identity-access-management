using Archon.Infrastructure.Services;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.Contracts;
using IdentityManagement.Application.Responses.Contracts;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class ContractService : CrudService<Contract>, IContractService
    {
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public ContractService(DbContext dbContext, IStringLocalizer<IdentityManagementResource> Localizer) : base(dbContext)
        {
            this.Localizer = Localizer;
        }

        public async Task<ContractSummaryResponse> CreateContract(CreateContractRequest request, CancellationToken cancellationToken = default)
        {
            ValidateDateRange(request.StartDate, request.EndDate);
            await EnsureDependencies(request.CompanyId, request.SystemApplicationId, cancellationToken);

            Contract? existingContract = await GetByCompanyAndSystemApplication(request.CompanyId, request.SystemApplicationId, cancellationToken);
            if (existingContract is not null && existingContract.IsActive)
            {
                throw new InvalidOperationException(Localizer["contract.activeDuplicate"]);
            }

            Contract contract = new Contract(
                request.CompanyId,
                request.SystemApplicationId);

            contract.Update(
                request.CompanyId,
                request.SystemApplicationId,
                request.StartDate,
                request.EndDate,
                true);

            bool success = await Insert(cancellationToken, contract);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            await ApplySystemRoleTemplates(contract.Id, contract.SystemApplicationId, cancellationToken);

            Contract hydratedContract = await GetByIdWithRelations(contract.Id, cancellationToken)
                ?? throw new InvalidOperationException(Localizer["contract.loadAfterCreate.failed"]);

            return ToSummaryResponse(hydratedContract);
        }

        public async Task<ContractSummaryResponse> UpdateContract(long id, UpdateContractRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(Localizer["request.route.idMismatch"]);
            }

            ValidateDateRange(request.StartDate, request.EndDate);

            Contract? contract = await (
                from item in DbContext.Set<Contract>().AsTracking()
                where item.Id == id
                select item)
                .FirstOrDefaultAsync(cancellationToken);

            if (contract is null)
            {
                throw new InvalidOperationException(Localizer["contract.notFound"]);
            }

            await EnsureSystemApplicationChangeAllowed(contract, request.SystemApplicationId, cancellationToken);
            await EnsureDependencies(request.CompanyId, request.SystemApplicationId, cancellationToken);

            Contract? existingContract = await GetByCompanyAndSystemApplication(request.CompanyId, request.SystemApplicationId, cancellationToken);
            if (existingContract is not null && existingContract.Id != id && existingContract.IsActive)
            {
                throw new InvalidOperationException(Localizer["contract.activeDuplicate"]);
            }

            contract.Update(
                request.CompanyId,
                request.SystemApplicationId,
                request.StartDate,
                request.EndDate,
                request.IsActive);

            Contract? result = await Update(contract, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            Contract hydratedContract = await GetByIdWithRelations(result.Id, cancellationToken)
                ?? throw new InvalidOperationException(Localizer["contract.loadAfterUpdate.failed"]);

            return ToSummaryResponse(hydratedContract);
        }

        public async Task<IReadOnlyCollection<ContractSummaryResponse>> GetByCompanyId(long companyId, CancellationToken cancellationToken = default)
        {
            List<ContractSummaryResponse> contracts = await (
                from contract in DbContext.Set<Contract>().AsNoTracking()
                join company in DbContext.Set<Company>().AsNoTracking() on contract.CompanyId equals company.Id
                join systemApplication in DbContext.Set<SystemApplication>().AsNoTracking() on contract.SystemApplicationId equals systemApplication.Id
                where contract.CompanyId == companyId
                orderby company.LegalName, systemApplication.Name
                select new ContractSummaryResponse
                {
                    Id = contract.Id,
                    CompanyId = contract.CompanyId,
                    SystemApplicationId = contract.SystemApplicationId,
                    TenantId = contract.TenantId,
                    CompanyName = company.LegalName,
                    SystemApplicationName = systemApplication.Name,
                    StartDate = contract.StartDate,
                    EndDate = contract.EndDate,
                    IsActive = contract.IsActive,
                    IsValid = contract.IsActive && DateTimeOffset.UtcNow >= contract.StartDate && (!contract.EndDate.HasValue || DateTimeOffset.UtcNow <= contract.EndDate.Value)
                })
                .ToListAsync(cancellationToken);

            return contracts;
        }

        public async Task<IReadOnlyCollection<ContractSummaryResponse>> GetBySystemApplicationId(long systemApplicationId, CancellationToken cancellationToken = default)
        {
            List<ContractSummaryResponse> contracts = await (
                from contract in DbContext.Set<Contract>().AsNoTracking()
                join company in DbContext.Set<Company>().AsNoTracking() on contract.CompanyId equals company.Id
                join systemApplication in DbContext.Set<SystemApplication>().AsNoTracking() on contract.SystemApplicationId equals systemApplication.Id
                where contract.SystemApplicationId == systemApplicationId
                orderby company.LegalName, systemApplication.Name
                select new ContractSummaryResponse
                {
                    Id = contract.Id,
                    CompanyId = contract.CompanyId,
                    SystemApplicationId = contract.SystemApplicationId,
                    TenantId = contract.TenantId,
                    CompanyName = company.LegalName,
                    SystemApplicationName = systemApplication.Name,
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
                join systemApplication in DbContext.Set<SystemApplication>().AsNoTracking() on contract.SystemApplicationId equals systemApplication.Id
                where contract.IsActive && now >= contract.StartDate && (!contract.EndDate.HasValue || now <= contract.EndDate.Value)
                orderby company.LegalName, systemApplication.Name
                select new ContractSummaryResponse
                {
                    Id = contract.Id,
                    SystemApplicationId = contract.SystemApplicationId,
                    TenantId = contract.TenantId,
                    CompanyName = company.LegalName,
                    SystemApplicationName = systemApplication.Name,
                    StartDate = contract.StartDate,
                    EndDate = contract.EndDate,
                    IsActive = contract.IsActive,
                    IsValid = true
                })
                .ToListAsync(cancellationToken);

            return contracts;
        }

        public Task<Contract?> GetByCompanyAndSystemApplication(long companyId, long systemApplicationId, CancellationToken cancellationToken = default)
        {
            return (
                from contract in DbContext.Set<Contract>().AsNoTracking()
                where contract.CompanyId == companyId && contract.SystemApplicationId == systemApplicationId
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

        public async Task<IReadOnlyCollection<ContractSelectionResponseItem>> GetActiveContractSelectionsByUserId(long userId, long? systemApplicationId = null, CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            List<ContractSelectionResponseItem> rows = await (
                from userRole in DbContext.Set<UserRole>().AsNoTracking()
                join role in DbContext.Set<Role>().AsNoTracking() on userRole.RoleId equals role.Id
                join contract in DbContext.Set<Contract>().AsNoTracking() on role.ContractId equals contract.Id
                join company in DbContext.Set<Company>().AsNoTracking() on contract.CompanyId equals company.Id
                join systemApplication in DbContext.Set<SystemApplication>().AsNoTracking() on contract.SystemApplicationId equals systemApplication.Id
                where userRole.UserId == userId &&
                      userRole.IsActive &&
                      !userRole.RevokedAt.HasValue &&
                      contract.IsActive &&
                      (!systemApplicationId.HasValue || contract.SystemApplicationId == systemApplicationId.Value) &&
                      now >= contract.StartDate &&
                      (!contract.EndDate.HasValue || now <= contract.EndDate.Value)
                orderby company.LegalName, systemApplication.Name, role.Name
                select new ContractSelectionResponseItem
                {
                    ContractId = contract.Id,
                    SystemApplicationName = systemApplication.Name,
                    CompanyName = company.LegalName,
                    RoleName = role.Name,
                    PortalUrl = (
                        from client in DbContext.Set<OAuthClient>()
                        join redirectUri in DbContext.Set<OAuthClientRedirectUri>() on client.Id equals redirectUri.OAuthClientId
                        where client.SystemApplicationId == contract.SystemApplicationId &&
                              client.IsDefault &&
                              client.IsActive &&
                              redirectUri.Type == Domain.ValueObjects.OAuthRedirectUriType.SignIn &&
                              redirectUri.IsActive
                        select redirectUri.Uri)
                        .FirstOrDefault() ?? string.Empty
                })
                .ToListAsync(cancellationToken);

            List<ContractSelectionResponseItem> contracts = rows
                .GroupBy(item => new
                {
                    item.ContractId,
                    item.SystemApplicationName,
                    item.CompanyName
                })
                .Select(group => new ContractSelectionResponseItem
                {
                    ContractId = group.Key.ContractId,
                    SystemApplicationName = group.Key.SystemApplicationName,
                    CompanyName = group.Key.CompanyName,
                    RoleName = group.Select(item => item.RoleName).FirstOrDefault() ?? string.Empty,
                    PortalUrl = ResolvePortalUrl(group.Select(item => item.PortalUrl).FirstOrDefault(item => !string.IsNullOrWhiteSpace(item)))
                })
                .ToList();

            return contracts;
        }

        public Task<Contract?> GetByIdWithRelations(long id, CancellationToken cancellationToken = default)
        {
            return DbContext.Set<Contract>()
                .AsNoTracking()
                .Include(item => item.Company)
                .Include(item => item.SystemApplication)
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

        private static ContractSummaryResponse ToSummaryResponse(Contract contract)
        {
            return new ContractSummaryResponse
            {
                Id = contract.Id,
                CompanyId = contract.CompanyId,
                SystemApplicationId = contract.SystemApplicationId,
                TenantId = contract.TenantId,
                CompanyName = contract.Company.LegalName,
                SystemApplicationName = contract.SystemApplication.Name,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                IsActive = contract.IsActive,
                IsValid = contract.IsValid()
            };
        }

        private async Task EnsureDependencies(long companyId, long systemApplicationId, CancellationToken cancellationToken)
        {
            bool companyExists = await DbContext.Set<Company>().AnyAsync(item => item.Id == companyId && item.IsActive, cancellationToken);
            if (!companyExists)
            {
                throw new InvalidOperationException(Localizer["company.notFoundOrInactive"]);
            }

            bool systemApplicationExists = await DbContext.Set<SystemApplication>().AnyAsync(item => item.Id == systemApplicationId && item.IsActive, cancellationToken);
            if (!systemApplicationExists)
            {
                throw new InvalidOperationException(Localizer["systemApplication.notFoundOrInactive"]);
            }
        }

        private async Task ApplySystemRoleTemplates(long contractId, long systemApplicationId, CancellationToken cancellationToken)
        {
            List<SystemRoleTemplate> templates = await (
                from template in DbContext.Set<SystemRoleTemplate>().AsNoTracking()
                where template.SystemApplicationId == systemApplicationId && template.IsActive
                orderby template.Name
                select template)
                .ToListAsync(cancellationToken);

            if (templates.Count == 0)
            {
                return;
            }

            List<Role> roles = templates
                .Select(template => new Role(template.Name, template.Description, contractId, template.IsRoot, template.IsDefault))
                .ToList();

            await DbContext.Set<Role>().AddRangeAsync(roles, cancellationToken);
            await DbContext.SaveChangesAsync(cancellationToken);

            List<long> templateIds = templates
                .Select(item => item.Id)
                .ToList();

            List<SystemRoleTemplateAccessResource> templateLinks = await (
                from link in DbContext.Set<SystemRoleTemplateAccessResource>().AsNoTracking()
                where templateIds.Contains(link.SystemRoleTemplateId) && link.IsActive
                select link)
                .ToListAsync(cancellationToken);

            List<RoleAccessResource> roleAccessResources = [];

            foreach (SystemRoleTemplate template in templates)
            {
                Role role = roles.First(item => item.Name == template.Name);

                List<RoleAccessResource> currentRoleAccessResources = templateLinks
                    .Where(item => item.SystemRoleTemplateId == template.Id)
                    .Select(item => new RoleAccessResource(role.Id, item.AccessResourceId))
                    .ToList();

                roleAccessResources.AddRange(currentRoleAccessResources);
            }

            if (roleAccessResources.Count > 0)
            {
                await DbContext.Set<RoleAccessResource>().AddRangeAsync(roleAccessResources, cancellationToken);
                await DbContext.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task EnsureSystemApplicationChangeAllowed(Contract contract, long requestedSystemApplicationId, CancellationToken cancellationToken)
        {
            if (contract.SystemApplicationId == requestedSystemApplicationId)
            {
                return;
            }

            bool hasRoles = await DbContext.Set<Role>()
                .AsNoTracking()
                .AnyAsync(item => item.ContractId == contract.Id, cancellationToken);

            if (hasRoles)
            {
                throw new InvalidOperationException(Localizer["contract.systemApplication.changeNotAllowedAfterRoles"]);
            }
        }

        private void ValidateDateRange(DateTimeOffset startDate, DateTimeOffset? endDate)
        {
            if (endDate.HasValue && endDate.Value <= startDate)
            {
                throw new InvalidOperationException(Localizer["date.endMustBeGreaterThanStart"]);
            }
        }

        private static string ResolvePortalUrl(string? signInRedirectUri)
        {
            if (string.IsNullOrWhiteSpace(signInRedirectUri))
            {
                return string.Empty;
            }

            if (!Uri.TryCreate(signInRedirectUri, UriKind.Absolute, out Uri? parsed))
            {
                return signInRedirectUri;
            }

            return $"{parsed.GetLeftPart(UriPartial.Authority)}/";
        }
    }
}
