using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    public class SystemIntegration : Entity
    {
        private readonly List<SystemIntegrationParameter> parameters = [];

        public long SystemApplicationId { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string BaseUrl { get; private set; } = string.Empty;

        public bool IsActive { get; private set; } = true;

        public IReadOnlyCollection<SystemIntegrationParameter> Parameters => parameters.AsReadOnly();

        private SystemIntegration()
        {
        }

        public SystemIntegration(long systemApplicationId, string name, string baseUrl)
        {
            if (systemApplicationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(systemApplicationId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

            SystemApplicationId = systemApplicationId;
            Name = name.Trim();
            BaseUrl = baseUrl.Trim();
        }

        public void Update(string name, string baseUrl)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

            Name = name.Trim();
            BaseUrl = baseUrl.Trim();
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void AddParameter(SystemIntegrationParameter parameter)
        {
            ArgumentNullException.ThrowIfNull(parameter);
            parameters.Add(parameter);
        }

        public void ReplaceParameters(IEnumerable<SystemIntegrationParameter> newParameters)
        {
            ArgumentNullException.ThrowIfNull(newParameters);
            parameters.Clear();
            parameters.AddRange(newParameters);
        }
    }
}
