import { useEffect, useMemo, useState } from 'react';
import { Badge, DataTable, PageLayout, SearchableSelect, Sheet, SheetContent, SheetPreviewField, SheetPreviewGrid, SheetPreviewHeader, SheetPreviewSection, toast, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn, PaginatedResult } from 'archon-ui';
import SystemRoleTemplateFormModal from '../../../components/modals/SystemRoleTemplateFormModal';
import { AccessResourceService } from '../../../services/accessResourceService';
import { SystemRoleTemplateService } from '../../../services/systemRoleTemplateService';
import { SystemApplicationService } from '../../../services/systemApplicationService';
import type { AccessResource } from '../../../types/accessResource';
import type { SystemRoleTemplate } from '../../../types/systemRoleTemplate';
import type { SystemApplication } from '../../../types/systemApplication';

export default function SystemRoleTemplates() {
  const { t } = useI18n()
  const [selectedPerfisPadrao, setSelectedPerfisPadrao] = useState<SystemRoleTemplate[]>([]);
  const [previewPerfilPadrao, setPreviewPerfilPadrao] = useState<SystemRoleTemplate | null>(null);
  const [perfisPadrao, setPerfisPadrao] = useState<SystemRoleTemplate[]>([]);
  const [sistemas, setSistemas] = useState<SystemApplication[]>([]);
  const [accessResources, setAccessResources] = useState<AccessResource[]>([]);
  const [selectedSystemApplicationId, setSelectedSystemApplicationId] = useState<number | undefined>(undefined);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingPerfilPadrao, setEditingPerfilPadrao] = useState<SystemRoleTemplate | undefined>();
  const pageSize = 1000;

  const loadSistemasApi = useApi({
    onSuccess: (data: SystemApplication[]) => {
      const activeSistemas = data.filter((item) => item.isActive);
      setSistemas(activeSistemas);
    },
  });

  const loadAccessResourcesApi = useApi({
    onSuccess: (data: AccessResource[]) => {
      setAccessResources(data);
    },
  });

  const loadPerfisPadraoApi = useApi({
    onSuccess: (data: PaginatedResult<SystemRoleTemplate>) => {
      setPerfisPadrao(data.data);
    },
    onError: (error) => {
      toast({
        title: t('common.toast.errorTitle'),
        description: error.message,
        variant: 'destructive',
      });
    },
  });

  useEffect(() => {
    loadSistemasApi.execute(() => SystemApplicationService.getActive());
    loadAccessResourcesApi.execute(() => AccessResourceService.getAll());
  }, []);

  useEffect(() => {
    if (!selectedSystemApplicationId) {
      setPerfisPadrao([]);
      return;
    }

    loadPerfisPadraoApi.execute(() =>
      SystemRoleTemplateService.getBySystemApplicationId(selectedSystemApplicationId, {
        page: 1,
        pageSize,
        orderBy: 'id',
      })
    );
  }, [selectedSystemApplicationId]);

  const handleAddPerfilPadrao = () => {
    if (!selectedSystemApplicationId) {
      toast({
        variant: 'warning',
        title: t('common.toast.warningTitle'),
        description: t('systemRoleTemplate.list.validation.selectSystemFirst'),
      });
      return;
    }

    setEditingPerfilPadrao(undefined);
    setIsModalOpen(true);
  };

  const handleEditPerfilPadrao = () => {
    if (selectedPerfisPadrao.length === 0) {
      toast({
        variant: 'warning',
        title: t('common.toast.warningTitle'),
        description: t('systemRoleTemplate.list.validation.selectOneToEdit'),
      });
      return;
    }

    if (selectedPerfisPadrao.length > 1) {
      toast({
        variant: 'warning',
        title: t('common.toast.warningTitle'),
        description: t('systemRoleTemplate.list.validation.selectOnlyOneToEdit'),
      });
      return;
    }

    setEditingPerfilPadrao(selectedPerfisPadrao[0]);
    setIsModalOpen(true);
  };

  const handleRefresh = () => {
    if (!selectedSystemApplicationId) {
      return;
    }

    loadPerfisPadraoApi.execute(() =>
      SystemRoleTemplateService.getBySystemApplicationId(selectedSystemApplicationId, {
        page: 1,
        pageSize,
        orderBy: 'id',
      })
    );
  };

  const handleModalSuccess = () => {
    setSelectedPerfisPadrao([]);
    handleRefresh();
  };

  const handleModalClose = () => {
    setIsModalOpen(false);
    setEditingPerfilPadrao(undefined);
  };

  const selectedSystemName = useMemo(() => {
    return sistemas.find((item) => item.id === selectedSystemApplicationId)?.name ?? '';
  }, [sistemas, selectedSystemApplicationId]);

  const previewResources = useMemo(() => {
    if (!previewPerfilPadrao) {
      return [];
    }

    return accessResources
      .filter((resource) => previewPerfilPadrao.accessResourceIds.includes(resource.id))
      .sort((left, right) => left.name.localeCompare(right.name, 'pt-BR', { sensitivity: 'base' }));
  }, [accessResources, previewPerfilPadrao]);

  const columns: DataTableColumn<SystemRoleTemplate>[] = [
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
      render: (value: string) => value || t('common.value.notAvailable'),
    },
    {
      key: 'isRoot',
      title: t('role.field.isRoot'),
      dataIndex: 'isRoot',
      render: (value: boolean) => (value ? t('common.boolean.yes') : t('common.boolean.no')),
    },
    {
      key: 'isDefault',
      title: t('role.field.isDefault'),
      dataIndex: 'isDefault',
      render: (value: boolean) => (value ? t('common.boolean.yes') : t('common.boolean.no')),
    },
    {
      key: 'accessResourceIds',
      title: t('common.field.permissions'),
      dataIndex: 'accessResourceIds',
      render: (value: number[]) => value.length,
    },
    {
      key: 'isActive',
      title: t('common.column.status'),
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>{value ? t('common.status.active') : t('common.status.inactive')}</Badge>
      ),
    },
  ];

  return (
    <>
      <PageLayout
        title={t('systemRoleTemplate.list.title')}
        subtitle={t('systemRoleTemplate.list.subtitle')}
        onAdd={handleAddPerfilPadrao}
        onEdit={handleEditPerfilPadrao}
        onRefresh={handleRefresh}
        selectedRowsCount={selectedPerfisPadrao.length}
      >
        <div className="mb-6 rounded-lg border bg-card p-4">
          <div className="flex items-end gap-4">
            <div className="flex flex-1 flex-col gap-2">
              <label className="text-sm font-medium">{t('systemRoleTemplate.list.systemLabel')}</label>
              <SearchableSelect
                options={sistemas.map((sistema) => ({
                  label: sistema.name,
                  value: sistema.id.toString(),
                }))}
                value={selectedSystemApplicationId?.toString()}
                onValueChange={(value) => {
                  setSelectedSystemApplicationId(value ? parseInt(value) : undefined);
                  setSelectedPerfisPadrao([]);
                  setPreviewPerfilPadrao(null);
                }}
                placeholder={t('systemRoleTemplate.list.systemPlaceholder')}
                searchPlaceholder={t('systemRoleTemplate.list.systemSearchPlaceholder')}
                disabled={loadSistemasApi.isLoading}
              />
            </div>
          </div>
        </div>

        {selectedSystemApplicationId ? (
          <DataTable
            columns={columns}
            data={perfisPadrao}
            loading={loadPerfisPadraoApi.isLoading}
            rowKey="id"
            selectable
            selectedRows={selectedPerfisPadrao}
            onSelectionChange={setSelectedPerfisPadrao}
            onRowDoubleClick={setPreviewPerfilPadrao}
          />
        ) : (
          <div className="py-12 text-center text-muted-foreground">
            <p>{t('systemRoleTemplate.list.emptyWithoutSystem')}</p>
          </div>
        )}

        <SystemRoleTemplateFormModal
          isOpen={isModalOpen}
          onClose={handleModalClose}
          perfilPadrao={editingPerfilPadrao}
          systemApplicationId={selectedSystemApplicationId}
          onSuccess={handleModalSuccess}
        />
      </PageLayout>

      <Sheet open={!!previewPerfilPadrao} onOpenChange={(open) => !open && setPreviewPerfilPadrao(null)}>
        <SheetContent side="right" className="w-full sm:max-w-md">
          {previewPerfilPadrao ? (
            <div className="flex h-full flex-col">
              <SheetPreviewHeader
                title={previewPerfilPadrao.name}
                meta={
                  <>
                    <Badge variant={previewPerfilPadrao.isActive ? 'success' : 'destructive'}>
                      {previewPerfilPadrao.isActive ? t('common.status.active') : t('common.status.inactive')}
                    </Badge>
                    {previewPerfilPadrao.isDefault ? <Badge variant="secondary">{t('common.badge.default')}</Badge> : null}
                    {previewPerfilPadrao.isRoot ? <Badge variant="warning">{t('common.badge.root')}</Badge> : null}
                  </>
                }
                description={t('systemRoleTemplate.preview.description')}
              />

              <div className="mt-6 flex-1 space-y-4 overflow-y-auto">
                <SheetPreviewSection title={t('systemRoleTemplate.preview.contextTitle')} description={t('systemRoleTemplate.preview.contextDescription')}>
                  <SheetPreviewGrid>
                    <SheetPreviewField label={t('systemRoleTemplate.list.systemLabel')} value={selectedSystemName || t('common.value.notAvailable')} />
                    <SheetPreviewField label={t('common.field.permissions')} value={previewPerfilPadrao.accessResourceIds.length} />
                    <SheetPreviewField className="sm:col-span-2" label={t('common.field.description')} value={previewPerfilPadrao.description || t('common.value.notAvailable')} />
                  </SheetPreviewGrid>
                </SheetPreviewSection>

                <SheetPreviewSection
                  title={t('systemRoleTemplate.preview.resourcesTitle')}
                  description={t('systemRoleTemplate.preview.resourcesDescription')}
                >
                  <div className="space-y-2">
                    {previewResources.length > 0 ? (
                      previewResources.map((resource) => (
                        <div key={resource.id} className="rounded-lg border px-3 py-2">
                          <div className="flex items-center justify-between gap-3">
                            <span className="font-medium">{resource.name}</span>
                            <Badge variant="outline">{resource.httpMethod}</Badge>
                          </div>
                          <p className="mt-1 text-sm text-muted-foreground">{resource.description || resource.route}</p>
                        </div>
                      ))
                    ) : (
                      <p className="text-sm text-muted-foreground">{t('systemRoleTemplate.preview.emptyResources')}</p>
                    )}
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
