using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    /// <summary>
    /// "Essencial" -> "Completo" no plano publico.
    ///
    /// O nome antigo prometia ser o piso da grade, e este plano e o oposto: tem tudo. Quando os
    /// planos MENORES existirem, "Essencial" volta a nomear o que a palavra significa — e por isso
    /// o rename acontece AGORA, enquanto ninguem esta preso a um plano com esse nome.
    ///
    /// Renomeia as linhas existentes em vez de criar novas: as assinaturas apontam para o id, entao
    /// criar plano novo desligaria todo mundo do plano vigente.
    ///
    /// ATENCAO: o cadastro publico resolve o plano PELO NOME (Signup:MonthlyPlanName /
    /// AnnualPlanName). Esta migration precisa andar junto com a config do IdM — sem isso o signup
    /// recusa com "plano nao configurado".
    /// </summary>
    [Migration(202608210002)]
    public sealed class Migration_202608210002_RenamePublicPlanToCompleto : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("plans").Exists())
            {
                return;
            }

            Execute.Sql("UPDATE plans SET name = 'Completo Mensal' WHERE name = 'Essencial Mensal';");
            Execute.Sql("UPDATE plans SET name = 'Completo Anual' WHERE name = 'Essencial Anual';");
        }

        public override void Down()
        {
            if (!Schema.Table("plans").Exists())
            {
                return;
            }

            Execute.Sql("UPDATE plans SET name = 'Essencial Mensal' WHERE name = 'Completo Mensal';");
            Execute.Sql("UPDATE plans SET name = 'Essencial Anual' WHERE name = 'Completo Anual';");
        }
    }
}
