import { useState, useEffect } from 'react';
import { FileText, Ban, RotateCcw } from 'lucide-react';
import { PageLayout, DataTable, Badge, Button, ConfirmModal, toast, useApi } from 'd-rts';
import type { DataTableColumn, PaginatedResult, PageAction } from 'd-rts';
import { ContratoService } from '../../services/contratoService';
import type { Contrato } from '../../types/contrato';
import ContratoModal from '../../components/modals/ContratoModal';

export default function Contratos() {
  const [selectedContratos, setSelectedContratos] = useState<Contrato[]>([]);
  const [contratos, setContratos] = useState<Contrato[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingContrato, setEditingContrato] = useState<Contrato | undefined>();
  const [isConfirmToggleOpen, setIsConfirmToggleOpen] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const pageSize = 30;

  const loadContratosApi = useApi({
    onSuccess: (data: PaginatedResult<Contrato>) => {
      setContratos(data.data);
      setHasMore(data.data.length === pageSize);
    }
  });

  const loadMoreContratosApi = useApi({
    onSuccess: (data: PaginatedResult<Contrato>) => {
      setContratos(prev => [...prev, ...data.data]);
      setHasMore(data.data.length === pageSize);
    }
  });

  const toggleContratoApi = useApi({
    onSuccess: () => {
      const wasInactive = selectedContratos[0] && !selectedContratos[0].isActive;
      toast({
        variant: 'success',
        title: 'Sucesso',
        description: wasInactive ? 'Contrato ativado com sucesso' : 'Contrato inativado com sucesso',
      });
      loadContratos(true);
      setSelectedContratos([]);
      setIsConfirmToggleOpen(false);
    },
    onError: () => {
      setIsConfirmToggleOpen(false);
    }
  });

  const hasActiveSelected = selectedContratos.some(c => c.isActive);
  const hasInactiveSelected = selectedContratos.some(c => !c.isActive);
  const isSelectedInactive = selectedContratos.length > 0 && !selectedContratos[0].isActive;

  const loadContratos = async (reset = false) => {
    if (reset) {
      setContratos([]);
    }
    await loadContratosApi.execute(() =>
      ContratoService.getAll({
        page: 1,
        pageSize: pageSize,
        orderBy: 'startDate'
      })
    );
  };

  const loadMoreContratos = async () => {
    const currentPage = Math.floor(contratos.length / pageSize) + 1;
    await loadMoreContratosApi.execute(() =>
      ContratoService.getAll({
        page: currentPage + 1,
        pageSize: pageSize,
        orderBy: 'startDate'
      })
    );
  };

  useEffect(() => {
    loadContratos(true);
  }, []);

  const handleAddContrato = () => {
    setEditingContrato(undefined);
    setIsModalOpen(true);
  };

  const handleEditContrato = () => {
    if (selectedContratos.length === 0) {
      toast({
        variant: 'warning',
        title: 'Atenção',
        description: 'Selecione um contrato para editar',
      });
      return;
    }
    setEditingContrato(selectedContratos[0]);
    setIsModalOpen(true);
  };

  const handleToggleContrato = () => {
    if (selectedContratos.length === 0) {
      toast({
        variant: 'warning',
        title: 'Atenção',
        description: 'Selecione um contrato para ativar/inativar',
      });
      return;
    }
    setIsConfirmToggleOpen(true);
  };

  const handleConfirmToggle = async () => {
    if (selectedContratos.length === 0) return;
    const contrato = selectedContratos[0];
    await toggleContratoApi.execute(() => ContratoService.toggleActive(contrato.id, contrato));
  };

  const handleRefresh = () => {
    loadContratos(true);
  };

  const formatDate = (dateString: string) => {
    if (!dateString) return '-';
    const date = new Date(dateString);
    return date.toLocaleDateString('pt-BR');
  };

  const columns: DataTableColumn<Contrato>[] = [
    {
      key: 'empresaName',
      title: 'Empresa',
      dataIndex: 'empresaName',
      sortable: false,
    },
    {
      key: 'sistemaName',
      title: 'Sistema',
      dataIndex: 'sistemaName',
      sortable: false,
    },
    {
      key: 'clientId',
      title: 'Client ID',
      dataIndex: 'clientId',
      sortable: false,
      render: (value: string) => value || '-',
    },
    {
      key: 'startDate',
      title: 'Data Início',
      dataIndex: 'startDate',
      sortable: false,
      render: (value: string) => formatDate(value),
    },
    {
      key: 'endDate',
      title: 'Data Término',
      dataIndex: 'endDate',
      sortable: false,
      render: (value: string) => formatDate(value),
    },
    {
      key: 'isActive',
      title: 'Status',
      dataIndex: 'isActive',
      sortable: false,
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? 'Ativo' : 'Inativo'}
        </Badge>
      )
    },
  ];

  const handleModalSuccess = () => {
    setSelectedContratos([]);
    loadContratos(true);
  };

  const handleSelectionChange = (selected: Contrato[]) => {
    setSelectedContratos(selected);
  };

  const handleModalClose = () => {
    setIsModalOpen(false);
    setEditingContrato(undefined);
  };

  const customActions: PageAction[] = [];

  if (hasActiveSelected) {
    customActions.push({
      key: 'inactivate',
      label: 'Inativar',
      icon: <Ban className="h-4 w-4" />,
      variant: 'outline',
      onClick: handleToggleContrato,
      disabled: selectedContratos.length === 0
    });
  }

  if (hasInactiveSelected) {
    customActions.push({
      key: 'reactivate',
      label: 'Reativar',
      icon: <RotateCcw className="h-4 w-4" />,
      variant: 'outline',
      onClick: handleToggleContrato,
      disabled: selectedContratos.length === 0
    });
  }

  return (
    <PageLayout
      title="Contratos"
      icon={<FileText size={24} />}
      onAdd={handleAddContrato}
      onEdit={handleEditContrato}
      onRefresh={handleRefresh}
      selectedRowsCount={selectedContratos.length}
      actions={customActions}
    >
      <DataTable
        columns={columns}
        data={contratos}
        loading={loadContratosApi.isLoading}
        rowKey="id"
        selectable
        selectedRows={selectedContratos}
        onSelectionChange={handleSelectionChange}
      />

      {hasMore && (
        <div className="flex justify-end mt-4">
          <Button
            variant="outline"
            onClick={loadMoreContratos}
            loading={loadMoreContratosApi.isLoading}
          >
            Carregar mais
          </Button>
        </div>
      )}

      <ContratoModal
        isOpen={isModalOpen}
        onClose={handleModalClose}
        contrato={editingContrato}
        onSuccess={handleModalSuccess}
      />

      <ConfirmModal
        open={isConfirmToggleOpen}
        onOpenChange={(open) => setIsConfirmToggleOpen(open)}
        onConfirm={handleConfirmToggle}
        title={isSelectedInactive ? 'Confirmar Ativação' : 'Confirmar Inativação'}
        description={
          selectedContratos.length > 0
            ? `Deseja ${isSelectedInactive ? 'ativar' : 'inativar'} o contrato entre "${selectedContratos[0].empresaName}" e "${selectedContratos[0].sistemaName}"?`
            : `Deseja ${isSelectedInactive ? 'ativar' : 'inativar'} este contrato?`
        }
        confirmText={isSelectedInactive ? 'Ativar' : 'Inativar'}
        variant={isSelectedInactive ? 'primary' : 'danger'}
        loading={toggleContratoApi.isLoading}
      />
    </PageLayout>
  );
}
