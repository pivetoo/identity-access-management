using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityManagement.Infrastructure.Persistence.EF.Configurations
{
    public sealed class PendingSignupConfiguration : IEntityTypeConfiguration<PendingSignup>
    {
        public void Configure(EntityTypeBuilder<PendingSignup> builder)
        {
            builder.ToTable("pendingsignups");

            builder.Property(entity => entity.LegalName).IsRequired().HasMaxLength(200);
            builder.Property(entity => entity.TradeName).IsRequired().HasMaxLength(200);
            builder.Property(entity => entity.Document).IsRequired().HasMaxLength(14);
            builder.Property(entity => entity.Email).IsRequired().HasMaxLength(200);
            builder.Property(entity => entity.PhoneNumber).HasMaxLength(20);
            builder.Property(entity => entity.Token).IsRequired().HasMaxLength(64);
            builder.Property(entity => entity.SourceIp).HasMaxLength(64);

            // A confirmacao busca EXCLUSIVAMENTE por hash de token: sem indice unico aqui a tabela
            // vira varredura sequencial no caminho quente do cadastro.
            builder.HasIndex(entity => entity.Token).IsUnique();
            builder.HasIndex(entity => entity.ExpiresAt);
        }
    }
}
