using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    public class AccessResource : Entity
    {
        private readonly List<RoleAccessResource> roleAccessResources = [];

        public long SystemApplicationId { get; private set; }

        public SystemApplication SystemApplication { get; private set; } = null!;

        public string Name { get; private set; } = string.Empty;

        public string Description { get; private set; } = string.Empty;

        public string Controller { get; private set; } = string.Empty;

        public string Action { get; private set; } = string.Empty;

        public string HttpMethod { get; private set; } = string.Empty;

        public string Route { get; private set; } = string.Empty;

        public bool IsActive { get; private set; } = true;

        public IReadOnlyCollection<RoleAccessResource> RoleAccessResources => roleAccessResources.AsReadOnly();

        private AccessResource()
        {
        }

        public AccessResource(long systemApplicationId, string name, string description, string controller, string action, string httpMethod, string route)
        {
            if (systemApplicationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(systemApplicationId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(description);
            ArgumentException.ThrowIfNullOrWhiteSpace(controller);
            ArgumentException.ThrowIfNullOrWhiteSpace(action);
            ArgumentException.ThrowIfNullOrWhiteSpace(httpMethod);
            ArgumentException.ThrowIfNullOrWhiteSpace(route);

            SystemApplicationId = systemApplicationId;
            Name = name.Trim();
            Description = description.Trim();
            Controller = controller.Trim();
            Action = action.Trim();
            HttpMethod = httpMethod.Trim().ToUpperInvariant();
            Route = route.Trim();
        }

        public void Update(long systemApplicationId, string description, string controller, string action, string httpMethod, string route)
        {
            if (systemApplicationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(systemApplicationId));
            }

            ArgumentNullException.ThrowIfNull(description);
            ArgumentException.ThrowIfNullOrWhiteSpace(controller);
            ArgumentException.ThrowIfNullOrWhiteSpace(action);
            ArgumentException.ThrowIfNullOrWhiteSpace(httpMethod);
            ArgumentException.ThrowIfNullOrWhiteSpace(route);

            SystemApplicationId = systemApplicationId;
            Description = description.Trim();
            Controller = controller.Trim();
            Action = action.Trim();
            HttpMethod = httpMethod.Trim().ToUpperInvariant();
            Route = route.Trim();
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
}
