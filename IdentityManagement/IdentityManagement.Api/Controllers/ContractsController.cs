using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.Contracts;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Api.Controllers
{
    public sealed class ContractsController : ApiControllerBase
    {
        private readonly IContractService contractService;
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public ContractsController(IContractService contractService, IStringLocalizer<IdentityManagementResource> Localizer)
        {
            this.contractService = contractService;
            this.Localizer = Localizer;
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateContractRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var response = await contractService.CreateContract(request, cancellationToken);
            return Http201(response, Localizer["contract.created"]);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateContractRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var response = await contractService.UpdateContract(id, request, cancellationToken);
            return Http200(response, Localizer["contract.updated"]);
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            var contracts = await contractService.GetActive(cancellationToken);
            return Http200(contracts);
        }

        [RequireAccess]
        [GetEndpoint("company/{companyId:long}")]
        public async Task<IActionResult> GetByCompanyId(long companyId, CancellationToken cancellationToken)
        {
            var contracts = await contractService.GetByCompanyId(companyId, cancellationToken);
            return Http200(contracts);
        }

        [RequireAccess]
        [GetEndpoint("system-application/{systemApplicationId:long}")]
        public async Task<IActionResult> GetBySystemApplicationId(long systemApplicationId, CancellationToken cancellationToken)
        {
            var contracts = await contractService.GetBySystemApplicationId(systemApplicationId, cancellationToken);
            return Http200(contracts);
        }

        [RequireAccess]
        [GetEndpoint("{id:long}/secrets")]
        public async Task<IActionResult> GetSecrets(long id, CancellationToken cancellationToken)
        {
            var secrets = await contractService.GetContractSecrets(id, cancellationToken);
            if (secrets is null)
            {
                return Http404(Localizer["contract.notFound"]);
            }

            return Http200(secrets);
        }
    }
}
