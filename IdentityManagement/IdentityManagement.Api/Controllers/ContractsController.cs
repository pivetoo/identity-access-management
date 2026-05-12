using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.Contracts;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Api.Controllers
{
    public sealed class ContractsController : ApiControllerBase
    {
        private readonly IContractService contractService;
        private readonly IConfiguration configuration;
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public ContractsController(IContractService contractService, IConfiguration configuration, IStringLocalizer<IdentityManagementResource> Localizer)
        {
            this.contractService = contractService;
            this.configuration = configuration;
            this.Localizer = Localizer;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            var contracts = await contractService.GetActive(cancellationToken);
            return Http200(contracts);
        }

        [RequireAccess]
        [GetEndpoint("{companyId:long}")]
        public async Task<IActionResult> GetByCompanyId(long companyId, CancellationToken cancellationToken)
        {
            var contracts = await contractService.GetByCompanyId(companyId, cancellationToken);
            return Http200(contracts);
        }

        [RequireAccess]
        [GetEndpoint("{systemApplicationId:long}")]
        public async Task<IActionResult> GetBySystemApplicationId(long systemApplicationId, CancellationToken cancellationToken)
        {
            var contracts = await contractService.GetBySystemApplicationId(systemApplicationId, cancellationToken);
            return Http200(contracts);
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateContractRequest request, CancellationToken cancellationToken)
        {
            string setupBaseUrl = configuration["Oidc:Issuer"] ?? string.Empty;
            var response = await contractService.CreateContract(request, setupBaseUrl, cancellationToken);
            return Http201(response, Localizer["contract.created"]);
        }

        [RequireAccess]
        [PostEndpoint("{id:long}")]
        public async Task<IActionResult> ResendInvitation(long id, CancellationToken cancellationToken)
        {
            string setupBaseUrl = configuration["Oidc:Issuer"] ?? string.Empty;
            await contractService.ResendAdminInvitation(id, setupBaseUrl, cancellationToken);
            return Http200(message: Localizer["contract.adminInvitation.resent"]);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateContractRequest request, CancellationToken cancellationToken)
        {
            var response = await contractService.UpdateContract(id, request, cancellationToken);
            return Http200(response, Localizer["contract.updated"]);
        }
    }
}
