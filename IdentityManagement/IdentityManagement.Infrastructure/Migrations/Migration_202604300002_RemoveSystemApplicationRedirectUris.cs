using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202604300002)]
    public sealed class Migration_202604300002_RemoveSystemApplicationRedirectUris : Migration
    {
        public override void Up()
        {
            if (Schema.Table("systemapplications").Column("redirecturis").Exists())
            {
                Delete.Column("redirecturis").FromTable("systemapplications");
            }
        }

        public override void Down()
        {
            if (!Schema.Table("systemapplications").Column("redirecturis").Exists())
            {
                Alter.Table("systemapplications")
                    .AddColumn("redirecturis").AsString(2000).NotNullable().WithDefaultValue(string.Empty);
            }
        }
    }
}
