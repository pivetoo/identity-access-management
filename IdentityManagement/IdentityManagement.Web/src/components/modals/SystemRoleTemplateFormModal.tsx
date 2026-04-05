import { useEffect, useState } from 'react';
import { Button, Input, Modal, ModalContent, ModalFooter, ModalHeader, ModalTitle, SearchableSelect, Switch, toast, useApi, useFormErrors, useI18n } from 'archon-ui';
import { AccessResourceService } from '../../services/accessResourceService';
import { SystemRoleTemplateService } from '../../services/systemRoleTemplateService';
import { SystemApplicationService } from '../../services/systemApplicationService';
import type { AccessResource } from '../../types/accessResource';
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
  const [isPermissionsModalOpen, setIsPermissionsModalOpen] = useState(false);
  const [selectedResourceIds, setSelectedResourceIds] = useState<number[]>([]);
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

  const availableAccessResources = formData.systemApplicationId
    ? accessResources.filter((resource) => resource.systemApplicationId === formData.systemApplicationId)
    : [];

  const handleInputChange = (field: string, value: string | number | boolean) => {
    setFormData((current) => ({
      ...current,
      [field]: value,
    }));
  };

  const handleSave = async () => {
    if (perfilPadrao) {
      const request: UpdateSystemRoleTemplateRequest = {
        name: formData.name,
        description: formData.description,
        isRoot: formData.isRoot,
        isDefault: formData.isDefault,
        isActive: formData.isActive,
        accessResourceIds: selectedResourceIds,
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
    };

    await savePerfilPadraoApi.execute(() => SystemRoleTemplateService.create(request));
  };

  const isValid = formData.systemApplicationId > 0 && formData.name.trim() && formData.description.trim();

  return (
    <>
      <Modal open={isOpen} onOpenChange={onClose}>
        <ModalContent size="xl">
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
              <div className="rounded-lg border bg-muted/20 p-4">
                <div className="flex items-start justify-between gap-4">
                  <div className="space-y-1">
                    <p className="text-sm font-medium">{t('systemRoleTemplate.form.permissionsTitle')}</p>
                    <p className="text-sm text-muted-foreground">
                      {t('systemRoleTemplate.form.permissionsDescription')}
                    </p>
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

      <RolePermissionsModal
        isOpen={isPermissionsModalOpen}
        onClose={() => setIsPermissionsModalOpen(false)}
        resources={availableAccessResources}
        selectedResourceIds={selectedResourceIds}
        onConfirm={setSelectedResourceIds}
      />
    </>
  );
}
