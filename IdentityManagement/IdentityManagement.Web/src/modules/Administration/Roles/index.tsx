import { useState, useEffect } from 'react';
import { PageLayout, DataTable, Button, ConfirmModal, toast, useApi, SearchableSelect } from 'archon-ui';
import type { DataTableColumn, PaginatedResult } from 'archon-ui';
import { RoleService } from '../../../services/roleService';
import { ContractService } from '../../../services/contractService';
import type { Role } from '../../../types/role';
import type { Contract } from '../../../types/contract';
import RoleFormModal from '../../../components/modals/RoleFormModal';

export default function Roles() {
  const [selectedPerfis, setSelectedPerfis] = useState<Role[]>([]);
  const [perfis, setPerfis] = useState<Role[]>([]);
  const [contratos, setContratos] = useState<Contract[]>([]);
  const [selectedContratoId, setSelectedContratoId] = useState<number | undefined>(undefined);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingPerfil, setEditingPerfil] = useState<Role | undefined>();
  const [isConfirmDeleteOpen, setIsConfirmDeleteOpen] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const pageSize = 30;

  const loadPerfisApi = useApi({
    onSuccess: (data: PaginatedResult<Role>) => {
      setPerfis(prev => [...prev, ...data.data] as Role[]);
      setHasMore(false);
    }
  });

  const loadMorePerfisApi = useApi({
    onSuccess: (data: PaginatedResult<Role>) => {
      setPerfis(prev => [...prev, ...data.data]);
      setHasMore(data.data.length === pageSize);
    }
  });

  const deletePerfilApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: 'Sucesso',
        description: 'Role excluido com sucesso',
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
    onSuccess: (data: Contract[]) => {
      setContratos(data);
    }
  });

  const loadPerfis = async (reset = false, contractId?: number) => {
    if (!contractId && !selectedContratoId) {
      setPerfis([]);
      return;
    }

    const targetContratoId = contractId || selectedContratoId;
    if (reset) {
      setPerfis([]);
    }
    await loadPerfisApi.execute(async () => {
      const data = await RoleService.getByContrato(targetContratoId!, {
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
      RoleService.getByContrato(selectedContratoId, {
        page: currentPage + 1,
        pageSize: pageSize
      })
    );
  };

  useEffect(() => {
    loadContratosApi.execute(() => ContractService.getActive());
  }, []);

  const handleContratoChange = (contractId: string) => {
    const id = contractId ? parseInt(contractId) : undefined;
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
      await deletePerfilApi.execute(() => RoleService.delete(perfil.id));
    }
  };

  const handleRefresh = () => {
    if (selectedContratoId) {
      loadPerfis(true, selectedContratoId);
    }
  };

  const columns: DataTableColumn<Role>[] = [
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
      key: 'isRoot',
      title: 'Super Usuário',
      dataIndex: 'isRoot',
      render: (value: boolean) => value ? 'Sim' : 'Não'
    },
    {
      key: 'isDefault',
      title: 'Padrão',
      dataIndex: 'isDefault',
      render: (value: boolean) => value ? 'Sim' : 'Não'
    }
  ];

  const handleSelectionChange = (selected: Role[]) => {
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
      subtitle="Defina perfis de acesso e comportamento padrão por contrato."
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
              Contract (Company - SystemApplication)
            </label>
            <SearchableSelect
              options={contratos.map((contrato) => ({
                label: `${contrato.companyName} - ${contrato.systemApplicationName}`,
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

      <RoleFormModal
        isOpen={isModalOpen}
        onClose={handleModalClose}
        role={editingPerfil}
        contractId={selectedContratoId}
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
