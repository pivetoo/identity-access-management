using Archon.Infrastructure.Services;
using IdentityManagement.Application.Requests.SystemApplications;
using IdentityManagement.Application.Responses.SystemApplications;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class SystemApplicationService : CrudService<SystemApplication>, ISystemApplicationService
    {
        public SystemApplicationService(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<SystemApplicationResponse> CreateSystemApplication(CreateSystemApplicationRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureUniqueSystemApplication(request.Name, request.Audience, null, cancellationToken);

            SystemApplication systemApplication = new SystemApplication(request.Name, request.Description, request.RedirectUris, request.Audience, request.Type);
            bool success = await Insert(cancellationToken, systemApplication);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return ToResponse(systemApplication);
        }

        public async Task<SystemApplicationResponse> UpdateSystemApplication(long id, UpdateSystemApplicationRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("Route id does not match body id.");
            }

            SystemApplication? systemApplication = await (
                from item in DbContext.Set<SystemApplication>().AsTracking()
                where item.Id == id
                select item)
                .FirstOrDefaultAsync(cancellationToken);

            if (systemApplication is null)
            {
                throw new InvalidOperationException("System application not found.");
            }

            await EnsureUniqueSystemApplication(request.Name, request.Audience, id, cancellationToken);

            systemApplication.Update(request.Name, request.Description, request.RedirectUris, request.Audience, request.Type, request.IsActive);

            SystemApplication? result = await Update(systemApplication, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return ToResponse(result);
        }

        public async Task<IReadOnlyCollection<SystemApplicationResponse>> GetActiveSystemApplications(CancellationToken cancellationToken = default)
        {
            List<SystemApplicationResponse> systemApplications = await (
                from systemApplication in DbContext.Set<SystemApplication>().AsNoTracking()
                where systemApplication.IsActive
                orderby systemApplication.Name
                select new SystemApplicationResponse
                {
                    Id = systemApplication.Id,
                    Name = systemApplication.Name,
                    Description = systemApplication.Description,
                    RedirectUris = systemApplication.RedirectUris,
                    IsActive = systemApplication.IsActive,
                    Audience = systemApplication.Audience,
                    Type = systemApplication.Type,
                    CreatedAt = systemApplication.CreatedAt,
                    UpdatedAt = systemApplication.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return systemApplications;
        }

        private async Task EnsureUniqueSystemApplication(string name, string audience, long? currentSystemApplicationId, CancellationToken cancellationToken)
        {
            bool nameExists = await (
                from systemApplication in DbContext.Set<SystemApplication>().AsNoTracking()
                where systemApplication.Name == name && (!currentSystemApplicationId.HasValue || systemApplication.Id != currentSystemApplicationId.Value)
                select systemApplication.Id)
                .AnyAsync(cancellationToken);

            if (nameExists)
            {
                throw new InvalidOperationException("System application name already exists.");
            }

            bool audienceExists = await (
                from systemApplication in DbContext.Set<SystemApplication>().AsNoTracking()
                where systemApplication.Audience == audience && (!currentSystemApplicationId.HasValue || systemApplication.Id != currentSystemApplicationId.Value)
                select systemApplication.Id)
                .AnyAsync(cancellationToken);

            if (audienceExists)
            {
                throw new InvalidOperationException("System application audience already exists.");
            }
        }

        private static SystemApplicationResponse ToResponse(SystemApplication systemApplication)
        {
            return new SystemApplicationResponse
            {
                Id = systemApplication.Id,
                Name = systemApplication.Name,
                Description = systemApplication.Description,
                RedirectUris = systemApplication.RedirectUris,
                IsActive = systemApplication.IsActive,
                Audience = systemApplication.Audience,
                Type = systemApplication.Type,
                CreatedAt = systemApplication.CreatedAt,
                UpdatedAt = systemApplication.UpdatedAt
            };
        }
    }
}
