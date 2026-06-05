# Onboarding consolidado de cliente + auto-provisionamento — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Um unico endpoint cria empresa + contratos + bancos de tenant (auto-provisionados) e dispara UM convite que resulta em UM admin com acesso a todos os sistemas do cliente.

**Architecture:** Backend orquestra tudo num `ClientOnboardingService` transacional, com `PostgresTenantProvisioner` isolando `CREATE/DROP DATABASE`. Convite passa a ser por empresa; o setup atribui o root role de todos os contratos ativos. Frontend simplifica o wizard (sem campos de infra) e chama o endpoint unico.

**Tech Stack:** .NET 10, EF Core + Npgsql, NUnit + FluentAssertions (unit), Testcontainers.PostgreSql (integracao), React + TypeScript + archon-ui (frontend), Resend (e-mail).

Spec: `docs/superpowers/specs/2026-06-04-idm-onboarding-consolidado-design.md`.

---

## File Structure

Backend (criar):
- `IdentityManagement.Infrastructure/Tenancy/TenantNaming.cs` — slug, nome do banco, validacao de identificador (puro, sem I/O).
- `IdentityManagement.Application/Services/ITenantProvisioner.cs` — contrato de provisionamento.
- `IdentityManagement.Infrastructure/Services/PostgresTenantProvisioner.cs` — CREATE/DROP DATABASE, connection string, gerar ApiKey.
- `IdentityManagement.Application/Services/IClientOnboardingService.cs` — contrato do caso de uso.
- `IdentityManagement.Infrastructure/Services/ClientOnboardingService.cs` — orquestracao + compensacao.
- `IdentityManagement.Application/Requests/Clients/OnboardClientRequest.cs` (+ `OnboardClientSystemItem`).
- `IdentityManagement.Application/Responses/Clients/OnboardClientResponse.cs`.
- `IdentityManagement.Api/Controllers/ClientsController.cs`.
- `IdentityManagement.IntegrationTests/` — novo projeto Testcontainers (harness + testes).

Backend (modificar):
- `IdentityManagement.Domain/Entities/ContractAdminInvitation.cs` — `+ CompanyId`.
- `IdentityManagement.Infrastructure/Migrations/Migration_<ts>_AddInvitationCompanyId.cs` — coluna idempotente.
- `IdentityManagement.Infrastructure/Persistence/EF/Configurations/ContractAdminInvitationConfiguration.cs` — mapear `companyid`.
- `IdentityManagement.Infrastructure/Services/AuthService.cs` — `ValidateAdminInvitation`, `SetupAdmin`.
- `IdentityManagement.Application/Responses/Auth/AdminInvitationInfoResponse.cs` — `+ SystemApplicationNames`.
- `IdentityManagement.Application/Services/IEmailSender.cs` + `Infrastructure/Services/ResendEmailSender.cs` — e-mail consolidado.
- `IdentityManagement.Infrastructure/Services/ContractService.cs` — `CreateContract` avulso provisiona + concede a admin existente.
- `IdentityManagement.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` — registrar novos servicos.

Frontend (modificar/criar):
- `IdentityManagement.Web/src/services/clientService.ts` (novo).
- `.../NewClientWizard/index.tsx`, `Step2Systems.tsx`, `Step3Review.tsx`.
- `.../Authentication/SetupAdmin/index.tsx`.

CI (criar): `system/identity-access-management/.github/workflows/tests.yml` (espelha os outros sistemas).

> Premissa confirmada: `ApplySystemRoleTemplates` cria a role `IsRoot` por contrato (ContractService.cs:368). Os SystemApplications-alvo precisam ter um template root configurado.

---

## Task 1: TenantNaming (slug + nome do banco + validacao)

**Files:**
- Create: `IdentityManagement.Infrastructure/Tenancy/TenantNaming.cs`
- Test: `IdentityManagement.Testing/Infrastructure/Tenancy/TenantNamingTests.cs`

- [ ] **Step 1: Escrever os testes que falham**

```csharp
using AgencyCampaign = IdentityManagement.Infrastructure.Tenancy;
using IdentityManagement.Infrastructure.Tenancy;

namespace IdentityManagement.Testing.Infrastructure.Tenancy
{
    [TestFixture]
    public sealed class TenantNamingTests
    {
        [TestCase("Mainstay ME", "mainstay")]
        [TestCase("  Construtora Silva ", "construtora")]
        [TestCase("José & Cia", "jose")]
        [TestCase("3M do Brasil", "3m")]
        [TestCase("açaí top", "acai")]
        public void Slugify_first_word_lowercase_no_accents(string input, string expected)
        {
            TenantNaming.Slugify(input).Should().Be(expected);
        }

        [Test]
        public void Slugify_empty_or_non_latin_falls_back_to_tenant()
        {
            TenantNaming.Slugify("   ").Should().Be("tenant");
            TenantNaming.Slugify("名前").Should().Be("tenant");
        }

        [Test]
        public void DatabaseName_builds_prefix_slug_id()
        {
            TenantNaming.DatabaseName("agency-campaign", "mainstay", 6).Should().Be("agencycampaign_mainstay_6");
            TenantNaming.DatabaseName("integration-platform", "mainstay", 6).Should().Be("integrationplatform_mainstay_6");
        }

        [Test]
        public void DatabaseName_truncates_slug_to_fit_63_bytes()
        {
            string longSlug = new string('a', 80);
            string name = TenantNaming.DatabaseName("agency-campaign", longSlug, 6);
            name.Length.Should().BeLessThanOrEqualTo(63);
            name.Should().StartWith("agencycampaign_").And.EndWith("_6");
        }

        [TestCase("agencycampaign_mainstay_6", true)]
        [TestCase("Robert'); DROP TABLE", false)]
        [TestCase("1bad", false)]
        [TestCase("with-hyphen", false)]
        public void IsValidIdentifier(string name, bool valid)
        {
            TenantNaming.IsValidIdentifier(name).Should().Be(valid);
        }
    }
}
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test IdentityManagement.Testing --filter TenantNamingTests`
Expected: FAIL (TenantNaming nao existe).

- [ ] **Step 3: Implementar TenantNaming**

```csharp
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace IdentityManagement.Infrastructure.Tenancy
{
    public static class TenantNaming
    {
        private const int MaxIdentifier = 63;
        private static readonly Regex ValidIdentifier = new("^[a-z][a-z0-9_]{0,62}$", RegexOptions.Compiled);

        public static string Slugify(string name)
        {
            string firstWord = (name ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            string decomposed = firstWord.Normalize(NormalizationForm.FormD);
            StringBuilder builder = new();
            foreach (char ch in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }
                char lower = char.ToLowerInvariant(ch);
                if (lower is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
                {
                    builder.Append(lower);
                }
            }
            string slug = builder.ToString();
            return slug.Length == 0 ? "tenant" : slug;
        }

        public static string DatabaseName(string audience, string slug, long companyId)
        {
            string prefix = (audience ?? string.Empty).Replace("-", string.Empty);
            string suffix = "_" + companyId.ToString(CultureInfo.InvariantCulture);
            int maxSlug = MaxIdentifier - prefix.Length - 1 - suffix.Length;
            string slugPart = maxSlug <= 0 ? string.Empty : slug[..Math.Min(slug.Length, maxSlug)];
            return slugPart.Length == 0 ? prefix + suffix : prefix + "_" + slugPart + suffix;
        }

        public static bool IsValidIdentifier(string name) => name is not null && ValidIdentifier.IsMatch(name);
    }
}
```

- [ ] **Step 4: Rodar e ver passar**

Run: `dotnet test IdentityManagement.Testing --filter TenantNamingTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add IdentityManagement.Infrastructure/Tenancy/TenantNaming.cs IdentityManagement.Testing/Infrastructure/Tenancy/TenantNamingTests.cs
git commit -m "feat: TenantNaming (slug, nome do banco e validacao de identificador)"
```

---

## Task 2: Scaffold IdentityManagement.IntegrationTests (Testcontainers)

Espelhar `system/agency-campaign-os/AgencyCampaign/AgencyCampaign.IntegrationTests` (mesmo padrao: `TestHarness` `[SetUpFixture]` sobe Postgres `postgres:16-alpine`, builda a DI real via `AddIdentityManagementInfrastructure` com `RunMigrations=true` e `TenantDatabases:test`).

**Files:**
- Create: `IdentityManagement.IntegrationTests/IdentityManagement.IntegrationTests.csproj`
- Create: `IdentityManagement.IntegrationTests/TestSupport/TestHarness.cs`
- Create: `IdentityManagement.IntegrationTests/TestSupport/IntegrationTestBase.cs`
- Create: `IdentityManagement.Testing/coverlet.runsettings` (se ainda nao existir)
- Modify: `IdentityManagement.slnx`

- [ ] **Step 1: csproj** — copiar de `AgencyCampaign.IntegrationTests.csproj` (net10, coverlet.collector 6.0.4, FluentAssertions 7, Microsoft.NET.Test.Sdk 17.14.0, NUnit 4.3.2, NUnit3TestAdapter 5.0.0, Testcontainers.PostgreSql 4.0.0), referenciando Domain/Application/Infrastructure do IdM.

- [ ] **Step 2: TestHarness/IntegrationTestBase** — copiar de AgencyCampaign, trocando `AddAgencyCampaignInfrastructure` por `AddIdentityManagementInfrastructure` (confirmar o nome em `IdentityManagement.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`), `DbName` = `identityintegrationtests`. TRUNCATE dinamico (DO $$) preservando `_migrations`. Adicionar registro de `ICurrentUser` test-double se algum servico exigir (verificar ao compilar/rodar).

- [ ] **Step 3: Registrar no .slnx** — adicionar `<Project Path="IdentityManagement.IntegrationTests/IdentityManagement.IntegrationTests.csproj" />`.

- [ ] **Step 4: Build local**

Run: `dotnet build IdentityManagement.IntegrationTests -c Release`
Expected: Build succeeded, 0 errors. (Os testes que usam Docker so rodam no CI.)

- [ ] **Step 5: Commit**

```bash
git add IdentityManagement.IntegrationTests IdentityManagement.slnx IdentityManagement.Testing/coverlet.runsettings
git commit -m "test: scaffold IdentityManagement.IntegrationTests (Testcontainers Postgres)"
```

---

## Task 3: PostgresTenantProvisioner (CREATE/DROP DATABASE, connection string, ApiKey)

**Files:**
- Create: `IdentityManagement.Application/Services/ITenantProvisioner.cs`
- Create: `IdentityManagement.Infrastructure/Services/PostgresTenantProvisioner.cs`
- Test: `IdentityManagement.IntegrationTests/Services/PostgresTenantProvisionerIntegrationTests.cs`

- [ ] **Step 1: Contrato**

```csharp
namespace IdentityManagement.Application.Services
{
    public interface ITenantProvisioner
    {
        string BuildTenantConnectionString(string databaseName);
        string GenerateApiKey();
        Task<bool> DatabaseExistsAsync(string databaseName, CancellationToken ct = default);
        Task CreateDatabaseAsync(string databaseName, CancellationToken ct = default);
        Task DropDatabaseAsync(string databaseName, CancellationToken ct = default);
    }
}
```

- [ ] **Step 2: Teste de integracao que falha**

```csharp
using IdentityManagement.Application.Services;
using IdentityManagement.IntegrationTests; // TestHarness
using Microsoft.Extensions.DependencyInjection;

namespace IdentityManagement.IntegrationTests.Services
{
    [TestFixture]
    public sealed class PostgresTenantProvisionerIntegrationTests : IntegrationTestBase
    {
        [Test]
        public async Task Creates_and_drops_a_real_database()
        {
            await InScopeAsync(async sp =>
            {
                ITenantProvisioner provisioner = sp.GetRequiredService<ITenantProvisioner>();
                string db = "it_provision_test_db";

                (await provisioner.DatabaseExistsAsync(db)).Should().BeFalse();
                await provisioner.CreateDatabaseAsync(db);
                (await provisioner.DatabaseExistsAsync(db)).Should().BeTrue();

                string cs = provisioner.BuildTenantConnectionString(db);
                cs.Should().Contain("Database=it_provision_test_db");

                await provisioner.DropDatabaseAsync(db);
                (await provisioner.DatabaseExistsAsync(db)).Should().BeFalse();
            });
        }

        [Test]
        public async Task CreateDatabase_rejects_invalid_identifier()
        {
            await InScopeAsync(async sp =>
            {
                ITenantProvisioner provisioner = sp.GetRequiredService<ITenantProvisioner>();
                Func<Task> act = () => provisioner.CreateDatabaseAsync("bad; DROP DATABASE x");
                await act.Should().ThrowAsync<ArgumentException>();
            });
        }
    }
}
```

- [ ] **Step 3: Implementar PostgresTenantProvisioner**

Usar a connection string do tenant atual do IdM (mesmo mecanismo de `IdentityManagementTenantResolver` — injetar `ITenantContext` ou a connection string self). Parsear com `NpgsqlConnectionStringBuilder`. `AssertValid` via `TenantNaming.IsValidIdentifier`. `CreateDatabaseAsync`: conectar em `Database=postgres` e `EXECUTE "CREATE DATABASE \"{name}\""`. `DropDatabaseAsync`: `DROP DATABASE IF EXISTS "{name}" WITH (FORCE)`. `GenerateApiKey`: `RandomNumberGenerator.GetBytes(32)` + Base64Url (mesmo padrao de `ContractService.GenerateOpaqueToken`). `DatabaseExistsAsync`: `SELECT 1 FROM pg_database WHERE datname=@n`.

```csharp
using System.Security.Cryptography;
using IdentityManagement.Application.Services;
using IdentityManagement.Infrastructure.Tenancy;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class PostgresTenantProvisioner : ITenantProvisioner
    {
        private readonly string adminConnectionString;
        private readonly string host;
        private readonly int port;
        private readonly string username;
        private readonly string password;

        public PostgresTenantProvisioner(/* self tenant connection string via DI */ string selfConnectionString)
        {
            NpgsqlConnectionStringBuilder builder = new(selfConnectionString);
            host = builder.Host!;
            port = builder.Port;
            username = builder.Username!;
            password = builder.Password!;
            NpgsqlConnectionStringBuilder admin = new(selfConnectionString) { Database = "postgres" };
            adminConnectionString = admin.ConnectionString;
        }

        public string BuildTenantConnectionString(string databaseName)
        {
            AssertValid(databaseName);
            return new NpgsqlConnectionStringBuilder
            {
                Host = host, Port = port, Database = databaseName, Username = username, Password = password
            }.ConnectionString;
        }

        public string GenerateApiKey() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

        public async Task<bool> DatabaseExistsAsync(string databaseName, CancellationToken ct = default)
        {
            await using NpgsqlConnection conn = new(adminConnectionString);
            await conn.OpenAsync(ct);
            await using NpgsqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = @n";
            cmd.Parameters.AddWithValue("n", databaseName);
            return await cmd.ExecuteScalarAsync(ct) is not null;
        }

        public async Task CreateDatabaseAsync(string databaseName, CancellationToken ct = default)
        {
            AssertValid(databaseName);
            await using NpgsqlConnection conn = new(adminConnectionString);
            await conn.OpenAsync(ct);
            await using NpgsqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task DropDatabaseAsync(string databaseName, CancellationToken ct = default)
        {
            AssertValid(databaseName);
            await using NpgsqlConnection conn = new(adminConnectionString);
            await conn.OpenAsync(ct);
            await using NpgsqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)";
            await cmd.ExecuteNonQueryAsync(ct);
        }

        private static void AssertValid(string databaseName)
        {
            if (!TenantNaming.IsValidIdentifier(databaseName))
            {
                throw new ArgumentException($"Invalid database identifier: {databaseName}");
            }
        }
    }
}
```

> Nota de DI (Task 8): registrar `ITenantProvisioner` resolvendo a connection string self do tenant (a mesma que o `IdentityManagementTenantResolver` usa). Confirmar a fonte exata ao implementar.

- [ ] **Step 4: Registrar no harness + rodar no CI** (o build local valida tipos; o teste real roda no CI com Docker). Commit.

```bash
git add IdentityManagement.Application/Services/ITenantProvisioner.cs IdentityManagement.Infrastructure/Services/PostgresTenantProvisioner.cs IdentityManagement.IntegrationTests/Services/PostgresTenantProvisionerIntegrationTests.cs
git commit -m "feat: PostgresTenantProvisioner (CREATE/DROP DATABASE + connection string + apikey)"
```

---

## Task 4: ContractAdminInvitation + CompanyId (entidade + migration + config)

**Files:**
- Modify: `IdentityManagement.Domain/Entities/ContractAdminInvitation.cs`
- Create: `IdentityManagement.Infrastructure/Migrations/Migration_<timestamp>_AddInvitationCompanyId.cs`
- Modify: `IdentityManagement.Infrastructure/Persistence/EF/Configurations/ContractAdminInvitationConfiguration.cs`

- [ ] **Step 1: Entidade** — adicionar `public long? CompanyId { get; private set; }` e um construtor `ContractAdminInvitation(long companyId, string token, DateTimeOffset expiresAt, bool companyScoped = true)` que seta `CompanyId` (e deixa `ContractId` 0). Manter o construtor por-contrato existente.

- [ ] **Step 2: Migration idempotente** (padrao do projeto, `Execute.Sql` com `IF NOT EXISTS`):

```csharp
[Migration(<timestamp>)]
public sealed class Migration_<timestamp>_AddInvitationCompanyId : Migration
{
    public override void Up() => Execute.Sql(@"
        ALTER TABLE contractadmininvitations ADD COLUMN IF NOT EXISTS companyid BIGINT NULL;
        CREATE INDEX IF NOT EXISTS ix_contractadmininvitations_companyid ON contractadmininvitations (companyid);
    ");
    public override void Down() => Execute.Sql(@"
        DROP INDEX IF EXISTS ix_contractadmininvitations_companyid;
        ALTER TABLE contractadmininvitations DROP COLUMN IF EXISTS companyid;
    ");
}
```

- [ ] **Step 3: EF config** — mapear `CompanyId` para coluna `companyid` (nullable).

- [ ] **Step 4: Build + commit.** (A migration roda no startup e no harness de integracao.)

```bash
git add IdentityManagement.Domain IdentityManagement.Infrastructure/Migrations IdentityManagement.Infrastructure/Persistence
git commit -m "feat: ContractAdminInvitation com escopo por empresa (companyid)"
```

---

## Task 5: Requests/Responses do onboarding + AdminInvitationInfoResponse

**Files:**
- Create: `IdentityManagement.Application/Requests/Clients/OnboardClientRequest.cs`
- Create: `IdentityManagement.Application/Responses/Clients/OnboardClientResponse.cs`
- Modify: `IdentityManagement.Application/Responses/Auth/AdminInvitationInfoResponse.cs`

- [ ] **Step 1: OnboardClientRequest**

```csharp
using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Clients
{
    public sealed class OnboardClientRequest
    {
        [Required] public string LegalName { get; set; } = string.Empty;
        [Required] public string TradeName { get; set; } = string.Empty;
        [Required] public string Document { get; set; } = string.Empty;
        [Required][EmailAddress] public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        [Required][MinLength(1)] public List<OnboardClientSystemItem> Systems { get; set; } = new();
    }

    public sealed class OnboardClientSystemItem
    {
        [Required][Range(1, long.MaxValue)] public long SystemApplicationId { get; set; }
        [Required] public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
    }
}
```

- [ ] **Step 2: OnboardClientResponse** — `{ long CompanyId; long[] ContractIds; string[] DatabaseNames; }`.

- [ ] **Step 3: AdminInvitationInfoResponse** — adicionar `public string[] SystemApplicationNames { get; set; } = Array.Empty<string>();` (manter `SystemApplicationName` legado).

- [ ] **Step 4: Build + commit.**

---

## Task 6: ClientOnboardingService (orquestracao + compensacao)

**Files:**
- Create: `IdentityManagement.Application/Services/IClientOnboardingService.cs`
- Create: `IdentityManagement.Infrastructure/Services/ClientOnboardingService.cs`
- Test: `IdentityManagement.IntegrationTests/Services/ClientOnboardingServiceIntegrationTests.cs`

- [ ] **Step 1: Contrato** — `Task<OnboardClientResponse> OnboardClient(OnboardClientRequest request, string setupBaseUrl, CancellationToken ct)`.

- [ ] **Step 2: Teste de integracao (caminho feliz + compensacao)**

```csharp
[Test]
public async Task OnboardClient_creates_company_contracts_databases_and_one_invitation()
{
    // seed 2 SystemApplications nao-centrais (audience agency-campaign / integration-platform) + role templates root
    // chamar OnboardClient com os 2 sistemas
    // assert: 1 company, 2 contracts, 2 tenantdatabases, 2 bancos existem (provisioner.DatabaseExistsAsync), 1 invitation company-scoped
}

[Test]
public async Task OnboardClient_compensates_on_provisioning_failure()
{
    // forcar falha no 2o CREATE DATABASE (ex.: provisioner fake que lanca no 2o)
    // assert: nenhuma company, nenhum contract, nenhum tenantdatabase, e o 1o banco criado foi DROPado
}
```

- [ ] **Step 3: Implementar** seguindo a secao 4.1 da spec: transacao EF (Company -> Contracts -> ApplySystemRoleTemplates -> registros TenantDatabase -> invitation company-scoped, SaveChanges sem commit); validar que nenhum sistema e o central (`audience == "identity-management"` -> erro); fase de `CreateDatabaseAsync` acumulando `createdDatabases`; commit; e-mail. Em falha: `DropDatabaseAsync` em cada `createdDatabases` + rollback + relancar. (Para o teste de compensacao, permitir injetar um `ITenantProvisioner` que falha no 2o create.)

- [ ] **Step 4: rodar no CI; commit.**

```bash
git commit -m "feat: ClientOnboardingService (onboarding transacional + compensacao de bancos)"
```

---

## Task 7: AuthService company-scoped (SetupAdmin + ValidateAdminInvitation)

**Files:**
- Modify: `IdentityManagement.Infrastructure/Services/AuthService.cs` (linhas 118-179)
- Test: `IdentityManagement.IntegrationTests/Services/SetupAdminCompanyScopedIntegrationTests.cs`

- [ ] **Step 1: Teste de integracao** — seed empresa + 2 contratos (cada um com root role) + 1 invitation company-scoped; chamar `SetupAdmin`; assert: 1 usuario criado, e `userroles` contem o root role dos 2 contratos.

- [ ] **Step 2: `ValidateAdminInvitation`** — se `CompanyId` setado, carregar a empresa + os contratos ativos (Include SystemApplication) e devolver `SystemApplicationNames` (lista) + `CompanyName` + `CompanyEmail`. Manter caminho legado por-contrato.

- [ ] **Step 3: `SetupAdmin`** — se company-scoped: `userService.Register` uma vez; para cada contrato ativo da empresa, buscar `Role` com `ContractId == c.Id && IsRoot` e adicionar `UserRole(user.Id, role.Id)`; marcar invitation usada; tudo na transacao ja existente. Legado por-contrato mantem o comportamento atual.

- [ ] **Step 4: rodar no CI; commit.**

---

## Task 8: E-mail consolidado (IEmailSender + ResendEmailSender)

**Files:**
- Modify: `IdentityManagement.Application/Services/IEmailSender.cs`
- Modify: `IdentityManagement.Infrastructure/Services/ResendEmailSender.cs`

- [ ] **Step 1:** adicionar `Task SendClientAdminInvitationEmailAsync(string toEmail, string companyName, IReadOnlyCollection<string> systemNames, string setupLink, CancellationToken ct = default);`.
- [ ] **Step 2:** implementar no `ResendEmailSender` (template lista os sistemas, em vez de um). Reutilizar o layout do `SendAdminInvitationEmailAsync` existente.
- [ ] **Step 3:** build + commit.

---

## Task 9: Endpoint + DI

**Files:**
- Create: `IdentityManagement.Api/Controllers/ClientsController.cs`
- Modify: `IdentityManagement.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`

- [ ] **Step 1: Controller** — `[PostEndpoint("onboard")]` `Onboard([FromBody] OnboardClientRequest request)` com `[RequireAccess(...)]` de administracao (espelhar o usado em `ContractsController`), resolvendo `setupBaseUrl` da config (igual ao `ResendInvitation`). Chama `IClientOnboardingService.OnboardClient`.
- [ ] **Step 2: DI** — registrar `IClientOnboardingService -> ClientOnboardingService`, `ITenantProvisioner -> PostgresTenantProvisioner` (com a connection string self do tenant). Como o auto-discovery do Archon pega `*Service` em namespace `*Services`, `ClientOnboardingService` entra sozinho; `PostgresTenantProvisioner` (sufixo `Provisioner`) precisa de registro manual.
- [ ] **Step 3: build + commit.**

---

## Task 10: CreateContract avulso provisiona + concede a admin existente

**Files:**
- Modify: `IdentityManagement.Infrastructure/Services/ContractService.cs` (`CreateContract`, `SendAdminInvitation`)
- Test: `IdentityManagement.IntegrationTests/Services/AddSystemLaterIntegrationTests.cs`

- [ ] **Step 1: Teste** — empresa que JA tem admin (invitation usada) + novo `CreateContract`: assert que o admin existente recebeu o root role do novo contrato e NENHUM novo convite/usuario foi criado. E empresa SEM admin + novo `CreateContract`: assert convite pendente company-scoped.
- [ ] **Step 2: Implementar** conforme secao 4.6 da spec (provisiona banco; se ha admin -> grant; senao -> garante invitation company-scoped). Reutilizar `ITenantProvisioner`.
- [ ] **Step 3: rodar no CI; commit.**

---

## Task 11: Frontend — clientService + wizard

**Files:**
- Create: `IdentityManagement.Web/src/services/clientService.ts`
- Modify: `.../NewClientWizard/index.tsx`, `Step2Systems.tsx`, `Step3Review.tsx`

- [ ] **Step 1: clientService** — `onboard(payload)` chamando `POST /api/clients/onboard` (seguir o padrao dos outros services em `src/services/`, usar o httpClient/useApi existentes).
- [ ] **Step 2: `SystemSelection`** — remover `connectionString`, `databaseProvider`, `schemaName`, `apiKey`. `Step2Systems` deixa so selecao de sistema (excluir o de audience `identity-management`) + datas. `canProceedStep2` valida so `selectedSystems.length > 0 && startDate`.
- [ ] **Step 3: `handleSubmit`** — substituir o bloco `CompanyService.create` + loop por uma chamada unica `ClientService.onboard({ ...companyData, systems: selectedSystems.map(s => ({ systemApplicationId, startDate, endDate })) })`. `Step3Review` remove infra.
- [ ] **Step 4: build do frontend** (`npm run build`) + commit.

---

## Task 12: Frontend — SetupAdmin lista sistemas

**Files:**
- Modify: `IdentityManagement.Web/src/modules/Authentication/SetupAdmin/index.tsx`

- [ ] **Step 1:** consumir `SystemApplicationNames` do `ValidateAdminInvitation` e exibir "voce vai administrar: {lista}". Manter o resto do fluxo de setup (username/senha/nome).
- [ ] **Step 2:** build + commit.

---

## Task 13: CI — tests.yml do IdM

**Files:**
- Create: `system/identity-access-management/.github/workflows/tests.yml`

- [ ] **Step 1:** espelhar o `tests.yml` do agency/integration (checkout IdM + archon-framework via GH_PAT, setup .NET 10, roda `IdentityManagement.Testing` + `IdentityManagement.IntegrationTests` com `--collect "XPlat Code Coverage"` + `coverlet.runsettings`, reportgenerator com a % combinada Domain/Application/Infrastructure).
- [ ] **Step 2:** commit + push; acompanhar o run (Testcontainers roda os testes de provisionamento/onboarding de verdade).

---

## Self-Review

- **Spec coverage:** nucleo (Task 6), provisioner (Task 3), naming (Task 1), convite/setup (Tasks 4,7), e-mail (Task 8), endpoint/DI (Task 9), add-later (Task 10), frontend (Tasks 11,12), testes (Tasks 2,3,6,7,10,13). Coberto.
- **Placeholders:** os passos de backend core trazem codigo real; frontend e wiring referenciam arquivos/patterns existentes (mecanico). Sem TBD.
- **Type consistency:** `ITenantProvisioner`/`PostgresTenantProvisioner`, `IClientOnboardingService`/`ClientOnboardingService`, `OnboardClientRequest`/`OnboardClientSystemItem`, `TenantNaming.{Slugify,DatabaseName,IsValidIdentifier}`, `AdminInvitationInfoResponse.SystemApplicationNames`, `SendClientAdminInvitationEmailAsync` — usados consistentemente.
- **Riscos a confirmar na execucao:** nome exato de `AddIdentityManagementInfrastructure`; fonte da connection string self pro provisioner; se algum servico do IdM exige `ICurrentUser` no harness; se os SystemApplications-alvo tem template root.
