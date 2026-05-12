using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202605110002)]
    public sealed class Migration_202605110002_CreateContractAdminInvitations : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("contractadmininvitations").Exists())
            {
                Create.Table("contractadmininvitations")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("contractid").AsInt64().NotNullable()
                    .WithColumn("token").AsString(64).NotNullable()
                    .WithColumn("expiresat").AsDateTimeOffset().NotNullable()
                    .WithColumn("usedat").AsDateTimeOffset().Nullable()
                    .WithColumn("revokedat").AsDateTimeOffset().Nullable()
                    .WithColumn("userid").AsInt64().Nullable()
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();

                Create.ForeignKey()
                    .FromTable("contractadmininvitations").ForeignColumn("contractid")
                    .ToTable("contracts").PrimaryColumn("id");

                Create.ForeignKey()
                    .FromTable("contractadmininvitations").ForeignColumn("userid")
                    .ToTable("users").PrimaryColumn("id");

                Create.Index()
                    .OnTable("contractadmininvitations")
                    .OnColumn("token")
                    .Unique();
            }
        }

        public override void Down()
        {
            if (Schema.Table("contractadmininvitations").Exists())
            {
                Delete.Table("contractadmininvitations");
            }
        }
    }
}
