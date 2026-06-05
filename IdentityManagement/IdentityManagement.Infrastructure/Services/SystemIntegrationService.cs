using IdentityManagement.Application.Requests.SystemIntegrations;
using IdentityManagement.Application.Responses.SystemIntegrations;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class SystemIntegrationService : ISystemIntegrationService
    {
        private readonly DbContext dbContext;

        public SystemIntegrationService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IReadOnlyCollection<SystemIntegrationResponse>> GetBySystemApplicationAsync(long systemApplicationId, CancellationToken cancellationToken = default)
        {
            List<SystemIntegration> integrations = await dbContext.Set<SystemIntegration>()
                .AsNoTracking()
                .Include(integration => integration.Parameters)
                .Where(integration => integration.SystemApplicationId == systemApplicationId)
                .OrderBy(integration => integration.Name)
                .ToListAsync(cancellationToken);

            return integrations.Select(ToResponse).ToList();
        }

        public async Task<SystemIntegrationResponse> CreateAsync(UpsertSystemIntegrationRequest request, CancellationToken cancellationToken = default)
        {
            bool systemExists = await dbContext.Set<SystemApplication>()
                .AsNoTracking()
                .AnyAsync(application => application.Id == request.SystemApplicationId, cancellationToken);

            if (!systemExists)
            {
                throw new InvalidOperationException("systemApplication.notFound");
            }

            SystemIntegration integration = new SystemIntegration(request.SystemApplicationId, request.Name, request.BaseUrl);
            ApplyActiveState(integration, request.IsActive);

            dbContext.Set<SystemIntegration>().Add(integration);
            await dbContext.SaveChangesAsync(cancellationToken);

            ReplaceParameters(integration, request.Parameters);
            await dbContext.SaveChangesAsync(cancellationToken);

            return ToResponse(integration);
        }

        public async Task<SystemIntegrationResponse> UpdateAsync(long id, UpsertSystemIntegrationRequest request, CancellationToken cancellationToken = default)
        {
            SystemIntegration? integration = await dbContext.Set<SystemIntegration>()
                .AsTracking()
                .Include(item => item.Parameters)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (integration is null)
            {
                throw new InvalidOperationException("systemIntegration.notFound");
            }

            integration.Update(request.Name, request.BaseUrl);
            ApplyActiveState(integration, request.IsActive);

            dbContext.Set<SystemIntegrationParameter>().RemoveRange(integration.Parameters);
            ReplaceParameters(integration, request.Parameters);

            dbContext.Set<SystemIntegration>().Update(integration);
            await dbContext.SaveChangesAsync(cancellationToken);

            return ToResponse(integration);
        }

        public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            SystemIntegration? integration = await dbContext.Set<SystemIntegration>()
                .AsTracking()
                .Include(item => item.Parameters)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (integration is null)
            {
                throw new InvalidOperationException("systemIntegration.notFound");
            }

            dbContext.Set<SystemIntegrationParameter>().RemoveRange(integration.Parameters);
            dbContext.Set<SystemIntegration>().Remove(integration);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static void ApplyActiveState(SystemIntegration integration, bool isActive)
        {
            if (isActive)
            {
                integration.Activate();
            }
            else
            {
                integration.Deactivate();
            }
        }

        private static void ReplaceParameters(SystemIntegration integration, IEnumerable<SystemIntegrationParameterRequest> parameters)
        {
            List<SystemIntegrationParameter> newParameters = parameters
                .Select(parameter => new SystemIntegrationParameter(
                    integration.Id,
                    parameter.Key,
                    parameter.Value,
                    parameter.IsSecret,
                    parameter.ValueSource,
                    parameter.SourceAudience))
                .ToList();

            integration.ReplaceParameters(newParameters);
        }

        private static SystemIntegrationResponse ToResponse(SystemIntegration integration)
        {
            return new SystemIntegrationResponse
            {
                Id = integration.Id,
                SystemApplicationId = integration.SystemApplicationId,
                Name = integration.Name,
                BaseUrl = integration.BaseUrl,
                IsActive = integration.IsActive,
                Parameters = integration.Parameters.Select(ToParameterResponse).ToList()
            };
        }

        private static SystemIntegrationParameterResponse ToParameterResponse(SystemIntegrationParameter parameter)
        {
            // Valor de parametros secretos nao volta na resposta de admin; o operador reinforma ao editar.
            return new SystemIntegrationParameterResponse
            {
                Key = parameter.Key,
                Value = parameter.IsSecret ? null : parameter.Value,
                IsSecret = parameter.IsSecret,
                ValueSource = parameter.ValueSource,
                SourceAudience = parameter.SourceAudience
            };
        }
    }
}
