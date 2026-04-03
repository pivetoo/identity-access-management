using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    public class Company : Entity
    {
        private readonly List<Contract> contracts = [];

        public string LegalName { get; private set; } = string.Empty;

        public string TradeName { get; private set; } = string.Empty;

        public string Document { get; private set; } = string.Empty;

        public string Email { get; private set; } = string.Empty;

        public string PhoneNumber { get; private set; } = string.Empty;

        public bool IsActive { get; private set; } = true;

        public IReadOnlyCollection<Contract> Contracts => contracts.AsReadOnly();

        private Company()
        {
        }

        public Company(string legalName, string tradeName, string document, string email, string phoneNumber)
        {
            SetNames(legalName, tradeName);
            Document = document.Trim();
            Email = email.Trim();
            PhoneNumber = phoneNumber.Trim();
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        private void SetNames(string legalName, string tradeName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(legalName);
            ArgumentException.ThrowIfNullOrWhiteSpace(tradeName);

            LegalName = legalName.Trim();
            TradeName = tradeName.Trim();
        }
    }
}
