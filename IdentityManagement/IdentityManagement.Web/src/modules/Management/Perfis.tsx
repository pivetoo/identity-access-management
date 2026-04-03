import { useState, useEffect } from 'react';
import { UserCheck } from 'lucide-react';
import {
  PageLayout,
  DataTable,
  Button,
  ConfirmModal,
  toast,
  useApi,
  SearchableSelect
} from 'd-rts';
import type { DataTableColumn } from 'd-rts';
import { PerfilService } from '../../services/perfilService';
import { ContratoService } from '../../services/contratoService';
import type { Perfil, PerfilSummaryViewModel } from '../../types/perfil';
import type { Contrato } from '../../types/contrato';
import type { PaginatedResult } from 'd-rts';
import PerfilModal from '../../components/modals/PerfilModal';

export default function Perfis() {
  const [selectedPerfis, setSelectedPerfis] = useState<Perfil[]>([]);
  const [perfis, setPerfis] = useState<Perfil[]>([]);
  const [contratos, setContratos] = useState<Contrato[]>([]);
  const [selectedContratoId, setSelectedContratoId] = useState<number | undefined>(undefined);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingPerfil, setEditingPerfil] = useState<Perfil | undefined>();
  const [isConfirmDeleteOpen, setIsConfirmDeleteOpen] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const pageSize = 30;

  const loadPerfisApi = useApi({
    onSuccess: (data: PaginatedResult<PerfilSummaryViewModel | Perfil>) => {
      setPerfis(prev => [...prev, ...data.data] as Perfil[]);
      setHasMore(false);
    }
  });

  const loadMorePerfisApi = useApi({
    onSuccess: (data: PaginatedResult<Perfil>) => {
      setPerfis(prev => [...prev, ...data.data]);
      setHasMore(data.data.length === pageSize);
    }
  });

  const deletePerfilApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: 'Sucesso',
        description: 'Perfil excluido com sucesso',
      });
      loadPerfis(true);
      setSelectedPerfis([]);
      setIsConfirmDeleteOpen(false);
    },
    onError: () => {
      setIsConfirmDeleteOpen(false);
    }
  });

  const loadContratosApi = useApi({
    onSuccess: (data: Contrato[]) => {
      setContratos(data);
    }
  });

  const loadPerfis = async (reset = false, contratoId?: number) => {
    if (!contratoId && !selectedContratoId) {
      setPerfis([]);
      return;
    }

    const targetContratoId = contratoId || selectedContratoId;
    if (reset) {
      setPerfis([]);
    }
    await loadPerfisApi.execute(async () => {
      const data = await PerfilService.getByContrato(targetContratoId!, {
        page: 1,
        pageSize: 1000
      });
      return data;
    });
  };

  const loadMorePerfis = async () => {
    if (!selectedContratoId) return;

    const currentPage = Math.floor(perfis.length / pageSize) + 1;
    await loadMorePerfisApi.execute(() =>
      PerfilService.getByContrato(selectedContratoId, {
        page: currentPage + 1,
        pageSize: pageSize
      })
    );
  };

  useEffect(() => {
    loadContratosApi.execute(() => ContratoService.getActive());
  }, []);

  const handleContratoChange = (contratoId: string) => {
    const id = contratoId ? parseInt(contratoId) : undefined;
    setSelectedContratoId(id);
    setSelectedPerfis([]);
    if (id) {
      loadPerfis(true, id);
    } else {
      setPerfis([]);
    }
  };

  const handleAddPerfil = () => {
    if (!selectedContratoId) {
      toast({
        variant: 'warning',
        title: 'Atenção',
        description: 'Selecione um contrato primeiro',
      });
      return;
    }
    setEditingPerfil(undefined);
    setIsModalOpen(true);
  };

  const handleEditPerfil = () => {
    if (selectedPerfis.length === 0) {
      toast({
        variant: 'warning',
        title: 'Atenção',
        description: 'Selecione um perfil para editar',
      });
      return;
    }
    if (selectedPerfis.length > 1) {
      toast({
        variant: 'warning',
        title: 'Atenção',
        description: 'Selecione apenas um perfil para editar',
      });
      return;
    }
    setEditingPerfil(selectedPerfis[0]);
    setIsModalOpen(true);
  };

  const handleDeletePerfil = () => {
    if (selectedPerfis.length === 0) {
      toast({
        variant: 'warning',
        title: 'Atenção',
        description: 'Selecione um ou mais perfis para excluir',
      });
      return;
    }
    setIsConfirmDeleteOpen(true);
  };

  const handleConfirmDelete = async () => {
    for (const perfil of selectedPerfis) {
      await deletePerfilApi.execute(() => PerfilService.delete(perfil.id));
    }
  };

  const handleRefresh = () => {
    if (selectedContratoId) {
      loadPerfis(true, selectedContratoId);
    }
  };

  const columns: DataTableColumn<Perfil>[] = [
    {
      key: 'name',
      title: 'Nome',
      dataIndex: 'name',
      sortable: false,
    },
    {
      key: 'description',
      title: 'Descrição',
      dataIndex: 'description',
      render: (value: string) => value || '-'
    },
    {
      key: 'isSuperUser',
      title: 'Super Usuário',
      dataIndex: 'isSuperUser',
      render: (value: boolean) => value ? 'Sim' : 'Não'
    },
    {
      key: 'isDefault',
      title: 'Padrão',
      dataIndex: 'isDefault',
      render: (value: boolean) => value ? 'Sim' : 'Não'
    }
  ];

  const handleSelectionChange = (selected: Perfil[]) => {
    setSelectedPerfis(selected);
  };

  const handleModalSuccess = () => {
    setSelectedPerfis([]);
    if (selectedContratoId) {
      loadPerfis(true, selectedContratoId);
    }
  };

  const handleModalClose = () => {
    setIsModalOpen(false);
    setEditingPerfil(undefined);
  };

  return (
    <PageLayout
      title="Perfis de Permissão"
      icon={<UserCheck size={24} />}
      onAdd={handleAddPerfil}
      onEdit={handleEditPerfil}
      onDelete={handleDeletePerfil}
      onRefresh={handleRefresh}
      selectedRowsCount={selectedPerfis.length}
    >
      <div className="mb-6 p-4 bg-card rounded-lg border">
        <div className="flex gap-4 items-end">
          <div className="flex-1 flex flex-col gap-2">
            <label className="text-sm font-medium">
              Contrato (Empresa - Sistema)
            </label>
            <SearchableSelect
              options={contratos.map((contrato) => ({
                label: `${contrato.empresaName} - ${contrato.sistemaName}`,
                value: contrato.id.toString()
              }))}
              value={selectedContratoId?.toString()}
              onValueChange={handleContratoChange}
              placeholder="Selecione um contrato para listar os perfis"
              searchPlaceholder="Pesquisar contrato..."
              disabled={loadContratosApi.isLoading}
            />
          </div>
        </div>
      </div>

      {selectedContratoId ? (
        <>
          <DataTable
            columns={columns}
            data={perfis}
            loading={loadPerfisApi.isLoading || deletePerfilApi.isLoading}
            rowKey="id"
            selectable
            selectedRows={selectedPerfis}
            onSelectionChange={handleSelectionChange}
          />

          {hasMore && (
            <div className="flex justify-end mt-4">
              <Button
                variant="outline"
                onClick={loadMorePerfis}
                loading={loadMorePerfisApi.isLoading}
              >
                Carregar mais
              </Button>
            </div>
          )}
        </>
      ) : (
        <div className="text-center py-12 text-muted-foreground">
          <p>Selecione um contrato para visualizar os perfis de permissão.</p>
        </div>
      )}

      <PerfilModal
        isOpen={isModalOpen}
        onClose={handleModalClose}
        perfil={editingPerfil}
        contratoId={selectedContratoId}
        onSuccess={handleModalSuccess}
      />

      <ConfirmModal
        open={isConfirmDeleteOpen}
        onOpenChange={(open) => setIsConfirmDeleteOpen(open)}
        onConfirm={handleConfirmDelete}
        title="Confirmar Exclusão"
        description={
          selectedPerfis.length === 1
            ? `Deseja excluir o perfil "${selectedPerfis[0]?.name}"?`
            : `Deseja excluir ${selectedPerfis.length} perfis selecionados?`
        }
        confirmText="Excluir"
        variant="danger"
        loading={deletePerfilApi.isLoading}
      />
    </PageLayout>
  );
}
