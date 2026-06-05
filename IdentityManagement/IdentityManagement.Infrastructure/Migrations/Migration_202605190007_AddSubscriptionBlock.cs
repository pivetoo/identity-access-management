using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202605190007)]
    public sealed class Migration_202605190007_AddSubscriptionBlock : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("subscriptions").Exists())
            {
                return;
            }

            if (!Schema.Table("subscriptions").Column("isblocked").Exists())
            {
                Alter.Table("subscriptions")
                    .AddColumn("isblocked").AsBoolean().NotNullable().WithDefaultValue(false);
            }

            if (!Schema.Table("subscriptions").Column("blockreason").Exists())
            {
                Alter.Table("subscriptions")
                    .AddColumn("blockreason").AsInt32().NotNullable().WithDefaultValue(0);
            }

            if (!Schema.Table("subscriptions").Column("blockedat").Exists())
            {
                Alter.Table("subscriptions")
                    .AddColumn("blockedat").AsDateTimeOffset().Nullable();
            }
        }

        public override void Down()
        {
            if (!Schema.Table("subscriptions").Exists())
            {
                return;
            }

            if (Schema.Table("subscriptions").Column("blockedat").Exists())
            {
                Delete.Column("blockedat").FromTable("subscriptions");
            }

            if (Schema.Table("subscriptions").Column("blockreason").Exists())
            {
                Delete.Column("blockreason").FromTable("subscriptions");
            }

            if (Schema.Table("subscriptions").Column("isblocked").Exists())
            {
                Delete.Column("isblocked").FromTable("subscriptions");
            }
        }
    }
}
