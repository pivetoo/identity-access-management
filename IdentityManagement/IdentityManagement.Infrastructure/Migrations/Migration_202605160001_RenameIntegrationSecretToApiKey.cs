using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202605160001)]
    public sealed class Migration_202605160001_RenameIntegrationSecretToApiKey : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'tenantdatabases' AND column_name = 'integrationsecret'
                    ) THEN
                        ALTER TABLE tenantdatabases RENAME COLUMN integrationsecret TO apikey;
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM pg_indexes
                        WHERE tablename = 'tenantdatabases' AND indexname = 'ix_tenantdatabases_integrationsecret'
                    ) THEN
                        ALTER INDEX ix_tenantdatabases_integrationsecret RENAME TO ix_tenantdatabases_apikey;
                    END IF;
                END $$;
            ");
        }

        public override void Down()
        {
        }
    }
}
