using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("roles");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.HasOne(entity => entity.Contract)
                .WithMany(entity => entity.Roles)
                .HasForeignKey(entity => entity.ContractId);

            builder.HasIndex(entity => new { entity.ContractId, entity.Name })
                .IsUnique();
        }
    }
}
