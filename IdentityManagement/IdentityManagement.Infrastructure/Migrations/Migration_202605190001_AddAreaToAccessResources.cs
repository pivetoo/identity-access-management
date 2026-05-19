using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202605190001)]
    public sealed class Migration_202605190001_AddAreaToAccessResources : Migration
    {
        public override void Up()
        {
            Execute.Sql("ALTER TABLE accessresources ADD COLUMN IF NOT EXISTS area VARCHAR(255) NOT NULL DEFAULT '';");
        }

        public override void Down()
        {
            Execute.Sql("ALTER TABLE accessresources DROP COLUMN IF EXISTS area;");
        }
    }
}
