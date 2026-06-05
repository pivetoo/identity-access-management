import { useEffect, useState } from 'react'
import { CreditCard, Plus, RefreshCw, XCircle, PauseCircle, PlayCircle } from 'lucide-react'
import { Badge, Button, Card, CardContent, CardHeader, CardTitle, ConfirmModal, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, toast, useApi } from 'archon-ui'
import { SubscriptionService } from '../../../services/subscriptionService'
import { PlanService } from '../../../services/planService'
import type { Subscription } from '../../../types/subscription'
import type { Plan } from '../../../types/plan'
import { SubscriptionStatus } from '../../../types/subscription'
import { formatDate } from '../../../utils/date'

interface ClientSubscriptionTabProps {
  companyId: number
  refreshKey: number
}

type BadgeVariant = 'success' | 'info' | 'warning' | 'destructive' | 'secondary'

const statusLabel = (status: SubscriptionStatus): string => {
  if (status === SubscriptionStatus.Trialing) { return 'Em trial' }
  if (status === SubscriptionStatus.Active) { return 'Ativo' }
  if (status === SubscriptionStatus.PastDue) { return 'Inadimplente' }
  if (status === SubscriptionStatus.Suspended) { return 'Suspenso' }
  return 'Cancelado'
}

const statusVariant = (status: SubscriptionStatus): BadgeVariant => {
  if (status === SubscriptionStatus.Active) { return 'success' }
  if (status === SubscriptionStatus.Trialing) { return 'info' }
  if (status === SubscriptionStatus.PastDue) { return 'warning' }
  return 'destructive'
}

export default function ClientSubscriptionTab({ companyId, refreshKey }: ClientSubscriptionTabProps) {
  const [subscription, setSubscription] = useState<Subscription | null | undefined>(undefined)
  const [activePlans, setActivePlans] = useState<Plan[]>([])
  const [isAssignOpen, setIsAssignOpen] = useState(false)
  const [isChangePlanOpen, setIsChangePlanOpen] = useState(false)
  const [selectedPlanId, setSelectedPlanId] = useState<number | null>(null)
  const [confirmAction, setConfirmAction] = useState<'cancel' | 'suspend' | null>(null)

  const loadApi = useApi({
    onSuccess: (data: { subscription: Subscription | null; plans: Plan[] }) => {
      setSubscription(data.subscription)
      setActivePlans(data.plans)
    },
  })

  const assignApi = useApi({
    onSuccess: () => {
      toast({ variant: 'success', title: 'Sucesso', description: 'Assinatura atribuída com sucesso.' })
      setIsAssignOpen(false)
      setSelectedPlanId(null)
      reload()
    },
    onError: (error) => {
      toast({ title: 'Erro', description: error.message, variant: 'destructive' })
    },
  })

  const changePlanApi = useApi({
    onSuccess: () => {
      toast({ variant: 'success', title: 'Sucesso', description: 'Plano alterado com sucesso.' })
      setIsChangePlanOpen(false)
      setSelectedPlanId(null)
      reload()
    },
    onError: (error) => {
      toast({ title: 'Erro', description: error.message, variant: 'destructive' })
    },
  })

  const actionApi = useApi({
    onSuccess: () => {
      toast({ variant: 'success', title: 'Sucesso', description: 'Operação realizada com sucesso.' })
      setConfirmAction(null)
      reload()
    },
    onError: (error) => {
      toast({ title: 'Erro', description: error.message, variant: 'destructive' })
      setConfirmAction(null)
    },
  })

  const activateApi = useApi({
    onSuccess: () => {
      toast({ variant: 'success', title: 'Sucesso', description: 'Assinatura reativada.' })
      reload()
    },
    onError: (error) => {
      toast({ title: 'Erro', description: error.message, variant: 'destructive' })
    },
  })

  const reload = () => {
    loadApi.execute(async () => {
      const [sub, plans] = await Promise.all([
        SubscriptionService.getByCompany(companyId),
        PlanService.getActive(),
      ])
      return { subscription: sub, plans }
    })
  }

  useEffect(() => {
    reload()
  }, [companyId, refreshKey])

  const handleAssign = async () => {
    if (!selectedPlanId) {
      return
    }
    await assignApi.execute(() => SubscriptionService.assign(companyId, selectedPlanId))
  }

  const handleChangePlan = async () => {
    if (!selectedPlanId) {
      return
    }
    await changePlanApi.execute(() => SubscriptionService.changePlan(companyId, selectedPlanId))
  }

  const handleConfirmAction = async () => {
    if (!confirmAction) {
      return
    }
    if (confirmAction === 'cancel') {
      await actionApi.execute(() => SubscriptionService.cancel(companyId))
    } else {
      await actionApi.execute(() => SubscriptionService.suspend(companyId))
    }
  }

  const handleReactivate = async () => {
    await activateApi.execute(() => SubscriptionService.activate(companyId))
  }

  if (subscription === undefined && loadApi.isLoading) {
    return (
      <Card>
        <CardContent className="py-8">
          <div className="h-24 animate-pulse rounded-lg bg-muted/30" />
        </CardContent>
      </Card>
    )
  }

  return (
    <>
      <Card>
        <CardHeader className="flex flex-row items-start justify-between gap-4">
          <div>
            <CardTitle className="flex items-center gap-2">
              <CreditCard className="h-4 w-4 text-primary" />
              Assinatura
            </CardTitle>
            <p className="text-sm text-muted-foreground">
              Plano ativo e status de cobrança do cliente.
            </p>
          </div>
          {!subscription && (
            <Button variant="primary" size="sm" onClick={() => setIsAssignOpen(true)}>
              <Plus className="mr-2 h-4 w-4" />
              Atribuir plano
            </Button>
          )}
        </CardHeader>
        <CardContent>
          {!subscription ? (
            <div className="rounded-lg border border-dashed border-border bg-muted/20 py-8 text-center">
              <p className="text-sm font-medium text-foreground">Sem assinatura</p>
              <p className="mt-1 text-xs text-muted-foreground">Este cliente ainda não possui um plano ativo.</p>
            </div>
          ) : (
            <div className="space-y-5">
              <dl className="grid gap-x-6 gap-y-4 sm:grid-cols-2 lg:grid-cols-3">
                <Field label="Plano" value={subscription.planName} />
                <Field label="Status" value={<Badge variant={statusVariant(subscription.status)}>{statusLabel(subscription.status)}</Badge>} />
                {subscription.status === SubscriptionStatus.Trialing && subscription.trialEndsAt && (
                  <Field label="Trial até" value={formatDate(subscription.trialEndsAt)} />
                )}
                <Field label="Início" value={formatDate(subscription.startedAt)} />
                <Field label="Período atual" value={`${formatDate(subscription.currentPeriodStart)} → ${formatDate(subscription.currentPeriodEnd)}`} />
                {subscription.canceledAt && <Field label="Cancelado em" value={formatDate(subscription.canceledAt)} />}
                {subscription.providerName && <Field label="Provedor" value={subscription.providerName} />}
              </dl>

              <div className="flex flex-wrap gap-2 border-t border-border pt-4">
                <Button variant="outline" size="sm" onClick={() => { setSelectedPlanId(null); setIsChangePlanOpen(true) }}>
                  <RefreshCw className="mr-2 h-4 w-4" />
                  Trocar plano
                </Button>

                {subscription.status === SubscriptionStatus.Suspended ? (
                  <Button variant="outline" size="sm" onClick={handleReactivate} loading={activateApi.isLoading}>
                    <PlayCircle className="mr-2 h-4 w-4" />
                    Reativar
                  </Button>
                ) : subscription.status !== SubscriptionStatus.Canceled ? (
                  <>
                    <Button variant="outline" size="sm" onClick={() => setConfirmAction('suspend')}>
                      <PauseCircle className="mr-2 h-4 w-4" />
                      Suspender
                    </Button>
                    <Button variant="danger" size="sm" onClick={() => setConfirmAction('cancel')}>
                      <XCircle className="mr-2 h-4 w-4" />
                      Cancelar
                    </Button>
                  </>
                ) : null}
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      <PlanSelectModal
        isOpen={isAssignOpen}
        onClose={() => { setIsAssignOpen(false); setSelectedPlanId(null) }}
        title="Atribuir plano"
        description="Selecione o plano que será atribuído a este cliente."
        plans={activePlans}
        selectedPlanId={selectedPlanId}
        onSelectPlan={setSelectedPlanId}
        onConfirm={handleAssign}
        loading={assignApi.isLoading}
        confirmLabel="Atribuir"
      />

      <PlanSelectModal
        isOpen={isChangePlanOpen}
        onClose={() => { setIsChangePlanOpen(false); setSelectedPlanId(null) }}
        title="Trocar plano"
        description="Selecione o novo plano para este cliente."
        plans={activePlans}
        selectedPlanId={selectedPlanId}
        onSelectPlan={setSelectedPlanId}
        onConfirm={handleChangePlan}
        loading={changePlanApi.isLoading}
        confirmLabel="Trocar"
      />

      <ConfirmModal
        open={!!confirmAction}
        onOpenChange={(open) => !open && setConfirmAction(null)}
        onConfirm={handleConfirmAction}
        title={confirmAction === 'cancel' ? 'Cancelar assinatura' : 'Suspender assinatura'}
        description={confirmAction === 'cancel' ? 'Tem certeza que deseja cancelar a assinatura deste cliente? Esta ação não pode ser desfeita.' : 'Deseja suspender a assinatura deste cliente?'}
        confirmText={confirmAction === 'cancel' ? 'Cancelar assinatura' : 'Suspender'}
        variant="danger"
        loading={actionApi.isLoading}
      />
    </>
  )
}

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <dt className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{label}</dt>
      <dd className="mt-1 text-sm text-foreground">{value}</dd>
    </div>
  )
}

interface PlanSelectModalProps {
  isOpen: boolean
  onClose: () => void
  title: string
  description: string
  plans: Plan[]
  selectedPlanId: number | null
  onSelectPlan: (id: number) => void
  onConfirm: () => void
  loading: boolean
  confirmLabel: string
}

function PlanSelectModal({ isOpen, onClose, title, description, plans, selectedPlanId, onSelectPlan, onConfirm, loading, confirmLabel }: PlanSelectModalProps) {
  const billingLabel = (period: number) => period === 1 ? 'Mensal' : 'Anual'
  const priceLabel = (plan: Plan) => new Intl.NumberFormat('pt-BR', { style: 'currency', currency: plan.currency }).format(plan.priceAmount)

  return (
    <Modal open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <ModalContent size="md">
        <ModalHeader>
          <ModalTitle>{title}</ModalTitle>
        </ModalHeader>
        <div className="py-4">
          <p className="mb-4 text-sm text-muted-foreground">{description}</p>
          {plans.length === 0 ? (
            <p className="text-sm text-muted-foreground">Nenhum plano ativo disponível.</p>
          ) : (
            <div className="space-y-2">
              {plans.map((plan) => (
                <button
                  key={plan.id}
                  type="button"
                  onClick={() => onSelectPlan(plan.id)}
                  className={`w-full rounded-lg border px-4 py-3 text-left transition-colors ${selectedPlanId === plan.id ? 'border-primary bg-primary/5' : 'border-border hover:border-primary/40 hover:bg-muted/30'}`}
                >
                  <div className="flex items-center justify-between">
                    <span className="text-sm font-semibold text-foreground">{plan.name}</span>
                    <span className="text-sm font-medium text-primary">{priceLabel(plan)}/{billingLabel(plan.billingPeriod)}</span>
                  </div>
                  {plan.description && <p className="mt-0.5 text-xs text-muted-foreground">{plan.description}</p>}
                  {plan.trialDays > 0 && <p className="mt-0.5 text-xs text-muted-foreground">{plan.trialDays} dias de trial</p>}
                </button>
              ))}
            </div>
          )}
        </div>
        <ModalFooter>
          <Button variant="outline" onClick={onClose} disabled={loading}>Cancelar</Button>
          <Button variant="primary" onClick={onConfirm} loading={loading} disabled={!selectedPlanId}>{confirmLabel}</Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  )
}
