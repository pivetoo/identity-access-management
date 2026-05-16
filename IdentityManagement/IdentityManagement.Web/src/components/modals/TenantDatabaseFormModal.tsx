import { useState, useEffect, useMemo } from 'react';
import { Eye, EyeOff } from 'lucide-react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Input, Button, Switch, SearchableSelect, useApi, toast, useFormErrors, useI18n } from 'archon-ui';
import { TenantDatabaseService } from '../../services/tenantDatabaseService';
import { ContractService } from '../../services/contractService';
import type { TenantDatabase, CreateTenantDatabaseRequest, UpdateTenantDatabaseRequest, DatabaseProvider } from '../../types/tenantDatabase';
import { DatabaseProviderValue } from '../../types/tenantDatabase';
import type { Contract } from '../../types/contract';

interface TenantDatabaseFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenant?: TenantDatabase;
  existingContractIds: number[];
  onSuccess: () => void;
}

export default function TenantDatabaseFormModal({
  isOpen,
  onClose,
  tenant,
  existingContractIds,
  onSuccess
}: TenantDatabaseFormModalProps) {
  const { t } = useI18n();
  const { getError, setErrors, clearErrors } = useFormErrors();
  const [contratos, setContratos] = useState<Contract[]>([]);
  const [showSecret, setShowSecret] = useState(false);
  const [formData, setFormData] = useState({
    contractId: 0,
    connectionString: '',
    databaseProvider: DatabaseProviderValue.PostgreSql as DatabaseProvider,
    schemaName: 'public',
    apiKey: '',
    isActive: true,
  });

  const toSelectValue = (value?: number) =>
    typeof value === 'number' && Number.isFinite(value) && value > 0 ? value.toString() : undefined;

  const hasValidId = (value: unknown): value is number =>
    typeof value === 'number' && Number.isFinite(value) && value > 0;

  const loadContratosApi = useApi({
    onSuccess: (data: Contract[]) => {
      setContratos(data);
    },
    onError: () => {
      toast({
        title: t('common.toast.errorTitle'),
        description: t('tenantDatabase.form.toast.loadContractsError'),
        variant: 'destructive',
      });
    },
  });

  const saveApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: t('common.toast.successTitle'),
        description: tenant ? t('tenantDatabase.updated') : t('tenantDatabase.created'),
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

    setShowSecret(false);
    loadContratosApi.execute(() => ContractService.getActive());

    if (tenant) {
      setFormData({
        contractId: tenant.contractId,
        connectionString: tenant.connectionString,
        databaseProvider: tenant.databaseProvider,
        schemaName: tenant.schemaName || 'public',
        apiKey: tenant.apiKey,
        isActive: tenant.isActive,
      });
    } else {
      setFormData({
        contractId: 0,
        connectionString: '',
        databaseProvider: DatabaseProviderValue.PostgreSql,
        schemaName: 'public',
        apiKey: '',
        isActive: true,
      });
    }
  }, [isOpen, tenant]);

  const availableContracts = useMemo(() => {
    if (tenant) {
      return contratos.filter((contract) => contract.id === tenant.contractId);
    }

    return contratos.filter(
      (contract) => hasValidId(contract.id) && !existingContractIds.includes(contract.id),
    );
  }, [contratos, tenant, existingContractIds]);

  const handleInputChange = (field: string, value: string | boolean | number) => {
    setFormData((prev) => ({
      ...prev,
      [field]: value,
    }));
  };

  const handleSave = async () => {
    if (tenant) {
      const updateData: UpdateTenantDatabaseRequest = {
        id: tenant.id,
        connectionString: formData.connectionString,
        databaseProvider: formData.databaseProvider,
        schemaName: formData.schemaName || undefined,
        apiKey: formData.apiKey,
        isActive: formData.isActive,
      };
      await saveApi.execute(() => TenantDatabaseService.update(tenant.id, updateData));
    } else {
      const createData: CreateTenantDatabaseRequest = {
        contractId: formData.contractId,
        connectionString: formData.connectionString,
        databaseProvider: formData.databaseProvider,
        schemaName: formData.schemaName || undefined,
        apiKey: formData.apiKey,
      };
      await saveApi.execute(() => TenantDatabaseService.create(createData));
    }
  };

  const isValid =
    formData.contractId > 0 &&
    formData.connectionString.trim().length > 0 &&
    formData.apiKey.trim().length > 0;

  const providerOptions = [
    { label: 'PostgreSQL', value: DatabaseProviderValue.PostgreSql.toString() },
    { label: 'SQL Server', value: DatabaseProviderValue.SqlServer.toString() },
    { label: 'MySQL', value: DatabaseProviderValue.MySql.toString() },
  ];

  return (
    <Modal open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <ModalContent size="2xl">
        <ModalHeader>
          <ModalTitle>
            {tenant ? t('tenantDatabase.form.editTitle') : t('tenantDatabase.form.createTitle')}
          </ModalTitle>
        </ModalHeader>

        <div className="flex flex-col gap-4 py-4">
          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">
              {t('tenantDatabase.field.contract')} <span className="text-destructive">*</span>
            </label>
            <SearchableSelect
              options={availableContracts.map((contract) => ({
                label: `${contract.companyName} · ${contract.systemApplicationName}`,
                value: contract.id.toString(),
              }))}
              value={toSelectValue(formData.contractId)}
              onValueChange={(value) => handleInputChange('contractId', Number(value))}
              placeholder={t('tenantDatabase.form.contractPlaceholder')}
              searchPlaceholder={t('tenantDatabase.form.contractSearchPlaceholder')}
              disabled={!!tenant || loadContratosApi.isLoading}
            />
          </div>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">
                {t('tenantDatabase.field.databaseProvider')} <span className="text-destructive">*</span>
              </label>
              <SearchableSelect
                options={providerOptions}
                value={formData.databaseProvider.toString()}
                onValueChange={(value) => handleInputChange('databaseProvider', Number(value))}
                placeholder={t('tenantDatabase.form.providerPlaceholder')}
              />
            </div>

            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">{t('tenantDatabase.field.schemaName')}</label>
              <Input
                value={formData.schemaName}
                onChange={(e) => handleInputChange('schemaName', e.target.value)}
                placeholder="public"
                error={!!getError('schemaName')}
                helperText={getError('schemaName')}
              />
            </div>
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">
              {t('tenantDatabase.field.connectionString')} <span className="text-destructive">*</span>
            </label>
            <Input
              value={formData.connectionString}
              onChange={(e) => handleInputChange('connectionString', e.target.value)}
              placeholder="Host=...;Port=5432;Database=...;Username=...;Password=...;"
              error={!!getError('connectionString')}
              helperText={getError('connectionString')}
            />
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">
              {t('tenantDatabase.field.apiKey')} <span className="text-destructive">*</span>
            </label>
            <div className="relative">
              <Input
                type={showSecret ? 'text' : 'password'}
                value={formData.apiKey}
                onChange={(e) => handleInputChange('apiKey', e.target.value)}
                error={!!getError('apiKey')}
                helperText={getError('apiKey')}
                className="pr-10"
              />
              <button
                type="button"
                onClick={() => setShowSecret((prev) => !prev)}
                className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                tabIndex={-1}
              >
                {showSecret ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
              </button>
            </div>
          </div>

          {tenant && (
            <div className="flex items-center gap-2 pt-2">
              <Switch
                checked={formData.isActive}
                onCheckedChange={(checked) => handleInputChange('isActive', checked)}
              />
              <label className="text-sm font-medium cursor-pointer">
                {t('tenantDatabase.form.activeLabel')}
              </label>
            </div>
          )}
        </div>

        <ModalFooter>
          <Button variant="outline" onClick={onClose} disabled={saveApi.isLoading}>
            {t('common.action.cancel')}
          </Button>
          <Button
            variant="primary"
            onClick={handleSave}
            loading={saveApi.isLoading}
            disabled={!isValid}
          >
            {tenant ? t('common.action.update') : t('common.action.create')}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
