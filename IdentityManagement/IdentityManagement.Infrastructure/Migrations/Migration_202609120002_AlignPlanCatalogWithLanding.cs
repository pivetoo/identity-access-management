using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    /// <summary>
    /// Alinha o catalogo de planos ao que a landing anuncia, que e a oferta real.
    ///
    /// Antes divergiam em tres pontos: Essencial custava 997 na tabela e 297 na vitrine; o plano
    /// "Exclusivo" era o "Pro" da vitrine com o dobro do preco; e o cadastro publico contratava um
    /// plano "Fundador" que a vitrine nao mostrava. Quem se cadastrasse veria 297 e seria cobrado 497.
    ///
    /// O Fundador sai de vez. Preco promocional, quando voltar a existir, e o LaunchPriceAmount do
    /// proprio plano — mecanismo unico, em vez de um plano paralelo.
    /// </summary>
    [Migration(202609120002)]
    public sealed class Migration_202609120002_AlignPlanCatalogWithLanding : Migration
    {
        public override void Up()
        {
            Update.Table("plans").Set(new { priceamount = 297.00m, updatedat = System.DateTimeOffset.UtcNow })
                .Where(new { name = "Essencial Mensal" });

            Update.Table("plans").Set(new { priceamount = 2970.00m, updatedat = System.DateTimeOffset.UtcNow })
                .Where(new { name = "Essencial Anual" });

            // Renomeia em vez de criar linha nova: preserva o historico das assinaturas que ja apontam
            // para estes planos.
            Update.Table("plans").Set(new { name = "Pro Mensal", priceamount = 897.00m, updatedat = System.DateTimeOffset.UtcNow })
                .Where(new { name = "Exclusivo Mensal" });

            Update.Table("plans").Set(new { name = "Pro Anual", priceamount = 8970.00m, updatedat = System.DateTimeOffset.UtcNow })
                .Where(new { name = "Exclusivo Anual" });

            // Reaponta antes de apagar: subscriptions.planid tem FK e assinatura viva no Fundador
            // bloquearia o DELETE. NAO mexe em subscriptions.priceamount — o valor contratado e
            // aquele, nao o do plano, entao ninguem passa a pagar diferente por causa desta migration.
            // Correlacionado por periodicidade, o que a API fluente nao expressa.
            Execute.Sql(@"
                UPDATE subscriptions s SET planid = e.id, updatedat = now()
                FROM plans f, plans e
                WHERE s.planid = f.id AND f.name = 'Fundador Mensal' AND e.name = 'Essencial Mensal';

                UPDATE subscriptions s SET planid = e.id, updatedat = now()
                FROM plans f, plans e
                WHERE s.planid = f.id AND f.name = 'Fundador Anual' AND e.name = 'Essencial Anual';
            ");

            Delete.FromTable("plans").Row(new { name = "Fundador Mensal" });
            Delete.FromTable("plans").Row(new { name = "Fundador Anual" });
        }

        public override void Down()
        {
            Insert.IntoTable("plans").Row(new
            {
                name = "Fundador Mensal",
                description = "Condição de lançamento: preço garantido enquanto a assinatura estiver ativa.",
                priceamount = 497.00m,
                currency = "BRL",
                billingperiod = 1,
                trialdays = 30,
                isactive = true,
                createdat = System.DateTimeOffset.UtcNow
            });

            Insert.IntoTable("plans").Row(new
            {
                name = "Fundador Anual",
                description = "Condição de lançamento: preço garantido enquanto a assinatura estiver ativa. Dois meses grátis no anual.",
                priceamount = 4970.00m,
                currency = "BRL",
                billingperiod = 2,
                trialdays = 30,
                isactive = true,
                createdat = System.DateTimeOffset.UtcNow
            });

            Update.Table("plans").Set(new { name = "Exclusivo Mensal", priceamount = 1997.00m })
                .Where(new { name = "Pro Mensal" });

            Update.Table("plans").Set(new { name = "Exclusivo Anual", priceamount = 19970.00m })
                .Where(new { name = "Pro Anual" });

            Update.Table("plans").Set(new { priceamount = 997.00m }).Where(new { name = "Essencial Mensal" });
            Update.Table("plans").Set(new { priceamount = 9970.00m }).Where(new { name = "Essencial Anual" });
        }
    }
}
