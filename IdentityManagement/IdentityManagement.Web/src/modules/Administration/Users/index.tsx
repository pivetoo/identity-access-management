import { useEffect, useMemo, useState } from 'react';
import { PageLayout, DataTable, Badge, ConfirmModal, FilterPanel, TableToolbar, Sheet, SheetContent, SheetPreviewField, SheetPreviewGrid, SheetPreviewHeader, SheetPreviewSection, toast, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn, FilterSection, PaginatedResult } from 'archon-ui';
import { UserService } from '../../../services/userService';
import type { User } from '../../../types/user';
import UserFormModal from '../../../components/modals/UserFormModal';
import { formatDate } from '../../../utils/date';

export default function Users() {
  const { t } = useI18n();
  const [usuarios, setUsuarios] = useState<User[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [selectedUsuario, setSelectedUsuario] = useState<User | null>(null);
  const [previewUsuario, setPreviewUsuario] = useState<User | null>(null);
  const [editingUsuario, setEditingUsuario] = useState<User | undefined>();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isConfirmDeleteOpen, setIsConfirmDeleteOpen] = useState(false);

  const { execute: fetchUsuarios, loading, pagination } = useApi<PaginatedResult<User>>({ showErrorMessage: true });
  const deleteApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: t('common.toast.successTitle'),
        description: t('user.list.toast.deleted'),
      });
      void loadUsuarios();
      setSelectedUsuario(null);
      setIsConfirmDeleteOpen(false);
    },
    onError: () => setIsConfirmDeleteOpen(false),
  });

  const loadUsuarios = async () => {
    const result = await fetchUsuarios(() =>
      UserService.getAll({
        page,
        pageSize,
        search: debouncedSearch || undefined,
        orderBy: 'id',
      }),
    );
    if (result) {
      const items: User[] = result.data ?? [];
      const filtered = items.filter((u) => {
        if (statusFilter === 'active') return u.isActive;
        if (statusFilter === 'inactive') return !u.isActive;
        return true;
      });
      setUsuarios(filtered);
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
    void loadUsuarios();
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

  const handleAddUsuario = () => {
    setEditingUsuario(undefined);
    setIsModalOpen(true);
  };

  const handleEditUsuario = () => {
    if (!selectedUsuario) {
      toast({
        variant: 'warning',
        title: t('common.toast.warningTitle'),
        description: t('user.list.validation.selectOneToEdit'),
      });
      return;
    }
    setEditingUsuario(selectedUsuario);
    setIsModalOpen(true);
  };

  const handleDeleteUsuario = () => {
    if (!selectedUsuario) {
      toast({
        variant: 'warning',
        title: t('common.toast.warningTitle'),
        description: t('user.list.validation.selectToDelete'),
      });
      return;
    }
    setIsConfirmDeleteOpen(true);
  };

  const handleConfirmDelete = async () => {
    if (!selectedUsuario) return;
    await deleteApi.execute(() => UserService.delete(selectedUsuario.id));
  };

  const columns: DataTableColumn<User>[] = [
    {
      key: 'username',
      title: t('user.field.username'),
      dataIndex: 'username',
      sortable: true,
    },
    {
      key: 'name',
      title: t('user.field.fullName'),
      dataIndex: 'name',
      hiddenBelow: 'sm',
      render: (value: string) => value || t('common.value.notAvailable'),
    },
    {
      key: 'email',
      title: t('common.field.email'),
      dataIndex: 'email',
      hiddenBelow: 'md',
      sortable: true,
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
    {
      key: 'lastLoginAt',
      title: t('user.field.lastLoginAt'),
      dataIndex: 'lastLoginAt',
      hiddenBelow: 'lg',
      render: (value: string) => formatDate(value),
    },
    {
      key: 'createdAt',
      title: t('user.field.createdAt'),
      dataIndex: 'createdAt',
      hiddenBelow: 'lg',
      render: (value: string) => formatDate(value),
      sortable: true,
    },
  ];

  return (
    <>
      <PageLayout
        title={t('user.list.title')}
        subtitle={t('user.list.subtitle')}
        onAdd={handleAddUsuario}
        onEdit={handleEditUsuario}
        onDelete={handleDeleteUsuario}
        onRefresh={() => void loadUsuarios()}
        selectedRowsCount={selectedUsuario ? 1 : 0}
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
          data={usuarios}
          rowKey="id"
          loading={loading || deleteApi.isLoading}
          selectable
          selectedRows={selectedUsuario ? [selectedUsuario] : []}
          onSelectionChange={(rows) => setSelectedUsuario(rows[0] ?? null)}
          onRowDoubleClick={setPreviewUsuario}
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

        <UserFormModal
          isOpen={isModalOpen}
          onClose={() => {
            setIsModalOpen(false);
            setEditingUsuario(undefined);
          }}
          usuario={editingUsuario}
          onSuccess={() => {
            setIsModalOpen(false);
            setEditingUsuario(undefined);
            setSelectedUsuario(null);
            void loadUsuarios();
          }}
        />

        <ConfirmModal
          open={isConfirmDeleteOpen}
          onOpenChange={(open) => setIsConfirmDeleteOpen(open)}
          onConfirm={handleConfirmDelete}
          title={t('common.confirm.deleteTitle')}
          description={
            selectedUsuario
              ? t('user.list.confirmDeleteSingle').replace('{0}', selectedUsuario.username)
              : ''
          }
          confirmText={t('common.action.delete')}
          variant="danger"
          loading={deleteApi.isLoading}
        />
      </PageLayout>

      <Sheet open={!!previewUsuario} onOpenChange={(open) => !open && setPreviewUsuario(null)}>
        <SheetContent side="right" className="w-full sm:max-w-md">
          {previewUsuario ? (
            <div className="flex h-full flex-col">
              <SheetPreviewHeader
                title={previewUsuario.name || previewUsuario.username}
                meta={
                  <>
                    <Badge variant={previewUsuario.isActive ? 'success' : 'destructive'}>
                      {previewUsuario.isActive ? t('common.status.active') : t('common.status.inactive')}
                    </Badge>
                    <span className="text-xs font-medium text-muted-foreground">
                      @{previewUsuario.username}
                    </span>
                  </>
                }
                description={t('user.preview.description')}
              />

              <div className="mt-6 flex-1 space-y-4 overflow-y-auto">
                <SheetPreviewSection title={t('user.preview.accessTitle')} description={t('user.preview.accessDescription')}>
                  <SheetPreviewGrid>
                    <SheetPreviewField label={t('user.field.username')} value={previewUsuario.username} />
                    <SheetPreviewField
                      label={t('common.field.situation')}
                      value={
                        <Badge variant={previewUsuario.isActive ? 'success' : 'destructive'}>
                          {previewUsuario.isActive ? t('common.status.active') : t('common.status.inactive')}
                        </Badge>
                      }
                    />
                    <SheetPreviewField className="sm:col-span-2" label={t('common.field.email')} value={previewUsuario.email} />
                  </SheetPreviewGrid>
                </SheetPreviewSection>

                <SheetPreviewSection title={t('user.preview.activityTitle')} description={t('user.preview.activityDescription')}>
                  <SheetPreviewGrid>
                    <SheetPreviewField label={t('user.field.lastLoginAt')} value={formatDate(previewUsuario.lastLoginAt || '')} />
                    <SheetPreviewField label={t('user.field.createdAtLabel')} value={formatDate(previewUsuario.createdAt)} />
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
