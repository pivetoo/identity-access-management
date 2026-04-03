import { useState, useEffect } from 'react';
import { PageLayout, DataTable, Badge, Button, ConfirmModal, FilterDropdown, TableToolbar, Sheet, SheetContent, SheetPreviewField, SheetPreviewGrid, SheetPreviewHeader, SheetPreviewSection, toast, useApi } from 'd-rts';
import type { DataTableColumn } from 'd-rts';
import { UsuarioService } from '../../services/usuarioService';
import type { Usuario } from '../../types/usuario';
import type { PaginatedResult } from 'd-rts';
import UsuarioModal from '../../components/modals/UsuarioModal';

export default function Usuarios() {
  const [selectedUsuarios, setSelectedUsuarios] = useState<Usuario[]>([]);
  const [previewUsuario, setPreviewUsuario] = useState<Usuario | null>(null);
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingUsuario, setEditingUsuario] = useState<Usuario | undefined>();
  const [isConfirmDeleteOpen, setIsConfirmDeleteOpen] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'inactive'>('all');
  const pageSize = 30;

  const loadUsuariosApi = useApi({
    onSuccess: (data: PaginatedResult<Usuario>) => {
      setUsuarios(prev => [...prev, ...data.data]);
      setHasMore(data.data.length === pageSize);
    }
  });

  const loadMoreUsuariosApi = useApi({
    onSuccess: (data: PaginatedResult<Usuario>) => {
      setUsuarios(prev => [...prev, ...data.data]);
      setHasMore(data.data.length === pageSize);
    }
  });

  const deleteUsuarioApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: 'Sucesso',
        description: 'Usuário excluído com sucesso',
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
      UsuarioService.getAll({
        page: 1,
        pageSize: pageSize,
        orderBy: 'username'
      })
    );
  };

  const loadMoreUsuarios = async () => {
    const currentPage = Math.floor(usuarios.length / pageSize) + 1;
    await loadMoreUsuariosApi.execute(() =>
      UsuarioService.getAll({
        page: currentPage + 1,
        pageSize: pageSize,
        orderBy: 'username'
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
        title: 'Atenção',
        description: 'Selecione um usuário para editar',
      });
      return;
    }
    if (selectedUsuarios.length > 1) {
      toast({
        variant: 'warning',
        title: 'Atenção',
        description: 'Selecione apenas um usuário para editar',
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
        title: 'Atenção',
        description: 'Selecione um ou mais usuários para excluir',
      });
      return;
    }
    setIsConfirmDeleteOpen(true);
  };

  const handleConfirmDelete = async () => {
    for (const usuario of selectedUsuarios) {
      await deleteUsuarioApi.execute(() => UsuarioService.delete(usuario.id));
    }
  };

  const handleRefresh = () => {
    loadUsuarios(true);
  };

  const formatDate = (dateString: string) => {
    if (!dateString) return '-';
    const date = new Date(dateString);
    return date.toLocaleDateString('pt-BR');
  };

  const columns: DataTableColumn<Usuario>[] = [
    {
      key: 'username',
      title: 'Nome de Usuário',
      dataIndex: 'username',
      sortable: true,
    },
    {
      key: 'name',
      title: 'Nome Completo',
      dataIndex: 'name',
      render: (value: string) => value || '-'
    },
    {
      key: 'email',
      title: 'E-mail',
      dataIndex: 'email',
      sortable: true,
    },
    {
      key: 'isActive',
      title: 'Status',
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? 'Ativo' : 'Inativo'}
        </Badge>
      )
    },
    {
      key: 'lastLoginAt',
      title: 'Último Login',
      dataIndex: 'lastLoginAt',
      render: (value: string) => formatDate(value)
    },
    {
      key: 'createdAt',
      title: 'Data Criação',
      dataIndex: 'createdAt',
      render: (value: string) => formatDate(value),
      sortable: true,
    }
  ];

  const handleSelectionChange = (selected: Usuario[]) => {
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
        title="Usuários"
        subtitle="Gerencie contas, status de acesso e informações básicas dos usuários."
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
            searchPlaceholder="Buscar por usuário, nome ou e-mail"
            rightSlot={
              <FilterDropdown
                label="Filtrar usuários"
                value={statusFilter}
                onChange={(value) => setStatusFilter(value as 'all' | 'active' | 'inactive')}
                options={[
                  { value: 'active', label: 'Apenas ativos' },
                  { value: 'inactive', label: 'Apenas inativos' },
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
                Carregar mais
              </Button>
            </div>
          )}
        </div>

        <UsuarioModal
          isOpen={isModalOpen}
          onClose={handleModalClose}
          usuario={editingUsuario}
          onSuccess={handleModalSuccess}
        />

        <ConfirmModal
          open={isConfirmDeleteOpen}
          onOpenChange={(open) => setIsConfirmDeleteOpen(open)}
          onConfirm={handleConfirmDelete}
          title="Confirmar Exclusão"
          description={
            selectedUsuarios.length === 1
              ? `Deseja excluir o usuário "${selectedUsuarios[0]?.username}"?`
              : `Deseja excluir ${selectedUsuarios.length} usuários selecionados?`
          }
          confirmText="Excluir"
          variant="danger"
          loading={deleteUsuarioApi.isLoading}
        />
      </PageLayout>

      <Sheet open={!!previewUsuario} onOpenChange={(open) => !open && setPreviewUsuario(null)}>
        <SheetContent side="right" className="w-full sm:max-w-md">
          {previewUsuario ? (
            <div className="flex h-full flex-col">
              <SheetPreviewHeader
                eyebrow="Usuário"
                title={previewUsuario.name || previewUsuario.username}
                meta={
                  <>
                    <Badge variant={previewUsuario.isActive ? 'success' : 'destructive'}>
                      {previewUsuario.isActive ? 'Ativo' : 'Inativo'}
                    </Badge>
                    <span className="text-xs font-medium text-muted-foreground">
                      @{previewUsuario.username}
                    </span>
                  </>
                }
                description="Dados de acesso e atividade do usuário selecionado."
              />

              <div className="mt-6 flex-1 space-y-4 overflow-y-auto">
                <SheetPreviewSection title="Acesso" description="Identificação e status do usuário">
                  <SheetPreviewGrid>
                    <SheetPreviewField label="Usuário" value={previewUsuario.username} />
                    <SheetPreviewField
                      label="Situação"
                      value={
                        <Badge variant={previewUsuario.isActive ? 'success' : 'destructive'}>
                          {previewUsuario.isActive ? 'Ativo' : 'Inativo'}
                        </Badge>
                      }
                    />
                    <SheetPreviewField className="sm:col-span-2" label="E-mail" value={previewUsuario.email} />
                  </SheetPreviewGrid>
                </SheetPreviewSection>

                <SheetPreviewSection title="Atividade" description="Datas principais de uso e cadastro">
                  <SheetPreviewGrid>
                    <SheetPreviewField label="Último login" value={formatDate(previewUsuario.lastLoginAt || '')} />
                    <SheetPreviewField label="Criado em" value={formatDate(previewUsuario.createdAt)} />
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
