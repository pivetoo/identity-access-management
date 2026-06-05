using IdentityManagement.Application.Services;

namespace IdentityManagement.Api.BackgroundJobs
{
    // Rotina diaria de dunning: bloqueia (sem inativar) assinaturas que permaneceram PastDue
    // alem do periodo de tolerancia. Single-tenant: abre um scope por tick e resolve o servico nele.
    public sealed class SubscriptionDunningJob : BackgroundService
    {
        private const int DefaultGraceDays = 7;
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan TickInterval = TimeSpan.FromHours(24);

        private readonly IServiceScopeFactory scopeFactory;
        private readonly IConfiguration configuration;
        private readonly ILogger<SubscriptionDunningJob> logger;

        public SubscriptionDunningJob(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<SubscriptionDunningJob> logger)
        {
            this.scopeFactory = scopeFactory;
            this.configuration = configuration;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(StartupDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            using PeriodicTimer timer = new(TickInterval);

            do
            {
                await RunOnce(stoppingToken);
            }
            while (await WaitForNextTick(timer, stoppingToken));
        }

        private static async Task<bool> WaitForNextTick(PeriodicTimer timer, CancellationToken stoppingToken)
        {
            try
            {
                return await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private async Task RunOnce(CancellationToken stoppingToken)
        {
            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                ISubscriptionService subscriptions = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

                int graceDays = ResolveGraceDays();
                int blocked = await subscriptions.BlockOverduePastGraceAsync(graceDays, DateTimeOffset.UtcNow, stoppingToken);

                if (blocked > 0)
                {
                    logger.LogInformation("SubscriptionDunningJob: {Blocked} subscriptions blocked for overdue payment.", blocked);
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "SubscriptionDunningJob run failed.");
            }
        }

        private int ResolveGraceDays()
        {
            string? raw = configuration["Billing:BlockGraceDays"];

            if (int.TryParse(raw, out int parsed) && parsed >= 0)
            {
                return parsed;
            }

            return DefaultGraceDays;
        }
    }
}
