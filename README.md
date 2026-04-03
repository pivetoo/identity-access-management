# Identity Access Management

Sistema de gerenciamento de identidade e acesso construído sobre o `Archon`.

## Estrutura

O backend está organizado em:

- `IdentityManagement.Api`
  host HTTP, controllers, autenticação local JWT e integração com `Archon.Api`.
- `IdentityManagement.Application`
  contratos, requests, responses e interfaces de services.
- `IdentityManagement.Domain`
  entidades e regras de negócio do domínio.
- `IdentityManagement.Infrastructure`
  persistência, mappings, migrations e implementações dos services.
- `IdentityManagement.Testing`
  projeto reservado para testes.

## Principais capacidades

- autenticação por usuário e senha;
- seleção de contrato quando o usuário possui mais de um contrato ativo;
- emissão de `access token`, `refresh token` e token temporário para seleção de contrato;
- autorização por roles e recursos de acesso;
- sincronização automática de recursos protegidos vindos das APIs que usam `Archon`;
- auditoria automática via `Archon`;
- migrations com `FluentMigrator`;
- documentação OpenAPI com `Scalar`.

## Fluxo de autenticação

O fluxo principal funciona assim:

1. o usuário informa `username` e `password`;
2. se existir apenas um contrato ativo, a API conclui o login e retorna os tokens;
3. se existir mais de um contrato ativo, a API retorna `authenticationStep = "contractSelection"` e um token temporário;
4. o cliente escolhe o contrato e chama o endpoint de login com contrato;
5. a API retorna o login final com `authenticationStep = "completed"`.

`SystemApplication.Type` influencia o comportamento do redirect:

- `External`
  permite montar `redirectUrl` para callback;
- `Internal`
  não gera redirect automático.

## Autorização

O sistema usa o padrão do `Archon`:

- claim `permission`
- claim `root=true`

Os endpoints protegidos usam `[RequireAccess]`, que resolve o acesso no formato:

- `controller.action`

Exemplo:

- `UsersController.Create` -> `users.create`

## Sync de recursos de acesso

As APIs construídas com `Archon` podem sincronizar automaticamente seus recursos com o IAM.

Endpoint receptor:

- `POST /api/access-resources/sync`

Proteção:

- header `X-Integration-Secret`

Configuração necessária no IAM:

```json
{
  "IntegrationSecret": "SUA_CHAVE_DE_INTEGRACAO"
}
```

O sync:

- cria recursos novos;
- atualiza recursos existentes;
- reativa recursos que voltarem a existir;
- inativa recursos que deixarem de ser enviados.

## Configuração

Exemplo mínimo de `appsettings.Development.json`:

```json
{
  "TenantDatabases": {
    "default": {
      "CompanyName": "Identity Management",
      "ApplicationId": "identity-management",
      "ConnectionString": "Host=localhost;Port=5432;Database=identitymanagement;Username=postgres;Password=postgres;",
      "DatabaseType": "PostgreSql",
      "Schema": "public"
    }
  },
  "Jwt": {
    "Issuer": "IdentityManagement",
    "Audience": "IdentityManagement",
    "JwtSecretKey": "SUA_CHAVE_LOCAL_DE_64_CARACTERES",
    "TemporarySecretKey": "SUA_CHAVE_TEMPORARIA_DE_64_CARACTERES"
  },
  "IntegrationSecret": "SUA_CHAVE_DE_INTEGRACAO",
  "RunMigrations": true
}
```

Observações:

- `Schema` é usado tanto pelo runtime quanto pelas migrations;
- `JwtSecretKey` valida os tokens da própria API;
- `TemporarySecretKey` assina o token temporário de seleção de contrato;
- não versionar credenciais reais no repositório.

## Migrations

O projeto roda migrations de dois assemblies:

- `Archon.Infrastructure`
  tabelas base do framework, incluindo auditoria;
- `IdentityManagement.Infrastructure`
  tabelas do domínio do IAM.

As tabelas e colunas estão padronizadas em lowercase.

## Documentação da API

Em ambiente de desenvolvimento:

- OpenAPI é exposto pela aplicação;
- `Scalar` fica disponível em `/scalar`.

O `launchSettings.json` já está configurado para abrir o navegador diretamente nessa rota.

## Endpoints principais

Auth:

- `POST /api/auth/identify`
- `POST /api/auth/loginwithcontract`
- `POST /api/auth/refreshtoken`
- `POST /api/auth/logout`
- `POST /api/auth/changepassword`
- `GET /api/auth/getuserbyusername/{username}`

Administração:

- `Users`
- `Companies`
- `SystemApplications`
- `Contracts`
- `Roles`
- `UserRoles`
- `AccessResources`

## Status atual

O backend está com a fundação principal montada:

- domínio;
- infraestrutura;
- camada de API;
- autenticação;
- autorização;
- migrations;
- sync de recursos;
- integração com `Archon`.