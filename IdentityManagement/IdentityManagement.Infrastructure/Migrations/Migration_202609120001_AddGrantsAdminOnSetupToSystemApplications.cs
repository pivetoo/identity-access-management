using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    /// <summary>
    /// Marca quais sistemas entram no convite de administrador do tenant.
    ///
    /// O IntegrationPlatform sai: ele e o motor de integracoes do Mainstay, provisionado junto com a
    /// agencia porque o AgencyCampaign precisa da API key do tenant (ValueSource TenantApiKey) para
    /// as integracoes nascerem configuradas. Acesso humano a ele e outra coisa — quem precisar recebe
    /// de um administrador, pelo vinculo de perfil no contrato.
    ///
    /// Sem efeito retroativo: os administradores ja provisionados mantem o vinculo que tem.
    /// </summary>
    [Migration(202609120001)]
    public sealed class Migration_202609120001_AddGrantsAdminOnSetupToSystemApplications : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("systemapplications").Column("grantsadminonsetup").Exists())
            {
                Alter.Table("systemapplications")
                    .AddColumn("grantsadminonsetup").AsBoolean().NotNullable().WithDefaultValue(true);
            }

            Update.Table("systemapplications")
                .Set(new { grantsadminonsetup = false })
                .Where(new { audience = "integration-platform" });
        }

        public override void Down()
        {
            if (Schema.Table("systemapplications").Column("grantsadminonsetup").Exists())
            {
                Delete.Column("grantsadminonsetup").FromTable("systemapplications");
            }
        }
    }
}
