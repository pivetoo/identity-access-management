using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202604040003)]
    public sealed class Migration_202604040003_AddDescriptionToAccessResources : Migration
    {
        public override void Up()
        {
            Alter.Table("accessresources")
                .AddColumn("description").AsString(500).NotNullable().WithDefaultValue(string.Empty);
        }

        public override void Down()
        {
            Delete.Column("description")
                .FromTable("accessresources");
        }
    }
}
