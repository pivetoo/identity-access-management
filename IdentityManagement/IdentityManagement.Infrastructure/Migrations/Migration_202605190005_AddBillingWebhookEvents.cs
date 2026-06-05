using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202605190005)]
    public sealed class Migration_202605190005_AddBillingWebhookEvents : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("billingwebhookevents").Exists())
            {
                Create.Table("billingwebhookevents")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("externaleventid").AsString(200).NotNullable()
                    .WithColumn("eventtype").AsString(120).NotNullable()
                    .WithColumn("processedat").AsDateTimeOffset().NotNullable()
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();

                Create.Index("ux_billingwebhookevents_externaleventid")
                    .OnTable("billingwebhookevents")
                    .OnColumn("externaleventid").Ascending()
                    .WithOptions().Unique();
            }
        }

        public override void Down()
        {
            if (Schema.Table("billingwebhookevents").Exists())
            {
                if (Schema.Table("billingwebhookevents").Index("ux_billingwebhookevents_externaleventid").Exists())
                {
                    Delete.Index("ux_billingwebhookevents_externaleventid").OnTable("billingwebhookevents");
                }

                Delete.Table("billingwebhookevents");
            }
        }
    }
}
