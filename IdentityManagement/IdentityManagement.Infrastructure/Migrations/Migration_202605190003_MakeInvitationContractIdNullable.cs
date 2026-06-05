using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202605190003)]
    public sealed class Migration_202605190003_MakeInvitationContractIdNullable : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE contractadmininvitations ALTER COLUMN contractid DROP NOT NULL;
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                ALTER TABLE contractadmininvitations ALTER COLUMN contractid SET NOT NULL;
            ");
        }
    }
}
