using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202607260002)]
    public sealed class Migration_202607260002_AddUserLockout : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("users").Exists())
            {
                return;
            }

            if (!Schema.Table("users").Column("failedloginattempts").Exists())
            {
                Alter.Table("users")
                    .AddColumn("failedloginattempts").AsInt32().NotNullable().WithDefaultValue(0);
            }

            if (!Schema.Table("users").Column("lockeduntil").Exists())
            {
                Alter.Table("users")
                    .AddColumn("lockeduntil").AsDateTimeOffset().Nullable();
            }
        }

        public override void Down()
        {
            if (!Schema.Table("users").Exists())
            {
                return;
            }

            if (Schema.Table("users").Column("lockeduntil").Exists())
            {
                Delete.Column("lockeduntil").FromTable("users");
            }

            if (Schema.Table("users").Column("failedloginattempts").Exists())
            {
                Delete.Column("failedloginattempts").FromTable("users");
            }
        }
    }
}
