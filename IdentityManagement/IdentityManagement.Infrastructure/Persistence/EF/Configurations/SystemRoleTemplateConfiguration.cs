using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class SystemRoleTemplateConfiguration : IEntityTypeConfiguration<SystemRoleTemplate>
    {
        public void Configure(EntityTypeBuilder<SystemRoleTemplate> builder)
        {
            builder.ToTable("systemroletemplates");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(entity => entity.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.HasOne(entity => entity.SystemApplication)
                .WithMany()
                .HasForeignKey(entity => entity.SystemApplicationId);

            builder.HasIndex(entity => new { entity.SystemApplicationId, entity.Name })
                .IsUnique();
        }
    }
}
