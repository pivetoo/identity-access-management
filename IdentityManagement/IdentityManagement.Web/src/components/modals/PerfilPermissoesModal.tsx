import { useMemo, useState } from 'react'
import { Badge, Button, Checkbox, Input, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle } from 'archon-ui'
import type { AccessResource } from '../../types/accessResource'

function getHttpMethodClassName(httpMethod: string): string {
  switch (httpMethod.toUpperCase()) {
    case 'GET':
      return 'border-emerald-200 bg-emerald-50 text-emerald-700'
    case 'POST':
      return 'border-amber-200 bg-amber-50 text-amber-700'
    case 'PUT':
      return 'border-orange-200 bg-orange-50 text-orange-700'
    case 'DELETE':
      return 'border-red-200 bg-red-50 text-red-700'
    default:
      return ''
  }
}

interface PerfilPermissoesModalProps {
  isOpen: boolean
  onClose: () => void
  resources: AccessResource[]
  selectedResourceIds: number[]
  onConfirm: (accessResourceIds: number[]) => void
  disabled?: boolean
}

export default function PerfilPermissoesModal({
  isOpen,
  onClose,
  resources,
  selectedResourceIds,
  onConfirm,
  disabled = false,
}: PerfilPermissoesModalProps) {
  const [search, setSearch] = useState('')
  const [draftResourceIds, setDraftResourceIds] = useState<number[]>(selectedResourceIds)

  const normalizedSearch = search.trim().toLowerCase()

  const filteredGroups = useMemo(() => {
    const groupedResources = resources.reduce<Map<string, AccessResource[]>>((groups, resource) => {
      const groupKey = resource.controller
      const groupResources = groups.get(groupKey) ?? []
      groupResources.push(resource)
      groups.set(groupKey, groupResources)
      return groups
    }, new Map())

    return Array.from(groupedResources.entries())
      .map(([controller, groupResources]) => ({
        title: controller,
        description: `Recursos protegidos do controller ${controller}.`,
        resources: groupResources
          .filter((resource) => {
            if (!normalizedSearch) {
              return true
            }

            return (
              resource.name.toLowerCase().includes(normalizedSearch) ||
              resource.controller.toLowerCase().includes(normalizedSearch) ||
              resource.action.toLowerCase().includes(normalizedSearch) ||
              resource.route.toLowerCase().includes(normalizedSearch)
            )
          })
          .sort((left, right) => left.name.localeCompare(right.name)),
      }))
      .filter((group) => group.resources.length > 0)
      .sort((left, right) => left.title.localeCompare(right.title))
  }, [normalizedSearch, resources])

  const toggleResource = (resourceId: number, checked: boolean) => {
    setDraftResourceIds((current) => {
      if (checked) {
        return current.includes(resourceId) ? current : [...current, resourceId]
      }

      return current.filter((item) => item !== resourceId)
    })
  }

  const toggleGroup = (resourceIds: number[], checked: boolean) => {
    setDraftResourceIds((current) => {
      if (checked) {
        return Array.from(new Set([...current, ...resourceIds]))
      }

      return current.filter((item) => !resourceIds.includes(item))
    })
  }

  const handleConfirm = () => {
    onConfirm(draftResourceIds)
    onClose()
  }

  const handleOpenChange = (open: boolean) => {
    if (open) {
      setDraftResourceIds(selectedResourceIds)
      return
    }

    onClose()
  }

  return (
    <Modal open={isOpen} onOpenChange={handleOpenChange}>
      <ModalContent size="full" className="h-[92vh] max-w-[96vw] grid-rows-[auto_auto_minmax(0,1fr)_auto]">
        <ModalHeader>
          <ModalTitle>Selecionar permissões</ModalTitle>
        </ModalHeader>

        <div className="flex flex-col gap-4 py-4 min-h-0">
          <div className="rounded-lg border bg-muted/20 p-4">
            <div className="flex items-start justify-between gap-4">
              <div className="space-y-1">
                <p className="text-sm font-medium">Catálogo de recursos</p>
                <p className="text-sm text-muted-foreground">
                  Selecione os recursos que este perfil poderá acessar. O catálogo é carregado do backend e sincronizado pelo Archon conforme os endpoints protegidos.
                </p>
              </div>
            </div>
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">Pesquisar permissões</label>
            <Input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Buscar por recurso, controller, ação ou rota"
              disabled={disabled}
            />
          </div>

          <div className="min-h-0 space-y-4 overflow-y-auto pr-1">
            {filteredGroups.length === 0 ? (
              <div className="rounded-lg border border-dashed p-8 text-center">
                <p className="text-sm font-medium">Nenhum recurso encontrado</p>
                <p className="mt-1 text-sm text-muted-foreground">
                  Ajuste o termo de busca para localizar os recursos desejados.
                </p>
              </div>
            ) : (
              filteredGroups.map((group) => {
                const resourceIds = group.resources.map((resource) => resource.id)
                const totalInGroup = resourceIds.length
                const selectedInGroup = resourceIds.filter((resourceId) => draftResourceIds.includes(resourceId)).length
                const allSelected = totalInGroup > 0 && selectedInGroup === totalInGroup

                return (
                  <div key={group.title} className="rounded-lg border bg-background">
                    <div className="flex items-start justify-between gap-4 border-b px-4 py-3">
                      <div className="space-y-1">
                        <div className="flex items-center gap-2">
                          <p className="text-sm font-semibold">{group.title}</p>
                          <Badge variant="outline">{selectedInGroup}/{totalInGroup}</Badge>
                        </div>
                        <p className="text-sm text-muted-foreground">{group.description}</p>
                      </div>

                      <label className="flex items-center gap-2 text-sm">
                        <Checkbox
                          checked={allSelected}
                          onCheckedChange={(checked) => toggleGroup(resourceIds, checked === true)}
                          disabled={disabled}
                        />
                        Selecionar grupo
                      </label>
                    </div>

                    <div className="grid gap-3 px-4 py-4 md:grid-cols-2 xl:grid-cols-3">
                      {group.resources.map((resource) => (
                        <label
                          key={resource.id}
                          className="flex min-w-0 items-start gap-3 rounded-md border p-3 text-sm transition-colors hover:bg-muted/30"
                        >
                          <Checkbox
                            checked={draftResourceIds.includes(resource.id)}
                            onCheckedChange={(checked) => toggleResource(resource.id, checked === true)}
                            disabled={disabled}
                          />
                          <div className="min-w-0 flex-1 space-y-1">
                            <div className="flex min-w-0 items-center gap-2">
                              <span className="min-w-0 flex-1 overflow-hidden text-ellipsis whitespace-nowrap font-medium" title={resource.name}>
                                {resource.name}
                              </span>
                              <Badge variant="outline" className={getHttpMethodClassName(resource.httpMethod)}>
                                {resource.httpMethod}
                              </Badge>
                            </div>
                            {resource.description ? (
                              <p className="overflow-hidden text-ellipsis whitespace-nowrap text-xs text-muted-foreground" title={resource.description}>
                                {resource.description}
                              </p>
                            ) : null}
                            <p className="overflow-hidden text-ellipsis whitespace-nowrap text-xs text-muted-foreground" title={resource.route}>
                              {resource.route}
                            </p>
                          </div>
                        </label>
                      ))}
                    </div>
                  </div>
                )
              })
            )}
          </div>
        </div>

        <ModalFooter>
          <Button variant="outline" onClick={onClose}>
            Cancelar
          </Button>
          <Button variant="primary" onClick={handleConfirm} disabled={disabled}>
            Aplicar permissões
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  )
}
