using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.ToTable("userroles");

            builder.HasOne(entity => entity.User)
                .WithMany(entity => entity.UserRoles)
                .HasForeignKey(entity => entity.UserId);

            builder.HasOne(entity => entity.Role)
                .WithMany(entity => entity.UserRoles)
                .HasForeignKey(entity => entity.RoleId);

            builder.HasIndex(entity => new { entity.UserId, entity.RoleId })
                .IsUnique();
        }
    }
}
