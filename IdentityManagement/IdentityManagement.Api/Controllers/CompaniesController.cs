using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.Companies;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Api.Controllers
{
    public sealed class CompaniesController : ApiControllerBase
    {
        private readonly ICompanyService companyService;
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public CompaniesController(ICompanyService companyService, IStringLocalizer<IdentityManagementResource> Localizer)
        {
            this.companyService = companyService;
            this.Localizer = Localizer;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            var companies = await companyService.GetActiveCompanies(cancellationToken);
            return Http200(companies);
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateCompanyRequest request, CancellationToken cancellationToken)
        {
            var response = await companyService.CreateCompany(request, cancellationToken);
            return Http201(response, Localizer["company.created"]);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCompanyRequest request, CancellationToken cancellationToken)
        {
            var response = await companyService.UpdateCompany(id, request, cancellationToken);
            return Http200(response, Localizer["company.updated"]);
        }
    }
}
