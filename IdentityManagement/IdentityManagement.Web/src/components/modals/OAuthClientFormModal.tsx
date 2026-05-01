import { useEffect, useState } from 'react'
import { Button, Input, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, SearchableSelect, Switch, toast, useApi, useFormErrors } from 'archon-ui'
import { OAuthClientService } from '../../services/oauthClientService'
import { SystemApplicationService } from '../../services/systemApplicationService'
import type { OAuthClient, OAuthClientRedirectUri, OAuthClientType, UpdateOAuthClientRequest } from '../../types/oauthClient'
import type { SystemApplication } from '../../types/systemApplication'

interface OAuthClientFormModalProps {
  isOpen: boolean
  onClose: () => void
  client?: OAuthClient
  onSuccess: () => void
}

const defaultScopes = 'openid profile email offline_access'

const parseRedirectUris = (value: string): OAuthClientRedirectUri[] =>
  value
    .split('\n')
    .map((item) => item.trim())
    .filter(Boolean)
    .map((uri) => ({ uri, type: 1 }))

const parsePostLogoutUris = (value: string): OAuthClientRedirectUri[] =>
  value
    .split('\n')
    .map((item) => item.trim())
    .filter(Boolean)
    .map((uri) => ({ uri, type: 2 }))

const parseScopes = (value: string): string[] =>
  value
    .split(' ')
    .map((item) => item.trim())
    .filter(Boolean)

export default function OAuthClientFormModal({ isOpen, onClose, client, onSuccess }: OAuthClientFormModalProps) {
  const { getError, setErrors, clearErrors } = useFormErrors()
  const [systems, setSystems] = useState<SystemApplication[]>([])
  const [formData, setFormData] = useState({
    systemApplicationId: 0,
    clientId: '',
    clientName: '',
    clientType: 1 as OAuthClientType,
    clientSecret: '',
    rotateClientSecret: false,
    requirePkce: true,
    requireConsent: false,
    allowOfflineAccess: true,
    isActive: true,
    accessTokenLifetime: 900,
    identityTokenLifetime: 900,
    refreshTokenLifetime: 2592000,
    refreshTokenRotationEnabled: true,
    redirectUris: '',
    postLogoutRedirectUris: '',
    scopes: defaultScopes,
  })

  const loadSystemsApi = useApi({
    onSuccess: (data: SystemApplication[]) => setSystems(data),
  })

  const saveApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: 'Sucesso',
        description: client ? 'OAuth client atualizado.' : 'OAuth client criado.',
      })
      clearErrors()
      onSuccess()
      onClose()
    },
    onError: (error) => {
      setErrors(error)
      toast({
        title: 'Erro',
        description: error.message,
        variant: 'destructive',
      })
    },
  })

  useEffect(() => {
    if (!isOpen) {
      return
    }

    loadSystemsApi.execute(() => SystemApplicationService.getActive())

    if (client) {
      setFormData({
        systemApplicationId: client.systemApplicationId,
        clientId: client.clientId,
        clientName: client.clientName,
        clientType: client.clientType,
        clientSecret: '',
        rotateClientSecret: false,
        requirePkce: client.requirePkce,
        requireConsent: client.requireConsent,
        allowOfflineAccess: client.allowOfflineAccess,
        isActive: client.isActive,
        accessTokenLifetime: client.accessTokenLifetime,
        identityTokenLifetime: client.identityTokenLifetime,
        refreshTokenLifetime: client.refreshTokenLifetime,
        refreshTokenRotationEnabled: client.refreshTokenRotationEnabled,
        redirectUris: client.redirectUris.filter((item) => item.type === 1).map((item) => item.uri).join('\n'),
        postLogoutRedirectUris: client.redirectUris.filter((item) => item.type === 2).map((item) => item.uri).join('\n'),
        scopes: client.scopes.join(' '),
      })
      return
    }

    setFormData({
      systemApplicationId: 0,
      clientId: '',
      clientName: '',
      clientType: 1,
      clientSecret: '',
      rotateClientSecret: false,
      requirePkce: true,
      requireConsent: false,
      allowOfflineAccess: true,
      isActive: true,
      accessTokenLifetime: 900,
      identityTokenLifetime: 900,
      refreshTokenLifetime: 2592000,
      refreshTokenRotationEnabled: true,
      redirectUris: '',
      postLogoutRedirectUris: '',
      scopes: defaultScopes,
    })
  }, [isOpen, client])

  const handleInputChange = (field: string, value: string | boolean | number) => {
    setFormData((prev) => ({ ...prev, [field]: value }))
  }

  const buildRedirectUris = () => [
    ...parseRedirectUris(formData.redirectUris),
    ...parsePostLogoutUris(formData.postLogoutRedirectUris),
  ]

  const handleSave = async () => {
    if (client) {
      const request: UpdateOAuthClientRequest = {
        id: client.id,
        clientName: formData.clientName,
        clientType: formData.clientType,
        clientSecret: formData.clientSecret || undefined,
        rotateClientSecret: formData.rotateClientSecret,
        requirePkce: formData.requirePkce,
        requireConsent: formData.requireConsent,
        allowOfflineAccess: formData.allowOfflineAccess,
        isActive: formData.isActive,
        accessTokenLifetime: formData.accessTokenLifetime,
        identityTokenLifetime: formData.identityTokenLifetime,
        refreshTokenLifetime: formData.refreshTokenLifetime,
        refreshTokenRotationEnabled: formData.refreshTokenRotationEnabled,
        redirectUris: buildRedirectUris(),
        scopes: parseScopes(formData.scopes),
      }
      await saveApi.execute(() => OAuthClientService.update(client.id, request))
      return
    }

    await saveApi.execute(() =>
      OAuthClientService.create({
        systemApplicationId: formData.systemApplicationId,
        clientId: formData.clientId,
        clientName: formData.clientName,
        clientType: formData.clientType,
        clientSecret: formData.clientSecret || undefined,
        requirePkce: formData.requirePkce,
        requireConsent: formData.requireConsent,
        allowOfflineAccess: formData.allowOfflineAccess,
        accessTokenLifetime: formData.accessTokenLifetime,
        identityTokenLifetime: formData.identityTokenLifetime,
        refreshTokenLifetime: formData.refreshTokenLifetime,
        refreshTokenRotationEnabled: formData.refreshTokenRotationEnabled,
        redirectUris: buildRedirectUris(),
        scopes: parseScopes(formData.scopes),
      })
    )
  }

  const isConfidential = formData.clientType === 2
  const isValid =
    formData.clientName.trim() &&
    formData.redirectUris.trim() &&
    formData.scopes.includes('openid') &&
    (client ? true : formData.systemApplicationId > 0 && formData.clientId.trim()) &&
    (!isConfidential || client || formData.clientSecret.trim())

  return (
    <Modal open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <ModalContent size="4xl">
        <ModalHeader>
          <ModalTitle>{client ? 'Editar OAuth Client' : 'Novo OAuth Client'}</ModalTitle>
        </ModalHeader>

        <div className="flex max-h-[80vh] flex-col gap-4 overflow-y-auto py-4 pr-1">
          <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">Sistema *</label>
              <SearchableSelect
                options={systems.map((system) => ({ label: system.name, value: system.id.toString() }))}
                value={formData.systemApplicationId > 0 ? formData.systemApplicationId.toString() : undefined}
                onValueChange={(value) => handleInputChange('systemApplicationId', Number(value))}
                placeholder="Selecione o sistema"
                searchPlaceholder="Buscar sistema"
                disabled={!!client || loadSystemsApi.isLoading}
              />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">Tipo *</label>
              <SearchableSelect
                options={[
                  { label: 'Public', value: '1' },
                  { label: 'Confidential', value: '2' },
                  { label: 'Machine', value: '3' },
                ]}
                value={formData.clientType.toString()}
                onValueChange={(value) => handleInputChange('clientType', Number(value) as OAuthClientType)}
                placeholder="Selecione o tipo"
              />
            </div>
          </div>

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">Client ID *</label>
              <Input value={formData.clientId} onChange={(event) => handleInputChange('clientId', event.target.value)} disabled={!!client} error={!!getError('clientId')} helperText={getError('clientId')} />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">Nome *</label>
              <Input value={formData.clientName} onChange={(event) => handleInputChange('clientName', event.target.value)} error={!!getError('clientName')} helperText={getError('clientName')} />
            </div>
          </div>

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">Client Secret {isConfidential && !client ? '*' : ''}</label>
              <Input type="password" value={formData.clientSecret} onChange={(event) => handleInputChange('clientSecret', event.target.value)} helperText={client ? 'Preencha apenas para rotacionar o segredo.' : undefined} />
            </div>
            {client && (
              <div className="flex items-center gap-2 pt-7">
                <Switch checked={formData.rotateClientSecret} onCheckedChange={(checked) => handleInputChange('rotateClientSecret', checked)} />
                <label className="text-sm font-medium">Rotacionar segredo</label>
              </div>
            )}
          </div>

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">Access token lifetime</label>
              <Input type="number" value={formData.accessTokenLifetime} onChange={(event) => handleInputChange('accessTokenLifetime', Number(event.target.value))} />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">ID token lifetime</label>
              <Input type="number" value={formData.identityTokenLifetime} onChange={(event) => handleInputChange('identityTokenLifetime', Number(event.target.value))} />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">Refresh token lifetime</label>
              <Input type="number" value={formData.refreshTokenLifetime} onChange={(event) => handleInputChange('refreshTokenLifetime', Number(event.target.value))} />
            </div>
          </div>

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">Redirect URIs *</label>
              <textarea className="min-h-40 rounded-md border border-input bg-background px-3 py-2 text-sm" value={formData.redirectUris} onChange={(event) => handleInputChange('redirectUris', event.target.value)} placeholder="https://app.exemplo.com/callback" />
            </div>

            <div className="flex flex-col gap-4">
              <div className="flex flex-col gap-2">
                <label className="text-sm font-medium">Post logout redirect URIs</label>
                <textarea className="min-h-24 rounded-md border border-input bg-background px-3 py-2 text-sm" value={formData.postLogoutRedirectUris} onChange={(event) => handleInputChange('postLogoutRedirectUris', event.target.value)} placeholder="https://app.exemplo.com/logout/callback" />
              </div>

              <div className="flex flex-col gap-2">
                <label className="text-sm font-medium">Scopes *</label>
                <Input value={formData.scopes} onChange={(event) => handleInputChange('scopes', event.target.value)} helperText="Separados por espaço. Deve conter openid." />
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 gap-3 lg:grid-cols-3">
            {[
              ['Require PKCE', 'requirePkce'],
              ['Require consent', 'requireConsent'],
              ['Offline access', 'allowOfflineAccess'],
              ['Refresh rotation', 'refreshTokenRotationEnabled'],
              ['Ativo', 'isActive'],
            ].map(([label, field]) => (
              <div className="flex items-center gap-2" key={field}>
                <Switch checked={Boolean(formData[field as keyof typeof formData])} onCheckedChange={(checked) => handleInputChange(field, checked)} disabled={field === 'isActive' && !client} />
                <label className="text-sm font-medium">{label}</label>
              </div>
            ))}
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
