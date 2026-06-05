import { useEffect, useState } from 'react'
import { Badge, Button, ConfirmModal, DataTable, Input, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, PageLayout, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, toast, useApi } from 'archon-ui'
import type { DataTableColumn, PaginatedResult } from 'archon-ui'
import { PlanService } from '../../../services/planService'
import type { Plan, CreatePlanRequest, UpdatePlanRequest } from '../../../types/plan'
import { BillingPeriod } from '../../../types/plan'

const billingPeriodLabel = (period: number) => period === BillingPeriod.Monthly ? 'Mensal' : 'Anual'

const formatCurrency = (amount: number, currency = 'BRL') =>
  new Intl.NumberFormat('pt-BR', { style: 'currency', currency }).format(amount)

export default function Plans() {
  const [plans, setPlans] = useState<Plan[]>([])
  const [selectedPlans, setSelectedPlans] = useState<Plan[]>([])
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingPlan, setEditingPlan] = useState<Plan | undefined>()
  const [isConfirmToggleOpen, setIsConfirmToggleOpen] = useState(false)
  const [toggleTarget, setToggleTarget] = useState<Plan | null>(null)
  const [hasMore, setHasMore] = useState(true)
  const pageSize = 30

  const loadApi = useApi({
    onSuccess: (data: PaginatedResult<Plan>) => {
      setPlans((prev) => [...prev, ...data.data])
      setHasMore(data.data.length === pageSize)
    },
  })

  const loadMoreApi = useApi({
    onSuccess: (data: PaginatedResult<Plan>) => {
      setPlans((prev) => [...prev, ...data.data])
      setHasMore(data.data.length === pageSize)
    },
  })

  const toggleApi = useApi({
    onSuccess: () => {
      toast({ variant: 'success', title: 'Sucesso', description: toggleTarget?.isActive ? 'Plano desativado.' : 'Plano ativado.' })
      setIsConfirmToggleOpen(false)
      setToggleTarget(null)
      setSelectedPlans([])
      loadPlans(true)
    },
    onError: () => setIsConfirmToggleOpen(false),
  })

  const loadPlans = async (reset = false) => {
    if (reset) {
      setPlans([])
    }
    await loadApi.execute(() => PlanService.getAll({ page: 1, pageSize, orderBy: 'id' }))
  }

  const loadMore = async () => {
    const currentPage = Math.floor(plans.length / pageSize) + 1
    await loadMoreApi.execute(() => PlanService.getAll({ page: currentPage + 1, pageSize, orderBy: 'id' }))
  }

  useEffect(() => {
    loadPlans(true)
  }, [])

  const handleAdd = () => {
    setEditingPlan(undefined)
    setIsFormOpen(true)
  }

  const handleEdit = () => {
    if (selectedPlans.length !== 1) {
      toast({ variant: 'warning', title: 'Atenção', description: selectedPlans.length === 0 ? 'Selecione um plano para editar.' : 'Selecione apenas um plano para editar.' })
      return
    }
    setEditingPlan(selectedPlans[0])
    setIsFormOpen(true)
  }

  const handleToggleStatus = (plan: Plan) => {
    setToggleTarget(plan)
    setIsConfirmToggleOpen(true)
  }

  const handleConfirmToggle = async () => {
    if (!toggleTarget) {
      return
    }
    if (toggleTarget.isActive) {
      await toggleApi.execute(() => PlanService.deactivate(toggleTarget.id))
    } else {
      await toggleApi.execute(() => PlanService.activate(toggleTarget.id))
    }
  }

  const columns: DataTableColumn<Plan>[] = [
    { key: 'name', title: 'Nome', dataIndex: 'name', sortable: true },
    {
      key: 'priceAmount',
      title: 'Preço',
      dataIndex: 'priceAmount',
      render: (value: number, record: Plan) => formatCurrency(value, record.currency),
    },
    {
      key: 'billingPeriod',
      title: 'Período',
      dataIndex: 'billingPeriod',
      render: (value: number) => billingPeriodLabel(value),
    },
    {
      key: 'trialDays',
      title: 'Trial (dias)',
      dataIndex: 'trialDays',
      render: (value: number) => (value > 0 ? `${value}d` : '-'),
    },
    {
      key: 'isActive',
      title: 'Status',
      dataIndex: 'isActive',
      render: (value: boolean, record: Plan) => (
        <div className="flex items-center gap-2">
          <Badge variant={value ? 'success' : 'destructive'}>{value ? 'Ativo' : 'Inativo'}</Badge>
          <Button variant="outline" size="sm" onClick={(e) => { e.stopPropagation(); handleToggleStatus(record) }}>
            {value ? 'Desativar' : 'Ativar'}
          </Button>
        </div>
      ),
    },
  ]

  return (
    <>
      <PageLayout
        title="Planos"
        subtitle="Gerencie os planos de assinatura disponíveis para os clientes."
        onAdd={handleAdd}
        onEdit={handleEdit}
        onRefresh={() => loadPlans(true)}
        selectedRowsCount={selectedPlans.length}
      >
        <DataTable
          columns={columns}
          data={plans}
          loading={loadApi.isLoading || toggleApi.isLoading}
          rowKey="id"
          selectable
          selectedRows={selectedPlans}
          onSelectionChange={setSelectedPlans}
        />

        {hasMore && (
          <div className="mt-4 flex justify-end">
            <Button variant="outline" onClick={loadMore} loading={loadMoreApi.isLoading}>
              Carregar mais
            </Button>
          </div>
        )}

        <ConfirmModal
          open={isConfirmToggleOpen}
          onOpenChange={setIsConfirmToggleOpen}
          onConfirm={handleConfirmToggle}
          title={toggleTarget?.isActive ? 'Desativar plano' : 'Ativar plano'}
          description={toggleTarget?.isActive ? `Deseja desativar o plano "${toggleTarget?.name}"?` : `Deseja ativar o plano "${toggleTarget?.name}"?`}
          confirmText={toggleTarget?.isActive ? 'Desativar' : 'Ativar'}
          variant={toggleTarget?.isActive ? 'danger' : undefined}
          loading={toggleApi.isLoading}
        />
      </PageLayout>

      <PlanFormModal
        isOpen={isFormOpen}
        onClose={() => { setIsFormOpen(false); setEditingPlan(undefined) }}
        plan={editingPlan}
        onSuccess={() => { setSelectedPlans([]); loadPlans(true) }}
      />
    </>
  )
}

interface PlanFormModalProps {
  isOpen: boolean
  onClose: () => void
  plan?: Plan
  onSuccess: () => void
}

const defaultForm = {
  name: '',
  description: '',
  priceAmount: 0,
  currency: 'BRL',
  billingPeriod: BillingPeriod.Monthly as BillingPeriod,
  trialDays: 0,
}

function PlanFormModal({ isOpen, onClose, plan, onSuccess }: PlanFormModalProps) {
  const [form, setForm] = useState(defaultForm)

  const saveApi = useApi({
    onSuccess: () => {
      toast({ variant: 'success', title: 'Sucesso', description: plan ? 'Plano atualizado.' : 'Plano criado.' })
      onSuccess()
      onClose()
    },
    onError: (error) => {
      toast({ title: 'Erro', description: error.message, variant: 'destructive' })
    },
  })

  useEffect(() => {
    if (!isOpen) {
      return
    }
    if (plan) {
      setForm({
        name: plan.name,
        description: plan.description ?? '',
        priceAmount: plan.priceAmount,
        currency: plan.currency,
        billingPeriod: plan.billingPeriod,
        trialDays: plan.trialDays,
      })
    } else {
      setForm(defaultForm)
    }
  }, [isOpen, plan])

  const set = (field: string, value: string | number) => setForm((prev) => ({ ...prev, [field]: value }))

  const handleSave = async () => {
    if (plan) {
      const request: UpdatePlanRequest = {
        name: form.name,
        priceAmount: Number(form.priceAmount),
        billingPeriod: form.billingPeriod,
        currency: form.currency,
        trialDays: Number(form.trialDays),
        description: form.description || undefined,
      }
      await saveApi.execute(() => PlanService.update(plan.id, request))
    } else {
      const request: CreatePlanRequest = {
        name: form.name,
        priceAmount: Number(form.priceAmount),
        billingPeriod: form.billingPeriod,
        currency: form.currency || undefined,
        trialDays: Number(form.trialDays) || undefined,
        description: form.description || undefined,
      }
      await saveApi.execute(() => PlanService.create(request))
    }
  }

  const isValid = form.name.trim() && Number(form.priceAmount) >= 0

  return (
    <Modal open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <ModalContent size="lg">
        <ModalHeader>
          <ModalTitle>{plan ? 'Editar plano' : 'Novo plano'}</ModalTitle>
        </ModalHeader>

        <div className="flex flex-col gap-4 py-4">
          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">Nome *</label>
            <Input value={form.name} onChange={(e) => set('name', e.target.value)} placeholder="Ex: Profissional" />
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">Descrição</label>
            <Input value={form.description} onChange={(e) => set('description', e.target.value)} placeholder="Descrição opcional do plano" />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">Preço *</label>
              <Input type="number" min={0} step={0.01} value={form.priceAmount} onChange={(e) => set('priceAmount', e.target.value)} />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">Moeda</label>
              <Input value={form.currency} onChange={(e) => set('currency', e.target.value.toUpperCase())} placeholder="BRL" maxLength={3} />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">Período de cobrança</label>
              <Select value={form.billingPeriod.toString()} onValueChange={(v) => set('billingPeriod', Number(v))}>
                <SelectTrigger>
                  <SelectValue placeholder="Selecione o período" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={BillingPeriod.Monthly.toString()}>Mensal</SelectItem>
                  <SelectItem value={BillingPeriod.Yearly.toString()}>Anual</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">Trial (dias)</label>
              <Input type="number" min={0} value={form.trialDays} onChange={(e) => set('trialDays', e.target.value)} placeholder="0" />
            </div>
          </div>
        </div>

        <ModalFooter>
          <Button variant="outline" onClick={onClose} disabled={saveApi.isLoading}>Cancelar</Button>
          <Button variant="primary" onClick={handleSave} loading={saveApi.isLoading} disabled={!isValid}>Salvar</Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  )
}
