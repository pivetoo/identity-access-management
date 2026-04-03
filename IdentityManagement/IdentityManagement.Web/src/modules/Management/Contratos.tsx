import { useState, useEffect } from 'react';
import { Ban, Filter, RotateCcw } from 'lucide-react';
import { PageLayout, DataTablePreview, Badge, Button, ConfirmModal, TableToolbar, toast, useApi } from 'd-rts';
import type { DataTablePreviewColumn, PaginatedResult, PageAction } from 'd-rts';
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
  const [searchTerm, setSearchTerm] = useState('');
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

  const formatLifetime = (seconds: number) => {
    if (!seconds || seconds <= 0) {
      return '-';
    }

    const days = seconds / 86400;
    if (Number.isInteger(days) && days >= 1) {
      return `${days} ${days === 1 ? 'dia' : 'dias'}`;
    }

    const hours = seconds / 3600;
    if (Number.isInteger(hours) && hours >= 1) {
      return `${hours} ${hours === 1 ? 'hora' : 'horas'}`;
    }

    const minutes = seconds / 60;
    if (Number.isInteger(minutes) && minutes >= 1) {
      return `${minutes} ${minutes === 1 ? 'minuto' : 'minutos'}`;
    }

    return `${seconds} s`;
  };

  const columns: DataTablePreviewColumn<Contrato>[] = [
    {
      key: 'empresaName',
      title: 'Empresa',
      dataIndex: 'empresaName',
    },
    {
      key: 'sistemaName',
      title: 'Sistema',
      dataIndex: 'sistemaName',
    },
    {
      key: 'startDate',
      title: 'Data Início',
      dataIndex: 'startDate',
      render: (value: string) => formatDate(value),
    },
    {
      key: 'endDate',
      title: 'Data Término',
      dataIndex: 'endDate',
      render: (value: string) => formatDate(value),
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

  const handleModalSuccess = () => {
    setSelectedContratos([]);
    loadContratos(true);
  };

  const handleModalClose = () => {
    setIsModalOpen(false);
    setEditingContrato(undefined);
  };

  const filteredContratos = contratos.filter((contrato) => {
    const search = searchTerm.trim().toLowerCase();
    if (!search) {
      return true;
    }

    return [
      contrato.empresaName ?? '',
      contrato.sistemaName ?? '',
      contrato.clientId ?? ''
    ].some((value) => value.toLowerCase().includes(search));
  });

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
      subtitle="Controle vigência, credenciais e configuração de acesso entre empresa e sistema."
      onAdd={handleAddContrato}
      onEdit={handleEditContrato}
      onRefresh={handleRefresh}
      selectedRowsCount={selectedContratos.length}
      actions={customActions}
    >
      <div className="space-y-4">
        <TableToolbar
          searchValue={searchTerm}
          onSearchChange={setSearchTerm}
          searchPlaceholder="Buscar por empresa, sistema ou client id"
          rightSlot={<Button variant="outline" size="sm" icon={<Filter className="h-4 w-4" />} />}
        />

        <DataTablePreview
          columns={columns}
          data={filteredContratos}
          rowKey="id"
          selectedRow={selectedContratos[0] ?? null}
          onRowSelect={(selected) => setSelectedContratos(selected ? [selected] : [])}
          renderDetail={(record) => (
            <div className="space-y-5 p-5">
              <div className="space-y-1">
                <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-primary/80">
                  Preview do contrato
                </div>
                <h3 className="text-xl font-semibold text-primary">
                  {record.empresaName} · {record.sistemaName}
                </h3>
                <p className="text-sm text-primary/80">
                  Visualização rápida do contrato selecionado, com identificação e vigência.
                </p>
              </div>

                <div className="grid gap-3 sm:grid-cols-2">
                  <div className="rounded-lg border border-border/70 bg-muted/20 p-3">
                    <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                      Client ID
                    </div>
                    <div
                      className="mt-1 truncate text-sm font-medium text-foreground"
                      title={record.clientId || '-'}
                    >
                      {record.clientId || '-'}
                    </div>
                  </div>

                <div className="rounded-lg border border-border/70 bg-muted/20 p-3">
                  <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                    Situação
                  </div>
                  <div className="mt-2">
                    <Badge variant={record.isActive ? 'success' : 'destructive'}>
                      {record.isActive ? 'Ativo' : 'Inativo'}
                    </Badge>
                  </div>
                </div>
              </div>

              <div className="grid gap-3 sm:grid-cols-2">
                <div className="rounded-lg border border-border/70 bg-muted/20 p-3">
                  <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                    Início
                  </div>
                  <div className="mt-1 text-sm font-medium text-foreground">{formatDate(record.startDate)}</div>
                </div>

                <div className="rounded-lg border border-border/70 bg-muted/20 p-3">
                  <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                    Término
                  </div>
                  <div className="mt-1 text-sm font-medium text-foreground">{formatDate(record.endDate)}</div>
                </div>
              </div>

              <div className="grid gap-3 sm:grid-cols-2">
                <div className="rounded-lg border border-border/70 bg-muted/20 p-3">
                  <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                    Access Token
                  </div>
                  <div className="mt-1 text-sm font-medium text-foreground">{formatLifetime(record.accessTokenLifetime)}</div>
                </div>

                <div className="rounded-lg border border-border/70 bg-muted/20 p-3">
                  <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                    Refresh Token
                  </div>
                  <div className="mt-1 text-sm font-medium text-foreground">{formatLifetime(record.refreshTokenLifetime)}</div>
                </div>
              </div>

              <Button
                variant="outline-primary"
                size="sm"
                onClick={() => {
                  setEditingContrato(record);
                  setIsModalOpen(true);
                }}
              >
                Abrir detalhes
              </Button>
            </div>
          )}
        />

        {hasMore && (
          <div className="flex justify-end">
            <Button
              variant="outline"
              onClick={loadMoreContratos}
              loading={loadMoreContratosApi.isLoading}
            >
              Carregar mais
            </Button>
          </div>
        )}
      </div>

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
