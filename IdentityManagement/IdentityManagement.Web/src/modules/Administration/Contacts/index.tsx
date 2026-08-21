import { useCallback, useEffect, useMemo, useState } from 'react'
import { Badge, Button, DataTable, Modal, ModalContent, ModalHeader, ModalTitle, PageLayout, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, useApi } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Check, Copy, Mail, RotateCcw } from 'lucide-react'
import { ContactService } from '../../../services/contactService'
import type { ContactRequest } from '../../../types/contact'
import { formatDateTime } from '../../../utils/date'

type Filtro = 'pendentes' | 'tratados' | 'todos'

export default function Contacts() {
  const [contacts, setContacts] = useState<ContactRequest[]>([])
  const [filtro, setFiltro] = useState<Filtro>('pendentes')
  const [selected, setSelected] = useState<ContactRequest | null>(null)
  const [copied, setCopied] = useState(false)

  const listApi = useApi<ContactRequest[]>({ showErrorMessage: true })
  const handledApi = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const load = useCallback(async () => {
    const pending = filtro === 'todos' ? undefined : filtro === 'pendentes'
    const result = await listApi.execute(() => ContactService.list({ pending, take: 200 }))
    setContacts(result ?? [])
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filtro])

  useEffect(() => {
    void load()
  }, [load])

  const toggleHandled = async (contact: ContactRequest) => {
    const result = await handledApi.execute(() => ContactService.setHandled(contact.id, !contact.handledAt))
    if (result !== null) {
      setSelected(null)
      await load()
    }
  }

  const copyEmail = async (email: string) => {
    await navigator.clipboard.writeText(email)
    setCopied(true)
    window.setTimeout(() => setCopied(false), 2000)
  }

  const pendentes = useMemo(() => contacts.filter((item) => !item.handledAt).length, [contacts])

  const columns: DataTableColumn<ContactRequest>[] = [
    {
      key: 'createdAt',
      title: 'Recebido',
      dataIndex: 'createdAt',
      sortable: true,
      render: (value: string) => formatDateTime(value),
    },
    { key: 'name', title: 'Nome', dataIndex: 'name' },
    {
      key: 'companyName',
      title: 'Agência',
      dataIndex: 'companyName',
      render: (value?: string | null) => value || '-',
    },
    { key: 'email', title: 'E-mail', dataIndex: 'email' },
    {
      key: 'phoneNumber',
      title: 'Telefone',
      dataIndex: 'phoneNumber',
      render: (value?: string | null) => value || '-',
    },
    {
      key: 'notificationSent',
      title: 'Aviso',
      dataIndex: 'notificationSent',
      // Falso aqui significa que o e-mail ao time falhou: o lead existe e ninguem foi avisado.
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>{value ? 'Enviado' : 'Falhou'}</Badge>
      ),
    },
    {
      key: 'handledAt',
      title: 'Status',
      dataIndex: 'handledAt',
      render: (value?: string | null) => (
        <Badge variant={value ? 'success' : 'warning'}>{value ? 'Tratado' : 'Pendente'}</Badge>
      ),
    },
    {
      key: 'actions',
      title: '',
      dataIndex: 'id',
      render: (_: unknown, record: ContactRequest) => (
        <Button variant="secondary" size="sm" onClick={(e) => { e.stopPropagation(); setSelected(record) }}>
          Ver mensagem
        </Button>
      ),
    },
  ]

  return (
    <>
      <PageLayout
        title="Contatos do site"
        subtitle="Mensagens enviadas pelo formulário da landing."
        onRefresh={load}
      >
        <div className="mb-4 flex items-center gap-3">
          <Select value={filtro} onValueChange={(value) => setFiltro(value as Filtro)}>
            <SelectTrigger className="w-48">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="pendentes">Pendentes</SelectItem>
              <SelectItem value="tratados">Tratados</SelectItem>
              <SelectItem value="todos">Todos</SelectItem>
            </SelectContent>
          </Select>

          {pendentes > 0 && (
            <Badge variant="warning">{pendentes} pendente{pendentes > 1 ? 's' : ''}</Badge>
          )}
        </div>

        <DataTable
          columns={columns}
          data={contacts}
          loading={listApi.isLoading}
          rowKey="id"
          onRowDoubleClick={(record: ContactRequest) => setSelected(record)}
        />
      </PageLayout>

      <Modal open={!!selected} onOpenChange={(open) => { if (!open) { setSelected(null); setCopied(false) } }}>
        <ModalContent size="2xl">
          <ModalHeader>
            <ModalTitle>{selected?.name}</ModalTitle>
          </ModalHeader>

          {selected && (
            <div className="flex flex-col gap-4">
              <dl className="grid grid-cols-1 gap-x-8 gap-y-2 text-sm sm:grid-cols-2">
                <div className="flex gap-2">
                  <dt className="text-muted-foreground">E-mail:</dt>
                  <dd className="font-medium text-foreground">{selected.email}</dd>
                </div>
                <div className="flex gap-2">
                  <dt className="text-muted-foreground">Telefone:</dt>
                  <dd className="font-medium text-foreground">{selected.phoneNumber || '-'}</dd>
                </div>
                <div className="flex gap-2">
                  <dt className="text-muted-foreground">Agência:</dt>
                  <dd className="font-medium text-foreground">{selected.companyName || '-'}</dd>
                </div>
                <div className="flex gap-2">
                  <dt className="text-muted-foreground">Recebido:</dt>
                  <dd className="font-medium text-foreground">{formatDateTime(selected.createdAt)}</dd>
                </div>
                <div className="flex gap-2">
                  <dt className="text-muted-foreground">Origem:</dt>
                  <dd className="font-medium text-foreground">{selected.sourceIp || '-'}</dd>
                </div>
              </dl>

              <div className="rounded-md bg-muted p-4">
                <p className="whitespace-pre-wrap text-sm text-foreground">{selected.message}</p>
              </div>

              <div className="flex flex-wrap justify-end gap-2">
                <Button variant="outline" icon={copied ? <Check /> : <Copy />} iconPosition="left" onClick={() => copyEmail(selected.email)}>
                  {copied ? 'Copiado' : 'Copiar e-mail'}
                </Button>
                <a
                  href={`mailto:${selected.email}?subject=${encodeURIComponent('Mainstay — seu contato pelo site')}`}
                  className="inline-flex items-center gap-2 rounded-md border border-border px-4 py-2 text-sm font-medium text-foreground transition-colors hover:bg-muted"
                >
                  <Mail className="h-4 w-4" />
                  Responder
                </a>
                <Button
                  icon={selected.handledAt ? <RotateCcw /> : <Check />}
                  iconPosition="left"
                  loading={handledApi.isLoading}
                  onClick={() => toggleHandled(selected)}
                >
                  {selected.handledAt ? 'Reabrir' : 'Marcar como tratado'}
                </Button>
              </div>
            </div>
          )}
        </ModalContent>
      </Modal>
    </>
  )
}
