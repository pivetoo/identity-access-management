using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManagement.Api.Controllers
{
    public sealed class DashboardController : ApiControllerBase
    {
        private readonly IDashboardService dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            this.dashboardService = dashboardService;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> GetKpis(CancellationToken cancellationToken)
        {
            var response = await dashboardService.GetKpis(cancellationToken);
            return Http200(response);
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> GetUsersByCompany(CancellationToken cancellationToken)
        {
            var response = await dashboardService.GetUsersByCompany(cancellationToken);
            return Http200(response);
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> GetTopSystems([FromQuery] int limit = 5, CancellationToken cancellationToken = default)
        {
            var response = await dashboardService.GetTopSystems(limit, cancellationToken);
            return Http200(response);
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> GetActiveSessions([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var response = await dashboardService.GetActiveSessions(page, pageSize, cancellationToken);
            return Http200(response);
        }
    }
}
