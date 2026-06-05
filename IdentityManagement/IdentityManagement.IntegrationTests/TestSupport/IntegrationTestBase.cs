using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityManagement.IntegrationTests
{
    // Base compartilhada dos fixtures de integracao: isola cada teste limpando o grafo operacional
    // e abre escopos DI reais. Permanece no namespace raiz para ser visivel a todas as pastas de teste.
    public abstract class IntegrationTestBase
    {
        // O FluentMigrator (Archon) versiona em "_migrations"; preservar.
        private const string ResetSql = @"
DO $$
DECLARE
    tables text;
BEGIN
    SELECT string_agg(format('%I', tablename), ', ')
    INTO tables
    FROM pg_tables
    WHERE schemaname = 'public'
      AND tablename NOT IN ('_migrations');

    IF tables IS NOT NULL THEN
        EXECUTE 'TRUNCATE TABLE ' || tables || ' RESTART IDENTITY CASCADE';
    END IF;
END $$;";

        // Limpa o grafo operacional antes de cada teste (o container e compartilhado entre testes).
        [SetUp]
        public async Task ResetDatabase()
        {
            using IServiceScope scope = TestHarness.Services.CreateScope();
            DbContext dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
            await dbContext.Database.ExecuteSqlRawAsync(ResetSql);
        }

        protected static async Task InScopeAsync(Func<IServiceProvider, Task> action)
        {
            using IServiceScope scope = TestHarness.Services.CreateScope();
            await action(scope.ServiceProvider);
        }
    }
}
