using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
    {
        public void Configure(EntityTypeBuilder<Plan> builder)
        {
            builder.ToTable("plans");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(160);

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.Property(entity => entity.PriceAmount)
                .IsRequired()
                .HasColumnType("numeric(14,2)");

            builder.Property(entity => entity.LaunchPriceAmount)
                .HasColumnType("numeric(14,2)");

            builder.Property(entity => entity.Currency)
                .IsRequired()
                .HasMaxLength(3);

            builder.Property(entity => entity.BillingPeriod)
                .IsRequired();

            builder.Property(entity => entity.TrialDays)
                .IsRequired();

            builder.Property(entity => entity.IsActive)
                .IsRequired();
        }
    }
}
