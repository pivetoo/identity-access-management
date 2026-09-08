using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    /// <summary>
    /// Perfis padrao que todo tenant do Mainstay recebe ao ser provisionado (ContractService copia os
    /// templates ativos do sistema para o contrato novo).
    ///
    /// Seed condicional em SQL, e nao pela API fluente: os templates dependem do id da aplicacao
    /// resolvido por audience em tempo de execucao e precisam ser idempotentes (a migration roda em
    /// ambientes que ja tem parte dos perfis criados a mao).
    /// </summary>
    [Migration(202609080003)]
    public sealed class Migration_202609080003_SeedMainstayRoleTemplates : Migration
    {
        private const string Audience = "agency-campaign";
        private const string RootTemplate = "Administrador";
        private const string DefaultTemplate = "Somente leitura";

        public override void Up()
        {
            SeedTemplate(
                "Gestor",
                "Acompanha e aprova comercial, produção e financeiro. Não administra usuários nem integrações.",
                "painel.ver",
                "comercial.ver", "comercial.editar", "comercial.aprovar",
                "producao.ver", "producao.editar", "producao.aprovar",
                "financeiro.ver", "financeiro.editar", "financeiro.movimentar", "financeiro.aprovar",
                "fiscal.ver",
                "relatorios.comercial", "relatorios.producao", "relatorios.financeiro", "relatorios.fiscal",
                "configuracoes.ver",
                "auditoria.ver");

            SeedTemplate(
                "Comercial",
                "Funil, propostas e marcas. Acompanha a produção do que vendeu.",
                "painel.ver",
                "comercial.ver", "comercial.editar",
                "producao.ver",
                "relatorios.comercial");

            SeedTemplate(
                "Produção",
                "Campanhas, creators e entregas, incluindo a aprovação de conteúdo.",
                "painel.ver",
                "producao.ver", "producao.editar", "producao.aprovar",
                "comercial.ver",
                "relatorios.producao");

            // Sem financeiro.aprovar de proposito: a alcada existe para que quem paga nao seja quem aprova.
            SeedTemplate(
                "Financeiro",
                "Contas a pagar e a receber, cobranças, repasses e conciliação bancária.",
                "painel.ver",
                "financeiro.ver", "financeiro.editar", "financeiro.movimentar",
                "fiscal.ver",
                "relatorios.financeiro", "relatorios.fiscal");

            SeedTemplate(
                "Contador",
                "Documentos fiscais, retenções e o pacote do contador.",
                "financeiro.ver",
                "fiscal.ver", "fiscal.editar",
                "relatorios.financeiro", "relatorios.fiscal");

            SeedTemplate(
                DefaultTemplate,
                "Enxerga os módulos e relatórios sem alterar nada.",
                "painel.ver",
                "comercial.ver", "producao.ver", "financeiro.ver", "fiscal.ver",
                "relatorios.comercial", "relatorios.producao", "relatorios.financeiro", "relatorios.fiscal");

            // O perfil pre-selecionado ao cadastrar um usuario passa a ser o de menor privilegio: com
            // "Administrador" como padrao, um clique distraido dava acesso total a agencia inteira.
            Execute.Sql($@"
                UPDATE systemroletemplates SET isdefault = false, updatedat = now()
                WHERE name = '{RootTemplate}'
                  AND systemapplicationid IN (SELECT id FROM systemapplications WHERE audience = '{Audience}');

                UPDATE systemroletemplates SET isdefault = true, updatedat = now()
                WHERE name = '{DefaultTemplate}'
                  AND systemapplicationid IN (SELECT id FROM systemapplications WHERE audience = '{Audience}');
            ");
        }

        public override void Down()
        {
            string[] seeded = ["Gestor", "Comercial", "Produção", "Financeiro", "Contador", DefaultTemplate];
            string names = string.Join(", ", seeded.Select(name => $"'{name}'"));

            Execute.Sql($@"
                DELETE FROM systemroletemplatecapabilities
                WHERE systemroletemplateid IN (
                    SELECT t.id FROM systemroletemplates t
                    JOIN systemapplications sa ON sa.id = t.systemapplicationid
                    WHERE sa.audience = '{Audience}' AND t.name IN ({names})
                );

                DELETE FROM systemroletemplateaccessresources
                WHERE systemroletemplateid IN (
                    SELECT t.id FROM systemroletemplates t
                    JOIN systemapplications sa ON sa.id = t.systemapplicationid
                    WHERE sa.audience = '{Audience}' AND t.name IN ({names})
                );

                DELETE FROM systemroletemplates
                WHERE name IN ({names})
                  AND systemapplicationid IN (SELECT id FROM systemapplications WHERE audience = '{Audience}');

                UPDATE systemroletemplates SET isdefault = true, updatedat = now()
                WHERE name = '{RootTemplate}'
                  AND systemapplicationid IN (SELECT id FROM systemapplications WHERE audience = '{Audience}');
            ");
        }

        private void SeedTemplate(string name, string description, params string[] capabilities)
        {
            Execute.Sql($@"
                INSERT INTO systemroletemplates (systemapplicationid, name, description, isroot, isdefault, isactive, createdat)
                SELECT sa.id, '{name}', '{description}', false, false, true, now()
                FROM systemapplications sa
                WHERE sa.audience = '{Audience}'
                  AND NOT EXISTS (
                      SELECT 1 FROM systemroletemplates t
                      WHERE t.systemapplicationid = sa.id AND t.name = '{name}'
                  );
            ");

            string values = string.Join(", ", capabilities.Select(capability => $"('{capability}')"));

            Execute.Sql($@"
                INSERT INTO systemroletemplatecapabilities (systemroletemplateid, capabilitykey, isactive, createdat)
                SELECT t.id, k.capabilitykey, true, now()
                FROM systemroletemplates t
                JOIN systemapplications sa ON sa.id = t.systemapplicationid
                CROSS JOIN (VALUES {values}) AS k(capabilitykey)
                WHERE sa.audience = '{Audience}' AND t.name = '{name}'
                  AND NOT EXISTS (
                      SELECT 1 FROM systemroletemplatecapabilities c
                      WHERE c.systemroletemplateid = t.id AND c.capabilitykey = k.capabilitykey
                  );
            ");
        }
    }
}
