import { useState, useEffect } from 'react';
import { PageLayout, DataTable, Badge, Button, ConfirmModal, FilterDropdown, TableToolbar, Sheet, SheetContent, SheetPreviewField, SheetPreviewGrid, SheetPreviewHeader, SheetPreviewSection, toast, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn, PaginatedResult } from 'archon-ui';
import { CompanyService } from '../../../services/companyService';
import type { Company } from '../../../types/company';
import CompanyFormModal from '../../../components/modals/CompanyFormModal';

export default function Companies() {
  const { t } = useI18n()
  const [selectedEmpresas, setSelectedEmpresas] = useState<Company[]>([]);
  const [previewEmpresa, setPreviewEmpresa] = useState<Company | null>(null);
  const [empresas, setEmpresas] = useState<Company[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingEmpresa, setEditingEmpresa] = useState<Company | undefined>();
  const [isConfirmDeleteOpen, setIsConfirmDeleteOpen] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'inactive'>('all');
  const pageSize = 30;

  const loadEmpresasApi = useApi({
    onSuccess: (data: PaginatedResult<Company>) => {
      setEmpresas(prev => [...prev, ...data.data]);
      setHasMore(data.data.length === pageSize);
    }
  });

  const loadMoreEmpresasApi = useApi({
    onSuccess: (data: PaginatedResult<Company>) => {
      setEmpresas(prev => [...prev, ...data.data]);
      setHasMore(data.data.length === pageSize);
    }
  });

  const deleteEmpresaApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: t('common.toast.successTitle'),
        description: t('company.list.toast.deleted'),
      });
      loadEmpresas(true);
      setSelectedEmpresas([]);
      setIsConfirmDeleteOpen(false);
    },
    onError: () => {
      setIsConfirmDeleteOpen(false);
    }
  });

  const loadEmpresas = async (reset = false) => {
    if (reset) {
      setEmpresas([]);
    }
    await loadEmpresasApi.execute(() =>
      CompanyService.getAll({
        page: 1,
        pageSize: pageSize,
        orderBy: 'id'
      })
    );
  };

  const loadMoreEmpresas = async () => {
    const currentPage = Math.floor(empresas.length / pageSize) + 1;
    await loadMoreEmpresasApi.execute(() =>
      CompanyService.getAll({
        page: currentPage + 1,
        pageSize: pageSize,
        orderBy: 'id'
      })
    );
  };

  useEffect(() => {
    loadEmpresas(true);
  }, []);

  const handleAddEmpresa = () => {
    setEditingEmpresa(undefined);
    setIsModalOpen(true);
  };

  const handleEditEmpresa = () => {
    if (selectedEmpresas.length === 0) {
      toast({
        variant: 'warning',
        title: t('common.toast.warningTitle'),
        description: t('company.list.validation.selectOneToEdit'),
      });
      return;
    }
    if (selectedEmpresas.length > 1) {
      toast({
        variant: 'warning',
        title: t('common.toast.warningTitle'),
        description: t('company.list.validation.selectOnlyOneToEdit'),
      });
      return;
    }
    setEditingEmpresa(selectedEmpresas[0]);
    setIsModalOpen(true);
  };

  const handleDeleteEmpresa = () => {
    if (selectedEmpresas.length === 0) {
      toast({
        variant: 'warning',
        title: t('common.toast.warningTitle'),
        description: t('company.list.validation.selectToDelete'),
      });
      return;
    }
    setIsConfirmDeleteOpen(true);
  };

  const handleConfirmDelete = async () => {
    for (const empresa of selectedEmpresas) {
      await deleteEmpresaApi.execute(() => CompanyService.delete(empresa.id));
    }
  };

  const handleRefresh = () => {
    loadEmpresas(true);
  };

  const columns: DataTableColumn<Company>[] = [
    {
      key: 'nome',
      title: t('common.column.name'),
      dataIndex: 'legalName',
      sortable: true,
    },
    {
      key: 'nomeFantasia',
      title: t('company.field.tradeName'),
      dataIndex: 'tradeName',
    },
    {
      key: 'documento',
      title: t('company.field.document'),
      dataIndex: 'document',
      render: (value: string) => {
        if (!value) return '-';
        return value.replace(/^(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})$/, '$1.$2.$3/$4-$5');
      }
    },
    {
      key: 'email',
      title: t('common.field.email'),
      dataIndex: 'email',
    },
    {
      key: 'telefone',
      title: t('common.field.phoneNumber'),
      dataIndex: 'phoneNumber',
      render: (value: string) => {
        if (!value) return '-';
        return value.replace(/^(\d{2})(\d{5})(\d{4})$/, '($1) $2-$3');
      }
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

  const handleSelectionChange = (selected: Company[]) => {
    setSelectedEmpresas(selected);
  };

  const handleModalSuccess = () => {
    setSelectedEmpresas([]);
    loadEmpresas(true);
  };

  const handleModalClose = () => {
    setIsModalOpen(false);
    setEditingEmpresa(undefined);
  };

  const filteredEmpresas = empresas.filter((empresa) => {
    const search = searchTerm.trim().toLowerCase();
    const matchesSearch = !search || [empresa.legalName, empresa.tradeName, empresa.email, empresa.document]
      .some((value) => value.toLowerCase().includes(search));
    const matchesStatus =
      statusFilter === 'all' ||
      (statusFilter === 'active' && empresa.isActive) ||
      (statusFilter === 'inactive' && !empresa.isActive);

    return matchesSearch && matchesStatus;
  });

  return (
    <>
      <PageLayout
        title={t('company.list.title')}
        subtitle={t('company.list.subtitle')}
        onAdd={handleAddEmpresa}
        onEdit={handleEditEmpresa}
        onDelete={handleDeleteEmpresa}
        onRefresh={handleRefresh}
        selectedRowsCount={selectedEmpresas.length}
      >
        <div className="space-y-4">
          <TableToolbar
            searchValue={searchTerm}
            onSearchChange={setSearchTerm}
            searchPlaceholder={t('company.list.searchPlaceholder')}
            rightSlot={
              <FilterDropdown
                label={t('company.list.filterLabel')}
                value={statusFilter}
                onChange={(value) => setStatusFilter(value as 'all' | 'active' | 'inactive')}
                options={[
                  { value: 'active', label: t('common.filter.activeOnly') },
                  { value: 'inactive', label: t('common.filter.inactiveOnly') },
                ]}
              />
            }
          />

          <DataTable
            columns={columns}
            data={filteredEmpresas}
            loading={loadEmpresasApi.isLoading || deleteEmpresaApi.isLoading}
            rowKey="id"
            selectable
            selectedRows={selectedEmpresas}
            onSelectionChange={handleSelectionChange}
            onRowDoubleClick={setPreviewEmpresa}
          />

          {hasMore && (
            <div className="mt-4 flex justify-end">
              <Button
                variant="outline"
                onClick={loadMoreEmpresas}
                loading={loadMoreEmpresasApi.isLoading}
              >
                {t('common.action.loadMore')}
              </Button>
            </div>
          )}
        </div>

        <CompanyFormModal
          isOpen={isModalOpen}
          onClose={handleModalClose}
          company={editingEmpresa}
          onSuccess={handleModalSuccess}
        />

        <ConfirmModal
          open={isConfirmDeleteOpen}
          onOpenChange={(open) => setIsConfirmDeleteOpen(open)}
          onConfirm={handleConfirmDelete}
          title={t('common.confirm.deleteTitle')}
          description={
            selectedEmpresas.length === 1
              ? t('company.list.confirmDeleteSingle').replace('{0}', selectedEmpresas[0]?.legalName ?? '')
              : t('company.list.confirmDeleteMultiple').replace('{0}', String(selectedEmpresas.length))
          }
          confirmText={t('common.action.delete')}
          variant="danger"
          loading={deleteEmpresaApi.isLoading}
        />
      </PageLayout>

      <Sheet open={!!previewEmpresa} onOpenChange={(open) => !open && setPreviewEmpresa(null)}>
        <SheetContent side="right" className="w-full sm:max-w-md">
          {previewEmpresa ? (
            <div className="flex h-full flex-col">
              <SheetPreviewHeader
                title={previewEmpresa.legalName}
                meta={
                  <>
                    <Badge variant={previewEmpresa.isActive ? 'success' : 'destructive'}>
                      {previewEmpresa.isActive ? t('common.status.active') : t('common.status.inactive')}
                    </Badge>
                    <span className="text-xs font-medium text-muted-foreground">
                      {previewEmpresa.document || t('company.preview.documentNotProvided')}
                    </span>
                  </>
                }
                description={t('company.preview.description')}
              />

              <div className="mt-6 flex-1 space-y-4 overflow-y-auto">
                <SheetPreviewSection title={t('company.preview.registrationTitle')} description={t('company.preview.registrationDescription')}>
                  <SheetPreviewGrid>
                    <SheetPreviewField label={t('company.field.legalName')} value={previewEmpresa.legalName || t('common.value.notAvailable')} />
                    <SheetPreviewField label={t('company.field.document')} value={previewEmpresa.document || t('common.value.notAvailable')} />
                    <SheetPreviewField className="sm:col-span-2" label={t('company.field.tradeName')} value={previewEmpresa.tradeName || t('common.value.notAvailable')} />
                  </SheetPreviewGrid>
                </SheetPreviewSection>

                <SheetPreviewSection title={t('company.preview.contactTitle')} description={t('company.preview.contactDescription')}>
                  <div className="grid gap-4">
                    <SheetPreviewField label={t('common.field.phoneNumber')} value={previewEmpresa.phoneNumber || t('common.value.notAvailable')} />
                    <SheetPreviewField label={t('common.field.email')} value={previewEmpresa.email || t('common.value.notAvailable')} />
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
