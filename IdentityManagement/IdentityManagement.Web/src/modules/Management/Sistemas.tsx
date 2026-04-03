import { useState, useEffect } from 'react';
import { Layers } from 'lucide-react';
import { PageLayout, DataTable, Badge, Button, ConfirmModal, toast, useApi } from 'd-rts';
import type { DataTableColumn } from 'd-rts';
import { SistemaService } from '../../services/sistemaService';
import type { Sistema } from '../../types/sistema';
import type { PaginatedResult } from 'd-rts';
import SistemaModal from '../../components/modals/SistemaModal';

export default function Sistemas() {
  const [selectedSistemas, setSelectedSistemas] = useState<Sistema[]>([]);
  const [sistemas, setSistemas] = useState<Sistema[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingSistema, setEditingSistema] = useState<Sistema | undefined>();
  const [isConfirmDeleteOpen, setIsConfirmDeleteOpen] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const pageSize = 30;

  const loadSistemasApi = useApi({
    onSuccess: (data: PaginatedResult<Sistema>) => {
      setSistemas(prev => [...prev, ...data.data]);
      setHasMore(data.data.length === pageSize);
    }
  });

  const loadMoreSistemasApi = useApi({
    onSuccess: (data: PaginatedResult<Sistema>) => {
      setSistemas(prev => [...prev, ...data.data]);
      setHasMore(data.data.length === pageSize);
    }
  });

  const deleteSistemaApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: 'Sucesso',
        description: 'Sistema excluido com sucesso',
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
      SistemaService.getAll({
        page: 1,
        pageSize: pageSize,
        orderBy: 'name'
      })
    );
  };

  const loadMoreSistemas = async () => {
    const currentPage = Math.floor(sistemas.length / pageSize) + 1;
    await loadMoreSistemasApi.execute(() =>
      SistemaService.getAll({
        page: currentPage + 1,
        pageSize: pageSize,
        orderBy: 'name'
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
        title: 'Atenção',
        description: 'Selecione um sistema para editar',
      });
      return;
    }
    if (selectedSistemas.length > 1) {
      toast({
        variant: 'warning',
        title: 'Atenção',
        description: 'Selecione apenas um sistema para editar',
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
        title: 'Atenção',
        description: 'Selecione um ou mais sistemas para excluir',
      });
      return;
    }
    setIsConfirmDeleteOpen(true);
  };

  const handleConfirmDelete = async () => {
    for (const sistema of selectedSistemas) {
      await deleteSistemaApi.execute(() => SistemaService.delete(sistema.id));
    }
  };

  const handleRefresh = () => {
    loadSistemas(true);
  };

  const columns: DataTableColumn<Sistema>[] = [
    {
      key: 'name',
      title: 'Nome',
      dataIndex: 'name',
      sortable: true,
    },
    {
      key: 'description',
      title: 'Descrição',
      dataIndex: 'description',
      render: (value: string) => value || '-'
    },
    {
      key: 'audience',
      title: 'Audience',
      dataIndex: 'audience',
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
  ];

  const handleSelectionChange = (selected: Sistema[]) => {
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
    <PageLayout
      title="Sistemas"
      icon={<Layers size={24} />}
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
      />

      {hasMore && (
        <div className="flex justify-end mt-4">
          <Button
            variant="outline"
            onClick={loadMoreSistemas}
            loading={loadMoreSistemasApi.isLoading}
          >
            Carregar mais
          </Button>
        </div>
      )}

      <SistemaModal
        isOpen={isModalOpen}
        onClose={handleModalClose}
        sistema={editingSistema}
        onSuccess={handleModalSuccess}
      />

      <ConfirmModal
        open={isConfirmDeleteOpen}
        onOpenChange={(open) => setIsConfirmDeleteOpen(open)}
        onConfirm={handleConfirmDelete}
        title="Confirmar Exclusão"
        description={
          selectedSistemas.length === 1
            ? `Deseja excluir o sistema "${selectedSistemas[0]?.name}"?`
            : `Deseja excluir ${selectedSistemas.length} sistemas selecionados?`
        }
        confirmText="Excluir"
        variant="danger"
        loading={deleteSistemaApi.isLoading}
      />
    </PageLayout>
  );
}
