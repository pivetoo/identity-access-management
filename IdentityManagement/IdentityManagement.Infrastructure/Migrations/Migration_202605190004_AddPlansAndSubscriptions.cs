using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202605190004)]
    public sealed class Migration_202605190004_AddPlansAndSubscriptions : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("plans").Exists())
            {
                Create.Table("plans")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("name").AsString(160).NotNullable()
                    .WithColumn("description").AsString(500).Nullable()
                    .WithColumn("priceamount").AsDecimal(14, 2).NotNullable()
                    .WithColumn("currency").AsString(3).NotNullable()
                    .WithColumn("billingperiod").AsInt32().NotNullable()
                    .WithColumn("trialdays").AsInt32().NotNullable()
                    .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("subscriptions").Exists())
            {
                Create.Table("subscriptions")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("companyid").AsInt64().NotNullable()
                    .WithColumn("planid").AsInt64().NotNullable()
                    .WithColumn("status").AsInt32().NotNullable()
                    .WithColumn("startedat").AsDateTimeOffset().NotNullable()
                    .WithColumn("trialendsat").AsDateTimeOffset().Nullable()
                    .WithColumn("currentperiodstart").AsDateTimeOffset().NotNullable()
                    .WithColumn("currentperiodend").AsDateTimeOffset().NotNullable()
                    .WithColumn("canceledat").AsDateTimeOffset().Nullable()
                    .WithColumn("externalcustomerid").AsString(200).Nullable()
                    .WithColumn("externalsubscriptionid").AsString(200).Nullable()
                    .WithColumn("providername").AsString(120).Nullable()
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();

                Create.ForeignKey("fk_subscriptions_companies")
                    .FromTable("subscriptions").ForeignColumn("companyid")
                    .ToTable("companies").PrimaryColumn("id");

                Create.ForeignKey("fk_subscriptions_plans")
                    .FromTable("subscriptions").ForeignColumn("planid")
                    .ToTable("plans").PrimaryColumn("id");

                Create.Index("ix_subscriptions_companyid")
                    .OnTable("subscriptions")
                    .OnColumn("companyid").Ascending();
            }
        }

        public override void Down()
        {
            if (Schema.Table("subscriptions").Exists())
            {
                if (Schema.Table("subscriptions").Index("ix_subscriptions_companyid").Exists())
                {
                    Delete.Index("ix_subscriptions_companyid").OnTable("subscriptions");
                }

                if (Schema.Table("subscriptions").Constraint("fk_subscriptions_plans").Exists())
                {
                    Delete.ForeignKey("fk_subscriptions_plans").OnTable("subscriptions");
                }

                if (Schema.Table("subscriptions").Constraint("fk_subscriptions_companies").Exists())
                {
                    Delete.ForeignKey("fk_subscriptions_companies").OnTable("subscriptions");
                }

                Delete.Table("subscriptions");
            }

            if (Schema.Table("plans").Exists())
            {
                Delete.Table("plans");
            }
        }
    }
}
