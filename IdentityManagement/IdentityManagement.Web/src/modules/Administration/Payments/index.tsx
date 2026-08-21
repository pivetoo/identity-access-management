import { useEffect, useState } from 'react'
import { Badge, Button, DataTable, Modal, ModalContent, ModalHeader, ModalTitle, PageLayout, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, Tabs, TabsContent, TabsList, TabsTrigger, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Check, Copy, Receipt, Webhook, WrapText } from 'lucide-react'
import { PaymentService } from '../../../services/paymentService'
import type { Payment, WebhookEvent } from '../../../types/payment'
import { PaymentStatus } from '../../../types/payment'
import { formatDate, formatDateTime } from '../../../utils/date'

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value)

const billingTypeLabel = (type?: string): string => {
  if (!type) {
    return '-'
  }
  const map: Record<string, string> = {
    PIX: 'PIX',
    BOLETO: 'Boleto',
    CREDIT_CARD: 'Cartao',
  }
  return map[type] ?? type
}

type BadgeVariant = 'warning' | 'info' | 'success' | 'destructive' | 'default'

const statusConfig: Record<number, { label: string; variant: BadgeVariant }> = {
  [PaymentStatus.Pending]: { label: 'Pendente', variant: 'warning' },
  [PaymentStatus.Confirmed]: { label: 'Confirmado', variant: 'info' },
  [PaymentStatus.Received]: { label: 'Recebido', variant: 'success' },
  [PaymentStatus.Overdue]: { label: 'Atrasado', variant: 'warning' },
  [PaymentStatus.Refunded]: { label: 'Estornado', variant: 'destructive' },
  [PaymentStatus.ChargebackRequested]: { label: 'Chargeback', variant: 'destructive' },
  [PaymentStatus.Deleted]: { label: 'Removido', variant: 'destructive' },
}

const STATUS_ALL = 'all'

export default function Payments() {
  const [activeTab, setActiveTab] = useState('charges')
  const [payments, setPayments] = useState<Payment[]>([])
  const [events, setEvents] = useState<WebhookEvent[]>([])
  const [statusFilter, setStatusFilter] = useState<string>(STATUS_ALL)
  const [payloadEvent, setPayloadEvent] = useState<WebhookEvent | null>(null)
  const [payloadFormatted, setPayloadFormatted] = useState(true)
  const [payloadCopied, setPayloadCopied] = useState(false)

  const paymentsApi = useApi({
    onSuccess: (data: Payment[]) => setPayments(data),
  })

  const eventsApi = useApi({
    onSuccess: (data: WebhookEvent[]) => setEvents(data),
  })

  const loadPayments = (status?: number) => {
    paymentsApi.execute(() =>
      PaymentService.list(status !== undefined ? { status: status as Payment['status'] } : undefined)
    )
  }

  const loadEvents = () => {
    eventsApi.execute(() => PaymentService.listWebhookEvents())
  }

  useEffect(() => {
    loadPayments()
    loadEvents()
  }, [])

  const handleStatusFilter = (value: string) => {
    setStatusFilter(value)
    if (value === STATUS_ALL) {
      loadPayments()
    } else {
      loadPayments(Number(value))
    }
  }

  const handleRefresh = () => {
    const status = statusFilter !== STATUS_ALL ? Number(statusFilter) : undefined
    loadPayments(status)
    loadEvents()
  }

  const paymentColumns: DataTableColumn<Payment>[] = [
    {
      key: 'company',
      title: 'Empresa',
      dataIndex: 'companyName',
      render: (_: unknown, record: Payment) =>
        record.companyName || record.externalSubscriptionId || '-',
    },
    {
      key: 'value',
      title: 'Valor',
      dataIndex: 'value',
      render: (value: number) => formatCurrency(value),
    },
    {
      key: 'billingType',
      title: 'Metodo',
      dataIndex: 'billingType',
      render: (value?: string) => billingTypeLabel(value),
    },
    {
      key: 'status',
      title: 'Status',
      dataIndex: 'status',
      render: (value: number) => {
        const cfg = statusConfig[value]
        if (!cfg) {
          return <Badge variant="default">{value}</Badge>
        }
        return <Badge variant={cfg.variant}>{cfg.label}</Badge>
      },
    },
    {
      key: 'dueDate',
      title: 'Vencimento',
      dataIndex: 'dueDate',
      render: (value?: string) => formatDate(value),
    },
    {
      key: 'paidDate',
      title: 'Pago em',
      dataIndex: 'paidDate',
      render: (value?: string) => (value ? formatDate(value) : '-'),
    },
  ]

  const eventColumns: DataTableColumn<WebhookEvent>[] = [
    {
      key: 'processedAt',
      title: 'Data',
      dataIndex: 'processedAt',
      render: (value: string) => formatDateTime(value),
    },
    {
      key: 'eventType',
      title: 'Tipo',
      dataIndex: 'eventType',
    },
    {
      key: 'externalPaymentId',
      title: 'Cobranca',
      dataIndex: 'externalPaymentId',
      render: (value?: string) => value || '-',
    },
    {
      key: 'outcome',
      title: 'Resultado',
      dataIndex: 'outcome',
      render: (value?: string) => value || '-',
    },
    {
      key: 'actions',
      title: '',
      dataIndex: 'rawPayload',
      render: (_: unknown, record: WebhookEvent) =>
        record.rawPayload ? (
          <Button variant="secondary" size="sm" onClick={(e) => { e.stopPropagation(); setPayloadEvent(record) }}>
            Ver payload
          </Button>
        ) : null,
    },
  ]

  const copyPayload = async () => {
    const text = payloadFormatted ? prettyPayload(payloadEvent?.rawPayload) : payloadEvent?.rawPayload
    if (!text) {
      return
    }

    await navigator.clipboard.writeText(text)
    setPayloadCopied(true)
    window.setTimeout(() => setPayloadCopied(false), 2000)
  }

  const prettyPayload = (raw?: string): string => {
    if (!raw) {
      return ''
    }
    try {
      return JSON.stringify(JSON.parse(raw), null, 2)
    } catch {
      return raw
    }
  }

  return (
    <>
      <PageLayout
        title="Pagamentos"
        subtitle="Acompanhe cobranças e eventos de webhook do gateway de pagamento."
        onRefresh={handleRefresh}
        showDefaultActions={false}
      >
        <Tabs value={activeTab} onValueChange={setActiveTab} className="pt-2">
          <TabsList variant="underline" className="mb-6">
            <TabsTrigger value="charges">
              <Receipt className="h-4 w-4" /> Cobranças
            </TabsTrigger>
            <TabsTrigger value="webhook-events">
              <Webhook className="h-4 w-4" /> Eventos de webhook
            </TabsTrigger>
          </TabsList>

          <TabsContent value="charges" className="mt-0">
            <div className="mb-4 flex items-center gap-3">
              <Select value={statusFilter} onValueChange={handleStatusFilter}>
                <SelectTrigger className="w-48">
                  <SelectValue placeholder="Filtrar por status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={STATUS_ALL}>Todos</SelectItem>
                  <SelectItem value={String(PaymentStatus.Pending)}>Pendente</SelectItem>
                  <SelectItem value={String(PaymentStatus.Confirmed)}>Confirmado</SelectItem>
                  <SelectItem value={String(PaymentStatus.Received)}>Recebido</SelectItem>
                  <SelectItem value={String(PaymentStatus.Overdue)}>Atrasado</SelectItem>
                  <SelectItem value={String(PaymentStatus.Refunded)}>Estornado</SelectItem>
                  <SelectItem value={String(PaymentStatus.ChargebackRequested)}>Chargeback</SelectItem>
                  <SelectItem value={String(PaymentStatus.Deleted)}>Removido</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <DataTable
              columns={paymentColumns}
              data={payments}
              loading={paymentsApi.isLoading}
              rowKey="id"
              emptyText="Nenhuma cobranca encontrada."
            />
          </TabsContent>

          <TabsContent value="webhook-events" className="mt-0">
            <DataTable
              columns={eventColumns}
              data={events}
              loading={eventsApi.isLoading}
              rowKey="id"
              emptyText="Nenhum evento de webhook encontrado."
            />
          </TabsContent>
        </Tabs>
      </PageLayout>

      <Modal
        open={!!payloadEvent}
        onOpenChange={(open) => {
          if (!open) {
            setPayloadEvent(null)
            setPayloadFormatted(true)
            setPayloadCopied(false)
          }
        }}
      >
        <ModalContent size="3xl">
          <ModalHeader>
            <ModalTitle>Payload do evento</ModalTitle>
          </ModalHeader>

          <div className="mb-3 flex flex-wrap gap-2">
            <Button
              variant="outline"
              size="sm"
              icon={<WrapText />}
              iconPosition="left"
              onClick={() => setPayloadFormatted((current) => !current)}
            >
              {payloadFormatted ? 'Ver original' : 'Formatar JSON'}
            </Button>
            <Button
              variant="outline"
              size="sm"
              icon={payloadCopied ? <Check /> : <Copy />}
              iconPosition="left"
              onClick={copyPayload}
            >
              {payloadCopied ? 'Copiado' : 'Copiar'}
            </Button>
          </div>

          <div className="max-h-[60vh] overflow-auto rounded-md bg-muted p-4">
            <pre className="font-mono text-xs text-foreground whitespace-pre-wrap break-all">
              {payloadFormatted ? prettyPayload(payloadEvent?.rawPayload) : payloadEvent?.rawPayload}
            </pre>
          </div>
        </ModalContent>
      </Modal>
    </>
  )
}
