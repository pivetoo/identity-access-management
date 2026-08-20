namespace IdentityManagement.Application.Responses.Contracts
{
    public class ContractSelectionResponseItem
    {
        public long ContractId { get; set; }

        public string SystemApplicationName { get; set; } = string.Empty;

        /// <summary>
        /// Audience da aplicacao do contrato (ex.: "agency-campaign"). Permite ao consumidor filtrar
        /// contratos por produto sem depender do nome de exibicao, que muda.
        /// </summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Tenant da empresa do contrato. Sem ele o consumidor precisa casar por razao social, e duas
        /// empresas homonimas viram ambiguidade — ou, pior, colocam a pessoa no tenant errado.
        /// </summary>
        public Guid TenantId { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public string RoleName { get; set; } = string.Empty;

        public string PortalUrl { get; set; } = string.Empty;
    }
}
