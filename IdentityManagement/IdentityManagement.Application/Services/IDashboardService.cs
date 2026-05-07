using Archon.Core.Pagination;
using IdentityManagement.Application.Responses.Dashboard;

namespace IdentityManagement.Application.Services
{
    public interface IDashboardService
    {
        Task<DashboardKpiResponse> GetKpis(CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<UsersByCompanyResponse>> GetUsersByCompany(CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<TopSystemApplicationResponse>> GetTopSystems(int limit, CancellationToken cancellationToken = default);

        Task<PagedResult<ActiveSessionResponse>> GetActiveSessions(int page, int pageSize, CancellationToken cancellationToken = default);

        Task<DashboardOverviewResponse> GetOverview(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default);
    }
}
