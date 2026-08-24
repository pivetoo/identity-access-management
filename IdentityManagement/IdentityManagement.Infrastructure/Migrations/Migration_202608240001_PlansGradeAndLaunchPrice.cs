using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    // Grade de lancamento: Essencial (997, condicao Fundador 497 ate 31/12/2026) e Exclusivo (1997).
    // A assinatura passa a guardar o preco CONTRATADO (priceamount): e o que o gateway cobra e o que
    // preserva o preco de Fundador de quem entrou na janela.
    [Migration(202608240001)]
    public sealed class Migration_202608240001_PlansGradeAndLaunchPrice : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("plans").Column("launchpriceamount").Exists())
            {
                Alter.Table("plans").AddColumn("launchpriceamount").AsDecimal(14, 2).Nullable();
                Alter.Table("plans").AddColumn("launchpriceuntil").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("subscriptions").Column("priceamount").Exists())
            {
                Alter.Table("subscriptions").AddColumn("priceamount").AsDecimal(14, 2).NotNullable().WithDefaultValue(0);
                // Backfill ANTES de mudar a tabela dos planos: o preco contratado das assinaturas
                // existentes e o preco vigente ate aqui.
                Execute.Sql("UPDATE subscriptions s SET priceamount = p.priceamount FROM plans p WHERE p.id = s.planid AND s.priceamount = 0;");
            }

            // 31/12/2026 23:59:59 em Brasilia.
            Execute.Sql(@"
                UPDATE plans SET name = 'Essencial Mensal', priceamount = 997.00,
                       launchpriceamount = 497.00, launchpriceuntil = '2027-01-01T02:59:59Z'
                WHERE name = 'Completo Mensal';
                UPDATE plans SET name = 'Essencial Anual', priceamount = 9970.00,
                       launchpriceamount = 4970.00, launchpriceuntil = '2027-01-01T02:59:59Z'
                WHERE name = 'Completo Anual';
                INSERT INTO plans (name, description, priceamount, currency, billingperiod, trialdays, isactive, createdat)
                SELECT 'Exclusivo Mensal', 'Tudo do Essencial, sem limites, com implantação assistida e canal direto.', 1997.00, 'BRL', 1, 14, true, now()
                WHERE NOT EXISTS (SELECT 1 FROM plans WHERE name = 'Exclusivo Mensal');
                INSERT INTO plans (name, description, priceamount, currency, billingperiod, trialdays, isactive, createdat)
                SELECT 'Exclusivo Anual', 'Tudo do Essencial, sem limites, com implantação assistida e canal direto. Dois meses grátis no anual.', 19970.00, 'BRL', 2, 14, true, now()
                WHERE NOT EXISTS (SELECT 1 FROM plans WHERE name = 'Exclusivo Anual');
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                UPDATE plans SET name = 'Completo Mensal', priceamount = 497.00, launchpriceamount = NULL, launchpriceuntil = NULL WHERE name = 'Essencial Mensal';
                UPDATE plans SET name = 'Completo Anual', priceamount = 4970.00, launchpriceamount = NULL, launchpriceuntil = NULL WHERE name = 'Essencial Anual';
                DELETE FROM plans WHERE name IN ('Exclusivo Mensal', 'Exclusivo Anual') AND NOT EXISTS (SELECT 1 FROM subscriptions s JOIN plans p ON p.id = s.planid WHERE p.name IN ('Exclusivo Mensal', 'Exclusivo Anual'));
            ");

            if (Schema.Table("subscriptions").Column("priceamount").Exists())
            {
                Delete.Column("priceamount").FromTable("subscriptions");
            }

            if (Schema.Table("plans").Column("launchpriceamount").Exists())
            {
                Delete.Column("launchpriceamount").FromTable("plans");
                Delete.Column("launchpriceuntil").FromTable("plans");
            }
        }
    }
}
