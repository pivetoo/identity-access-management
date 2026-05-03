import { useEffect, useState } from 'react'
import { Badge, Button, ConfirmModal, DataTable, PageLayout, Sheet, SheetContent, SheetPreviewField, SheetPreviewGrid, SheetPreviewHeader, SheetPreviewSection, toast, useApi } from 'archon-ui'
import type { DataTableColumn, PaginatedResult } from 'archon-ui'
import OAuthClientFormModal from '../../../components/modals/OAuthClientFormModal'
import { OAuthClientService } from '../../../services/oauthClientService'
import type { OAuthClient } from '../../../types/oauthClient'

const clientTypeLabel = (type: number) => {
  if (type === 2) {
    return 'Confidential'
  }

  if (type === 3) {
    return 'Machine'
  }

  return 'Public'
}

export default function OAuthClients() {
  const [clients, setClients] = useState<OAuthClient[]>([])
  const [selectedClients, setSelectedClients] = useState<OAuthClient[]>([])
  const [previewClient, setPreviewClient] = useState<OAuthClient | null>(null)
  const [editingClient, setEditingClient] = useState<OAuthClient | undefined>()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [isConfirmDisableOpen, setIsConfirmDisableOpen] = useState(false)
  const [hasMore, setHasMore] = useState(true)
  const pageSize = 30

  const loadClientsApi = useApi({
    onSuccess: (data: PaginatedResult<OAuthClient>) => {
      setClients((prev) => [...prev, ...data.data])
      setHasMore(data.data.length === pageSize)
    },
  })

  const loadMoreClientsApi = useApi({
    onSuccess: (data: PaginatedResult<OAuthClient>) => {
      setClients((prev) => [...prev, ...data.data])
      setHasMore(data.data.length === pageSize)
    },
  })

  const disableClientApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: 'Sucesso',
        description: 'OAuth client desativado.',
      })
      setSelectedClients([])
      setIsConfirmDisableOpen(false)
      loadClients(true)
    },
    onError: () => setIsConfirmDisableOpen(false),
  })

  const loadClients = async (reset = false) => {
    if (reset) {
      setClients([])
    }

    await loadClientsApi.execute(() =>
      OAuthClientService.getAll({
        page: 1,
        pageSize,
        orderBy: 'id',
      })
    )
  }

  const loadMoreClients = async () => {
    const currentPage = Math.floor(clients.length / pageSize) + 1
    await loadMoreClientsApi.execute(() =>
      OAuthClientService.getAll({
        page: currentPage + 1,
        pageSize,
        orderBy: 'id',
      })
    )
  }

  useEffect(() => {
    loadClients(true)
  }, [])

  const handleAdd = () => {
    setEditingClient(undefined)
    setIsModalOpen(true)
  }

  const handleEdit = () => {
    if (selectedClients.length !== 1) {
      toast({
        variant: 'warning',
        title: 'Atenção',
        description: selectedClients.length === 0 ? 'Selecione um OAuth client para editar.' : 'Selecione apenas um OAuth client para editar.',
      })
      return
    }

    setEditingClient(selectedClients[0])
    setIsModalOpen(true)
  }

  const handleDisable = () => {
    if (selectedClients.length === 0) {
      toast({
        variant: 'warning',
        title: 'Atenção',
        description: 'Selecione um ou mais OAuth clients para desativar.',
      })
      return
    }

    setIsConfirmDisableOpen(true)
  }

  const handleConfirmDisable = async () => {
    for (const client of selectedClients) {
      await disableClientApi.execute(() => OAuthClientService.disable(client))
    }
  }

  const columns: DataTableColumn<OAuthClient>[] = [
    {
      key: 'clientName',
      title: 'Nome',
      dataIndex: 'clientName',
      sortable: true,
    },
    {
      key: 'clientId',
      title: 'Client ID',
      dataIndex: 'clientId',
    },
    {
      key: 'systemApplicationName',
      title: 'Sistema',
      dataIndex: 'systemApplicationName',
    },
    {
      key: 'clientType',
      title: 'Tipo',
      dataIndex: 'clientType',
      render: (value: number) => clientTypeLabel(value),
    },
    {
      key: 'isDefault',
      title: 'Padrão',
      dataIndex: 'isDefault',
      render: (value: boolean) => value ? <Badge variant="secondary">Padrão</Badge> : '-',
    },
    {
      key: 'isActive',
      title: 'Status',
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? 'Ativo' : 'Inativo'}
        </Badge>
      ),
    },
  ]

  return (
    <>
      <PageLayout
        title="OAuth Clients"
        subtitle="Gerencie clients OpenID Connect/OAuth2, redirect URIs, scopes e secrets."
        onAdd={handleAdd}
        onEdit={handleEdit}
        onDelete={handleDisable}
        onRefresh={() => loadClients(true)}
        selectedRowsCount={selectedClients.length}
      >
        <DataTable
          columns={columns}
          data={clients}
          loading={loadClientsApi.isLoading || disableClientApi.isLoading}
          rowKey="id"
          selectable
          selectedRows={selectedClients}
          onSelectionChange={setSelectedClients}
          onRowDoubleClick={setPreviewClient}
        />

        {hasMore && (
          <div className="mt-4 flex justify-end">
            <Button variant="outline" onClick={loadMoreClients} loading={loadMoreClientsApi.isLoading}>
              Carregar mais
            </Button>
          </div>
        )}

        <OAuthClientFormModal
          isOpen={isModalOpen}
          onClose={() => {
            setIsModalOpen(false)
            setEditingClient(undefined)
          }}
          client={editingClient}
          onSuccess={() => {
            setSelectedClients([])
            loadClients(true)
          }}
        />

        <ConfirmModal
          open={isConfirmDisableOpen}
          onOpenChange={setIsConfirmDisableOpen}
          onConfirm={handleConfirmDisable}
          title="Desativar OAuth Clients"
          description={`Deseja desativar ${selectedClients.length} OAuth client(s) selecionado(s)?`}
          confirmText="Desativar"
          variant="danger"
          loading={disableClientApi.isLoading}
        />
      </PageLayout>

      <Sheet open={!!previewClient} onOpenChange={(open) => !open && setPreviewClient(null)}>
        <SheetContent side="right" className="w-full sm:max-w-lg">
          {previewClient ? (
            <div className="flex h-full flex-col">
              <SheetPreviewHeader
                title={previewClient.clientName}
                meta={
                  <>
                    <Badge variant={previewClient.isActive ? 'success' : 'destructive'}>
                      {previewClient.isActive ? 'Ativo' : 'Inativo'}
                    </Badge>
                    {previewClient.isDefault ? <Badge variant="secondary">Padrão</Badge> : null}
                    <span className="text-xs font-medium text-muted-foreground">{previewClient.clientId}</span>
                  </>
                }
                description="Configuração OIDC/OAuth2 do client selecionado."
              />

              <div className="mt-6 flex-1 space-y-4 overflow-y-auto">
                <SheetPreviewSection title="Identificação" description="Sistema, tipo e políticas principais.">
                  <SheetPreviewGrid>
                    <SheetPreviewField label="Sistema" value={previewClient.systemApplicationName} />
                    <SheetPreviewField label="Tipo" value={clientTypeLabel(previewClient.clientType)} />
                    <SheetPreviewField label="Padrão" value={previewClient.isDefault ? 'Sim' : 'Não'} />
                    <SheetPreviewField label="PKCE" value={previewClient.requirePkce ? 'Obrigatório' : 'Opcional'} />
                    <SheetPreviewField label="Offline access" value={previewClient.allowOfflineAccess ? 'Permitido' : 'Bloqueado'} />
                  </SheetPreviewGrid>
                </SheetPreviewSection>

                <SheetPreviewSection title="Lifetimes" description="Valores em segundos.">
                  <SheetPreviewGrid>
                    <SheetPreviewField label="Access token" value={previewClient.accessTokenLifetime} />
                    <SheetPreviewField label="ID token" value={previewClient.identityTokenLifetime} />
                    <SheetPreviewField label="Refresh token" value={previewClient.refreshTokenLifetime} />
                    <SheetPreviewField label="Rotação" value={previewClient.refreshTokenRotationEnabled ? 'Ativa' : 'Inativa'} />
                  </SheetPreviewGrid>
                </SheetPreviewSection>

                <SheetPreviewSection title="Scopes" description="Escopos permitidos para o client.">
                  <SheetPreviewField label="Scopes" value={previewClient.scopes.join(' ')} />
                </SheetPreviewSection>

                <SheetPreviewSection title="Redirect URIs" description="URIs de login e logout configuradas.">
                  <div className="space-y-2">
                    {previewClient.redirectUris.map((item) => (
                      <div key={`${item.type}-${item.uri}`} className="rounded-md border border-border p-2 text-sm">
                        <Badge variant={item.type === 1 ? 'info' : 'secondary'}>{item.type === 1 ? 'Sign-in' : 'Logout'}</Badge>
                        <div className="mt-1 break-all text-muted-foreground">{item.uri}</div>
                      </div>
                    ))}
                  </div>
                </SheetPreviewSection>
              </div>
            </div>
          ) : null}
        </SheetContent>
      </Sheet>
    </>
  )
}
