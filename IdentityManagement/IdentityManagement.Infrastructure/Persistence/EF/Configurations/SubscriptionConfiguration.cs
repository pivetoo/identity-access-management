using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
    {
        public void Configure(EntityTypeBuilder<Subscription> builder)
        {
            builder.ToTable("subscriptions");

            builder.HasOne(entity => entity.Company)
                .WithMany()
                .HasForeignKey(entity => entity.CompanyId);

            builder.HasOne(entity => entity.Plan)
                .WithMany()
                .HasForeignKey(entity => entity.PlanId);

            builder.Property(entity => entity.Status)
                .IsRequired();

            builder.Property(entity => entity.StartedAt)
                .IsRequired();

            builder.Property(entity => entity.CurrentPeriodStart)
                .IsRequired();

            builder.Property(entity => entity.CurrentPeriodEnd)
                .IsRequired();

            builder.Property(entity => entity.ExternalCustomerId)
                .HasMaxLength(200);

            builder.Property(entity => entity.ExternalSubscriptionId)
                .HasMaxLength(200);

            builder.Property(entity => entity.ProviderName)
                .HasMaxLength(120);

            builder.HasIndex(entity => entity.CompanyId);
        }
    }
}
