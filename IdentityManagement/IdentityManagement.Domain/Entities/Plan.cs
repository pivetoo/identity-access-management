using Archon.Core.Entities;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Domain.Entities
{
    public class Plan : Entity
    {
        public string Name { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public decimal PriceAmount { get; private set; }

        public string Currency { get; private set; } = "BRL";

        public BillingPeriod BillingPeriod { get; private set; }

        public int TrialDays { get; private set; }

        public bool IsActive { get; private set; } = true;

        private Plan()
        {
        }

        public Plan(
            string name,
            decimal priceAmount,
            BillingPeriod billingPeriod,
            string currency = "BRL",
            int trialDays = 0,
            string? description = null)
        {
            ValidateName(name);
            ValidatePriceAmount(priceAmount);
            ValidateTrialDays(trialDays);
            ValidateCurrency(currency);

            Name = name.Trim();
            PriceAmount = priceAmount;
            BillingPeriod = billingPeriod;
            Currency = currency.Trim();
            TrialDays = trialDays;
            Description = description?.Trim();
        }

        public void Update(
            string name,
            decimal priceAmount,
            BillingPeriod billingPeriod,
            string currency,
            int trialDays,
            string? description)
        {
            ValidateName(name);
            ValidatePriceAmount(priceAmount);
            ValidateTrialDays(trialDays);
            ValidateCurrency(currency);

            Name = name.Trim();
            PriceAmount = priceAmount;
            BillingPeriod = billingPeriod;
            Currency = currency.Trim();
            TrialDays = trialDays;
            Description = description?.Trim();
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name cannot be blank.", nameof(name));
            }
        }

        private static void ValidatePriceAmount(decimal priceAmount)
        {
            if (priceAmount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(priceAmount), "Price amount must be zero or greater.");
            }
        }

        private static void ValidateTrialDays(int trialDays)
        {
            if (trialDays < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(trialDays), "Trial days must be zero or greater.");
            }
        }

        private static void ValidateCurrency(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
            {
                throw new ArgumentException("Currency cannot be blank.", nameof(currency));
            }
        }
    }
}
