using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202605190006)]
    public sealed class Migration_202605190006_AddPaymentsAndWebhookDetails : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("payments").Exists())
            {
                Create.Table("payments")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("externalpaymentid").AsString(120).NotNullable()
                    .WithColumn("companyid").AsInt64().Nullable()
                    .WithColumn("subscriptionid").AsInt64().Nullable()
                    .WithColumn("externalsubscriptionid").AsString(200).Nullable()
                    .WithColumn("value").AsDecimal(14, 2).NotNullable()
                    .WithColumn("billingtype").AsString(60).Nullable()
                    .WithColumn("status").AsInt32().NotNullable()
                    .WithColumn("duedate").AsDateTimeOffset().Nullable()
                    .WithColumn("paiddate").AsDateTimeOffset().Nullable()
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();

                Create.Index("ux_payments_externalpaymentid")
                    .OnTable("payments")
                    .OnColumn("externalpaymentid").Ascending()
                    .WithOptions().Unique();

                Create.Index("ix_payments_companyid")
                    .OnTable("payments")
                    .OnColumn("companyid").Ascending();
            }

            if (Schema.Table("billingwebhookevents").Exists())
            {
                if (!Schema.Table("billingwebhookevents").Column("externalpaymentid").Exists())
                {
                    Alter.Table("billingwebhookevents")
                        .AddColumn("externalpaymentid").AsString(120).Nullable();
                }

                if (!Schema.Table("billingwebhookevents").Column("outcome").Exists())
                {
                    Alter.Table("billingwebhookevents")
                        .AddColumn("outcome").AsString(200).Nullable();
                }

                if (!Schema.Table("billingwebhookevents").Column("rawpayload").Exists())
                {
                    Alter.Table("billingwebhookevents")
                        .AddColumn("rawpayload").AsCustom("text").Nullable();
                }
            }
        }

        public override void Down()
        {
            if (Schema.Table("billingwebhookevents").Exists())
            {
                if (Schema.Table("billingwebhookevents").Column("rawpayload").Exists())
                {
                    Delete.Column("rawpayload").FromTable("billingwebhookevents");
                }

                if (Schema.Table("billingwebhookevents").Column("outcome").Exists())
                {
                    Delete.Column("outcome").FromTable("billingwebhookevents");
                }

                if (Schema.Table("billingwebhookevents").Column("externalpaymentid").Exists())
                {
                    Delete.Column("externalpaymentid").FromTable("billingwebhookevents");
                }
            }

            if (Schema.Table("payments").Exists())
            {
                if (Schema.Table("payments").Index("ix_payments_companyid").Exists())
                {
                    Delete.Index("ix_payments_companyid").OnTable("payments");
                }

                if (Schema.Table("payments").Index("ux_payments_externalpaymentid").Exists())
                {
                    Delete.Index("ux_payments_externalpaymentid").OnTable("payments");
                }

                Delete.Table("payments");
            }
        }
    }
}
