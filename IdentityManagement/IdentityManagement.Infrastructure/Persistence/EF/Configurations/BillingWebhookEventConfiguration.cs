using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class BillingWebhookEventConfiguration : IEntityTypeConfiguration<BillingWebhookEvent>
    {
        public void Configure(EntityTypeBuilder<BillingWebhookEvent> builder)
        {
            builder.ToTable("billingwebhookevents");

            builder.Property(entity => entity.ExternalEventId)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(entity => entity.EventType)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(entity => entity.ProcessedAt)
                .IsRequired();

            builder.HasIndex(entity => entity.ExternalEventId)
                .IsUnique();
        }
    }
}
