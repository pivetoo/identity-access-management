import { useState, useEffect } from 'react';
import { Ban, RotateCcw } from 'lucide-react';
import { PageLayout, DataTablePreview, Badge, Button, ConfirmModal, FilterDropdown, TableToolbar, toast, useApi, useI18n } from 'archon-ui';
import type { DataTablePreviewColumn, PageAction, PaginatedResult } from 'archon-ui';
import { ContractService } from '../../../services/contractService';
import type { Contract } from '../../../types/contract';
import ContractFormModal from '../../../components/modals/ContractFormModal';
import { formatDate } from '../../../utils/date';

export default function Contracts() {
  const { t } = useI18n()
  const [selectedContratos, setSelectedContratos] = useState<Contract[]>([]);
  const [contratos, setContratos] = useState<Contract[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingContrato, setEditingContrato] = useState<Contract | undefined>();
  const [isConfirmToggleOpen, setIsConfirmToggleOpen] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'inactive'>('all');
  const pageSize = 30;

  const loadContratosApi = useApi({
    onSuccess: (data: PaginatedResult<Contract>) => {
      setContratos(data.data);
      setHasMore(data.data.length === pageSize);
    }
  });

  const loadMoreContratosApi = useApi({
    onSuccess: (data: PaginatedResult<Contract>) => {
      setContratos(prev => [...prev, ...data.data]);
      setHasMore(data.data.length === pageSize);
    }
  });

  const toggleContratoApi = useApi({
    onSuccess: () => {
      const wasInactive = selectedContratos[0] && !selectedContratos[0].isActive;
      toast({
        variant: 'success',
        title: t('common.toast.successTitle'),
        description: wasInactive ? t('contract.list.toast.activated') : t('contract.list.toast.inactivated'),
      });
      loadContratos(true);
      setSelectedContratos([]);
      setIsConfirmToggleOpen(false);
    },
    onError: () => {
      setIsConfirmToggleOpen(false);
    }
  });

  const hasActiveSelected = selectedContratos.some(c => c.isActive);
  const hasInactiveSelected = selectedContratos.some(c => !c.isActive);
  const isSelectedInactive = selectedContratos.length > 0 && !selectedContratos[0].isActive;

  const loadContratos = async (reset = false) => {
    if (reset) {
      setContratos([]);
    }
    await loadContratosApi.execute(() =>
      ContractService.getAll({
        page: 1,
        pageSize: pageSize,
        orderBy: 'id'
      })
    );
  };

  const loadMoreContratos = async () => {
    const currentPage = Math.floor(contratos.length / pageSize) + 1;
    await loadMoreContratosApi.execute(() =>
      ContractService.getAll({
        page: currentPage + 1,
        pageSize: pageSize,
        orderBy: 'id'
      })
    );
  };

  useEffect(() => {
    loadContratos(true);
  }, []);

  const handleAddContrato = () => {
    setEditingContrato(undefined);
    setIsModalOpen(true);
  };

  const handleEditContrato = () => {
    if (selectedContratos.length === 0) {
      toast({
        variant: 'warning',
        title: t('common.toast.warningTitle'),
        description: t('contract.list.validation.selectToEdit'),
      });
      return;
    }
    setEditingContrato(selectedContratos[0]);
    setIsModalOpen(true);
  };

  const handleToggleContrato = () => {
    if (selectedContratos.length === 0) {
      toast({
        variant: 'warning',
        title: t('common.toast.warningTitle'),
        description: t('contract.list.validation.selectToToggle'),
      });
      return;
    }
    setIsConfirmToggleOpen(true);
  };

  const handleConfirmToggle = async () => {
    if (selectedContratos.length === 0) return;
    const contrato = selectedContratos[0];
    await toggleContratoApi.execute(() => ContractService.toggleActive(contrato.id, contrato));
  };

  const handleRefresh = () => {
    loadContratos(true);
  };

  const formatLifetime = (seconds: number) => {
    if (!seconds || seconds <= 0) {
      return t('common.value.notAvailable');
    }

    const days = seconds / 86400;
    if (Number.isInteger(days) && days >= 1) {
      return t(days === 1 ? 'common.duration.day' : 'common.duration.days').replace('{0}', String(days));
    }

    const hours = seconds / 3600;
    if (Number.isInteger(hours) && hours >= 1) {
      return t(hours === 1 ? 'common.duration.hour' : 'common.duration.hours').replace('{0}', String(hours));
    }

    const minutes = seconds / 60;
    if (Number.isInteger(minutes) && minutes >= 1) {
      return t(minutes === 1 ? 'common.duration.minute' : 'common.duration.minutes').replace('{0}', String(minutes));
    }

    return `${seconds} s`;
  };

  const columns: DataTablePreviewColumn<Contract>[] = [
    {
      key: 'companyName',
      title: t('contract.field.company'),
      dataIndex: 'companyName',
    },
    {
      key: 'systemApplicationName',
      title: t('contract.field.systemApplication'),
      dataIndex: 'systemApplicationName',
    },
    {
      key: 'startDate',
      title: t('common.field.startDate'),
      dataIndex: 'startDate',
      render: (value: string) => formatDate(value),
    },
    {
      key: 'endDate',
      title: t('common.field.endDate'),
      dataIndex: 'endDate',
      render: (value: string) => formatDate(value),
    },
    {
      key: 'isActive',
      title: t('common.column.status'),
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? t('common.status.active') : t('common.status.inactive')}
        </Badge>
      )
    },
  ];

  const handleModalSuccess = () => {
    setSelectedContratos([]);
    loadContratos(true);
  };

  const handleModalClose = () => {
    setIsModalOpen(false);
    setEditingContrato(undefined);
  };

  const filteredContratos = contratos.filter((contrato) => {
    const search = searchTerm.trim().toLowerCase();
    const matchesSearch = !search || [
      contrato.companyName ?? '',
      contrato.systemApplicationName ?? ''
    ].some((value) => value.toLowerCase().includes(search));
    const matchesStatus =
      statusFilter === 'all' ||
      (statusFilter === 'active' && contrato.isActive) ||
      (statusFilter === 'inactive' && !contrato.isActive);

    return matchesSearch && matchesStatus;
  });

  const customActions: PageAction[] = [];

  if (hasActiveSelected) {
    customActions.push({
      key: 'inactivate',
      label: t('common.action.inactivate'),
      icon: <Ban className="h-4 w-4" />,
      variant: 'outline',
      onClick: handleToggleContrato,
      disabled: selectedContratos.length === 0
    });
  }

  if (hasInactiveSelected) {
    customActions.push({
      key: 'reactivate',
      label: t('common.action.reactivate'),
      icon: <RotateCcw className="h-4 w-4" />,
      variant: 'outline',
      onClick: handleToggleContrato,
      disabled: selectedContratos.length === 0
    });
  }

  return (
    <PageLayout
      title={t('contract.list.title')}
      subtitle={t('contract.list.subtitle')}
      onAdd={handleAddContrato}
      onEdit={handleEditContrato}
      onRefresh={handleRefresh}
      selectedRowsCount={selectedContratos.length}
      actions={customActions}
    >
      <div className="space-y-4">
        <TableToolbar
          searchValue={searchTerm}
          onSearchChange={setSearchTerm}
          searchPlaceholder={t('contract.list.searchPlaceholder')}
          rightSlot={
            <FilterDropdown
              label={t('contract.list.filterLabel')}
              value={statusFilter}
              onChange={(value) => setStatusFilter(value as 'all' | 'active' | 'inactive')}
              options={[
                { value: 'active', label: t('common.filter.activeOnly') },
                { value: 'inactive', label: t('common.filter.inactiveOnly') },
              ]}
            />
          }
        />

        <DataTablePreview
          columns={columns}
          data={filteredContratos}
          rowKey="id"
          selectedRow={selectedContratos[0] ?? null}
          onRowSelect={(selected) => setSelectedContratos(selected ? [selected] : [])}
          renderDetail={(record) => (
            <div className="space-y-5 p-5">
              <div className="space-y-1">
                <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-primary/80">
                  {t('contract.preview.title')}
                </div>
                <h3 className="text-xl font-semibold text-primary">
                  {record.companyName} · {record.systemApplicationName}
                </h3>
                <p className="text-sm text-primary/80">
                  {t('contract.preview.subtitle')}
                </p>
              </div>

              <div className="grid gap-3 sm:grid-cols-2">
                <div className="rounded-lg border border-border/70 bg-muted/20 p-3">
                  <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                    {t('common.field.situation')}
                  </div>
                  <div className="mt-2">
                    <Badge variant={record.isActive ? 'success' : 'destructive'}>
                      {record.isActive ? t('common.status.active') : t('common.status.inactive')}
                    </Badge>
                  </div>
                </div>
              </div>

              <div className="grid gap-3 sm:grid-cols-2">
                <div className="rounded-lg border border-border/70 bg-muted/20 p-3">
                  <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                    {t('common.field.startDate')}
                  </div>
                  <div className="mt-1 text-sm font-medium text-foreground">{formatDate(record.startDate)}</div>
                </div>

                <div className="rounded-lg border border-border/70 bg-muted/20 p-3">
                  <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                    {t('common.field.endDate')}
                  </div>
                  <div className="mt-1 text-sm font-medium text-foreground">{formatDate(record.endDate)}</div>
                </div>
              </div>

              <div className="grid gap-3 sm:grid-cols-2">
                <div className="rounded-lg border border-border/70 bg-muted/20 p-3">
                  <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                    {t('contract.preview.accessToken')}
                  </div>
                  <div className="mt-1 text-sm font-medium text-foreground">{formatLifetime(record.accessTokenLifetime)}</div>
                </div>

                <div className="rounded-lg border border-border/70 bg-muted/20 p-3">
                  <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                    {t('contract.preview.refreshToken')}
                  </div>
                  <div className="mt-1 text-sm font-medium text-foreground">{formatLifetime(record.refreshTokenLifetime)}</div>
                </div>
              </div>
            </div>
          )}
        />

        {hasMore && (
          <div className="flex justify-end">
            <Button
              variant="outline"
              onClick={loadMoreContratos}
              loading={loadMoreContratosApi.isLoading}
            >
              {t('common.action.loadMore')}
            </Button>
          </div>
        )}
      </div>

      <ContractFormModal
        isOpen={isModalOpen}
        onClose={handleModalClose}
        contrato={editingContrato}
        onSuccess={handleModalSuccess}
      />

      <ConfirmModal
        open={isConfirmToggleOpen}
        onOpenChange={(open) => setIsConfirmToggleOpen(open)}
        onConfirm={handleConfirmToggle}
        title={isSelectedInactive ? t('contract.list.confirmActivateTitle') : t('contract.list.confirmInactivateTitle')}
        description={
          selectedContratos.length > 0
            ? t(isSelectedInactive ? 'contract.list.confirmActivateDescription' : 'contract.list.confirmInactivateDescription')
              .replace('{0}', selectedContratos[0].companyName)
              .replace('{1}', selectedContratos[0].systemApplicationName)
            : t(isSelectedInactive ? 'contract.list.confirmActivateDescriptionFallback' : 'contract.list.confirmInactivateDescriptionFallback')
        }
        confirmText={isSelectedInactive ? t('common.action.activate') : t('common.action.inactivate')}
        variant={isSelectedInactive ? 'primary' : 'danger'}
        loading={toggleContratoApi.isLoading}
      />
    </PageLayout>
  );
}
