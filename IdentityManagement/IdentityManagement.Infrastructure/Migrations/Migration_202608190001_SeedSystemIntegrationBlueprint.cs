using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    // O blueprint de integracoes sempre foi dado digitado a mao no ambiente, e se perdeu na migracao
    // do VPS antigo para o novo: sem ele, o onboarding provisiona o tenant sem a fiacao entre
    // AgencyCampaign e IntegrationPlatform. Este seed torna a fiacao reproduzivel por ambiente,
    // derivando as URLs do proprio cadastro de systemapplications em vez de fixar dominio de producao.
    [Migration(202608190001)]
    public sealed class Migration_202608190001_SeedSystemIntegrationBlueprint : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("systemintegrations").Exists() || !Schema.Table("systemintegrationparameters").Exists())
            {
                return;
            }

            // Lado consumidor: o AgencyCampaign chama a IntegrationPlatform autenticando por X-Api-Key
            // do proprio tenant (valuesource 1 = TenantApiKey) e identifica-se pelo TenantId (valuesource 2).
            Execute.Sql("""
                INSERT INTO systemintegrations (systemapplicationid, name, baseurl, isactive, createdat, updatedat)
                SELECT consumer.id, 'integration-platform', provider.baseurl, true, now(), now()
                FROM systemapplications consumer
                JOIN systemapplications provider ON provider.audience = 'integration-platform'
                WHERE consumer.audience = 'agency-campaign'
                  AND coalesce(provider.baseurl, '') <> ''
                  AND NOT EXISTS (SELECT 1 FROM systemintegrations si
                                  WHERE si.systemapplicationid = consumer.id AND si.name = 'integration-platform');
                """);

            Execute.Sql("""
                INSERT INTO systemintegrationparameters (systemintegrationid, "key", "value", issecret, valuesource, sourceaudience, createdat, updatedat)
                SELECT si.id, p."key", p."value", p.issecret, p.valuesource, p.sourceaudience, now(), now()
                FROM systemintegrations si
                JOIN systemapplications sa ON sa.id = si.systemapplicationid
                CROSS JOIN (VALUES
                    ('TenantId',       NULL::text, false, 2, NULL::varchar),
                    ('ApiKey',         NULL::text, true,  1, 'integration-platform'),
                    ('CallbackSecret', NULL::text, true,  3, NULL::varchar)
                ) AS p("key", "value", issecret, valuesource, sourceaudience)
                WHERE sa.audience = 'agency-campaign' AND si.name = 'integration-platform'
                  AND NOT EXISTS (SELECT 1 FROM systemintegrationparameters x
                                  WHERE x.systemintegrationid = si.id AND x."key" = p."key");
                """);

            // Lado provedor: a IntegrationPlatform precisa saber para onde devolver o callback e com qual
            // segredo assina-lo. CallbackSecret repete a Key do lado consumidor de proposito — o onboarding
            // compartilha GeneratedSecret por Key, entao os dois lados recebem o mesmo valor.
            Execute.Sql("""
                INSERT INTO systemintegrations (systemapplicationid, name, baseurl, isactive, createdat, updatedat)
                SELECT consumer.id, 'agency-campaign', provider.baseurl, true, now(), now()
                FROM systemapplications consumer
                JOIN systemapplications provider ON provider.audience = 'agency-campaign'
                WHERE consumer.audience = 'integration-platform'
                  AND coalesce(provider.baseurl, '') <> ''
                  AND NOT EXISTS (SELECT 1 FROM systemintegrations si
                                  WHERE si.systemapplicationid = consumer.id AND si.name = 'agency-campaign');
                """);

            Execute.Sql("""
                INSERT INTO systemintegrationparameters (systemintegrationid, "key", "value", issecret, valuesource, sourceaudience, createdat, updatedat)
                SELECT si.id, p."key", p."value", p.issecret, p.valuesource, p.sourceaudience, now(), now()
                FROM systemintegrations si
                JOIN systemapplications sa ON sa.id = si.systemapplicationid
                CROSS JOIN LATERAL (VALUES
                    ('CallbackSecret',  NULL::text, true,  3, NULL::varchar),
                    ('callbackBaseUrl', si.baseurl, false, 0, NULL::varchar)
                ) AS p("key", "value", issecret, valuesource, sourceaudience)
                WHERE sa.audience = 'integration-platform' AND si.name = 'agency-campaign'
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
                  AND ((sa.audience = 'agency-campaign' AND si.name = 'integration-platform' AND p."key" IN ('TenantId', 'ApiKey', 'CallbackSecret'))
                    OR (sa.audience = 'integration-platform' AND si.name = 'agency-campaign' AND p."key" IN ('CallbackSecret', 'callbackBaseUrl')));
                """);

            Execute.Sql("""
                DELETE FROM systemintegrations si
                USING systemapplications sa
                WHERE sa.id = si.systemapplicationid
                  AND ((sa.audience = 'agency-campaign' AND si.name = 'integration-platform')
                    OR (sa.audience = 'integration-platform' AND si.name = 'agency-campaign'));
                """);
        }
    }
}
