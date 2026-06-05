import { useEffect, useState } from 'react';
import { Eye, EyeOff, Pencil, Plus, Trash2, Plug, AlertTriangle, X } from 'lucide-react';
import { Badge, Button, Card, CardContent, CardHeader, CardTitle, ConfirmModal, Input, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, Select, SelectContent, SelectItem, SelectTrigger, SelectValue, Switch, toast, useApi, useI18n } from 'archon-ui';
import { SystemIntegrationService } from '../../../services/systemIntegrationService';
import type { SystemIntegration, SystemIntegrationParameter, UpsertSystemIntegrationRequest } from '../../../types/systemIntegration';
import { SystemIntegrationParameterSource } from '../../../types/systemIntegration';

interface SystemApplicationIntegrationsTabProps {
  systemApplicationId: number;
  refreshKey: number;
  onRefresh: () => void;
}

const emptyParameter = (): SystemIntegrationParameter => ({
  key: '',
  value: null,
  isSecret: false,
  valueSource: SystemIntegrationParameterSource.Static,
  sourceAudience: null,
})

const emptyForm = (systemApplicationId: number): UpsertSystemIntegrationRequest => ({
  systemApplicationId,
  name: '',
  baseUrl: '',
  isActive: true,
  parameters: [],
})

interface ParameterEditorProps {
  params: SystemIntegrationParameter[]
  onChange: (params: SystemIntegrationParameter[]) => void
}

function ParameterEditor({ params, onChange }: ParameterEditorProps) {
  const [visibleSecrets, setVisibleSecrets] = useState<Record<number, boolean>>({})

  const update = (index: number, field: keyof SystemIntegrationParameter, value: string | boolean | number) => {
    const updated = params.map((p, i) => {
      if (i !== index) {
        return p
      }
      if (field === 'valueSource') {
        return { ...p, valueSource: value as SystemIntegrationParameterSource, value: null, sourceAudience: null }
      }
      return { ...p, [field]: value }
    })
    onChange(updated)
  }

  const add = () => onChange([...params, emptyParameter()])

  const remove = (index: number) => {
    onChange(params.filter((_, i) => i !== index))
    setVisibleSecrets((prev) => {
      const next = { ...prev }
      delete next[index]
      return next
    })
  }

  const toggleVisible = (index: number) => setVisibleSecrets((prev) => ({ ...prev, [index]: !prev[index] }))

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium">Parametros</span>
        <Button type="button" variant="outline" size="sm" onClick={add}>
          <Plus className="mr-1.5 h-3.5 w-3.5" />
          Adicionar parametro
        </Button>
      </div>

      {params.length === 0 && (
        <p className="text-xs text-muted-foreground">Nenhum parametro configurado. Clique em "Adicionar parametro" para incluir.</p>
      )}

      {params.map((param, index) => (
        <div key={index} className="rounded-md border border-border/60 bg-muted/20 p-3 space-y-3">
          <div className="flex items-center justify-between gap-2">
            <div className="flex items-center gap-3 flex-1 min-w-0">
              <div className="flex-1 min-w-0">
                <label className="text-[11px] font-semibold uppercase tracking-[0.12em] text-muted-foreground">Chave</label>
                <Input
                  className="mt-1 h-7 text-xs font-mono"
                  value={param.key}
                  onChange={(e) => update(index, 'key', e.target.value)}
                  placeholder="Ex: Authorization, X-Api-Key"
                />
              </div>
              <div className="flex-1 min-w-0">
                <label className="text-[11px] font-semibold uppercase tracking-[0.12em] text-muted-foreground">Origem do valor</label>
                <Select value={param.valueSource.toString()} onValueChange={(v) => update(index, 'valueSource', Number(v))}>
                  <SelectTrigger className="mt-1 h-7 text-xs">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={SystemIntegrationParameterSource.Static.toString()}>Valor fixo</SelectItem>
                    <SelectItem value={SystemIntegrationParameterSource.TenantApiKey.toString()}>Secret do tenant</SelectItem>
                    <SelectItem value={SystemIntegrationParameterSource.TenantId.toString()}>TenantId do tenant</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <button
              type="button"
              onClick={() => remove(index)}
              className="mt-4 text-muted-foreground hover:text-destructive shrink-0"
              aria-label="Remover parametro"
            >
              <X className="h-4 w-4" />
            </button>
          </div>

          {param.valueSource === SystemIntegrationParameterSource.Static && (
            <div className="space-y-1">
              <div className="flex items-center justify-between">
                <label className="text-[11px] font-semibold uppercase tracking-[0.12em] text-muted-foreground">Valor</label>
                {param.isSecret && (
                  <button
                    type="button"
                    onClick={() => toggleVisible(index)}
                    className="text-xs text-muted-foreground hover:text-foreground"
                    aria-label={visibleSecrets[index] ? 'Ocultar valor' : 'Exibir valor'}
                  >
                    {visibleSecrets[index] ? <EyeOff className="h-3.5 w-3.5" /> : <Eye className="h-3.5 w-3.5" />}
                  </button>
                )}
              </div>
              <Input
                className="h-7 text-xs font-mono"
                type={param.isSecret && !visibleSecrets[index] ? 'password' : 'text'}
                value={param.value ?? ''}
                onChange={(e) => update(index, 'value', e.target.value)}
                placeholder={param.isSecret ? 'Deixe em branco para manter o valor atual' : 'Valor do parametro'}
              />
            </div>
          )}

          {param.valueSource === SystemIntegrationParameterSource.TenantApiKey && (
            <div className="space-y-1">
              <label className="text-[11px] font-semibold uppercase tracking-[0.12em] text-muted-foreground">Sistema de referencia (audience)</label>
              <Input
                className="h-7 text-xs font-mono"
                value={param.sourceAudience ?? ''}
                onChange={(e) => update(index, 'sourceAudience', e.target.value)}
                placeholder="Ex: integration-platform"
              />
              <p className="text-[11px] text-muted-foreground">
                No provisionamento de cada tenant, o apiKey gerado para o sistema informado sera injetado automaticamente neste parametro — sem necessidade de digitar um valor.
              </p>
            </div>
          )}

          {param.valueSource === SystemIntegrationParameterSource.TenantId && (
            <div className="rounded-md bg-muted/40 px-3 py-2">
              <p className="text-[11px] text-muted-foreground">
                O identificador do tenant (<span className="font-mono">Company.TenantId</span>) sera injetado automaticamente no provisionamento — nenhum valor precisa ser informado aqui.
              </p>
            </div>
          )}

          <div className="flex items-center gap-2">
            <Switch
              checked={param.isSecret}
              onCheckedChange={(checked) => update(index, 'isSecret', checked)}
            />
            <label className="text-xs text-muted-foreground">Valor sensivel (ocultar exibicao)</label>
          </div>
        </div>
      ))}
    </div>
  )
}

interface IntegrationFormModalProps {
  isOpen: boolean
  onClose: () => void
  systemApplicationId: number
  integration?: SystemIntegration
  onSuccess: () => void
}

function IntegrationFormModal({ isOpen, onClose, systemApplicationId, integration, onSuccess }: IntegrationFormModalProps) {
  const [form, setForm] = useState<UpsertSystemIntegrationRequest>(emptyForm(systemApplicationId))

  const saveApi = useApi({
    onSuccess: () => {
      toast({ variant: 'success', title: 'Sucesso', description: integration ? 'Integracao atualizada.' : 'Integracao criada.' })
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
    if (integration) {
      setForm({
        systemApplicationId,
        name: integration.name,
        baseUrl: integration.baseUrl,
        isActive: integration.isActive,
        parameters: integration.parameters.map((p) => ({ ...p })),
      })
    } else {
      setForm(emptyForm(systemApplicationId))
    }
  }, [isOpen, integration, systemApplicationId])

  const set = (field: keyof UpsertSystemIntegrationRequest, value: string | boolean | SystemIntegrationParameter[]) => {
    setForm((prev) => ({ ...prev, [field]: value }))
  }

  const handleSave = async () => {
    if (integration) {
      await saveApi.execute(() => SystemIntegrationService.update(integration.id, form))
    } else {
      await saveApi.execute(() => SystemIntegrationService.create(form))
    }
  }

  const isValid = form.name.trim() && form.baseUrl.trim()

  return (
    <Modal open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <ModalContent size="xl">
        <ModalHeader>
          <ModalTitle>{integration ? 'Editar integracao' : 'Nova integracao'}</ModalTitle>
        </ModalHeader>

        <div className="flex flex-col gap-4 py-4 max-h-[70vh] overflow-y-auto">
          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">Nome <span className="text-destructive">*</span></label>
              <Input
                value={form.name}
                onChange={(e) => set('name', e.target.value)}
                placeholder="Ex: identity-management, integration-platform"
              />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">URL base <span className="text-destructive">*</span></label>
              <Input
                value={form.baseUrl}
                onChange={(e) => set('baseUrl', e.target.value)}
                placeholder="https://api.exemplo.com"
              />
            </div>
          </div>

          <div className="flex items-center gap-2">
            <Switch
              checked={form.isActive}
              onCheckedChange={(checked) => set('isActive', checked)}
            />
            <label className="text-sm font-medium cursor-pointer">Ativa</label>
          </div>

          <div className="border-t border-border/40 pt-4">
            <ParameterEditor
              params={form.parameters}
              onChange={(params) => set('parameters', params)}
            />
          </div>
        </div>

        <ModalFooter>
          <Button variant="outline" onClick={onClose} disabled={saveApi.isLoading}>Cancelar</Button>
          <Button variant="primary" onClick={handleSave} loading={saveApi.isLoading} disabled={!isValid}>
            {integration ? 'Salvar alteracoes' : 'Criar integracao'}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  )
}

export default function SystemApplicationIntegrationsTab({ systemApplicationId, refreshKey, onRefresh }: SystemApplicationIntegrationsTabProps) {
  const { t } = useI18n();
  const [integrations, setIntegrations] = useState<SystemIntegration[]>([])
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingIntegration, setEditingIntegration] = useState<SystemIntegration | undefined>()
  const [deletingIntegration, setDeletingIntegration] = useState<SystemIntegration | null>(null)

  const loadApi = useApi({
    onSuccess: (data: SystemIntegration[]) => setIntegrations(data),
  })

  const deleteApi = useApi({
    onSuccess: () => {
      toast({ variant: 'success', title: t('common.toast.successTitle'), description: 'Integracao removida.' })
      setDeletingIntegration(null)
      onRefresh()
    },
    onError: () => setDeletingIntegration(null),
  })

  useEffect(() => {
    loadApi.execute(() => SystemIntegrationService.getBySystem(systemApplicationId))
  }, [systemApplicationId, refreshKey])

  const handleAdd = () => {
    setEditingIntegration(undefined)
    setIsFormOpen(true)
  }

  const handleEdit = (integration: SystemIntegration) => {
    setEditingIntegration(integration)
    setIsFormOpen(true)
  }

  const handleDelete = (integration: SystemIntegration) => {
    setDeletingIntegration(integration)
  }

  const handleConfirmDelete = async () => {
    if (!deletingIntegration) {
      return
    }
    await deleteApi.execute(() => SystemIntegrationService.remove(deletingIntegration.id))
  }

  if (loadApi.isLoading && integrations.length === 0) {
    return <div className="h-32 animate-pulse rounded-lg border border-border bg-muted/30" />
  }

  return (
    <>
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-sm font-semibold text-foreground">Integracoes do sistema</h3>
            <p className="text-xs text-muted-foreground">
              {integrations.length === 0
                ? 'Nenhuma integracao configurada. Estas integracoes serao semeadas em cada tenant no momento do provisionamento.'
                : `${integrations.length} ${integrations.length === 1 ? 'integracao configurada' : 'integracoes configuradas'}. Semeadas em cada tenant no provisionamento.`}
            </p>
          </div>
          <Button variant="primary" size="sm" onClick={handleAdd}>
            <Plus className="mr-2 h-4 w-4" />
            Nova integracao
          </Button>
        </div>

        {integrations.length === 0 ? (
          <Card>
            <CardContent className="flex flex-col items-center justify-center gap-3 py-12 text-center">
              <div className="rounded-full bg-muted/50 p-3">
                <AlertTriangle className="h-6 w-6 text-muted-foreground" />
              </div>
              <h3 className="text-sm font-semibold text-foreground">Sem integracoes configuradas</h3>
              <p className="max-w-md text-xs text-muted-foreground">
                Configure as integracoes que serao provisionadas automaticamente para cada tenant deste sistema.
              </p>
              <Button variant="primary" size="sm" onClick={handleAdd}>
                <Plus className="mr-2 h-4 w-4" />
                Adicionar primeira integracao
              </Button>
            </CardContent>
          </Card>
        ) : (
          <div className="grid gap-3 md:grid-cols-2">
            {integrations.map((integration) => (
              <Card key={integration.id}>
                <CardHeader className="flex flex-row items-start justify-between gap-3 pb-2">
                  <div className="flex items-center gap-2 min-w-0">
                    <div className="rounded-md bg-primary/15 p-1.5 text-primary shrink-0">
                      <Plug className="h-4 w-4" />
                    </div>
                    <div className="min-w-0">
                      <CardTitle className="truncate text-sm">{integration.name}</CardTitle>
                      <p className="truncate text-xs text-muted-foreground">{integration.baseUrl}</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-1 shrink-0">
                    <Badge variant={integration.isActive ? 'success' : 'destructive'} className="text-[10px]">
                      {integration.isActive ? 'Ativa' : 'Inativa'}
                    </Badge>
                    <button
                      type="button"
                      onClick={() => handleEdit(integration)}
                      className="ml-1 text-muted-foreground hover:text-foreground"
                      aria-label="Editar integracao"
                    >
                      <Pencil className="h-3.5 w-3.5" />
                    </button>
                    <button
                      type="button"
                      onClick={() => handleDelete(integration)}
                      className="text-muted-foreground hover:text-destructive"
                      aria-label="Remover integracao"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </button>
                  </div>
                </CardHeader>
                <CardContent className="pt-0">
                  {integration.parameters.length === 0 ? (
                    <p className="text-xs text-muted-foreground">Sem parametros configurados.</p>
                  ) : (
                    <div className="space-y-1">
                      <p className="text-[11px] font-semibold uppercase tracking-[0.12em] text-muted-foreground">
                        {integration.parameters.length} {integration.parameters.length === 1 ? 'parametro' : 'parametros'}
                      </p>
                      <div className="flex flex-wrap gap-1">
                        {integration.parameters.map((param, idx) => (
                          <Badge key={idx} variant={param.valueSource === SystemIntegrationParameterSource.TenantApiKey ? 'info' : param.valueSource === SystemIntegrationParameterSource.TenantId ? 'info' : 'outline'} className="text-[10px] font-mono">
                            {param.key}
                            {param.valueSource === SystemIntegrationParameterSource.TenantApiKey && (
                              <span className="ml-1 font-sans opacity-70">tenant-key</span>
                            )}
                            {param.valueSource === SystemIntegrationParameterSource.TenantId && (
                              <span className="ml-1 font-sans opacity-70">tenant-id</span>
                            )}
                            {param.isSecret && param.valueSource === SystemIntegrationParameterSource.Static && (
                              <span className="ml-1 font-sans opacity-70">secret</span>
                            )}
                          </Badge>
                        ))}
                      </div>
                    </div>
                  )}
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </div>

      <IntegrationFormModal
        isOpen={isFormOpen}
        onClose={() => { setIsFormOpen(false); setEditingIntegration(undefined) }}
        systemApplicationId={systemApplicationId}
        integration={editingIntegration}
        onSuccess={() => { setIsFormOpen(false); setEditingIntegration(undefined); onRefresh() }}
      />

      <ConfirmModal
        open={!!deletingIntegration}
        onOpenChange={(open) => !open && setDeletingIntegration(null)}
        onConfirm={handleConfirmDelete}
        title="Remover integracao"
        description={deletingIntegration ? `Deseja remover a integracao "${deletingIntegration.name}"? Esta acao nao pode ser desfeita.` : ''}
        confirmText="Remover"
        variant="danger"
        loading={deleteApi.isLoading}
      />
    </>
  )
}
