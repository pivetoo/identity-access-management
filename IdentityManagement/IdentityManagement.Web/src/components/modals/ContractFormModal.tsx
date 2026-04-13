import { useState, useEffect } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Input, Button, Switch, SearchableSelect, useApi, toast, useFormErrors, useI18n } from 'archon-ui';
import { ContractService } from '../../services/contractService';
import { CompanyService } from '../../services/companyService';
import { SystemApplicationService } from '../../services/systemApplicationService';
import type { Contract, CreateContractRequest, UpdateContractRequest } from '../../types/contract';
import type { Company } from '../../types/company';
import type { SystemApplication } from '../../types/systemApplication';

interface ContractFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  contrato?: Contract;
  onSuccess: () => void;
}

export default function ContractFormModal({
  isOpen,
  onClose,
  contrato,
  onSuccess
}: ContractFormModalProps) {
  const { t } = useI18n()
  const { getError, setErrors, clearErrors } = useFormErrors();
  const [empresas, setEmpresas] = useState<Company[]>([]);
  const [sistemas, setSistemas] = useState<SystemApplication[]>([]);
  const [formData, setFormData] = useState({
    companyId: 0,
    systemApplicationId: 0,
    startDate: new Date().toISOString().split('T')[0],
    endDate: '',
    isActive: true,
    accessTokenLifetime: 3600,
    refreshTokenLifetime: 2592000
  });

  const toSelectValue = (value?: number) =>
    typeof value === 'number' && Number.isFinite(value) && value > 0 ? value.toString() : undefined;

  const hasValidId = (value: unknown): value is number =>
    typeof value === 'number' && Number.isFinite(value) && value > 0;

  const defaultStartDate = new Date().toISOString().split('T')[0];

  const loadEmpresasApi = useApi({
    onSuccess: (data: Company[]) => {
      setEmpresas(data);
    },
    onError: () => {
      toast({
        title: t('common.toast.errorTitle'),
        description: t('contract.form.toast.loadCompaniesError'),
        variant: 'destructive',
      });
    }
  });

  const loadSistemasApi = useApi({
    onSuccess: (data: SystemApplication[]) => {
      setSistemas(data);
    },
    onError: () => {
      toast({
        title: t('common.toast.errorTitle'),
        description: t('contract.form.toast.loadSystemsError'),
        variant: 'destructive',
      });
    }
  });

  const saveContratoApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: t('common.toast.successTitle'),
        description: contrato ? t('contract.form.toast.updated') : t('contract.form.toast.created'),
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
      loadEmpresasApi.execute(() => CompanyService.getActive());
      loadSistemasApi.execute(() => SystemApplicationService.getActive());

      if (contrato) {
        setFormData({
          companyId: hasValidId(contrato.companyId) ? contrato.companyId : 0,
          systemApplicationId: hasValidId(contrato.systemApplicationId) ? contrato.systemApplicationId : 0,
          startDate: contrato.startDate?.split('T')[0] || defaultStartDate,
          endDate: contrato.endDate?.split('T')[0] || '',
          isActive: contrato.isActive,
          accessTokenLifetime: contrato.accessTokenLifetime ?? 3600,
          refreshTokenLifetime: contrato.refreshTokenLifetime ?? 2592000
        });
      } else {
        setFormData({
          companyId: 0,
          systemApplicationId: 0,
          startDate: defaultStartDate,
          endDate: '',
          isActive: true,
          accessTokenLifetime: 3600,
          refreshTokenLifetime: 2592000
        });
      }
    }
  }, [isOpen, contrato]);

  const handleInputChange = (field: string, value: string | boolean | number) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
  };

  const handleSave = async () => {
    if (contrato) {
      const updateData: UpdateContractRequest = {
        id: contrato.id,
        companyId: formData.companyId,
        systemApplicationId: formData.systemApplicationId,
        startDate: formData.startDate,
        endDate: formData.endDate || undefined,
        isActive: formData.isActive,
        accessTokenLifetime: formData.accessTokenLifetime,
        refreshTokenLifetime: formData.refreshTokenLifetime
      };
      await saveContratoApi.execute(() => ContractService.update(contrato.id, updateData));
    } else {
      const createData: CreateContractRequest = {
        companyId: formData.companyId,
        systemApplicationId: formData.systemApplicationId,
        startDate: formData.startDate,
        endDate: formData.endDate || undefined,
        accessTokenLifetime: formData.accessTokenLifetime,
        refreshTokenLifetime: formData.refreshTokenLifetime
      };
      await saveContratoApi.execute(() => ContractService.create(createData));
    }
  };

  const isValid = formData.companyId > 0 && formData.systemApplicationId > 0 && formData.startDate;

  return (
    <Modal open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <ModalContent size="2xl">
        <ModalHeader>
          <ModalTitle>{contrato ? t('contract.form.editTitle') : t('contract.form.createTitle')}</ModalTitle>
        </ModalHeader>

        <div className="flex flex-col gap-4 py-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">
                {t('contract.field.company')} <span className="text-destructive">*</span>
              </label>
              <SearchableSelect
                options={empresas
                  .filter((empresa) => hasValidId(empresa.id))
                  .map((empresa) => ({
                    label: empresa.legalName,
                    value: empresa.id.toString()
                  }))}
                value={toSelectValue(formData.companyId)}
                onValueChange={(value) => handleInputChange('companyId', Number(value))}
                placeholder={t('contract.form.companyPlaceholder')}
                searchPlaceholder={t('contract.form.companySearchPlaceholder')}
                disabled={!!contrato || loadEmpresasApi.isLoading}
              />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">
                {t('contract.field.systemApplication')} <span className="text-destructive">*</span>
              </label>
              <SearchableSelect
                options={sistemas
                  .filter((sistema) => hasValidId(sistema.id))
                  .map((sistema) => ({
                    label: sistema.name,
                    value: sistema.id.toString()
                  }))}
                value={toSelectValue(formData.systemApplicationId)}
                onValueChange={(value) => handleInputChange('systemApplicationId', Number(value))}
                placeholder={t('contract.form.systemPlaceholder')}
                searchPlaceholder={t('contract.form.systemSearchPlaceholder')}
                disabled={!!contrato || loadSistemasApi.isLoading}
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">
                {t('common.field.startDate')} <span className="text-destructive">*</span>
              </label>
              <Input
                type="date"
                value={formData.startDate}
                onChange={(e) => handleInputChange('startDate', e.target.value)}
                error={!!getError('startDate')}
                helperText={getError('startDate')}
              />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">{t('common.field.endDate')}</label>
              <Input
                type="date"
                value={formData.endDate}
                onChange={(e) => handleInputChange('endDate', e.target.value)}
                error={!!getError('endDate')}
                helperText={getError('endDate')}
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">
                {t('contract.form.accessTokenLifetime')} <span className="text-destructive">*</span>
              </label>
              <Input
                type="number"
                value={formData.accessTokenLifetime}
                onChange={(e) => handleInputChange('accessTokenLifetime', parseInt(e.target.value) || 0)}
                error={!!getError('accessTokenLifetime')}
                helperText={getError('accessTokenLifetime')}
              />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">
                {t('contract.form.refreshTokenLifetime')} <span className="text-destructive">*</span>
              </label>
              <Input
                type="number"
                value={formData.refreshTokenLifetime}
                onChange={(e) => handleInputChange('refreshTokenLifetime', parseInt(e.target.value) || 0)}
                error={!!getError('refreshTokenLifetime')}
                helperText={getError('refreshTokenLifetime')}
              />
            </div>
          </div>

          {contrato && (
            <div className="flex items-center gap-2 pt-2">
              <Switch
                checked={formData.isActive}
                onCheckedChange={(checked) => handleInputChange('isActive', checked)}
              />
              <label className="text-sm font-medium cursor-pointer">
                {t('contract.form.activeLabel')}
              </label>
            </div>
          )}
        </div>

        <ModalFooter>
          <Button
            variant="outline"
            onClick={onClose}
            disabled={saveContratoApi.isLoading}
          >
            {t('common.action.cancel')}
          </Button>
          <Button
            variant="primary"
            onClick={handleSave}
            loading={saveContratoApi.isLoading}
            disabled={!isValid}
          >
            {contrato ? t('common.action.update') : t('common.action.create')}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
