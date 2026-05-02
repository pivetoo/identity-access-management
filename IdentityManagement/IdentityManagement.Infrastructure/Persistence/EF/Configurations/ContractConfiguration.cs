using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class ContractConfiguration : IEntityTypeConfiguration<Contract>
    {
        public void Configure(EntityTypeBuilder<Contract> builder)
        {
            builder.ToTable("contracts");

            builder.HasOne(entity => entity.Company)
                .WithMany(entity => entity.Contracts)
                .HasForeignKey(entity => entity.CompanyId);

            builder.HasOne(entity => entity.SystemApplication)
                .WithMany(entity => entity.Contracts)
                .HasForeignKey(entity => entity.SystemApplicationId);

            builder.Property(entity => entity.TenantId)
                .HasColumnName("tenantid")
                .IsRequired();

            builder.HasIndex(entity => new { entity.CompanyId, entity.SystemApplicationId })
                .IsUnique();

            builder.HasIndex(entity => entity.TenantId)
                .IsUnique();
        }
    }
}
