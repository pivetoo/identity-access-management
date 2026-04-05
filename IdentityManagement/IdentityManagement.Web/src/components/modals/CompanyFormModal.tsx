import { useState, useEffect } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Input, Button, Switch, useApi, toast, useFormErrors, useI18n } from 'archon-ui';
import { CompanyService } from '../../services/companyService';
import type { Company, CreateCompanyRequest, UpdateCompanyRequest } from '../../types/company';

interface CompanyFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  company?: Company;
  onSuccess: () => void;
}

export default function CompanyFormModal({
  isOpen,
  onClose,
  company,
  onSuccess
}: CompanyFormModalProps) {
  const { t } = useI18n()
  const { getError, setErrors, clearErrors } = useFormErrors();
  const [formData, setFormData] = useState({
    legalName: '',
    tradeName: '',
    document: '',
    email: '',
    phoneNumber: '',
    isActive: true
  });

  const saveEmpresaApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: t('common.toast.successTitle'),
        description: company ? t('company.form.toast.updated') : t('company.form.toast.created'),
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
      if (company) {
        setFormData({
          legalName: company.legalName,
          tradeName: company.tradeName,
          document: company.document,
          email: company.email,
          phoneNumber: company.phoneNumber,
          isActive: company.isActive
        });
      } else {
        setFormData({
          legalName: '',
          tradeName: '',
          document: '',
          email: '',
          phoneNumber: '',
          isActive: true
        });
      }
    }
  }, [isOpen, company]);

  const handleInputChange = (field: string, value: string | boolean) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
  };

  const handleSave = async () => {
    if (company) {
      const updateData: UpdateCompanyRequest = {
        id: company.id,
        ...formData
      };
      await saveEmpresaApi.execute(() => CompanyService.update(company.id, updateData));
    } else {
      const createData: CreateCompanyRequest = {
        legalName: formData.legalName,
        tradeName: formData.tradeName,
        document: formData.document,
        email: formData.email,
        phoneNumber: formData.phoneNumber
      };
      await saveEmpresaApi.execute(() => CompanyService.create(createData));
    }
  };

  const isValid = formData.legalName && formData.document && formData.email;

  return (
    <Modal open={isOpen} onOpenChange={onClose}>
      <ModalContent size="xl">
        <ModalHeader>
          <ModalTitle>{company ? t('company.form.editTitle') : t('company.form.createTitle')}</ModalTitle>
        </ModalHeader>

        <div className="flex flex-col gap-4 py-4">
          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">
              {t('company.field.legalName')} <span className="text-destructive">*</span>
            </label>
            <Input
              value={formData.legalName}
              onChange={(e) => handleInputChange('legalName', e.target.value)}
              error={!!getError('legalName')}
              helperText={getError('legalName')}
            />
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">{t('company.field.tradeName')}</label>
            <Input
              value={formData.tradeName}
              onChange={(e) => handleInputChange('tradeName', e.target.value)}
              error={!!getError('tradeName')}
              helperText={getError('tradeName')}
            />
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">
              {t('company.field.document')} <span className="text-destructive">*</span>
            </label>
            <Input
              value={formData.document}
              onChange={(e) => handleInputChange('document', e.target.value)}
              error={!!getError('document')}
              helperText={getError('document')}
            />
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">
              {t('common.field.email')} <span className="text-destructive">*</span>
            </label>
            <Input
              type="email"
              value={formData.email}
              onChange={(e) => handleInputChange('email', e.target.value)}
              error={!!getError('email')}
              helperText={getError('email')}
            />
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">{t('common.field.phoneNumber')}</label>
            <Input
              value={formData.phoneNumber}
              onChange={(e) => handleInputChange('phoneNumber', e.target.value)}
              error={!!getError('phoneNumber')}
              helperText={getError('phoneNumber')}
            />
          </div>

          {company && (
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
            disabled={saveEmpresaApi.isLoading}
          >
            {t('common.action.cancel')}
          </Button>
          <Button
            variant="primary"
            onClick={handleSave}
            loading={saveEmpresaApi.isLoading}
            disabled={!isValid}
          >
            {company ? t('common.action.update') : t('common.action.create')}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
