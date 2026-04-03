import { useState, useEffect } from 'react';
import { Users as UsersIcon } from 'lucide-react';
import { PageLayout, DataTable, Badge, Button, ConfirmModal, toast, useApi } from 'd-rts';
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

  return (
    <PageLayout
      title="Usuários"
      icon={<UsersIcon size={24} />}
      onAdd={handleAddUsuario}
      onEdit={handleEditUsuario}
      onDelete={handleDeleteUsuario}
      onRefresh={handleRefresh}
      selectedRowsCount={selectedUsuarios.length}
    >
      <DataTable
        columns={columns}
        data={usuarios}
        loading={loadUsuariosApi.isLoading || deleteUsuarioApi.isLoading}
        rowKey="id"
        selectable
        selectedRows={selectedUsuarios}
        onSelectionChange={handleSelectionChange}
      />

      {hasMore && (
        <div className="flex justify-end mt-4">
          <Button
            variant="outline"
            onClick={loadMoreUsuarios}
            loading={loadMoreUsuariosApi.isLoading}
          >
            Carregar mais
          </Button>
        </div>
      )}

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
  );
}
