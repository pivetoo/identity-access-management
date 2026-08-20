using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    // Endereco de cobranca da empresa. Exigencia do checkout recorrente de cartao do Asaas, que
    // recusa a criacao sem CEP, logradouro, numero, bairro e cidade do pagador.
    // Nullable de proposito: o cadastro publico nao pede endereco, e quem paga por PIX nunca precisa.
    [Migration(202608200002)]
    public sealed class Migration_202608200002_AddCompanyBillingAddress : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("companies").Column("billingpostalcode").Exists())
            {
                Alter.Table("companies")
                    .AddColumn("billingpostalcode").AsString(8).Nullable()
                    .AddColumn("billingstreet").AsString(255).Nullable()
                    .AddColumn("billingnumber").AsString(20).Nullable()
                    .AddColumn("billingcomplement").AsString(100).Nullable()
                    .AddColumn("billingdistrict").AsString(100).Nullable()
                    .AddColumn("billingcity").AsString(100).Nullable()
                    .AddColumn("billingstate").AsString(2).Nullable();
            }
        }

        public override void Down()
        {
            if (Schema.Table("companies").Column("billingpostalcode").Exists())
            {
                Delete.Column("billingpostalcode")
                    .Column("billingstreet")
                    .Column("billingnumber")
                    .Column("billingcomplement")
                    .Column("billingdistrict")
                    .Column("billingcity")
                    .Column("billingstate")
                    .FromTable("companies");
            }
        }
    }
}
