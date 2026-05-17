import { useEffect, useMemo, useState } from 'react'
import { Badge, Button, Checkbox, Input, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, useI18n } from 'archon-ui'
import type { AccessResource } from '../../types/accessResource'
import { getHttpMethodClassName } from '../../utils/accessResource'

interface RolePermissionsModalProps {
  isOpen: boolean
  onClose: () => void
  resources: AccessResource[]
  selectedResourceIds: number[]
  onConfirm: (accessResourceIds: number[]) => void
  disabled?: boolean
}

export default function RolePermissionsModal({
  isOpen,
  onClose,
  resources,
  selectedResourceIds,
  onConfirm,
  disabled = false,
}: RolePermissionsModalProps) {
  const { t } = useI18n()
  const [search, setSearch] = useState('')
  const [draftResourceIds, setDraftResourceIds] = useState<number[]>(selectedResourceIds)

  useEffect(() => {
    if (isOpen) {
      setDraftResourceIds(selectedResourceIds)
      setSearch('')
    }
  }, [isOpen, selectedResourceIds])

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
        description: t('rolePermissions.groupDescription').replace('{0}', controller),
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
          <ModalTitle>{t('rolePermissions.title')}</ModalTitle>
        </ModalHeader>

        <div className="flex flex-col gap-4 py-4 min-h-0">
          <div className="rounded-lg border bg-muted/20 p-4">
            <div className="flex items-start justify-between gap-4">
              <div className="space-y-1">
                  <p className="text-sm font-medium">{t('rolePermissions.catalogTitle')}</p>
                  <p className="text-sm text-muted-foreground">
                  {t('rolePermissions.catalogDescription')}
                  </p>
              </div>
            </div>
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">{t('rolePermissions.searchLabel')}</label>
            <Input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder={t('rolePermissions.searchPlaceholder')}
              disabled={disabled}
            />
          </div>

          <div className="min-h-0 space-y-4 overflow-y-auto pr-1">
            {filteredGroups.length === 0 ? (
              <div className="rounded-lg border border-dashed p-8 text-center">
                <p className="text-sm font-medium">{t('rolePermissions.emptyTitle')}</p>
                <p className="mt-1 text-sm text-muted-foreground">
                  {t('rolePermissions.emptyDescription')}
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
                        {t('rolePermissions.selectGroup')}
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
            {t('common.action.cancel')}
          </Button>
          <Button variant="primary" onClick={handleConfirm} disabled={disabled}>
            {t('common.action.applyPermissions')}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  )
}
