# Identity Management

Identity Management é uma plataforma de identidade e acesso multi-tenant, criada para centralizar autenticação, autorização, gestão de usuários, contratos e clientes OAuth/OpenID Connect para aplicações corporativas.

O projeto foi desenvolvido como parte de um ecossistema de aplicações baseado no framework Archon, com foco em arquitetura limpa, separação de responsabilidades, login federado, seleção de contrato e integração entre sistemas.

## Visão Geral

Em ambientes corporativos, um mesmo usuário pode acessar mais de uma empresa, contrato ou sistema. Este projeto resolve esse fluxo oferecendo:

- login centralizado por usuário e senha;
- central de sistemas para escolha do contrato/aplicação;
- emissão de tokens OAuth2/OIDC com PKCE;
- suporte a access token, id token e refresh token;
- descoberta OIDC via `/.well-known/openid-configuration`;
- publicação de chaves públicas via JWKS;
- administração de usuários, empresas, sistemas, contratos, roles e permissões;
- integração com APIs que usam Archon para sincronização de recursos protegidos.

## Principais Features

- **OpenID Connect Authorization Code + PKCE**
  Fluxo padrão para SPAs e aplicações públicas, com validação de `client_id`, `redirect_uri`, `scope`, `state`, `nonce` e `code_challenge`.

- **Central de Sistemas**
  Quando o usuário acessa diretamente o Identity Management, ele visualiza os contratos disponíveis e entra na aplicação vinculada ao OAuth Client padrão daquele sistema.

- **Seleção de Contrato no Login**
  Quando o login é iniciado por uma aplicação, o Identity Management filtra os contratos pelo sistema do `client_id` solicitante e completa o authorize com o contrato escolhido.

- **OAuth Clients Administráveis**
  Cadastro de clients públicos, confidenciais e machine-to-machine, com redirect URIs de login/logout, scopes, lifetimes, PKCE, rotação de refresh token e flag de client padrão.

- **JWT Assinado com RSA**
  Tokens são assinados com chaves RSA gerenciadas pelo próprio Identity Management e publicadas via JWKS.

- **Autorização por Contrato e Role**
  O token carrega contexto de usuário, contrato, tenant, sistema, empresa e role, permitindo autorização contextual nas APIs consumidoras.

- **Refresh Token com Rotação**
  Suporte a renovação de sessão com revogação do token anterior.

- **Sincronização de Recursos**
  APIs integradas podem enviar seus recursos protegidos para o Identity Management, mantendo a matriz de permissões centralizada.

## Fluxos de Autenticação

### 1. Login iniciado pela aplicação

Este é o fluxo esperado quando uma aplicação protegida, por exemplo Integration Platform, redireciona o usuário para autenticar.

1. A aplicação monta uma URL `/connect/authorize` com seu próprio `client_id`.
2. O usuário é enviado para `/login?returnUrl=...`.
3. O Identity Management autentica usuário e senha.
4. A tela exibe apenas os contratos ligados ao sistema do `client_id`.
5. Ao selecionar o contrato, o frontend chama `/api/oidc/complete-authorize`.
6. O Identity Management emite o authorization code.
7. A aplicação recebe o callback e troca o code por tokens em `/connect/token`.

### 2. Login iniciado pela central de sistemas

Este fluxo acontece quando o usuário acessa diretamente a tela de login do Identity Management.

1. O usuário acessa `/login` sem `returnUrl`.
2. O Identity Management autentica usuário e senha.
3. A central de sistemas lista os contratos disponíveis.
4. Ao selecionar um contrato, o usuário é enviado para a aplicação do OAuth Client padrão daquele sistema.
5. A aplicação inicia o authorize com o próprio `client_id`.
6. O Identity Management completa o authorize usando o contrato escolhido.

Esse desenho evita que o PKCE seja criado no domínio errado. O `code_verifier` precisa existir no storage da aplicação que receberá o callback.

### 3. Login com sessão existente do Identity Management (SSO)

Acontece quando o usuário já está autenticado na SPA do Identity Management e abre outra aplicação em nova aba.

1. A aplicação redireciona para `/connect/authorize` e o Identity Management devolve `/login?returnUrl=...`, como no fluxo 1.
2. A tela de login detecta o `returnUrl` de authorize e uma sessão válida no `localStorage`, e chama `POST /api/auth/IdentifySession` com o token atual, em vez de mostrar o formulário.
3. O backend valida o usuário do token, cria a `PendingAuthorizationSession` e devolve os contratos do sistema solicitante, igual ao `Identify`, só que sem senha.
4. Com um contrato, o `complete-authorize` é chamado direto e o usuário chega à aplicação sem ver tela nenhuma. Com vários, a central de sistemas pede a empresa.

Sem sessão válida, o fluxo cai no formulário de senha normalmente. As sessões de cada aplicação continuam independentes: sair de uma não encerra as outras.

## Arquitetura

O backend segue uma organização em camadas:

```text
IdentityManagement/
  IdentityManagement.Api/             Host HTTP, controllers, CORS, auth e OpenAPI
  IdentityManagement.Application/     Requests, responses, contratos e interfaces
  IdentityManagement.Domain/          Entidades e regras do domínio
  IdentityManagement.Infrastructure/  EF Core, migrations, services e persistência
  IdentityManagement.Testing/         Projeto de testes
  IdentityManagement.Web/             SPA administrativa e telas de autenticação
```

## Stack

Backend:

- .NET
- ASP.NET Core
- Entity Framework Core
- FluentMigrator
- JWT Bearer Authentication
- RSA/JWKS
- BCrypt
- Scalar/OpenAPI

Frontend:

- React
- TypeScript
- Vite
- React Router
- Tailwind CSS
- Archon UI

Infra:

- PostgreSQL
- Docker Compose para deploy
- NGINX/reverse proxy em produção

## Módulos Administrativos

A interface web possui telas para:

- dashboard;
- usuários;
- empresas;
- sistemas;
- contratos;
- roles;
- vínculo de usuários a roles;
- templates de roles por sistema;
- OAuth Clients;
- recursos de acesso.

## Endpoints OIDC

Endpoints públicos principais:

- `GET /.well-known/openid-configuration`
- `GET /.well-known/jwks.json`
- `GET /connect/authorize`
- `POST /connect/token`
- `GET|POST /connect/userinfo`
- `POST /connect/revocation`
- `GET /connect/logout`

Endpoint usado pelo login centralizado:

- `POST /api/oidc/complete-authorize`

## Configuração

Exemplo simplificado de configuração local:

```json
{
  "TenantDatabases": {
    "default": {
      "CompanyName": "Identity Management",
      "TenantId": "00000000-0000-0000-0000-000000000000",
      "ConnectionString": "Host=localhost;Port=5432;Database=identitymanagement;Username=postgres;Password=postgres;",
      "DatabaseType": "PostgreSql",
      "Schema": "public"
    }
  },
  "Oidc": {
    "Issuer": "https://localhost:7211",
    "LoginPageUrl": "http://localhost:5173/login"
  },
  "Jwt": {
    "Issuer": "identity-management",
    "Audience": "identity-management"
  },
  "IntegrationSecret": "local-development-secret",
  "RunMigrations": true
}
```

Variáveis comuns do frontend:

```env
VITE_API_BASE_URL=https://localhost:7211/api
VITE_IDENTITY_MANAGEMENT_URL=https://localhost:7211
VITE_OIDC_CLIENT_ID=identity-management-dev
```

Não versionar credenciais reais, connection strings produtivas ou secrets.

## Como Rodar Localmente

Backend:

```bash
cd IdentityManagement
dotnet restore
dotnet run --project IdentityManagement.Api/IdentityManagement.Api.csproj
```

Frontend:

```bash
cd IdentityManagement/IdentityManagement.Web
npm install
npm run dev
```

Build:

```bash
dotnet build IdentityManagement/IdentityManagement.Api/IdentityManagement.Api.csproj
cd IdentityManagement/IdentityManagement.Web
npm run build
```

## Deploy

O diretório `deploy/` possui um `docker-compose.prod.yml` com dois serviços:

- `api`, expondo a API internamente;
- `web`, servindo a SPA.

Em produção, o reverse proxy deve encaminhar:

- `/api/` para a API;
- `/connect/` para a API;
- `/.well-known/` para a API;
- demais rotas para o frontend.

## Pontos de Destaque Técnico

- Separação entre Identity Provider e aplicações clientes.
- Suporte a múltiplos OAuth Clients por sistema.
- Client padrão por sistema para entrada pela central.
- Validação de redirect URI por client.
- Tokens enriquecidos com contexto de contrato.
- Discovery OIDC compatível com consumidores externos.
- Integração com Archon Framework para validação de tokens e recursos.
- Fluxo desenhado para evitar problemas de PKCE entre domínios diferentes.

## Status

O projeto está funcional como provedor de identidade para o ecossistema Mainstay/Archon, incluindo login centralizado, central de sistemas, OIDC, administração de contratos e integração com aplicações consumidoras.
