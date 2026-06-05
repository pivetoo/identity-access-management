using Archon.Core.ValueObjects;
using IdentityManagement.Application.Requests.Clients;
using IdentityManagement.Application.Requests.Contracts;
using IdentityManagement.Application.Responses.Clients;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class ClientOnboardingService : IClientOnboardingService
    {
        private const string CentralSystemAudience = "identity-management";

        private readonly DbContext dbContext;
        private readonly IContractService contractService;
        private readonly ITenantProvisioner provisioner;
        private readonly IEmailSender emailSender;
        private readonly ILogger<ClientOnboardingService> logger;

        public ClientOnboardingService(
            DbContext dbContext,
            IContractService contractService,
            ITenantProvisioner provisioner,
            IEmailSender emailSender,
            ILogger<ClientOnboardingService> logger)
        {
            this.dbContext = dbContext;
            this.contractService = contractService;
            this.provisioner = provisioner;
            this.emailSender = emailSender;
            this.logger = logger;
        }

        public async Task<OnboardClientResponse> OnboardClient(OnboardClientRequest request, string setupBaseUrl, CancellationToken ct = default)
        {
            await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(ct);

            List<string> plannedDatabases = new();
            List<long> contractIds = new();
            List<string> systemNames = new();
            List<string> createdDatabases = new();

            Company company = new Company(request.LegalName, request.TradeName, request.Document, request.Email, request.PhoneNumber ?? string.Empty);
            string setupLink = string.Empty;

            try
            {
                dbContext.Set<Company>().Add(company);
                await dbContext.SaveChangesAsync(ct);

                string slug = TenantNaming.Slugify(!string.IsNullOrWhiteSpace(request.TradeName) ? request.TradeName : request.LegalName);

                foreach (OnboardClientSystemItem item in request.Systems)
                {
                    SystemApplication systemApp = await dbContext.Set<SystemApplication>()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(application => application.Id == item.SystemApplicationId, ct)
                        ?? throw new InvalidOperationException("systemApplication.notFoundOrInactive");

                    if (systemApp.Audience == CentralSystemAudience)
                    {
                        throw new InvalidOperationException("onboarding.centralSystemNotAllowed");
                    }

                    if (!systemApp.IsActive)
                    {
                        throw new InvalidOperationException("systemApplication.notFoundOrInactive");
                    }

                    Contract contract = await contractService.CreateContractCore(new CreateContractRequest
                    {
                        CompanyId = company.Id,
                        SystemApplicationId = item.SystemApplicationId,
                        StartDate = item.StartDate,
                        EndDate = item.EndDate
                    }, ct);

                    string dbName = TenantNaming.DatabaseName(systemApp.Audience, slug, company.Id);
                    string apiKey = provisioner.GenerateApiKey();
                    string conn = provisioner.BuildTenantConnectionString(dbName);

                    TenantDatabase tenantDatabase = new TenantDatabase(contract.Id, conn, DatabaseProvider.PostgreSql, apiKey, "public");
                    dbContext.Set<TenantDatabase>().Add(tenantDatabase);

                    plannedDatabases.Add(dbName);
                    contractIds.Add(contract.Id);
                    systemNames.Add(systemApp.Name);
                }

                string token = GenerateOpaqueToken();
                ContractAdminInvitation invitation = new ContractAdminInvitation(company.Id, token, DateTimeOffset.UtcNow.AddDays(7), true);
                dbContext.Set<ContractAdminInvitation>().Add(invitation);
                setupLink = $"{setupBaseUrl.TrimEnd('/')}/setup-admin?token={token}";

                await dbContext.SaveChangesAsync(ct);

                foreach (string db in plannedDatabases)
                {
                    await provisioner.CreateDatabaseAsync(db, ct);
                    createdDatabases.Add(db);
                }

                await transaction.CommitAsync(ct);
            }
            catch (Exception)
            {
                foreach (string db in createdDatabases)
                {
                    try
                    {
                        await provisioner.DropDatabaseAsync(db, ct);
                    }
                    catch (Exception dropEx)
                    {
                        logger.LogError(dropEx, "Failed to drop tenant database '{Database}' during onboarding compensation.", db);
                    }
                }

                await transaction.RollbackAsync(ct);
                throw;
            }

            await emailSender.SendClientAdminInvitationEmailAsync(company.Email, company.LegalName, systemNames, setupLink, ct);

            return new OnboardClientResponse
            {
                CompanyId = company.Id,
                ContractIds = contractIds.ToArray(),
                DatabaseNames = plannedDatabases.ToArray()
            };
        }

        private static string GenerateOpaqueToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Base64UrlEncoder.Encode(bytes);
        }
    }
}
