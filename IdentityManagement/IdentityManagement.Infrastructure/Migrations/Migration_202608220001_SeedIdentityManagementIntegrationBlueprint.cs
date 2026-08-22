using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    // O blueprint de 2026-08-19 semeou so a fiacao AgencyCampaign <-> IntegrationPlatform. Faltou a
    // integracao 'identity-management' do lado do AgencyCampaign, que o Archon usa para listar usuarios,
    // perfis e recursos de acesso do contrato (tela Controle de Acesso) e para o access sync. Tenant
    // provisionado sem ela responde 500 em /api/UsersManagement/*.
    //
    // A chave e a do proprio tenant no AgencyCampaign (valuesource 1 = TenantApiKey, sourceaudience
    // 'agency-campaign'): o IdM valida X-Api-Key contra tenantdatabases do contrato. Nao existe contrato
    // com o sistema central, entao nao ha "api key do identity-management" a resolver.
    [Migration(202608220001)]
    public sealed class Migration_202608220001_SeedIdentityManagementIntegrationBlueprint : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("systemintegrations").Exists() || !Schema.Table("systemintegrationparameters").Exists())
            {
                return;
            }

            Execute.Sql("""
                INSERT INTO systemintegrations (systemapplicationid, name, baseurl, isactive, createdat, updatedat)
                SELECT consumer.id, 'identity-management', provider.baseurl, true, now(), now()
                FROM systemapplications consumer
                JOIN systemapplications provider ON provider.audience = 'identity-management'
                WHERE consumer.audience = 'agency-campaign'
                  AND coalesce(provider.baseurl, '') <> ''
                  AND NOT EXISTS (SELECT 1 FROM systemintegrations si
                                  WHERE si.systemapplicationid = consumer.id AND si.name = 'identity-management');
                """);

            Execute.Sql("""
                INSERT INTO systemintegrationparameters (systemintegrationid, "key", "value", issecret, valuesource, sourceaudience, createdat, updatedat)
                SELECT si.id, p."key", p."value", p.issecret, p.valuesource, p.sourceaudience, now(), now()
                FROM systemintegrations si
                JOIN systemapplications sa ON sa.id = si.systemapplicationid
                CROSS JOIN (VALUES
                    ('TenantId', NULL::text, false, 2, NULL::varchar),
                    ('ApiKey',   NULL::text, true,  1, 'agency-campaign')
                ) AS p("key", "value", issecret, valuesource, sourceaudience)
                WHERE sa.audience = 'agency-campaign' AND si.name = 'identity-management'
                  AND NOT EXISTS (SELECT 1 FROM systemintegrationparameters x
                                  WHERE x.systemintegrationid = si.id AND x."key" = p."key");
                """);
        }

        public override void Down()
        {
            if (!Schema.Table("systemintegrations").Exists() || !Schema.Table("systemintegrationparameters").Exists())
            {
                return;
            }

            Execute.Sql("""
                DELETE FROM systemintegrationparameters p
                USING systemintegrations si, systemapplications sa
                WHERE p.systemintegrationid = si.id
                  AND sa.id = si.systemapplicationid
                  AND sa.audience = 'agency-campaign' AND si.name = 'identity-management'
                  AND p."key" IN ('TenantId', 'ApiKey');
                """);

            Execute.Sql("""
                DELETE FROM systemintegrations si
                USING systemapplications sa
                WHERE sa.id = si.systemapplicationid
                  AND sa.audience = 'agency-campaign' AND si.name = 'identity-management';
                """);
        }
    }
}
