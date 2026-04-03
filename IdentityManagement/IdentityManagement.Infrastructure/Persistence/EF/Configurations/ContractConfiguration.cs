using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class ContractConfiguration : IEntityTypeConfiguration<Contract>
    {
        public void Configure(EntityTypeBuilder<Contract> builder)
        {
            builder.ToTable("Contracts");

            builder.Property(entity => entity.ClientId)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.ClientSecret)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(entity => entity.JwtSecretKey)
                .IsRequired()
                .HasMaxLength(255);

            builder.HasOne(entity => entity.Company)
                .WithMany(entity => entity.Contracts)
                .HasForeignKey(entity => entity.CompanyId);

            builder.HasOne(entity => entity.SystemApplication)
                .WithMany(entity => entity.Contracts)
                .HasForeignKey(entity => entity.SystemApplicationId);

            builder.HasIndex(entity => entity.ClientId)
                .IsUnique();

            builder.HasIndex(entity => new { entity.CompanyId, entity.SystemApplicationId })
                .IsUnique();
        }
    }
}
