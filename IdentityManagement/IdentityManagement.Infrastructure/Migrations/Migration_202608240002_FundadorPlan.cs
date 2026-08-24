using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    // "Fundador" vira plano nomeado (497/4.970), separado do Essencial de tabela (997/9.970).
    // Os planos que hoje carregam a condicao de lancamento viram o Fundador; o Essencial e recriado
    // como plano de tabela puro. O cadastro publico escolhe Fundador ate 31/12/2026 (SignupOptions).
    [Migration(202608240002)]
    public sealed class Migration_202608240002_FundadorPlan : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                UPDATE plans SET name = 'Fundador Mensal', priceamount = 497.00,
                       launchpriceamount = NULL, launchpriceuntil = NULL,
                       description = 'Condição de lançamento: preço garantido enquanto a assinatura estiver ativa.'
                WHERE name = 'Essencial Mensal';
                UPDATE plans SET name = 'Fundador Anual', priceamount = 4970.00,
                       launchpriceamount = NULL, launchpriceuntil = NULL,
                       description = 'Condição de lançamento: preço garantido enquanto a assinatura estiver ativa. Dois meses grátis no anual.'
                WHERE name = 'Essencial Anual';
                INSERT INTO plans (name, description, priceamount, currency, billingperiod, trialdays, isactive, createdat)
                SELECT 'Essencial Mensal', 'Produto completo para agências de influência.', 997.00, 'BRL', 1, 14, true, now()
                WHERE NOT EXISTS (SELECT 1 FROM plans WHERE name = 'Essencial Mensal');
                INSERT INTO plans (name, description, priceamount, currency, billingperiod, trialdays, isactive, createdat)
                SELECT 'Essencial Anual', 'Produto completo para agências de influência. Dois meses grátis no anual.', 9970.00, 'BRL', 2, 14, true, now()
                WHERE NOT EXISTS (SELECT 1 FROM plans WHERE name = 'Essencial Anual');
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                DELETE FROM plans WHERE name IN ('Essencial Mensal', 'Essencial Anual')
                    AND NOT EXISTS (SELECT 1 FROM subscriptions s WHERE s.planid = plans.id);
                UPDATE plans SET name = 'Essencial Mensal', priceamount = 997.00,
                       launchpriceamount = 497.00, launchpriceuntil = '2027-01-01T02:59:59Z'
                WHERE name = 'Fundador Mensal';
                UPDATE plans SET name = 'Essencial Anual', priceamount = 9970.00,
                       launchpriceamount = 4970.00, launchpriceuntil = '2027-01-01T02:59:59Z'
                WHERE name = 'Fundador Anual';
            ");
        }
    }
}
