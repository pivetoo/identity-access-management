using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202605190002)]
    public sealed class Migration_202605190002_AddInvitationCompanyId : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE contractadmininvitations ADD COLUMN IF NOT EXISTS companyid BIGINT NULL;
                CREATE INDEX IF NOT EXISTS ix_contractadmininvitations_companyid ON contractadmininvitations (companyid);
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                DROP INDEX IF EXISTS ix_contractadmininvitations_companyid;
                ALTER TABLE contractadmininvitations DROP COLUMN IF EXISTS companyid;
            ");
        }
    }
}
