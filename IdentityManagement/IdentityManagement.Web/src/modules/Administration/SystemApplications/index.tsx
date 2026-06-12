import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Check, ChevronRight, Copy, Eye, EyeOff } from 'lucide-react';
import { PageLayout, DataTable, Badge, ConfirmModal, FilterPanel, TableToolbar, Sheet, SheetContent, SheetPreviewField, SheetPreviewGrid, SheetPreviewHeader, SheetPreviewSection, toast, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn, FilterSection, PaginatedResult } from 'archon-ui';
import { SystemApplicationService } from '../../../services/systemApplicationService';
import type { SystemApplication } from '../../../types/systemApplication';
import SystemApplicationFormModal from '../../../components/modals/SystemApplicationFormModal';

export default function SystemApplications() {
  const { t } = useI18n();
  const navigate = useNavigate();
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
  const [showCatalogKey, setShowCatalogKey] = useState(false);
  const [catalogKeyCopied, setCatalogKeyCopied] = useState(false);

  const handleCopyCatalogKey = async (value: string) => {
    try {
      await navigator.clipboard.writeText(value);
      setCatalogKeyCopied(true);
      toast({ variant: 'success', title: t('common.toast.successTitle'), description: t('systemApplication.catalogApiKey.copied') });
      setTimeout(() => setCatalogKeyCopied(false), 2000);
    } catch {
      toast({ variant: 'destructive', title: t('common.toast.errorTitle'), description: t('systemApplication.catalogApiKey.copyError') });
    }
  };

  useEffect(() => {
    setShowCatalogKey(false);
    setCatalogKeyCopied(false);
  }, [previewSistema]);

  const { execute: fetchSistemas, loading, pagination } = useApi<PaginatedResult<SystemApplication>>({ showErrorMessage: true });
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
        orderBy: 'id',
      }),
    );
    if (result) {
      const items: SystemApplication[] = result.data ?? [];
      const filtered = items.filter((s) => {
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
      // No card (mobile): coluna-titulo.
      primary: true,
    },
    {
      key: 'audience',
      title: t('systemApplication.field.audience'),
      dataIndex: 'audience',
      render: (value: string) => value || '-',
    },
    {
      key: 'baseUrl',
      title: t('systemApplication.field.baseUrl'),
      dataIndex: 'baseUrl',
      // URL longa: fica fora do card e some no desktop estreito.
      hiddenBelow: 'lg',
      render: (value: string) => value || '-',
    },
    {
      key: 'isActive',
      title: t('common.column.status'),
      dataIndex: 'isActive',
      // No card (mobile): badge no canto superior direito.
      cardTag: true,
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? t('common.status.active') : t('common.status.inactive')}
        </Badge>
      ),
    },
    {
      key: 'actions',
      title: '',
      dataIndex: 'id',
      // Fora do card: no mobile a propria setinha do card abre os detalhes (evita botao redundante).
      hiddenBelow: 'md',
      render: (_value, record) => (
        <button
          type="button"
          className="inline-flex items-center gap-1 text-xs font-semibold text-primary hover:underline"
          onClick={(e) => {
            e.stopPropagation();
            navigate(`/management/system-applications/${record.id}`);
          }}
        >
          Detalhes
          <ChevronRight className="h-3.5 w-3.5" />
        </button>
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
          onRowDoubleClick={(record) => navigate(`/management/system-applications/${record.id}`)}
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

                <SheetPreviewSection
                  title={t('systemApplication.preview.catalogTitle')}
                  description={t('systemApplication.preview.catalogDescription')}
                >
                  <div className="space-y-2">
                    <div className="flex items-center justify-between">
                      <span className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                        {t('systemApplication.field.catalogApiKey')}
                      </span>
                      {previewSistema.catalogApiKey ? (
                        <div className="flex items-center gap-2">
                          <button
                            type="button"
                            onClick={() => setShowCatalogKey((prev) => !prev)}
                            className="text-xs text-muted-foreground hover:text-foreground"
                            aria-label={showCatalogKey ? t('common.action.hide') : t('common.action.show')}
                          >
                            {showCatalogKey ? <EyeOff className="h-3.5 w-3.5" /> : <Eye className="h-3.5 w-3.5" />}
                          </button>
                          <button
                            type="button"
                            onClick={() => void handleCopyCatalogKey(previewSistema.catalogApiKey!)}
                            className="text-xs text-muted-foreground hover:text-foreground"
                            aria-label={t('common.action.copy')}
                          >
                            {catalogKeyCopied ? <Check className="h-3.5 w-3.5 text-success" /> : <Copy className="h-3.5 w-3.5" />}
                          </button>
                        </div>
                      ) : null}
                    </div>
                    <div className="rounded-md border border-border/60 bg-muted/30 p-2 font-mono text-xs text-muted-foreground break-all">
                      {previewSistema.catalogApiKey
                        ? showCatalogKey
                          ? previewSistema.catalogApiKey
                          : '•'.repeat(Math.min(previewSistema.catalogApiKey.length, 32))
                        : t('common.value.notAvailable')}
                    </div>
                  </div>
                </SheetPreviewSection>
              </div>
            </div>
          ) : null}
        </SheetContent>
      </Sheet>
    </>
  );
}
