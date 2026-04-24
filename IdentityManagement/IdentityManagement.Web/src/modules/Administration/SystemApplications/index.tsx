import { useState, useEffect } from 'react';
import { PageLayout, DataTable, Badge, Button, ConfirmModal, Sheet, SheetContent, SheetPreviewField, SheetPreviewGrid, SheetPreviewHeader, SheetPreviewSection, toast, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn, PaginatedResult } from 'archon-ui';
import { SystemApplicationService } from '../../../services/systemApplicationService';
import type { SystemApplication } from '../../../types/systemApplication';
import SystemApplicationFormModal from '../../../components/modals/SystemApplicationFormModal';

export default function SystemApplications() {
  const { t } = useI18n()
  const [selectedSistemas, setSelectedSistemas] = useState<SystemApplication[]>([]);
  const [previewSistema, setPreviewSistema] = useState<SystemApplication | null>(null);
  const [sistemas, setSistemas] = useState<SystemApplication[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingSistema, setEditingSistema] = useState<SystemApplication | undefined>();
  const [isConfirmDeleteOpen, setIsConfirmDeleteOpen] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const pageSize = 30;

  const loadSistemasApi = useApi({
    onSuccess: (data: PaginatedResult<SystemApplication>) => {
      setSistemas(prev => [...prev, ...data.data]);
      setHasMore(data.data.length === pageSize);
    }
  });

  const loadMoreSistemasApi = useApi({
    onSuccess: (data: PaginatedResult<SystemApplication>) => {
      setSistemas(prev => [...prev, ...data.data]);
      setHasMore(data.data.length === pageSize);
    }
  });

  const deleteSistemaApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: t('common.toast.successTitle'),
        description: t('systemApplication.list.toast.deleted'),
      });
      loadSistemas(true);
      setSelectedSistemas([]);
      setIsConfirmDeleteOpen(false);
    },
    onError: () => {
      setIsConfirmDeleteOpen(false);
    }
  });

  const loadSistemas = async (reset = false) => {
    if (reset) {
      setSistemas([]);
    }
    await loadSistemasApi.execute(() =>
      SystemApplicationService.getAll({
        page: 1,
        pageSize: pageSize,
        orderBy: 'id'
      })
    );
  };

  const loadMoreSistemas = async () => {
    const currentPage = Math.floor(sistemas.length / pageSize) + 1;
    await loadMoreSistemasApi.execute(() =>
      SystemApplicationService.getAll({
        page: currentPage + 1,
        pageSize: pageSize,
        orderBy: 'id'
      })
    );
  };

  useEffect(() => {
    loadSistemas(true);
  }, []);

  const handleAddSistema = () => {
    setEditingSistema(undefined);
    setIsModalOpen(true);
  };

  const handleEditSistema = () => {
    if (selectedSistemas.length === 0) {
      toast({
        variant: 'warning',
        title: t('common.toast.warningTitle'),
        description: t('systemApplication.list.validation.selectOneToEdit'),
      });
      return;
    }
    if (selectedSistemas.length > 1) {
      toast({
        variant: 'warning',
        title: t('common.toast.warningTitle'),
        description: t('systemApplication.list.validation.selectOnlyOneToEdit'),
      });
      return;
    }
    setEditingSistema(selectedSistemas[0]);
    setIsModalOpen(true);
  };

  const handleDeleteSistema = () => {
    if (selectedSistemas.length === 0) {
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
    for (const sistema of selectedSistemas) {
      await deleteSistemaApi.execute(() => SystemApplicationService.delete(sistema.id));
    }
  };

  const handleRefresh = () => {
    loadSistemas(true);
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
      render: (value: string) => value || t('common.value.notAvailable')
    },
    {
      key: 'audience',
      title: t('systemApplication.field.audience'),
      dataIndex: 'audience',
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

  const handleSelectionChange = (selected: SystemApplication[]) => {
    setSelectedSistemas(selected);
  };

  const handleModalSuccess = () => {
    setSelectedSistemas([]);
    loadSistemas(true);
  };

  const handleModalClose = () => {
    setIsModalOpen(false);
    setEditingSistema(undefined);
  };

  return (
    <>
      <PageLayout
        title={t('systemApplication.list.title')}
        subtitle={t('systemApplication.list.subtitle')}
        onAdd={handleAddSistema}
        onEdit={handleEditSistema}
        onDelete={handleDeleteSistema}
        onRefresh={handleRefresh}
        selectedRowsCount={selectedSistemas.length}
      >
        <DataTable
          columns={columns}
          data={sistemas}
          loading={loadSistemasApi.isLoading || deleteSistemaApi.isLoading}
          rowKey="id"
          selectable
          selectedRows={selectedSistemas}
          onSelectionChange={handleSelectionChange}
          onRowDoubleClick={setPreviewSistema}
        />

        {hasMore && (
          <div className="mt-4 flex justify-end">
            <Button
              variant="outline"
              onClick={loadMoreSistemas}
              loading={loadMoreSistemasApi.isLoading}
            >
              {t('common.action.loadMore')}
            </Button>
          </div>
        )}

        <SystemApplicationFormModal
          isOpen={isModalOpen}
          onClose={handleModalClose}
          sistema={editingSistema}
          onSuccess={handleModalSuccess}
        />

        <ConfirmModal
          open={isConfirmDeleteOpen}
          onOpenChange={(open) => setIsConfirmDeleteOpen(open)}
          onConfirm={handleConfirmDelete}
          title={t('common.confirm.deleteTitle')}
          description={
            selectedSistemas.length === 1
              ? t('systemApplication.list.confirmDeleteSingle').replace('{0}', selectedSistemas[0]?.name ?? '')
              : t('systemApplication.list.confirmDeleteMultiple').replace('{0}', String(selectedSistemas.length))
          }
          confirmText={t('common.action.delete')}
          variant="danger"
          loading={deleteSistemaApi.isLoading}
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

                <SheetPreviewSection title={t('systemApplication.preview.endpointsTitle')} description={t('systemApplication.preview.endpointsDescription')}>
                  <SheetPreviewField label={t('systemApplication.field.redirectUris')} value={previewSistema.redirectUris || t('common.value.notAvailable')} />
                </SheetPreviewSection>
              </div>

            </div>
          ) : null}
        </SheetContent>
      </Sheet>
    </>
  );
}
