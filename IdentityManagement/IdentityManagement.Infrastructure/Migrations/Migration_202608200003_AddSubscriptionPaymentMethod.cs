using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    // Forma de pagamento vigente da assinatura (PIX ou CREDIT_CARD). As assinaturas que ja existem
    // foram todas criadas em PIX, que era a unica forma suportada ate agora — dai o backfill.
    [Migration(202608200003)]
    public sealed class Migration_202608200003_AddSubscriptionPaymentMethod : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("subscriptions").Column("paymentmethod").Exists())
            {
                Alter.Table("subscriptions")
                    .AddColumn("paymentmethod").AsString(20).Nullable();

                Execute.Sql("UPDATE subscriptions SET paymentmethod = 'PIX' WHERE externalsubscriptionid IS NOT NULL AND paymentmethod IS NULL;");
            }
        }

        public override void Down()
        {
            if (Schema.Table("subscriptions").Column("paymentmethod").Exists())
            {
                Delete.Column("paymentmethod").FromTable("subscriptions");
            }
        }
    }
}
