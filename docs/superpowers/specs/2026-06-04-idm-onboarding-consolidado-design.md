# Onboarding consolidado de cliente + auto-provisionamento de tenant (IdentityManagement)

Data: 2026-06-04
Status: aprovado (design); aguarda plano de implementacao

## 1. Contexto e problema

Hoje, criar um cliente novo no IdM passa pelo `NewClientWizard` (frontend) que, no `handleSubmit`
(`IdentityManagement.Web/src/modules/Administration/NewClientWizard/index.tsx`):

1. Cria a empresa (`CompanyService.create`).
2. Para CADA sistema selecionado, em loop: cria o contrato (`ContractService.create`) e logo um
   registro de banco de tenant (`TenantDatabaseService.create`), com o operador colando
   manualmente connection string, schema, provider e API key.

No backend, `ContractService.CreateContract` (`IdentityManagement.Infrastructure/Services/ContractService.cs:25`)
dispara `SendAdminInvitation` (linha 58) a cada contrato. `SendAdminInvitation` (linha 422) cria um
`ContractAdminInvitation` (token de 7 dias, por contrato) e manda um e-mail via Resend
(`emailSender.SendAdminInvitationEmailAsync`, por sistema) para `company.Email`.

No aceite, `AuthService.SetupAdmin` (`IdentityManagement.Infrastructure/Services/AuthService.cs:139`)
chama `userService.Register(...)` que cria um USUARIO NOVO e atribui o root role do contrato do convite.

### Dores

- **Multiplos e-mails / multiplos usuarios.** Um cliente com 2 sistemas (ex.: Integrations + Agencias)
  recebe 2 e-mails separados, cada um com seu setup. Como cada `SetupAdmin` chama `userService.Register`,
  o resultado sao 2 usuarios distintos (ou o 2o setup quebra por username/e-mail duplicado). O objetivo
  do cliente e ter UM unico admin com acesso aos dois sistemas.
- **Provisionamento manual.** Connection string e API key sao digitadas pelo operador; o banco do tenant
  e criado manualmente fora do fluxo.
- **Wizard nao-atomico.** Cria empresa e depois itera contratos/bancos sem transacao. Se o 2o sistema
  falha, sobra empresa meia-criada (1 contrato + 1 e-mail enviado, resto nao).

O login ja suporta multi-contrato: `AuthService.IdentifyUser` devolve uma selecao de contratos, entao
um usuario com role em varios contratos ja escolhe o sistema no login. Falta consolidar convite + setup.

## 2. Objetivo e escopo

**Sub-projeto A (esta spec):** onboarding consolidado admin-driven com auto-provisionamento de banco.
Um unico caso de uso de backend cria empresa + contratos + bancos de tenant numa operacao, gera UM
convite por empresa e UM setup que resulta em UM admin com root role em todos os sistemas do cliente.

**Fora de escopo (Sub-projeto B, futuro):** endpoint publico de self-service pela landing page,
verificacao de e-mail, rate limit / anti-abuso, selecao de plano / trial / billing, e a validacao de que
os sistemas-alvo migram corretamente um tenant adicionado em runtime. O design de A expoe o nucleo
(`IClientOnboardingService`) de forma que B reuse o mesmo servico.

## 3. Decisoes confirmadas

- Naming do banco: `{prefixo}_{slug}_{companyId}`, ex.: `agencycampaign_mainstay_6`,
  `integrationplatform_mainstay_6`. O id garante unicidade; o slug e legivel.
- `prefixo` deriva de `SystemApplication.Audience` sem hifen: `agency-campaign` -> `agencycampaign`,
  `integration-platform` -> `integrationplatform`.
- `appdatabase` tem privilegio `CREATEDB`; o IdM reusa a credencial do proprio tenant para provisionar.
- Os sistemas-alvo rodam as proprias migrations (o IdM cria o banco vazio + registra o tenant). A
  verificacao de que isso funciona em runtime fica para depois (fora de escopo).
- Destinatario do convite: `company.Email` (o admin preenche nome/usuario/senha no setup). Sem campo de
  e-mail de admin dedicado nesta rodada.
- Apenas sistemas que usam banco de tenant sao ofertados/provisionados. O Authenticator (`SystemApplication`
  audience `identity-management`) e central, tenant unico (Mainstay), e NAO entra na selecao do wizard.
- Falha no provisionamento: DROP apenas nos bancos criados nesta tentativa + rollback (compensacao).

## 4. Design — Backend

### 4.1 `ClientOnboardingService` (orquestracao)

Novo: `IClientOnboardingService` (Application) + `ClientOnboardingService` (Infrastructure).

Assinatura conceitual:

```
Task<OnboardClientResponse> OnboardClient(OnboardClientRequest request, string setupBaseUrl, CancellationToken ct)
```

`OnboardClientRequest`:
- Dados da empresa: `LegalName`, `TradeName`, `Document`, `Email`, `PhoneNumber`.
- `Systems`: lista de `{ SystemApplicationId, StartDate, EndDate? }` (sem connection string / api key — auto).

Orquestracao:
1. Abre transacao EF (`BeginTransactionAsync`).
2. Cria `Company` (SaveChanges -> id atribuido pelo EF).
3. `slug = TenantNaming.Slugify(TradeName se preenchido, senao LegalName)` (a extracao da primeira
   palavra acontece dentro de `Slugify`).
4. Para cada sistema em `Systems`:
   a. Valida que o `SystemApplication` existe, esta ativo e NAO e o central (audience `identity-management`).
   b. Cria `Contract(companyId, systemApplicationId)` + `Update(datas, ativo)` + `Insert`.
   c. `ApplySystemRoleTemplates(contract.Id, systemApplicationId)` (ja existente — gera o root role).
   d. `dbName = TenantNaming.DatabaseName(systemApp.Audience, slug, companyId)`.
   e. `apiKey = TenantSecrets.GenerateApiKey()`.
   f. `connectionString = provisioner.BuildTenantConnectionString(dbName)`.
   g. Cria registro `TenantDatabase(contract.Id, connectionString, PostgreSql, apiKey, "public")`.
   h. Acumula `dbName` numa lista `plannedDatabases`.
5. Cria UM `ContractAdminInvitation` company-scoped (ver 4.5) e o `setupLink`.
6. `SaveChanges` (ainda dentro da transacao, sem commit).
7. **Fase de provisionamento** (fora da transacao EF): para cada `dbName` em `plannedDatabases`,
   `provisioner.CreateDatabase(dbName)`, acumulando em `createdDatabases`.
8. Se tudo ok: `transaction.Commit()` e depois `emailSender.SendClientAdminInvitationEmailAsync(...)`
   (UM e-mail, lista de sistemas).
9. Se qualquer passo de 7 ou o commit falhar: para cada `dbName` em `createdDatabases`,
   `provisioner.DropDatabase(dbName)`; `transaction.Rollback()`; relanca o erro. Sem e-mail.

### 4.2 `ITenantProvisioner` (Postgres)

Novo: `ITenantProvisioner` (Application) + `PostgresTenantProvisioner` (Infrastructure), isolando toda a
interacao com o Postgres administrativo. Depende da connection string do proprio tenant do IdM
(`ITenantContext`/self tenant, hoje usado em `IdentityManagementTenantResolver`).

- `BuildTenantConnectionString(string dbName)`: parseia a connection string do IdM
  (`NpgsqlConnectionStringBuilder`) -> Host, Port, Username, Password; devolve
  `Host=...;Port=...;Database={dbName};Username=...;Password=...;`.
- `CreateDatabase(string dbName)`: `AssertValidIdentifier(dbName)`; abre `NpgsqlConnection` na base de
  manutencao (`Database=postgres`, mesmas credenciais); executa `CREATE DATABASE "{dbName}"`.
- `DropDatabase(string dbName)`: `AssertValidIdentifier(dbName)`; `DROP DATABASE IF EXISTS "{dbName}" WITH (FORCE)`.
- `DatabaseExists(string dbName)`: consulta `pg_database` (defensivo, antes do create).

**Seguranca (critico):** `CREATE/DROP DATABASE` sao DDL e nao aceitam parametro; o nome e interpolado.
`AssertValidIdentifier` exige `^[a-z][a-z0-9_]{0,62}$` e lanca se nao casar. Como o nome vem de
`audience` (controlado) + `slug` (sanitizado, ver 4.3) + `companyId` (numero), o nome e sempre seguro;
a assercao e a trava final contra injecao via nome de empresa.

### 4.3 Naming / slug — `TenantNaming`

- `Slugify(string name)`: pega a primeira palavra (split por espaco), minusculo, remove acento
  (normalizacao Unicode FormD + descarte de diacriticos), mantem so `[a-z0-9]`, descarta o resto.
  Se resultar vazio (nome em outro alfabeto), fallback `"tenant"`.
- `DatabaseName(string audience, string slug, long companyId)`:
  - `prefix = audience.Replace("-", "")`.
  - `suffix = "_" + companyId`.
  - Trunca `slug` para caber em 63 bytes: `maxSlug = 63 - prefix.Length - 1 - suffix.Length` (o `-1` do
    separador entre prefix e slug). Se `maxSlug <= 0`, usa so `prefix + suffix`.
  - Retorna `prefix + "_" + slugTruncado + suffix`.

### 4.4 Atomicidade e compensacao

Ja descrito em 4.1 (passos 1, 6, 7, 9). Pontos-chave:
- Toda a escrita relacional (Company, Contracts, TenantDatabase, Invitation) fica numa transacao EF e so
  commita apos os `CREATE DATABASE` darem certo.
- `CREATE DATABASE` roda fora da transacao (limitacao do Postgres), em conexao separada na base `postgres`.
- Em falha, dropa SOMENTE os bancos rastreados em `createdDatabases` (nunca pre-existentes), via
  `DROP DATABASE IF EXISTS ... WITH (FORCE)`, e faz rollback. E-mail so apos commit.
- Se o proprio DROP falhar (raro): logar em nivel de erro com o nome do banco para limpeza manual; nao
  engolir silenciosamente.

### 4.5 Convite por empresa

`ContractAdminInvitation` (`IdentityManagement.Domain/Entities/ContractAdminInvitation.cs`) ganha
`CompanyId` (nullable para compat). O fluxo novo cria a invitation com `CompanyId` setado.

- `AuthService.ValidateAdminInvitation(token)` passa a devolver `CompanyName` + LISTA de sistemas
  (`SystemApplicationNames`) dos contratos ativos da empresa. `AdminInvitationInfoResponse` ganha
  `SystemApplicationNames: string[]` (mantem `SystemApplicationName` legado opcional).
- `AuthService.SetupAdmin(request)`: resolve a invitation por token; registra o usuario UMA vez
  (`userService.Register`); para CADA contrato ativo da empresa, busca o root role
  (`Role.ContractId == contract.Id && IsRoot`) e cria um `UserRole(user.Id, rootRole.Id)`; marca a
  invitation como usada. Tudo numa transacao (ja existe `BeginTransactionAsync` no metodo).
- Convite legado (apenas `ContractId`): mantem o comportamento atual (grant do root role daquele
  contrato), para nao quebrar convites em transito. Como pre-lancamento tem dados minimos, o risco e baixo.

### 4.6 Adicionar sistema depois

`ContractService.CreateContract` (fluxo avulso, fora do wizard) passa a:
- Provisionar o banco do tenant (mesma rotina do provisioner) em vez de exigir connection string manual.
- Se a empresa JA tem admin (existe invitation usada / usuario com root role em algum contrato da
  empresa): concede o root role do novo contrato ao admin existente (cria `UserRole`), sem novo setup;
  e-mail de aviso opcional ("voce agora tem acesso ao {sistema}").
- Se a empresa ainda NAO tem admin: garante/atualiza um convite pendente company-scoped (o setup atribui
  todos os contratos ativos no momento do aceite, entao cobre o novo automaticamente).

### 4.7 Endpoint

Novo `ClientsController` (ou metodo em controller existente de Administration):
- `POST /api/clients/onboard` (autenticado, `[RequireAccess]` de administracao) -> `IClientOnboardingService.OnboardClient`.
- O `setupBaseUrl` resolve como hoje (config, igual ao usado em `ContractsController` para `ResendInvitation`).

## 5. Design — Frontend

`NewClientWizard`:
- **Step2Systems**: remove os campos de infra (connection string, API key, schema, provider). Passa a
  ser apenas: selecionar sistemas (excluindo o Authenticator central) + start/end date por sistema.
- **Step3Review**: lista empresa + sistemas + datas (sem infra).
- **`handleSubmit`**: uma unica chamada `ClientService.onboard({ company, systems })` em vez de
  `CompanyService.create` + loop de `ContractService.create` + `TenantDatabaseService.create`.
- `SystemSelection` perde `connectionString`, `databaseProvider`, `schemaName`, `apiKey`.

`SetupAdmin` (`IdentityManagement.Web/src/modules/Authentication/SetupAdmin/index.tsx`): exibe
"voce vai administrar: {lista de sistemas}" usando `SystemApplicationNames` do `ValidateAdminInvitation`.

## 6. E-mail

`IEmailSender` ganha `SendClientAdminInvitationEmailAsync(string toEmail, string companyName,
IReadOnlyCollection<string> systemNames, string setupLink, CancellationToken ct)`. O template (Resend,
`ResendEmailSender`) lista os sistemas contratados em vez de um unico. O `SendAdminInvitationEmailAsync`
por sistema permanece apenas para o caminho legado/avulso, se necessario.

## 7. Arquivos afetados (estimativa)

Backend:
- Novos: `Application/Services/IClientOnboardingService.cs`, `Infrastructure/Services/ClientOnboardingService.cs`,
  `Application/Services/ITenantProvisioner.cs`, `Infrastructure/Services/PostgresTenantProvisioner.cs`,
  `Infrastructure/Tenancy/TenantNaming.cs` (slug/dbname), `Api/Controllers/ClientsController.cs`,
  requests/responses (`OnboardClientRequest`, `OnboardClientResponse`).
- Alterados: `ContractAdminInvitation.cs` (+ `CompanyId`), `AuthService.cs`
  (`ValidateAdminInvitation`, `SetupAdmin`), `AdminInvitationInfoResponse.cs` (+ `SystemApplicationNames`),
  `IEmailSender.cs` + `ResendEmailSender.cs` (novo metodo), `ContractService.cs` (`CreateContract` avulso
  -> provisiona + grant a admin existente), DI (`ServiceCollectionExtensions`).

Frontend:
- `NewClientWizard/index.tsx`, `Step2Systems.tsx`, `Step3Review.tsx`, novo `services/clientService.ts`,
  `SetupAdmin/index.tsx`, tipos.

## 8. Migrations

- `contractadmininvitations`: `ADD COLUMN IF NOT EXISTS companyid BIGINT NULL` + FK para `companies(id)` +
  indice. Idempotente (padrao do projeto: `Execute.Sql` com `IF NOT EXISTS`).
- Sem mudanca em `tenantdatabases` (a entidade ja tem connection string + apikey + schema).

## 9. Tratamento de erros / edge cases

- Slug vazio apos sanitizacao -> fallback `tenant` (o id mantem unicidade).
- `dbName` ja existe em `pg_database` (colisao improvavel por causa do id) -> erro explicito antes do
  create; nao dropar (nao foi criado por esta tentativa).
- Sistema central (`identity-management`) na lista -> rejeitar com erro de validacao.
- Sistema duplicado / contrato ativo duplicado -> reaproveita a checagem existente
  (`contract.activeDuplicate`).
- `userService.Register` com username/e-mail ja existente no setup -> erro de validacao (o admin escolhe
  outro), sem criar usuario parcial (ja roda em transacao).

## 10. Testes

- Unit: `TenantNaming.Slugify` (acentos, espacos, alfabeto nao-latino, truncamento, fallback) e
  `DatabaseName` (limite de 63, montagem do prefixo). `AssertValidIdentifier` (rejeita injecao).
- Integracao (Testcontainers Postgres, no espirito da suite do AgencyCampaign): `PostgresTenantProvisioner`
  cria/dropa banco de verdade; `ClientOnboardingService` com falha simulada no 2o `CreateDatabase` deixa o
  servidor limpo (sem empresa, sem banco) — valida a compensacao.
- Integracao: `SetupAdmin` company-scoped cria UM usuario com root role em todos os contratos ativos.

## 11. Fora de escopo (Sub-projeto B)

- Endpoint publico de self-service (landing), verificacao de e-mail, rate limit / anti-abuso.
- Selecao de plano / trial / billing.
- Validacao de que os sistemas-alvo migram um tenant adicionado em runtime (sera verificado depois).
- Rotacao / ciclo de vida de API key de tenant.

## 12. Interface reutilizavel para a landing (B)

`IClientOnboardingService.OnboardClient` e o nucleo. O endpoint autenticado desta spec e um futuro
endpoint publico chamam o MESMO servico; B adiciona, por fora, verificacao de e-mail, rate limit e
selecao de plano antes de invocar o nucleo.
