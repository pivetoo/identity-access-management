using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    /// <summary>
    /// A oferta passa a ter duas faixas: Essencial ate 50 creators, e Escala acima disso, so por
    /// conversa com o time. O plano Pro deixa de existir.
    ///
    /// O Escala entra como linha de plano (preco 0) mesmo sendo contrato sob medida: com o Pro fora,
    /// nao sobraria plano nenhum para o console atribuir quando a venda fechar, e o time acabaria
    /// lancando Escala disfarcado de Essencial — o relatorio de receita passaria a mentir. O valor
    /// real de cada contrato vive no PriceAmount da assinatura, que ja e independente do plano.
    /// </summary>
    [Migration(202609120003)]
    public sealed class Migration_202609120003_ReplaceProWithEscala : Migration
    {
        public override void Up()
        {
            InsertPlan("Escala Mensal", "Contrato sob medida acima de 50 creators. O valor e o da assinatura, nao o do plano.", 1);
            InsertPlan("Escala Anual", "Contrato sob medida acima de 50 creators, com cobranca anual. O valor e o da assinatura, nao o do plano.", 2);

            // Defensivo: hoje nenhuma assinatura aponta para Pro, mas a migration nao pode presumir
            // isso em outro ambiente — subscriptions.planid tem FK e o DELETE quebraria. Nao mexe em
            // subscriptions.priceamount: o valor contratado continua sendo o da assinatura.
            Execute.Sql(@"
                UPDATE subscriptions s SET planid = e.id, updatedat = now()
                FROM plans p, plans e
                WHERE s.planid = p.id AND p.name = 'Pro Mensal' AND e.name = 'Essencial Mensal';

                UPDATE subscriptions s SET planid = e.id, updatedat = now()
                FROM plans p, plans e
                WHERE s.planid = p.id AND p.name = 'Pro Anual' AND e.name = 'Essencial Anual';
            ");

            Delete.FromTable("plans").Row(new { name = "Pro Mensal" });
            Delete.FromTable("plans").Row(new { name = "Pro Anual" });
        }

        public override void Down()
        {
            InsertPlan("Pro Mensal", "Tudo do Essencial, com casting maior e acompanhamento do time.", 1, 897.00m);
            InsertPlan("Pro Anual", "Tudo do Essencial, com casting maior e acompanhamento do time. Dois meses grátis no anual.", 2, 8970.00m);

            Delete.FromTable("plans").Row(new { name = "Escala Mensal" });
            Delete.FromTable("plans").Row(new { name = "Escala Anual" });
        }

        private void InsertPlan(string name, string description, int billingPeriod, decimal priceAmount = 0.00m)
        {
            Insert.IntoTable("plans").Row(new
            {
                name,
                description,
                priceamount = priceAmount,
                currency = "BRL",
                billingperiod = billingPeriod,
                trialdays = 30,
                isactive = true,
                createdat = System.DateTimeOffset.UtcNow
            });
        }
    }
}
