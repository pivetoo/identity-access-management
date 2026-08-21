using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityManagement.Api.BackgroundJobs
{
    // Poda os cadastros publicos que expiraram sem confirmacao. E o contrapeso do desenho em duas
    // etapas: a tabela de pendentes existe justamente para absorver cadastro que nunca vira nada,
    // entao ela PRECISA de quem varra — senao troca-se um problema de disco (bancos de tenant) por
    // outro menor, mas ainda crescente.
    //
    // So apaga linha expirada E nao consumida. Cadastro que virou empresa fica como trilha.
    public sealed class PendingSignupCleanupJob : BackgroundService
    {
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan TickInterval = TimeSpan.FromHours(6);

        /// <summary>
        /// Idade de uma reserva para ser considerada presa. Provisionar leva segundos; meia hora e
        /// folga larga o bastante para nunca disputar com uma confirmacao em andamento.
        /// </summary>
        private const int StuckClaimMinutes = 30;

        private readonly IServiceScopeFactory scopeFactory;
        private readonly ILogger<PendingSignupCleanupJob> logger;

        public PendingSignupCleanupJob(IServiceScopeFactory scopeFactory, ILogger<PendingSignupCleanupJob> logger)
        {
            this.scopeFactory = scopeFactory;
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
                DbContext dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();

                DateTimeOffset agora = DateTimeOffset.UtcNow;

                // Reserva presa: o cadastro foi reivindicado mas nunca virou empresa. O caminho normal
                // ja devolve a reserva quando o provisionamento falha; isto cobre o que aquele catch
                // nao alcanca — processo derrubado no meio. Sem isto o link morre para sempre e o
                // cliente fica sem saida a nao ser suporte.
                DateTimeOffset limite = agora.AddMinutes(-StuckClaimMinutes);

                int released = await dbContext.Set<PendingSignup>()
                    .Where(item => item.ConsumedAt != null && item.CompanyId == null && item.ConsumedAt < limite && item.ExpiresAt > agora)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(item => item.ConsumedAt, (DateTimeOffset?)null),
                        stoppingToken);

                if (released > 0)
                {
                    logger.LogWarning("PendingSignupCleanupJob: {Released} stuck pending signups released for retry.", released);
                }

                int removed = await dbContext.Set<PendingSignup>()
                    .Where(item => item.ConsumedAt == null && item.ExpiresAt < agora)
                    .ExecuteDeleteAsync(stoppingToken);

                if (removed > 0)
                {
                    logger.LogInformation("PendingSignupCleanupJob: {Removed} expired pending signups removed.", removed);
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "PendingSignupCleanupJob run failed.");
            }
        }
    }
}
