import { useState, useEffect } from 'react';
import { PageLayout, DataTable, Button, ConfirmModal, toast, useApi, SearchableSelect, useI18n } from 'archon-ui';
import type { DataTableColumn, PaginatedResult } from 'archon-ui';
import { RoleService } from '../../../services/roleService';
import { ContractService } from '../../../services/contractService';
import type { Role } from '../../../types/role';
import type { Contract } from '../../../types/contract';
import RoleFormModal from '../../../components/modals/RoleFormModal';

export default function Roles() {
  const { t } = useI18n()
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
        title: t('common.toast.successTitle'),
        description: t('role.list.toast.deleted'),
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
        title: t('common.toast.warningTitle'),
        description: t('role.list.validation.selectContractFirst'),
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
        title: t('common.toast.warningTitle'),
        description: t('role.list.validation.selectOneToEdit'),
      });
      return;
    }
    if (selectedPerfis.length > 1) {
      toast({
        variant: 'warning',
        title: t('common.toast.warningTitle'),
        description: t('role.list.validation.selectOnlyOneToEdit'),
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
        title: t('common.toast.warningTitle'),
        description: t('role.list.validation.selectToDelete'),
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
      title: t('common.column.name'),
      dataIndex: 'name',
      sortable: false,
    },
    {
      key: 'description',
      title: t('common.field.description'),
      dataIndex: 'description',
      render: (value: string) => value || t('common.value.notAvailable')
    },
    {
      key: 'isRoot',
      title: t('role.field.isRoot'),
      dataIndex: 'isRoot',
      render: (value: boolean) => value ? t('common.boolean.yes') : t('common.boolean.no')
    },
    {
      key: 'isDefault',
      title: t('role.field.isDefault'),
      dataIndex: 'isDefault',
      render: (value: boolean) => value ? t('common.boolean.yes') : t('common.boolean.no')
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
      title={t('role.list.title')}
      subtitle={t('role.list.subtitle')}
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
              {t('role.list.contractLabel')}
            </label>
            <SearchableSelect
              options={contratos.map((contrato) => ({
                label: `${contrato.companyName} - ${contrato.systemApplicationName}`,
                value: contrato.id.toString()
              }))}
              value={selectedContratoId?.toString()}
              onValueChange={handleContratoChange}
              placeholder={t('role.list.contractPlaceholder')}
              searchPlaceholder={t('role.list.contractSearchPlaceholder')}
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
                {t('common.action.loadMore')}
              </Button>
            </div>
          )}
        </>
      ) : (
        <div className="text-center py-12 text-muted-foreground">
          <p>{t('role.list.emptyWithoutContract')}</p>
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
        title={t('common.confirm.deleteTitle')}
        description={
          selectedPerfis.length === 1
            ? t('role.list.confirmDeleteSingle').replace('{0}', selectedPerfis[0]?.name ?? '')
            : t('role.list.confirmDeleteMultiple').replace('{0}', String(selectedPerfis.length))
        }
        confirmText={t('common.action.delete')}
        variant="danger"
        loading={deletePerfilApi.isLoading}
      />
    </PageLayout>
  );
}
