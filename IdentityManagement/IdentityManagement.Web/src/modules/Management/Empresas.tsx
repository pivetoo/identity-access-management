import { useState, useEffect } from 'react';
import { MapPin } from 'lucide-react';
import { PageLayout, DataTable, Badge, Button, ConfirmModal, toast, useApi } from 'd-rts';
import type { DataTableColumn } from 'd-rts';
import { EmpresaService } from '../../services/empresaService';
import type { Empresa } from '../../types/empresa';
import type { PaginatedResult } from 'd-rts';
import EmpresaModal from '../../components/modals/EmpresaModal';

export default function Empresas() {
  const [selectedEmpresas, setSelectedEmpresas] = useState<Empresa[]>([]);
  const [empresas, setEmpresas] = useState<Empresa[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingEmpresa, setEditingEmpresa] = useState<Empresa | undefined>();
  const [isConfirmDeleteOpen, setIsConfirmDeleteOpen] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const pageSize = 30;

  const loadEmpresasApi = useApi({
    onSuccess: (data: PaginatedResult<Empresa>) => {
      setEmpresas(prev => [...prev, ...data.data]);
      setHasMore(data.data.length === pageSize);
    }
  });

  const loadMoreEmpresasApi = useApi({
    onSuccess: (data: PaginatedResult<Empresa>) => {
      setEmpresas(prev => [...prev, ...data.data]);
      setHasMore(data.data.length === pageSize);
    }
  });

  const deleteEmpresaApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: 'Sucesso',
        description: 'Empresa excluida com sucesso',
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
      EmpresaService.getAll({
        page: 1,
        pageSize: pageSize,
        orderBy: 'nome'
      })
    );
  };

  const loadMoreEmpresas = async () => {
    const currentPage = Math.floor(empresas.length / pageSize) + 1;
    await loadMoreEmpresasApi.execute(() =>
      EmpresaService.getAll({
        page: currentPage + 1,
        pageSize: pageSize,
        orderBy: 'nome'
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
        title: 'Atenção',
        description: 'Selecione uma empresa para editar',
      });
      return;
    }
    if (selectedEmpresas.length > 1) {
      toast({
        variant: 'warning',
        title: 'Atenção',
        description: 'Selecione apenas uma empresa para editar',
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
        title: 'Atenção',
        description: 'Selecione uma ou mais empresas para excluir',
      });
      return;
    }
    setIsConfirmDeleteOpen(true);
  };

  const handleConfirmDelete = async () => {
    for (const empresa of selectedEmpresas) {
      await deleteEmpresaApi.execute(() => EmpresaService.delete(empresa.id));
    }
  };

  const handleRefresh = () => {
    loadEmpresas(true);
  };

  const columns: DataTableColumn<Empresa>[] = [
    {
      key: 'nome',
      title: 'Nome',
      dataIndex: 'nome',
      sortable: true,
    },
    {
      key: 'nomeFantasia',
      title: 'Nome Fantasia',
      dataIndex: 'nomeFantasia',
    },
    {
      key: 'documento',
      title: 'CNPJ',
      dataIndex: 'documento',
      render: (value: string) => {
        if (!value) return '-';
        return value.replace(/^(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})$/, '$1.$2.$3/$4-$5');
      }
    },
    {
      key: 'email',
      title: 'E-mail',
      dataIndex: 'email',
    },
    {
      key: 'telefone',
      title: 'Telefone',
      dataIndex: 'telefone',
      render: (value: string) => {
        if (!value) return '-';
        return value.replace(/^(\d{2})(\d{5})(\d{4})$/, '($1) $2-$3');
      }
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

  const handleSelectionChange = (selected: Empresa[]) => {
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

  return (
    <PageLayout
      title="Empresas"
      icon={<MapPin size={24} />}
      onAdd={handleAddEmpresa}
      onEdit={handleEditEmpresa}
      onDelete={handleDeleteEmpresa}
      onRefresh={handleRefresh}
      selectedRowsCount={selectedEmpresas.length}
    >
      <DataTable
        columns={columns}
        data={empresas}
        loading={loadEmpresasApi.isLoading || deleteEmpresaApi.isLoading}
        rowKey="id"
        selectable
        selectedRows={selectedEmpresas}
        onSelectionChange={handleSelectionChange}
      />

      {hasMore && (
        <div className="flex justify-end mt-4">
          <Button
            variant="outline"
            onClick={loadMoreEmpresas}
            loading={loadMoreEmpresasApi.isLoading}
          >
            Carregar mais
          </Button>
        </div>
      )}

      <EmpresaModal
        isOpen={isModalOpen}
        onClose={handleModalClose}
        empresa={editingEmpresa}
        onSuccess={handleModalSuccess}
      />

      <ConfirmModal
        open={isConfirmDeleteOpen}
        onOpenChange={(open) => setIsConfirmDeleteOpen(open)}
        onConfirm={handleConfirmDelete}
        title="Confirmar Exclusão"
        description={
          selectedEmpresas.length === 1
            ? `Deseja excluir a empresa "${selectedEmpresas[0]?.nome}"?`
            : `Deseja excluir ${selectedEmpresas.length} empresas selecionadas?`
        }
        confirmText="Excluir"
        variant="danger"
        loading={deleteEmpresaApi.isLoading}
      />
    </PageLayout>
  );
}
