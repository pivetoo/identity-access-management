# Planos e Assinaturas (Billing) - Parte 1 (backend)

Data: 2026-06-05. Sistema: IdentityManagement.

## Objetivo

Modelar planos de assinatura e assinaturas por empresa (tenant) no IdM, com transicoes
de status explicitas e um "assento" reservado para o gateway de pagamento (sem implementar
o provedor real ainda). Base para o modelo SaaS mensal da Mainstay/Kanvas.

## Escopo decidido

- Parte 1 e SO BACKEND (entidades + API + seam do gateway + testes de integracao).
- Plano enxuto: preco/periodicidade/trial. SEM limites/features (YAGNI; entram quando o gating exigir).
- Gating de acesso (suspenso -> bloqueia tenant) e FOLLOW-UP, fora da Parte 1.
- Tela admin e FOLLOW-UP (Parte 1b).

## Onde vive

IdentityManagement (dono de companies/contracts/tenants/acesso).

## Entidades

`Plan` (tabela `plans`): Name, Description?, PriceAmount (decimal), Currency (default "BRL"),
BillingPeriod (VO Monthly/Yearly), TrialDays (int, default 0), IsActive (default true).

`Subscription` (tabela `subscriptions`): CompanyId (FK companies), PlanId (FK plans),
Status (VO Trialing/Active/PastDue/Suspended/Canceled), StartedAt, TrialEndsAt?,
CurrentPeriodStart, CurrentPeriodEnd, CanceledAt?, e os ganchos do gateway:
ExternalCustomerId?, ExternalSubscriptionId?, ProviderName?.

## Maquina de estados (transicoes explicitas na Subscription)

StartTrial, Activate, MarkPastDue, Suspend, Cancel, Renew. Cada uma valida o status atual
e ajusta datas. Sao os pontos que o webhook do gateway vai disparar no futuro; hoje um admin
dispara via API.

## Assento do gateway (sem provedor real)

- Interface `IBillingGateway` (CreateCustomer/CreateSubscription/CancelSubscription) +
  `NoopBillingGateway` registrado no DI. O SubscriptionService chama a interface nos pontos
  certos, mas hoje e no-op.
- Controller de webhook placeholder `POST /api/billing/webhook` (valida/roteia, corpo vazio).
- Campos External* ja no modelo.

## API

`PlansController` (CRUD de planos). `SubscriptionsController` (atribuir plano a empresa,
trocar plano, cancelar, consultar por empresa). `BillingWebhookController` (placeholder).

## Notas tecnicas

- Timestamps TIMESTAMPTZ (Entity base usa DateTimeOffset).
- Nao compor operacoes do CrudService dentro de transacao EF externa (ver gotcha do Archon).
- VOs seguem o padrao de Domain/ValueObjects (ApplicationType/Language).
- Migration 202605190004.

## Fora de escopo (follow-ups)

Gating de acesso por status; tela admin; integracao real do gateway (Asaas/Iugu/Pagar.me) +
recorrencia/dunning/webhook handler; PIX.
