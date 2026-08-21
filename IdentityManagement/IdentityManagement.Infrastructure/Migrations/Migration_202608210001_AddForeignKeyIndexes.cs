using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    /// <summary>
    /// Indices nas chaves estrangeiras que faltavam.
    ///
    /// O Postgres cria indice sozinho para PK e unique, mas NAO para FK. Sem eles dois caminhos
    /// pagam caro: o JOIN pela coluna e, principalmente, o DELETE no pai — que varre a tabela filha
    /// inteira para validar a referencia.
    ///
    /// Nem toda FK ganhou indice: entram as tabelas que CRESCEM com o uso (sessao, token, codigo de
    /// autorizacao) e as filhas de pai que a gente realmente apaga (contrato, papel, recurso de
    /// acesso). Tabela pequena e estatica nao ganha, porque indice tambem custa escrita.
    ///
    /// Verificado antes: loginsessions tinha 342 varreduras sequenciais e ZERO uso de indice,
    /// authorizationcodes 277 e zero, pendingauthorizationsessions 554 e zero.
    /// </summary>
    [Migration(202608210001)]
    public sealed class Migration_202608210001_AddForeignKeyIndexes : Migration
    {
        private static readonly (string Table, string Column)[] Targets =
        [
            // Crescem a cada login e sao limpas por usuario/contrato.
            ("loginsessions", "userid"),
            ("loginsessions", "contractid"),
            ("refreshtokens", "userid"),
            ("refreshtokens", "contractid"),
            ("authorizationcodes", "userid"),
            ("authorizationcodes", "contractid"),
            ("pendingauthorizationsessions", "userid"),

            // Caminho de resolucao de tenant e de autorizacao.
            ("contracts", "systemapplicationid"),
            ("userroles", "roleid"),
            ("roleaccessresources", "accessresourceid"),

            // Gate de assinatura le por plano; e o plano e apagavel.
            ("subscriptions", "planid")
        ];

        public override void Up()
        {
            foreach ((string table, string column) in Targets)
            {
                string indexName = $"ix_{table}_{column}";

                if (!Schema.Table(table).Exists() || Schema.Table(table).Index(indexName).Exists())
                {
                    continue;
                }

                Create.Index(indexName)
                    .OnTable(table)
                    .OnColumn(column).Ascending();
            }
        }

        public override void Down()
        {
            foreach ((string table, string column) in Targets)
            {
                string indexName = $"ix_{table}_{column}";

                if (Schema.Table(table).Exists() && Schema.Table(table).Index(indexName).Exists())
                {
                    Delete.Index(indexName).OnTable(table);
                }
            }
        }
    }
}
