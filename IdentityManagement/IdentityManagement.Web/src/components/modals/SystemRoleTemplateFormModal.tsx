import { useEffect, useMemo, useState } from 'react';
import { Badge, Button, Checkbox, Input, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, SearchableSelect, Switch, toast, useApi, useFormErrors, useI18n } from 'archon-ui';
import { AccessResourceService } from '../../services/accessResourceService';
import { AccessCapabilityService } from '../../services/accessCapabilityService';
import { SystemRoleTemplateService } from '../../services/systemRoleTemplateService';
import { SystemApplicationService } from '../../services/systemApplicationService';
import type { AccessResource } from '../../types/accessResource';
import type { AccessCapability } from '../../types/accessCapability';
import type { CreateSystemRoleTemplateRequest, SystemRoleTemplate, UpdateSystemRoleTemplateRequest } from '../../types/systemRoleTemplate';
import type { SystemApplication } from '../../types/systemApplication';
import RolePermissionsModal from './RolePermissionsModal';

interface SystemRoleTemplateFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  perfilPadrao?: SystemRoleTemplate;
  systemApplicationId?: number;
  onSuccess: () => void;
}

interface CapabilityModule {
  key: string
  label: string
  items: AccessCapability[]
}

// Agrupa o catalogo por modulo respeitando a ordem declarada pelo sistema consumidor.
function groupByModule(capabilities: AccessCapability[]): CapabilityModule[] {
  const sorted = [...capabilities].sort(
    (a, b) => a.moduleOrder - b.moduleOrder || a.module.localeCompare(b.module) || a.order - b.order || a.key.localeCompare(b.key)
  )

  const modules = new Map<string, CapabilityModule>()
  for (const capability of sorted) {
    const current = modules.get(capability.module) ?? { key: capability.module, label: capability.moduleLabel || capability.module, items: [] }
    current.items.push(capability)
    modules.set(capability.module, current)
  }

  return Array.from(modules.values())
}

export default function SystemRoleTemplateFormModal({
  isOpen,
  onClose,
  perfilPadrao,
  systemApplicationId,
  onSuccess,
}: SystemRoleTemplateFormModalProps) {
  const { t } = useI18n()
  const { getError, setErrors, clearErrors } = useFormErrors();
  const [sistemas, setSistemas] = useState<SystemApplication[]>([]);
  const [accessResources, setAccessResources] = useState<AccessResource[]>([]);
  const [capabilities, setCapabilities] = useState<AccessCapability[]>([]);
  const [isPermissionsModalOpen, setIsPermissionsModalOpen] = useState(false);
  const [selectedResourceIds, setSelectedResourceIds] = useState<number[]>([]);
  const [selectedCapabilityKeys, setSelectedCapabilityKeys] = useState<string[]>([]);
  const [formData, setFormData] = useState({
    systemApplicationId: 0,
    name: '',
    description: '',
    isRoot: false,
    isDefault: false,
    isActive: true,
  });

  const loadSistemasApi = useApi({
    onSuccess: (data: SystemApplication[]) => {
      setSistemas(data.filter((item) => item.isActive));
    },
  });

  const loadAccessResourcesApi = useApi({
    onSuccess: (data: AccessResource[]) => {
      setAccessResources(data);
    },
    onError: (error) => {
      toast({
        title: t('common.toast.errorTitle'),
        description: error.message,
        variant: 'destructive',
      });
    },
  });

  const loadCapabilitiesApi = useApi({
    onSuccess: (data: AccessCapability[]) => {
      setCapabilities(data);
    },
    onError: () => {
      setCapabilities([]);
    },
  });

  const savePerfilPadraoApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: t('common.toast.successTitle'),
        description: perfilPadrao ? t('systemRoleTemplate.form.toast.updated') : t('systemRoleTemplate.form.toast.created'),
      });
      clearErrors();
      onSuccess();
      onClose();
    },
    onError: (error) => {
      setErrors(error);
      if (!error.errors) {
        toast({
          title: t('common.toast.errorTitle'),
          description: error.message,
          variant: 'destructive',
        });
      }
    },
  });

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    loadSistemasApi.execute(() => SystemApplicationService.getActive());
    loadAccessResourcesApi.execute(() => AccessResourceService.getAll());

    if (perfilPadrao) {
      setSelectedResourceIds(perfilPadrao.accessResourceIds ?? []);
      setSelectedCapabilityKeys(perfilPadrao.capabilityKeys ?? []);
      setFormData({
        systemApplicationId: perfilPadrao.systemApplicationId,
        name: perfilPadrao.name,
        description: perfilPadrao.description,
        isRoot: perfilPadrao.isRoot,
        isDefault: perfilPadrao.isDefault,
        isActive: perfilPadrao.isActive,
      });
      return;
    }

    setSelectedResourceIds([]);
    setSelectedCapabilityKeys([]);
    setFormData({
      systemApplicationId: systemApplicationId ?? 0,
      name: '',
      description: '',
      isRoot: false,
      isDefault: false,
      isActive: true,
    });
  }, [isOpen, perfilPadrao, systemApplicationId]);

  useEffect(() => {
    if (perfilPadrao || systemApplicationId || formData.systemApplicationId !== 0 || sistemas.length === 0) {
      return;
    }

    setFormData((current) => ({
      ...current,
      systemApplicationId: sistemas[0].id,
    }));
  }, [perfilPadrao, systemApplicationId, formData.systemApplicationId, sistemas]);

  useEffect(() => {
    if (!isOpen || formData.systemApplicationId <= 0) {
      return;
    }

    loadCapabilitiesApi.execute(() => AccessCapabilityService.getBySystemApplicationId(formData.systemApplicationId));
  }, [isOpen, formData.systemApplicationId]);

  const availableAccessResources = formData.systemApplicationId
    ? accessResources.filter((resource) => resource.systemApplicationId === formData.systemApplicationId)
    : [];

  const modules = useMemo(() => groupByModule(capabilities), [capabilities]);
  const baselineKeys = useMemo(() => capabilities.filter((item) => item.isBaseline).map((item) => item.key), [capabilities]);
  const hasCatalog = capabilities.length > 0;
  const selectableCount = capabilities.length - baselineKeys.length;
  const selectedCount = selectedCapabilityKeys.filter((key) => !baselineKeys.includes(key)).length;

  const handleInputChange = (field: string, value: string | number | boolean) => {
    setFormData((current) => ({
      ...current,
      [field]: value,
    }));
  };

  const toggleCapability = (key: string, checked: boolean) => {
    setSelectedCapabilityKeys((current) => {
      if (checked) {
        return current.includes(key) ? current : [...current, key];
      }
      return current.filter((item) => item !== key);
    });
  };

  const toggleModule = (module: CapabilityModule, checked: boolean) => {
    const keys = module.items.filter((item) => !item.isBaseline).map((item) => item.key);
    setSelectedCapabilityKeys((current) => {
      if (checked) {
        return Array.from(new Set([...current, ...keys]));
      }
      return current.filter((item) => !keys.includes(item));
    });
  };

  const handleSave = async () => {
    // As capacidades basicas entram sozinhas na emissao do token; nao precisam ficar no template.
    const capabilityKeys = selectedCapabilityKeys.filter((key) => !baselineKeys.includes(key));

    if (perfilPadrao) {
      const request: UpdateSystemRoleTemplateRequest = {
        name: formData.name,
        description: formData.description,
        isRoot: formData.isRoot,
        isDefault: formData.isDefault,
        isActive: formData.isActive,
        accessResourceIds: selectedResourceIds,
        capabilityKeys,
      };

      await savePerfilPadraoApi.execute(() => SystemRoleTemplateService.update(perfilPadrao.id, request));
      return;
    }

    const request: CreateSystemRoleTemplateRequest = {
      systemApplicationId: formData.systemApplicationId,
      name: formData.name,
      description: formData.description,
      isRoot: formData.isRoot,
      isDefault: formData.isDefault,
      accessResourceIds: selectedResourceIds,
      capabilityKeys,
    };

    await savePerfilPadraoApi.execute(() => SystemRoleTemplateService.create(request));
  };

  const isValid = formData.systemApplicationId > 0 && formData.name.trim() && formData.description.trim();

  const renderCapabilityMatrix = () => (
    <div className="flex flex-col gap-3">
      <div className="flex items-start justify-between gap-4">
        <div className="space-y-1">
          <p className="text-sm font-medium">{t('systemRoleTemplate.form.capabilitiesTitle')}</p>
          <p className="text-sm text-muted-foreground">{t('systemRoleTemplate.form.capabilitiesDescription')}</p>
        </div>
        <div className="text-xs text-muted-foreground">
          {selectedCount} / {selectableCount}
        </div>
      </div>

      <div className="max-h-[52vh] space-y-3 overflow-y-auto pr-1">
        {modules.map((module) => {
          const selectable = module.items.filter((item) => !item.isBaseline);
          const selectedInModule = selectable.filter((item) => selectedCapabilityKeys.includes(item.key)).length;
          const allSelected = selectable.length > 0 && selectedInModule === selectable.length;

          return (
            <div key={module.key} className="rounded-lg border bg-background">
              <div className="flex items-center justify-between gap-4 border-b px-4 py-2.5">
                <div className="flex items-center gap-2">
                  <p className="text-sm font-semibold">{module.label}</p>
                  {selectable.length > 0 ? (
                    <Badge variant="outline">
                      {selectedInModule}/{selectable.length}
                    </Badge>
                  ) : null}
                </div>
                {selectable.length > 1 ? (
                  <label className="flex items-center gap-2 text-xs text-muted-foreground">
                    <Checkbox checked={allSelected} onCheckedChange={(checked) => toggleModule(module, checked === true)} />
                    {t('systemRoleTemplate.form.capabilitiesModuleAll')}
                  </label>
                ) : null}
              </div>

              <div className="grid gap-2 px-4 py-3 md:grid-cols-2 xl:grid-cols-3">
                {module.items.map((capability) => {
                  const checked = capability.isBaseline || selectedCapabilityKeys.includes(capability.key);
                  return (
                    <label
                      key={capability.key}
                      className={`flex min-w-0 items-start gap-3 rounded-md border p-3 text-sm transition-colors ${capability.isBaseline ? 'bg-muted/30' : 'hover:bg-muted/30'}`}
                      title={capability.description || undefined}
                    >
                      <Checkbox
                        checked={checked}
                        disabled={capability.isBaseline}
                        onCheckedChange={(value) => toggleCapability(capability.key, value === true)}
                      />
                      <div className="min-w-0 flex-1 space-y-0.5">
                        <div className="flex flex-wrap items-center gap-2">
                          <span className="font-medium">{capability.label}</span>
                          {capability.isBaseline ? <Badge variant="outline">{t('systemRoleTemplate.form.capabilitiesBaseline')}</Badge> : null}
                        </div>
                        {capability.description ? (
                          <p className="text-xs text-muted-foreground">{capability.description}</p>
                        ) : null}
                      </div>
                    </label>
                  );
                })}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );

  const renderResourcePicker = () => (
    <div className="rounded-lg border bg-muted/20 p-4">
      <div className="flex items-start justify-between gap-4">
        <div className="space-y-1">
          <p className="text-sm font-medium">{t('systemRoleTemplate.form.permissionsTitle')}</p>
          <p className="text-sm text-muted-foreground">{t('systemRoleTemplate.form.permissionsDescription')}</p>
        </div>
      </div>

      <div className="mt-4 flex flex-wrap items-center gap-3">
        <Button
          variant="secondary"
          onClick={() => setIsPermissionsModalOpen(true)}
          disabled={formData.systemApplicationId <= 0}
        >
          {t('common.action.selectPermissions')}
        </Button>
      </div>
    </div>
  );

  return (
    <>
      <Modal open={isOpen} onOpenChange={onClose}>
        <ModalContent size={hasCatalog && !formData.isRoot ? '5xl' : 'xl'}>
          <ModalHeader>
            <ModalTitle>{perfilPadrao ? t('systemRoleTemplate.form.editTitle') : t('systemRoleTemplate.form.createTitle')}</ModalTitle>
          </ModalHeader>

          <div className="flex flex-col gap-4 py-4">
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">
                {t('systemRoleTemplate.list.systemLabel')} <span className="text-destructive">*</span>
              </label>
              <SearchableSelect
                options={sistemas.map((sistema) => ({
                  label: sistema.name,
                  value: sistema.id.toString(),
                }))}
                value={formData.systemApplicationId > 0 ? formData.systemApplicationId.toString() : undefined}
                onValueChange={(value) => handleInputChange('systemApplicationId', parseInt(value))}
                placeholder={t('systemRoleTemplate.form.systemPlaceholder')}
                searchPlaceholder={t('systemRoleTemplate.form.systemSearchPlaceholder')}
                disabled={!!perfilPadrao || !!systemApplicationId}
              />
              {getError('systemApplicationId') ? (
                <span className="text-sm text-destructive">{getError('systemApplicationId')}</span>
              ) : null}
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="flex flex-col gap-2">
                <label className="text-sm font-medium">
                  {t('common.column.name')} <span className="text-destructive">*</span>
                </label>
                <Input
                  value={formData.name}
                  onChange={(event) => handleInputChange('name', event.target.value)}
                  error={!!getError('name')}
                  helperText={getError('name')}
                />
              </div>

              <div className="flex flex-col gap-2">
                <label className="text-sm font-medium">
                  {t('common.field.description')} <span className="text-destructive">*</span>
                </label>
                <Input
                  value={formData.description}
                  onChange={(event) => handleInputChange('description', event.target.value)}
                  error={!!getError('description')}
                  helperText={getError('description')}
                />
              </div>
            </div>

            <div className="flex gap-6 pt-2">
              <div className="flex items-center gap-2">
                <Switch checked={formData.isRoot} onCheckedChange={(checked) => handleInputChange('isRoot', checked)} />
                <label className="cursor-pointer text-sm font-medium">{t('role.field.isRoot')}</label>
              </div>

              <div className="flex items-center gap-2">
                <Switch checked={formData.isDefault} onCheckedChange={(checked) => handleInputChange('isDefault', checked)} />
                  <label className="cursor-pointer text-sm font-medium">{t('systemRoleTemplate.form.defaultLabel')}</label>
              </div>

              {perfilPadrao ? (
                <div className="flex items-center gap-2">
                  <Switch checked={formData.isActive} onCheckedChange={(checked) => handleInputChange('isActive', checked)} />
                  <label className="cursor-pointer text-sm font-medium">{t('common.status.active')}</label>
                </div>
              ) : null}
            </div>

            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">{t('common.field.permissions')}</label>
              {formData.isRoot ? (
                <div className="rounded-md border border-dashed border-warning/40 bg-warning/10 p-3 text-sm">
                  {t('systemRoleTemplate.form.capabilitiesRootHint')}
                </div>
              ) : hasCatalog ? (
                renderCapabilityMatrix()
              ) : (
                renderResourcePicker()
              )}
            </div>
          </div>

          <ModalFooter>
            <Button variant="outline" onClick={onClose} disabled={savePerfilPadraoApi.isLoading}>
              {t('common.action.cancel')}
            </Button>
            <Button variant="primary" onClick={handleSave} loading={savePerfilPadraoApi.isLoading} disabled={!isValid}>
              {perfilPadrao ? t('common.action.update') : t('common.action.create')}
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>

      {hasCatalog ? null : (
        <RolePermissionsModal
          isOpen={isPermissionsModalOpen}
          onClose={() => setIsPermissionsModalOpen(false)}
          resources={availableAccessResources}
          selectedResourceIds={selectedResourceIds}
          onConfirm={setSelectedResourceIds}
        />
      )}
    </>
  );
}
