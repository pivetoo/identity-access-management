using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202604040003)]
    public sealed class Migration_202604040003_AddDescriptionToAccessResources : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("accessresources").Column("description").Exists())
            {
                Alter.Table("accessresources")
                    .AddColumn("description").AsString(500).NotNullable().WithDefaultValue(string.Empty);
            }
        }

        public override void Down()
        {
            if (Schema.Table("accessresources").Column("description").Exists())
            {
                Delete.Column("description")
                    .FromTable("accessresources");
            }
        }
    }
}
