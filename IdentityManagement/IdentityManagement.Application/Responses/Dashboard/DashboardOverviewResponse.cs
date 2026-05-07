namespace IdentityManagement.Application.Responses.Dashboard
{
    public sealed class DashboardOverviewResponse
    {
        public IReadOnlyCollection<DashboardLoginTrendResponse> LoginTrend { get; set; } = [];

        public DashboardContractHealthResponse ContractHealth { get; set; } = new();

        public IReadOnlyCollection<DashboardSessionsByHourResponse> SessionsByHour { get; set; } = [];

        public IReadOnlyCollection<DashboardTopSystemResponse> TopSystems { get; set; } = [];

        public DashboardSecurityPulseResponse SecurityPulse { get; set; } = new();
    }

    public sealed class DashboardLoginTrendResponse
    {
        public string Label { get; set; } = string.Empty;

        public int Logins { get; set; }

        public int Failures { get; set; }
    }

    public sealed class DashboardContractHealthResponse
    {
        public int Active { get; set; }

        public int ExpiringSoon { get; set; }

        public int Suspended { get; set; }

        public int WithoutOAuthClient { get; set; }
    }

    public sealed class DashboardSessionsByHourResponse
    {
        public string Label { get; set; } = string.Empty;

        public int Sessions { get; set; }
    }

    public sealed class DashboardTopSystemResponse
    {
        public long SystemApplicationId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Accesses { get; set; }
    }

    public sealed class DashboardSecurityPulseResponse
    {
        public int MfaCoverage { get; set; }

        public int ValidSessions { get; set; }

        public int RotatedTokens { get; set; }

        public int ReviewedAccesses { get; set; }
    }
}
