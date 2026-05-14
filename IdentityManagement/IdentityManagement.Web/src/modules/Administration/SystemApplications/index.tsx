import { useEffect, useMemo, useState } from 'react';
import { PageLayout, DataTable, Badge, ConfirmModal, FilterPanel, TableToolbar, Sheet, SheetContent, SheetPreviewField, SheetPreviewGrid, SheetPreviewHeader, SheetPreviewSection, toast, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn, FilterSection } from 'archon-ui';
import { SystemApplicationService } from '../../../services/systemApplicationService';
import type { SystemApplication } from '../../../types/systemApplication';
import SystemApplicationFormModal from '../../../components/modals/SystemApplicationFormModal';

export default function SystemApplications() {
  const { t } = useI18n();
  const [sistemas, setSistemas] = useState<SystemApplication[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [selectedSistema, setSelectedSistema] = useState<SystemApplication | null>(null);
  const [previewSistema, setPreviewSistema] = useState<SystemApplication | null>(null);
  const [editingSistema, setEditingSistema] = useState<SystemApplication | undefined>();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isConfirmDeleteOpen, setIsConfirmDeleteOpen] = useState(false);

  const { execute: fetchSistemas, loading, pagination } = useApi<SystemApplication[]>({ showErrorMessage: true });
  const deleteApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: t('common.toast.successTitle'),
        description: t('systemApplication.list.toast.deleted'),
      });
      void loadSistemas();
      setSelectedSistema(null);
      setIsConfirmDeleteOpen(false);
    },
    onError: () => setIsConfirmDeleteOpen(false),
  });

  const loadSistemas = async () => {
    const result = await fetchSistemas(() =>
      SystemApplicationService.getAll({
        page,
        pageSize,
        search: debouncedSearch || undefined,
        orderBy: 'name',
      }),
    );
    if (result) {
      const filtered = (result as SystemApplication[]).filter((s) => {
        if (statusFilter === 'active') return s.isActive;
        if (statusFilter === 'inactive') return !s.isActive;
        return true;
      });
      setSistemas(filtered);
    }
  };

  useEffect(() => {
    const timeout = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(timeout);
  }, [search]);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, statusFilter]);

  useEffect(() => {
    void loadSistemas();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, debouncedSearch, statusFilter]);

  const filterSections: FilterSection[] = useMemo(
    () => [
      {
        key: 'status',
        label: t('common.column.status'),
        value: statusFilter,
        onChange: setStatusFilter,
        options: [
          { value: 'active', label: t('common.filter.activeOnly') },
          { value: 'inactive', label: t('common.filter.inactiveOnly') },
        ],
        allLabel: t('common.filter.all'),
      },
    ],
    [statusFilter, t],
  );

  const clearFilters = () => setStatusFilter('');

  const handleAddSistema = () => {
    setEditingSistema(undefined);
    setIsModalOpen(true);
  };

  const handleEditSistema = () => {
    if (!selectedSistema) {
      toast({
        variant: 'warning',
        title: t('common.toast.warningTitle'),
        description: t('systemApplication.list.validation.selectOneToEdit'),
      });
      return;
    }
    setEditingSistema(selectedSistema);
    setIsModalOpen(true);
  };

  const handleDeleteSistema = () => {
    if (!selectedSistema) {
      toast({
        variant: 'warning',
        title: t('common.toast.warningTitle'),
        description: t('systemApplication.list.validation.selectToDelete'),
      });
      return;
    }
    setIsConfirmDeleteOpen(true);
  };

  const handleConfirmDelete = async () => {
    if (!selectedSistema) return;
    await deleteApi.execute(() => SystemApplicationService.delete(selectedSistema.id));
  };

  const columns: DataTableColumn<SystemApplication>[] = [
    {
      key: 'name',
      title: t('common.column.name'),
      dataIndex: 'name',
      sortable: true,
    },
    {
      key: 'description',
      title: t('common.field.description'),
      dataIndex: 'description',
      hiddenBelow: 'md',
      render: (value: string) => value || t('common.value.notAvailable'),
    },
    {
      key: 'audience',
      title: t('systemApplication.field.audience'),
      dataIndex: 'audience',
      hiddenBelow: 'sm',
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

  return (
    <>
      <PageLayout
        title={t('systemApplication.list.title')}
        subtitle={t('systemApplication.list.subtitle')}
        onAdd={handleAddSistema}
        onEdit={handleEditSistema}
        onDelete={handleDeleteSistema}
        onRefresh={() => void loadSistemas()}
        selectedRowsCount={selectedSistema ? 1 : 0}
      >
        <TableToolbar
          searchValue={search}
          onSearchChange={setSearch}
          searchPlaceholder={t('common.action.search')}
          rightSlot={<FilterPanel sections={filterSections} onClearAll={clearFilters} />}
          className="mb-3"
        />

        <DataTable
          columns={columns}
          data={sistemas}
          rowKey="id"
          loading={loading || deleteApi.isLoading}
          selectable
          selectedRows={selectedSistema ? [selectedSistema] : []}
          onSelectionChange={(rows) => setSelectedSistema(rows[0] ?? null)}
          onRowDoubleClick={setPreviewSistema}
          emptyText={t('common.state.empty')}
          pageSize={pageSize}
          pageSizeOptions={[10, 20, 50]}
          totalCount={pagination?.totalCount}
          page={page}
          onPageChange={setPage}
          onPageSizeChange={(s) => {
            setPageSize(s);
            setPage(1);
          }}
        />

        <SystemApplicationFormModal
          isOpen={isModalOpen}
          onClose={() => {
            setIsModalOpen(false);
            setEditingSistema(undefined);
          }}
          sistema={editingSistema}
          onSuccess={() => {
            setIsModalOpen(false);
            setEditingSistema(undefined);
            setSelectedSistema(null);
            void loadSistemas();
          }}
        />

        <ConfirmModal
          open={isConfirmDeleteOpen}
          onOpenChange={(open) => setIsConfirmDeleteOpen(open)}
          onConfirm={handleConfirmDelete}
          title={t('common.confirm.deleteTitle')}
          description={
            selectedSistema
              ? t('systemApplication.list.confirmDeleteSingle').replace('{0}', selectedSistema.name)
              : ''
          }
          confirmText={t('common.action.delete')}
          variant="danger"
          loading={deleteApi.isLoading}
        />
      </PageLayout>

      <Sheet open={!!previewSistema} onOpenChange={(open) => !open && setPreviewSistema(null)}>
        <SheetContent side="right" className="w-full sm:max-w-md">
          {previewSistema ? (
            <div className="flex h-full flex-col">
              <SheetPreviewHeader
                title={previewSistema.name}
                meta={
                  <>
                    <Badge variant={previewSistema.isActive ? 'success' : 'destructive'}>
                      {previewSistema.isActive ? t('common.status.active') : t('common.status.inactive')}
                    </Badge>
                    <span className="text-xs font-medium text-muted-foreground">
                      {previewSistema.audience || t('systemApplication.preview.audienceNotProvided')}
                    </span>
                  </>
                }
                description={t('systemApplication.preview.description')}
              />

              <div className="mt-6 flex-1 space-y-4 overflow-y-auto">
                <SheetPreviewSection title={t('systemApplication.preview.configurationTitle')} description={t('systemApplication.preview.configurationDescription')}>
                  <SheetPreviewGrid>
                    <SheetPreviewField label={t('systemApplication.field.audience')} value={previewSistema.audience || t('common.value.notAvailable')} />
                    <SheetPreviewField
                      label={t('common.field.situation')}
                      value={
                        <Badge variant={previewSistema.isActive ? 'success' : 'destructive'}>
                          {previewSistema.isActive ? t('common.status.active') : t('common.status.inactive')}
                        </Badge>
                      }
                    />
                    <SheetPreviewField className="sm:col-span-2" label={t('common.field.description')} value={previewSistema.description || t('common.value.notAvailable')} />
                  </SheetPreviewGrid>
                </SheetPreviewSection>
              </div>
            </div>
          ) : null}
        </SheetContent>
      </Sheet>
    </>
  );
}
