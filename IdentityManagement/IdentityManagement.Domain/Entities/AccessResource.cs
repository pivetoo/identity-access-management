using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    public class AccessResource : Entity
    {
        private readonly List<RoleAccessResource> roleAccessResources = [];

        public string Name { get; private set; } = string.Empty;

        public string Controller { get; private set; } = string.Empty;

        public string Action { get; private set; } = string.Empty;

        public string HttpMethod { get; private set; } = string.Empty;

        public string Route { get; private set; } = string.Empty;

        public bool IsActive { get; private set; } = true;

        public IReadOnlyCollection<RoleAccessResource> RoleAccessResources => roleAccessResources.AsReadOnly();

        private AccessResource()
        {
        }

        public AccessResource(string name, string controller, string action, string httpMethod, string route)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(controller);
            ArgumentException.ThrowIfNullOrWhiteSpace(action);
            ArgumentException.ThrowIfNullOrWhiteSpace(httpMethod);
            ArgumentException.ThrowIfNullOrWhiteSpace(route);

            Name = name.Trim();
            Controller = controller.Trim();
            Action = action.Trim();
            HttpMethod = httpMethod.Trim().ToUpperInvariant();
            Route = route.Trim();
        }

        public void Update(string controller, string action, string httpMethod, string route)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(controller);
            ArgumentException.ThrowIfNullOrWhiteSpace(action);
            ArgumentException.ThrowIfNullOrWhiteSpace(httpMethod);
            ArgumentException.ThrowIfNullOrWhiteSpace(route);

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
