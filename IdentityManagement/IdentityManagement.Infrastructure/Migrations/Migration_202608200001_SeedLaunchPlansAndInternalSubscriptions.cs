using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    // Lancamento da contratacao self-service: semeia os planos publicos (Essencial mensal/anual),
    // o plano Interno dos tenants da casa e da assinatura Ativa no Interno a toda empresa que
    // exista SEM assinatura neste momento. Esse grandfather e o que permite inverter o fail-open
    // do gate de assinatura (sem-sub passou a significar bloqueado) sem trancar os tenants atuais.
    [Migration(202608200001)]
    public sealed class Migration_202608200001_SeedLaunchPlansAndInternalSubscriptions : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("plans").Exists() || !Schema.Table("subscriptions").Exists())
            {
                return;
            }

            Execute.Sql("""
                INSERT INTO plans (name, description, priceamount, currency, billingperiod, trialdays, isactive, createdat)
                SELECT 'Essencial Mensal', 'Produto completo para agencias de influencia. Ate 100 creators ativos.', 497.00, 'BRL', 1, 14, true, now()
                WHERE NOT EXISTS (SELECT 1 FROM plans WHERE name = 'Essencial Mensal');
                """);

            Execute.Sql("""
                INSERT INTO plans (name, description, priceamount, currency, billingperiod, trialdays, isactive, createdat)
                SELECT 'Essencial Anual', 'Produto completo para agencias de influencia. Ate 100 creators ativos. Dois meses gratis no anual.', 4970.00, 'BRL', 2, 14, true, now()
                WHERE NOT EXISTS (SELECT 1 FROM plans WHERE name = 'Essencial Anual');
                """);

            Execute.Sql("""
                INSERT INTO plans (name, description, priceamount, currency, billingperiod, trialdays, isactive, createdat)
                SELECT 'Interno', 'Tenants da propria Mainstay. Nao listar em vitrine.', 0.00, 'BRL', 1, 0, false, now()
                WHERE NOT EXISTS (SELECT 1 FROM plans WHERE name = 'Interno');
                """);

            Execute.Sql("""
                INSERT INTO subscriptions (companyid, planid, status, startedat, currentperiodstart, currentperiodend, isblocked, createdat)
                SELECT c.id, (SELECT id FROM plans WHERE name = 'Interno'), 2, now(), now(), now() + interval '100 years', false, now()
                FROM companies c
                WHERE NOT EXISTS (SELECT 1 FROM subscriptions s WHERE s.companyid = c.id);
                """);
        }

        public override void Down()
        {
            if (!Schema.Table("plans").Exists() || !Schema.Table("subscriptions").Exists())
            {
                return;
            }

            Execute.Sql("""
                DELETE FROM subscriptions s
                USING plans p
                WHERE p.id = s.planid AND p.name = 'Interno';
                """);

            Execute.Sql("DELETE FROM plans WHERE name IN ('Essencial Mensal', 'Essencial Anual', 'Interno') AND NOT EXISTS (SELECT 1 FROM subscriptions s WHERE s.planid = plans.id);");
        }
    }
}
