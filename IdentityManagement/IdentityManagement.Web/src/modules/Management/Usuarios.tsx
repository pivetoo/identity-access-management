import { useState, useEffect } from 'react';
import { ChevronDown, Filter } from 'lucide-react';
import { PageLayout, DataTable, Badge, Button, ConfirmModal, TableToolbar, Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle, toast, useApi } from 'd-rts';
import type { DataTableColumn } from 'd-rts';
import { UsuarioService } from '../../services/usuarioService';
import type { Usuario } from '../../types/usuario';
import type { PaginatedResult } from 'd-rts';
import UsuarioModal from '../../components/modals/UsuarioModal';

export default function Usuarios() {
  const [selectedUsuarios, setSelectedUsuarios] = useState<Usuario[]>([]);
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingUsuario, setEditingUsuario] = useState<Usuario | undefined>();
  const [isConfirmDeleteOpen, setIsConfirmDeleteOpen] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
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
    if (!search) {
      return true;
    }

    return [usuario.username, usuario.name, usuario.email]
      .some((value) => value.toLowerCase().includes(search));
  });

  const previewUsuario = selectedUsuarios.length === 1 ? selectedUsuarios[0] : null;

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
            rightSlot={<Button variant="outline" size="sm" icon={<Filter className="h-4 w-4" />} />}
          />

          <DataTable
            columns={columns}
            data={filteredUsuarios}
            loading={loadUsuariosApi.isLoading || deleteUsuarioApi.isLoading}
            rowKey="id"
            selectable
            selectedRows={selectedUsuarios}
            onSelectionChange={handleSelectionChange}
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

      <Sheet open={!!previewUsuario} onOpenChange={(open) => !open && setSelectedUsuarios([])}>
        <SheetContent side="right" className="w-full sm:max-w-md">
          {previewUsuario ? (
            <div className="flex h-full flex-col">
              <SheetHeader className="space-y-4 border-b border-border/70 pb-5">
                <div className="inline-flex w-fit items-center rounded-full border border-primary/15 bg-primary/5 px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.18em] text-primary">
                  Usuário
                </div>
                <div className="space-y-2">
                  <SheetTitle>{previewUsuario.name || previewUsuario.username}</SheetTitle>
                  <div className="flex flex-wrap items-center gap-2">
                    <Badge variant={previewUsuario.isActive ? 'success' : 'destructive'}>
                      {previewUsuario.isActive ? 'Ativo' : 'Inativo'}
                    </Badge>
                    <span className="text-xs font-medium text-muted-foreground">
                      @{previewUsuario.username}
                    </span>
                  </div>
                </div>
                <SheetDescription>
                  Dados de acesso e atividade do usuário selecionado.
                </SheetDescription>
              </SheetHeader>

              <div className="mt-6 flex-1 space-y-4 overflow-y-auto">
                <details open className="group border-b border-border/70 pb-4">
                  <summary className="flex cursor-pointer list-none items-center justify-between px-4 py-3">
                    <div>
                      <div className="text-base font-bold tracking-[-0.01em] text-primary">Acesso</div>
                      <div className="text-xs text-muted-foreground">Identificação e status do usuário</div>
                    </div>
                    <ChevronDown className="h-4 w-4 text-muted-foreground transition-transform group-open:rotate-180" />
                  </summary>
                  <div className="grid gap-4 border-t border-border/60 px-4 pt-4 sm:grid-cols-2">
                    <div>
                      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-primary/70">Usuário</div>
                      <div className="mt-1 text-sm font-medium text-foreground">{previewUsuario.username}</div>
                    </div>
                    <div>
                      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-primary/70">Situação</div>
                      <div className="mt-2">
                        <Badge variant={previewUsuario.isActive ? 'success' : 'destructive'}>
                          {previewUsuario.isActive ? 'Ativo' : 'Inativo'}
                        </Badge>
                      </div>
                    </div>
                    <div className="sm:col-span-2">
                      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-primary/70">E-mail</div>
                      <div className="mt-1 text-sm font-medium text-foreground">{previewUsuario.email}</div>
                    </div>
                  </div>
                </details>

                <details open className="group pb-2">
                  <summary className="flex cursor-pointer list-none items-center justify-between px-4 py-3">
                    <div>
                      <div className="text-base font-bold tracking-[-0.01em] text-primary">Atividade</div>
                      <div className="text-xs text-muted-foreground">Datas principais de uso e cadastro</div>
                    </div>
                    <ChevronDown className="h-4 w-4 text-muted-foreground transition-transform group-open:rotate-180" />
                  </summary>
                  <div className="grid gap-4 border-t border-border/60 px-4 pt-4 sm:grid-cols-2">
                    <div>
                      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-primary/70">Último login</div>
                      <div className="mt-1 text-sm font-medium text-foreground">{formatDate(previewUsuario.lastLoginAt || '')}</div>
                    </div>
                    <div>
                      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-primary/70">Criado em</div>
                      <div className="mt-1 text-sm font-medium text-foreground">{formatDate(previewUsuario.createdAt)}</div>
                    </div>
                  </div>
                </details>
              </div>

            </div>
          ) : null}
        </SheetContent>
      </Sheet>
    </>
  );
}
