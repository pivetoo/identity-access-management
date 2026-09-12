using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    /// <summary>
    /// O e-mail deixa de identificar a empresa; so o CNPJ identifica.
    ///
    /// O indice unico obrigava a mesma pessoa a inventar um endereco por agencia: o formulario
    /// publico sempre chamou esse campo de "e-mail do administrador", e quem ja administra uma
    /// agencia batia em conflito ao cadastrar a segunda. O modelo sempre suportou um usuario em
    /// varios contratos (o login resolve pela selecao de contrato) — a trava estava no lugar errado.
    ///
    /// Nao recrio como indice comum: nenhuma consulta usa e-mail de empresa como chave.
    /// </summary>
    [Migration(202609110001)]
    public sealed class Migration_202609110001_DropUniqueCompanyEmail : Migration
    {
        private const string IndexName = "ix_companies_email";

        public override void Up()
        {
            if (Schema.Table("companies").Index(IndexName).Exists())
            {
                Delete.Index(IndexName).OnTable("companies");
            }
        }

        public override void Down()
        {
            // Falha de proposito se ja existirem duas empresas com o mesmo e-mail: voltar atras
            // exige decidir qual delas fica, e isso nao cabe numa migration.
            if (!Schema.Table("companies").Index(IndexName).Exists())
            {
                Create.Index(IndexName)
                    .OnTable("companies")
                    .OnColumn("email").Ascending()
                    .WithOptions().Unique();
            }
        }
    }
}
