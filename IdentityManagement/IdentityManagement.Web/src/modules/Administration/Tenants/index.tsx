import { useState, useEffect } from 'react';
import { Ban, RotateCcw } from 'lucide-react';
import { PageLayout, DataTablePreview, Badge, Button, ConfirmModal, FilterDropdown, TableToolbar, toast, useApi, useI18n } from 'archon-ui';
import type { DataTablePreviewColumn, PageAction, PaginatedResult } from 'archon-ui';
import { TenantDatabaseService } from '../../../services/tenantDatabaseService';
import type { TenantDatabase, DatabaseProvider } from '../../../types/tenantDatabase';
import { DatabaseProviderValue } from '../../../types/tenantDatabase';
import TenantDatabaseFormModal from '../../../components/modals/TenantDatabaseFormModal';
import { formatDate } from '../../../utils/date';

const providerLabels: Record<DatabaseProvider, string> = {
  [DatabaseProviderValue.PostgreSql]: 'PostgreSQL',
  [DatabaseProviderValue.SqlServer]: 'SQL Server',
  [DatabaseProviderValue.MySql]: 'MySQL',
};

const maskSecret = (value: string): string => {
  if (!value) {
    return '';
  }
  if (value.length <= 8) {
    return '••••••••';
  }
  return `${value.slice(0, 4)}••••${value.slice(-4)}`;
};

export default function Tenants() {
  const { t } = useI18n();
  const [selectedItems, setSelectedItems] = useState<TenantDatabase[]>([]);
  const [items, setItems] = useState<TenantDatabase[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<TenantDatabase | undefined>();
  const [isConfirmToggleOpen, setIsConfirmToggleOpen] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'inactive'>('all');
  const pageSize = 30;

  const loadApi = useApi({
    onSuccess: (data: PaginatedResult<TenantDatabase>) => {
      setItems(data.data);
      setHasMore(data.data.length === pageSize);
    },
  });

  const loadMoreApi = useApi({
    onSuccess: (data: PaginatedResult<TenantDatabase>) => {
      setItems((prev) => [...prev, ...data.data]);
      setHasMore(data.data.length === pageSize);
    },
  });

  const toggleApi = useApi({
    onSuccess: () => {
      const wasInactive = selectedItems[0] && !selectedItems[0].isActive;
      toast({
        variant: 'success',
        title: t('common.toast.successTitle'),
        description: wasInactive
          ? t('tenantDatabase.list.toast.activated')
          : t('tenantDatabase.list.toast.inactivated'),
      });
      loadItems(true);
      setSelectedItems([]);
      setIsConfirmToggleOpen(false);
    },
    onError: () => {
      setIsConfirmToggleOpen(false);
    },
  });

  const hasActiveSelected = selectedItems.some((item) => item.isActive);
  const hasInactiveSelected = selectedItems.some((item) => !item.isActive);
  const isSelectedInactive = selectedItems.length > 0 && !selectedItems[0].isActive;

  const loadItems = async (reset = false) => {
    if (reset) {
      setItems([]);
    }
    await loadApi.execute(() =>
      TenantDatabaseService.getAll({
        page: 1,
        pageSize,
        orderBy: 'id',
      }),
    );
  };

  const loadMoreItems = async () => {
    const currentPage = Math.floor(items.length / pageSize) + 1;
    await loadMoreApi.execute(() =>
      TenantDatabaseService.getAll({
        page: currentPage + 1,
        pageSize,
        orderBy: 'id',
      }),
    );
  };

  useEffect(() => {
    loadItems(true);
  }, []);

  const handleAdd = () => {
    setEditingItem(undefined);
    setIsModalOpen(true);
  };

  const handleEdit = () => {
    if (selectedItems.length === 0) {
      toast({
        variant: 'warning',
        title: t('common.toast.warningTitle'),
        description: t('tenantDatabase.list.validation.selectToEdit'),
      });
      return;
    }
    setEditingItem(selectedItems[0]);
    setIsModalOpen(true);
  };

  const handleToggle = () => {
    if (selectedItems.length === 0) {
      toast({
        variant: 'warning',
        title: t('common.toast.warningTitle'),
        description: t('tenantDatabase.list.validation.selectToToggle'),
      });
      return;
    }
    setIsConfirmToggleOpen(true);
  };

  const handleConfirmToggle = async () => {
    if (selectedItems.length === 0) {
      return;
    }
    await toggleApi.execute(() => TenantDatabaseService.toggleActive(selectedItems[0]));
  };

  const handleRefresh = () => {
    loadItems(true);
  };

  const handleModalSuccess = () => {
    setSelectedItems([]);
    loadItems(true);
  };

  const handleModalClose = () => {
    setIsModalOpen(false);
    setEditingItem(undefined);
  };

  const columns: DataTablePreviewColumn<TenantDatabase>[] = [
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
      key: 'databaseProvider',
      title: t('tenantDatabase.field.databaseProvider'),
      dataIndex: 'databaseProvider',
      render: (value: DatabaseProvider) => providerLabels[value] ?? '-',
    },
    {
      key: 'schemaName',
      title: t('tenantDatabase.field.schemaName'),
      dataIndex: 'schemaName',
    },
    {
      key: 'isActive',
      title: t('common.column.status'),
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? t('common.status.active') : t('common.status.inactive')}
        </Badge>
      ),
    },
  ];

  const filteredItems = items.filter((item) => {
    const search = searchTerm.trim().toLowerCase();
    const matchesSearch =
      !search ||
      [item.companyName ?? '', item.systemApplicationName ?? '', item.applicationId ?? ''].some(
        (value) => value.toLowerCase().includes(search),
      );
    const matchesStatus =
      statusFilter === 'all' ||
      (statusFilter === 'active' && item.isActive) ||
      (statusFilter === 'inactive' && !item.isActive);

    return matchesSearch && matchesStatus;
  });

  const customActions: PageAction[] = [];

  if (hasActiveSelected) {
    customActions.push({
      key: 'inactivate',
      label: t('common.action.inactivate'),
      icon: <Ban className="h-4 w-4" />,
      variant: 'outline',
      onClick: handleToggle,
      disabled: selectedItems.length === 0,
    });
  }

  if (hasInactiveSelected) {
    customActions.push({
      key: 'reactivate',
      label: t('common.action.reactivate'),
      icon: <RotateCcw className="h-4 w-4" />,
      variant: 'outline',
      onClick: handleToggle,
      disabled: selectedItems.length === 0,
    });
  }

  return (
    <PageLayout
      title={t('tenantDatabase.list.title')}
      subtitle={t('tenantDatabase.list.subtitle')}
      onAdd={handleAdd}
      onEdit={handleEdit}
      onRefresh={handleRefresh}
      selectedRowsCount={selectedItems.length}
      actions={customActions}
    >
      <div className="space-y-4">
        <TableToolbar
          searchValue={searchTerm}
          onSearchChange={setSearchTerm}
          searchPlaceholder={t('tenantDatabase.list.searchPlaceholder')}
          rightSlot={
            <FilterDropdown
              label={t('tenantDatabase.list.filterLabel')}
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
          data={filteredItems}
          rowKey="id"
          selectedRow={selectedItems[0] ?? null}
          onRowSelect={(selected) => setSelectedItems(selected ? [selected] : [])}
          renderDetail={(record) => (
            <div className="space-y-5 p-5">
              <div className="space-y-1">
                <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-primary/80">
                  {t('tenantDatabase.preview.title')}
                </div>
                <h3 className="text-xl font-semibold text-primary">
                  {record.companyName} · {record.systemApplicationName}
                </h3>
                <p className="text-sm text-primary/80">
                  {t('tenantDatabase.preview.subtitle')}
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

                <div className="rounded-lg border border-border/70 bg-muted/20 p-3">
                  <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                    TenantId
                  </div>
                  <div className="mt-1 font-mono text-xs text-muted-foreground break-all">{record.tenantId}</div>
                </div>
              </div>

              <div className="grid gap-3 sm:grid-cols-2">
                <div className="rounded-lg border border-border/70 bg-muted/20 p-3">
                  <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                    {t('tenantDatabase.field.databaseProvider')}
                  </div>
                  <div className="mt-1 text-sm font-medium text-foreground">
                    {providerLabels[record.databaseProvider] ?? '-'}
                  </div>
                </div>

                <div className="rounded-lg border border-border/70 bg-muted/20 p-3">
                  <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                    {t('tenantDatabase.field.schemaName')}
                  </div>
                  <div className="mt-1 text-sm font-medium text-foreground">{record.schemaName}</div>
                </div>
              </div>

              <div className="rounded-lg border border-border/70 bg-muted/20 p-3">
                <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                  {t('tenantDatabase.field.apiKey')}
                </div>
                <div className="mt-1 font-mono text-xs text-muted-foreground break-all">
                  {maskSecret(record.apiKey)}
                </div>
              </div>

              <div className="rounded-lg border border-border/70 bg-muted/20 p-3">
                <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                  {t('tenantDatabase.field.connectionString')}
                </div>
                <div className="mt-1 font-mono text-xs text-muted-foreground break-all">
                  {record.connectionString}
                </div>
              </div>

              {record.createdAt && (
                <div className="text-xs text-muted-foreground">
                  {formatDate(record.createdAt)}
                  {record.updatedAt && record.updatedAt !== record.createdAt
                    ? ` · ${formatDate(record.updatedAt)}`
                    : ''}
                </div>
              )}
            </div>
          )}
        />

        {hasMore && (
          <div className="flex justify-end">
            <Button
              variant="outline"
              onClick={loadMoreItems}
              loading={loadMoreApi.isLoading}
            >
              {t('common.action.loadMore')}
            </Button>
          </div>
        )}
      </div>

      <TenantDatabaseFormModal
        isOpen={isModalOpen}
        onClose={handleModalClose}
        tenant={editingItem}
        existingContractIds={items.map((item) => item.contractId)}
        onSuccess={handleModalSuccess}
      />

      <ConfirmModal
        open={isConfirmToggleOpen}
        onOpenChange={(open) => setIsConfirmToggleOpen(open)}
        onConfirm={handleConfirmToggle}
        title={
          isSelectedInactive
            ? t('tenantDatabase.list.confirmActivateTitle')
            : t('tenantDatabase.list.confirmInactivateTitle')
        }
        description={
          selectedItems.length > 0
            ? t(
                isSelectedInactive
                  ? 'tenantDatabase.list.confirmActivateDescription'
                  : 'tenantDatabase.list.confirmInactivateDescription',
              )
                .replace('{0}', selectedItems[0].companyName)
                .replace('{1}', selectedItems[0].systemApplicationName)
            : ''
        }
        confirmText={isSelectedInactive ? t('common.action.activate') : t('common.action.inactivate')}
        variant={isSelectedInactive ? 'primary' : 'danger'}
        loading={toggleApi.isLoading}
      />
    </PageLayout>
  );
}
