using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    // Origem do cadastro publico (utm_*, gclid, fbclid, pagina de entrada, referrer). Nasce no
    // cadastro pendente e e copiada para a empresa na confirmacao. Tudo nulo para cadastro direto.
    [Migration(202609060001)]
    public sealed class Migration_202609060001_AddSignupAttribution : Migration
    {
        private static readonly (string Column, int Size)[] Columns =
        {
            ("utmsource", 200),
            ("utmmedium", 200),
            ("utmcampaign", 200),
            ("utmcontent", 200),
            ("utmterm", 200),
            ("gclid", 200),
            ("fbclid", 200),
            ("landingpage", 500),
            ("referrer", 500)
        };

        public override void Up()
        {
            foreach (string table in new[] { "pendingsignups", "companies" })
            {
                foreach ((string column, int size) in Columns)
                {
                    if (!Schema.Table(table).Column(column).Exists())
                    {
                        Alter.Table(table).AddColumn(column).AsString(size).Nullable();
                    }
                }
            }
        }

        public override void Down()
        {
            foreach (string table in new[] { "pendingsignups", "companies" })
            {
                foreach ((string column, _) in Columns)
                {
                    if (Schema.Table(table).Column(column).Exists())
                    {
                        Delete.Column(column).FromTable(table);
                    }
                }
            }
        }
    }
}
