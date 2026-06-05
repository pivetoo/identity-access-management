import { useState, useEffect } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Input, Button, Switch, useApi, toast, useFormErrors, useI18n } from 'archon-ui';
import { SystemApplicationService } from '../../services/systemApplicationService';
import type { SystemApplication, CreateSystemApplicationRequest, UpdateSystemApplicationRequest } from '../../types/systemApplication';

interface SystemApplicationFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  sistema?: SystemApplication;
  onSuccess: () => void;
}

export default function SystemApplicationFormModal({
  isOpen,
  onClose,
  sistema,
  onSuccess
}: SystemApplicationFormModalProps) {
  const { t } = useI18n()
  const { getError, setErrors, clearErrors } = useFormErrors();
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    isActive: true,
    audience: '',
    baseUrl: '',
  });

  const saveSistemaApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: t('common.toast.successTitle'),
        description: sistema ? t('systemApplication.form.toast.updated') : t('systemApplication.form.toast.created'),
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
    }
  });

  useEffect(() => {
    if (isOpen) {
      if (sistema) {
        setFormData({
          name: sistema.name,
          description: sistema.description || '',
          isActive: sistema.isActive,
          audience: sistema.audience,
          baseUrl: sistema.baseUrl || '',
        });
      } else {
        setFormData({
          name: '',
          description: '',
          isActive: true,
          audience: '',
          baseUrl: '',
        });
      }
    }
  }, [isOpen, sistema]);

  const handleInputChange = (field: string, value: string | boolean | number) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
  };

  const handleSave = async () => {
    if (sistema) {
      const updateData: UpdateSystemApplicationRequest = {
        id: sistema.id,
        name: formData.name,
        description: formData.description,
        isActive: formData.isActive,
        audience: formData.audience,
        baseUrl: formData.baseUrl,
      };
      await saveSistemaApi.execute(() => SystemApplicationService.update(sistema.id, updateData));
    } else {
      const createData: CreateSystemApplicationRequest = {
        name: formData.name,
        description: formData.description,
        audience: formData.audience,
        baseUrl: formData.baseUrl,
      };
      await saveSistemaApi.execute(() => SystemApplicationService.create(createData));
    }
  };

  const isValid = formData.name && formData.audience;

  return (
    <Modal open={isOpen} onOpenChange={onClose}>
      <ModalContent size="xl">
        <ModalHeader>
          <ModalTitle>{sistema ? t('systemApplication.form.editTitle') : t('systemApplication.form.createTitle')}</ModalTitle>
        </ModalHeader>

        <div className="flex flex-col gap-4 py-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">
                {t('common.column.name')} <span className="text-destructive">*</span>
              </label>
              <Input
                value={formData.name}
                onChange={(e) => handleInputChange('name', e.target.value)}
                error={!!getError('name')}
                helperText={getError('name')}
              />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">
                {t('systemApplication.field.audience')} <span className="text-destructive">*</span>
              </label>
              <Input
                value={formData.audience}
                onChange={(e) => handleInputChange('audience', e.target.value)}
                error={!!getError('audience')}
                helperText={getError('audience')}
              />
            </div>
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">{t('systemApplication.field.baseUrl')}</label>
            <Input
              value={formData.baseUrl}
              onChange={(e) => handleInputChange('baseUrl', e.target.value)}
              placeholder="https://exemplo.mainstay.com.br"
              error={!!getError('baseUrl')}
              helperText={getError('baseUrl')}
            />
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">{t('common.field.description')}</label>
            <Input
              value={formData.description}
              onChange={(e) => handleInputChange('description', e.target.value)}
              error={!!getError('description')}
              helperText={getError('description')}
            />
          </div>

          {sistema && (
            <div className="flex items-center gap-2 pt-2">
              <Switch
                checked={formData.isActive}
                onCheckedChange={(checked) => handleInputChange('isActive', checked)}
              />
              <label className="text-sm font-medium cursor-pointer">
                {t('common.status.active')}
              </label>
            </div>
          )}
        </div>

        <ModalFooter>
          <Button
            variant="outline"
            onClick={onClose}
            disabled={saveSistemaApi.isLoading}
          >
            {t('common.action.cancel')}
          </Button>
          <Button
            variant="primary"
            onClick={handleSave}
            loading={saveSistemaApi.isLoading}
            disabled={!isValid}
          >
            {sistema ? t('common.action.update') : t('common.action.create')}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
