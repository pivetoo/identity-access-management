using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202605020002)]
    public sealed class Migration_202605020002_AddIsDefaultToOAuthClients : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("oauthclients").Column("isdefault").Exists())
            {
                Alter.Table("oauthclients")
                    .AddColumn("isdefault").AsBoolean().NotNullable().WithDefaultValue(false);
            }
        }

        public override void Down()
        {
            if (Schema.Table("oauthclients").Column("isdefault").Exists())
            {
                Delete.Column("isdefault").FromTable("oauthclients");
            }
        }
    }
}
