using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202605010002)]
    public sealed class Migration_202605010002_CreatePendingAuthorizationSessions : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("pendingauthorizationsessions").Exists())
            {
                Create.Table("pendingauthorizationsessions")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("userid").AsInt64().NotNullable()
                    .WithColumn("token").AsString(128).NotNullable()
                    .WithColumn("authorizerequesthash").AsString(128).Nullable()
                    .WithColumn("expiresat").AsDateTimeOffset().NotNullable()
                    .WithColumn("isused").AsBoolean().NotNullable().WithDefaultValue(false)
                    .WithColumn("usedat").AsDateTimeOffset().Nullable()
                    .WithColumn("isrevoked").AsBoolean().NotNullable().WithDefaultValue(false)
                    .WithColumn("revokedat").AsDateTimeOffset().Nullable()
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("pendingauthorizationsessions").Constraint("fk_pendingauthorizationsessions_users_userid").Exists())
            {
                Create.ForeignKey("fk_pendingauthorizationsessions_users_userid")
                    .FromTable("pendingauthorizationsessions").ForeignColumn("userid")
                    .ToTable("users").PrimaryColumn("id");
            }

            if (!Schema.Table("pendingauthorizationsessions").Index("ix_pendingauthorizationsessions_token").Exists())
            {
                Create.Index("ix_pendingauthorizationsessions_token")
                    .OnTable("pendingauthorizationsessions")
                    .OnColumn("token").Ascending()
                    .WithOptions().Unique();
            }
        }

        public override void Down()
        {
            if (!Schema.Table("pendingauthorizationsessions").Exists())
            {
                return;
            }

            if (Schema.Table("pendingauthorizationsessions").Index("ix_pendingauthorizationsessions_token").Exists())
            {
                Delete.Index("ix_pendingauthorizationsessions_token").OnTable("pendingauthorizationsessions");
            }

            if (Schema.Table("pendingauthorizationsessions").Constraint("fk_pendingauthorizationsessions_users_userid").Exists())
            {
                Delete.ForeignKey("fk_pendingauthorizationsessions_users_userid").OnTable("pendingauthorizationsessions");
            }

            Delete.Table("pendingauthorizationsessions");
        }
    }
}
