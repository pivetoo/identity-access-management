using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202605170001)]
    public sealed class Migration_202605170001_AddCatalogApiKeyToSystemApplications : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE systemapplications ADD COLUMN IF NOT EXISTS catalogapikey VARCHAR(255);
            ");

            Execute.Sql(@"
                UPDATE systemapplications SET catalogapikey = gen_random_uuid()::text WHERE catalogapikey IS NULL OR catalogapikey = '';
            ");

            Execute.Sql(@"
                ALTER TABLE systemapplications ALTER COLUMN catalogapikey SET NOT NULL;
                DROP INDEX IF EXISTS ix_systemapplications_catalogapikey;
                CREATE UNIQUE INDEX ix_systemapplications_catalogapikey ON systemapplications (catalogapikey);
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                DROP INDEX IF EXISTS ix_systemapplications_catalogapikey;
                ALTER TABLE systemapplications DROP COLUMN IF EXISTS catalogapikey;
            ");
        }
    }
}
