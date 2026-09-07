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

        public string Area { get; private set; } = string.Empty;

        public string Controller { get; private set; } = string.Empty;

        public string Action { get; private set; } = string.Empty;

        public string HttpMethod { get; private set; } = string.Empty;

        public string Route { get; private set; } = string.Empty;

        // Chaves de capacidade separadas por ";" (ex.: "financeiro.ver;producao.ver").
        public string Capabilities { get; private set; } = string.Empty;

        public bool IsActive { get; private set; } = true;

        public IReadOnlyCollection<RoleAccessResource> RoleAccessResources => roleAccessResources.AsReadOnly();

        private AccessResource()
        {
        }

        public AccessResource(long systemApplicationId, string name, string description, string area, string controller, string action, string httpMethod, string route)
        {
            if (systemApplicationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(systemApplicationId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(description);
            ArgumentNullException.ThrowIfNull(area);
            ArgumentException.ThrowIfNullOrWhiteSpace(controller);
            ArgumentException.ThrowIfNullOrWhiteSpace(action);
            ArgumentException.ThrowIfNullOrWhiteSpace(httpMethod);
            ArgumentException.ThrowIfNullOrWhiteSpace(route);

            SystemApplicationId = systemApplicationId;
            Name = name.Trim();
            Description = description.Trim();
            Area = area.Trim();
            Controller = controller.Trim();
            Action = action.Trim();
            HttpMethod = httpMethod.Trim().ToUpperInvariant();
            Route = route.Trim();
        }

        public void Update(long systemApplicationId, string description, string area, string controller, string action, string httpMethod, string route)
        {
            if (systemApplicationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(systemApplicationId));
            }

            ArgumentNullException.ThrowIfNull(description);
            ArgumentNullException.ThrowIfNull(area);
            ArgumentException.ThrowIfNullOrWhiteSpace(controller);
            ArgumentException.ThrowIfNullOrWhiteSpace(action);
            ArgumentException.ThrowIfNullOrWhiteSpace(httpMethod);
            ArgumentException.ThrowIfNullOrWhiteSpace(route);

            SystemApplicationId = systemApplicationId;
            Description = description.Trim();
            Area = area.Trim();
            Controller = controller.Trim();
            Action = action.Trim();
            HttpMethod = httpMethod.Trim().ToUpperInvariant();
            Route = route.Trim();
        }

        public const char CapabilitySeparator = ';';

        public static string JoinCapabilities(IEnumerable<string>? capabilities)
        {
            if (capabilities is null)
            {
                return string.Empty;
            }

            return string.Join(CapabilitySeparator, capabilities
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase));
        }

        public static IReadOnlyList<string> SplitCapabilities(string? capabilities)
        {
            if (string.IsNullOrWhiteSpace(capabilities))
            {
                return [];
            }

            return capabilities.Split(CapabilitySeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        public void SetCapabilities(IEnumerable<string>? capabilities)
        {
            Capabilities = JoinCapabilities(capabilities);
        }

        public IReadOnlyList<string> GetCapabilities()
        {
            return SplitCapabilities(Capabilities);
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
