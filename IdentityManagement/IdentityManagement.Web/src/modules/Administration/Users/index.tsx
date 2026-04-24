import { useState, useEffect } from 'react';
import { PageLayout, DataTable, Badge, Button, ConfirmModal, FilterDropdown, TableToolbar, Sheet, SheetContent, SheetPreviewField, SheetPreviewGrid, SheetPreviewHeader, SheetPreviewSection, toast, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn, PaginatedResult } from 'archon-ui';
import { UserService } from '../../../services/userService';
import type { User } from '../../../types/user';
import UserFormModal from '../../../components/modals/UserFormModal';
import { formatDate } from '../../../utils/date';

export default function Users() {
  const { t } = useI18n()
  const [selectedUsuarios, setSelectedUsuarios] = useState<User[]>([]);
  const [previewUsuario, setPreviewUsuario] = useState<User | null>(null);
  const [usuarios, setUsuarios] = useState<User[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingUsuario, setEditingUsuario] = useState<User | undefined>();
  const [isConfirmDeleteOpen, setIsConfirmDeleteOpen] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'inactive'>('all');
  const pageSize = 30;

  const loadUsuariosApi = useApi({
    onSuccess: (data: PaginatedResult<User>) => {
      setUsuarios(prev => [...prev, ...data.data]);
      setHasMore(data.data.length === pageSize);
    }
  });

  const loadMoreUsuariosApi = useApi({
    onSuccess: (data: PaginatedResult<User>) => {
      setUsuarios(prev => [...prev, ...data.data]);
      setHasMore(data.data.length === pageSize);
    }
  });

  const deleteUsuarioApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: t('common.toast.successTitle'),
        description: t('user.list.toast.deleted'),
      });
      loadUsuarios(true);
      setSelectedUsuarios([]);
      setIsConfirmDeleteOpen(false);
    },
    onError: () => {
      setIsConfirmDeleteOpen(false);
    }
  });

  const loadUsuarios = async (reset = false) => {
    if (reset) {
      setUsuarios([]);
    }
    await loadUsuariosApi.execute(() =>
      UserService.getAll({
        page: 1,
        pageSize: pageSize,
        orderBy: 'id'
      })
    );
  };

  const loadMoreUsuarios = async () => {
    const currentPage = Math.floor(usuarios.length / pageSize) + 1;
    await loadMoreUsuariosApi.execute(() =>
      UserService.getAll({
        page: currentPage + 1,
        pageSize: pageSize,
        orderBy: 'id'
      })
    );
  };

  useEffect(() => {
    loadUsuarios(true);
  }, []);

  const handleAddUsuario = () => {
    setEditingUsuario(undefined);
    setIsModalOpen(true);
  };

  const handleEditUsuario = () => {
    if (selectedUsuarios.length === 0) {
      toast({
        variant: 'warning',
        title: t('common.toast.warningTitle'),
        description: t('user.list.validation.selectOneToEdit'),
      });
      return;
    }
    if (selectedUsuarios.length > 1) {
      toast({
        variant: 'warning',
        title: t('common.toast.warningTitle'),
        description: t('user.list.validation.selectOnlyOneToEdit'),
      });
      return;
    }
    setEditingUsuario(selectedUsuarios[0]);
    setIsModalOpen(true);
  };

  const handleDeleteUsuario = () => {
    if (selectedUsuarios.length === 0) {
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
    for (const usuario of selectedUsuarios) {
      await deleteUsuarioApi.execute(() => UserService.delete(usuario.id));
    }
  };

  const handleRefresh = () => {
    loadUsuarios(true);
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
      render: (value: string) => value || t('common.value.notAvailable')
    },
    {
      key: 'email',
      title: t('common.field.email'),
      dataIndex: 'email',
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
      )
    },
    {
      key: 'lastLoginAt',
      title: t('user.field.lastLoginAt'),
      dataIndex: 'lastLoginAt',
      render: (value: string) => formatDate(value)
    },
    {
      key: 'createdAt',
      title: t('user.field.createdAt'),
      dataIndex: 'createdAt',
      render: (value: string) => formatDate(value),
      sortable: true,
    }
  ];

  const handleSelectionChange = (selected: User[]) => {
    setSelectedUsuarios(selected);
  };

  const handleModalSuccess = () => {
    setSelectedUsuarios([]);
    loadUsuarios(true);
  };

  const handleModalClose = () => {
    setIsModalOpen(false);
    setEditingUsuario(undefined);
  };

  const filteredUsuarios = usuarios.filter((usuario) => {
    const search = searchTerm.trim().toLowerCase();
    const matchesSearch = !search || [usuario.username, usuario.name, usuario.email]
      .some((value) => value.toLowerCase().includes(search));
    const matchesStatus =
      statusFilter === 'all' ||
      (statusFilter === 'active' && usuario.isActive) ||
      (statusFilter === 'inactive' && !usuario.isActive);

    return matchesSearch && matchesStatus;
  });

  return (
    <>
      <PageLayout
        title={t('user.list.title')}
        subtitle={t('user.list.subtitle')}
        onAdd={handleAddUsuario}
        onEdit={handleEditUsuario}
        onDelete={handleDeleteUsuario}
        onRefresh={handleRefresh}
        selectedRowsCount={selectedUsuarios.length}
      >
        <div className="space-y-4">
          <TableToolbar
            searchValue={searchTerm}
            onSearchChange={setSearchTerm}
            searchPlaceholder={t('user.list.searchPlaceholder')}
            rightSlot={
              <FilterDropdown
                label={t('user.list.filterLabel')}
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
            data={filteredUsuarios}
            loading={loadUsuariosApi.isLoading || deleteUsuarioApi.isLoading}
            rowKey="id"
            selectable
            selectedRows={selectedUsuarios}
            onSelectionChange={handleSelectionChange}
            onRowDoubleClick={setPreviewUsuario}
          />

          {hasMore && (
            <div className="mt-4 flex justify-end">
              <Button
                variant="outline"
                onClick={loadMoreUsuarios}
                loading={loadMoreUsuariosApi.isLoading}
              >
                {t('common.action.loadMore')}
              </Button>
            </div>
          )}
        </div>

        <UserFormModal
          isOpen={isModalOpen}
          onClose={handleModalClose}
          usuario={editingUsuario}
          onSuccess={handleModalSuccess}
        />

        <ConfirmModal
          open={isConfirmDeleteOpen}
          onOpenChange={(open) => setIsConfirmDeleteOpen(open)}
          onConfirm={handleConfirmDelete}
          title={t('common.confirm.deleteTitle')}
          description={
            selectedUsuarios.length === 1
              ? t('user.list.confirmDeleteSingle').replace('{0}', selectedUsuarios[0]?.username ?? '')
              : t('user.list.confirmDeleteMultiple').replace('{0}', String(selectedUsuarios.length))
          }
          confirmText={t('common.action.delete')}
          variant="danger"
          loading={deleteUsuarioApi.isLoading}
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
