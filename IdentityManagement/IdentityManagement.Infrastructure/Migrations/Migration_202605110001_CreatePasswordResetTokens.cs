using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202605110001)]
    public sealed class Migration_202605110001_CreatePasswordResetTokens : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("passwordresettokens").Exists())
            {
                Create.Table("passwordresettokens")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("userid").AsInt64().NotNullable()
                    .WithColumn("token").AsString(64).NotNullable()
                    .WithColumn("expiresat").AsDateTimeOffset().NotNullable()
                    .WithColumn("usedat").AsDateTimeOffset().Nullable()
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();

                Create.ForeignKey()
                    .FromTable("passwordresettokens").ForeignColumn("userid")
                    .ToTable("users").PrimaryColumn("id");

                Create.Index()
                    .OnTable("passwordresettokens")
                    .OnColumn("token")
                    .Unique();
            }
        }

        public override void Down()
        {
            if (Schema.Table("passwordresettokens").Exists())
            {
                Delete.Table("passwordresettokens");
            }
        }
    }
}
