using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class TenantDatabaseConfiguration : IEntityTypeConfiguration<TenantDatabase>
    {
        public void Configure(EntityTypeBuilder<TenantDatabase> builder)
        {
            builder.ToTable("tenantdatabases");

            builder.HasOne(entity => entity.Contract)
                .WithOne()
                .HasForeignKey<TenantDatabase>(entity => entity.ContractId);

            builder.Property(entity => entity.ConnectionString)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(entity => entity.DatabaseProvider)
                .IsRequired();

            builder.Property(entity => entity.SchemaName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(entity => entity.ApiKey)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("apikey");

            builder.HasIndex(entity => entity.ContractId)
                .IsUnique();

            builder.HasIndex(entity => entity.ApiKey)
                .IsUnique();

            builder.HasIndex(entity => entity.IsActive);
        }
    }
}
