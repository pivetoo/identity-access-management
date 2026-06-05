using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("payments");

            builder.Property(entity => entity.ExternalPaymentId)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(entity => entity.ExternalSubscriptionId)
                .HasMaxLength(200);

            builder.Property(entity => entity.Value)
                .IsRequired()
                .HasColumnType("numeric(14,2)");

            builder.Property(entity => entity.BillingType)
                .HasMaxLength(60);

            builder.Property(entity => entity.Status)
                .IsRequired();

            // FKs deliberadamente ausentes: um pagamento pode chegar antes/sem assinatura conhecida
            // (orfao). Mantemos apenas indices para consulta; o vinculo e logico, nao referencial.
            builder.HasIndex(entity => entity.ExternalPaymentId)
                .IsUnique();

            builder.HasIndex(entity => entity.CompanyId);
        }
    }
}
