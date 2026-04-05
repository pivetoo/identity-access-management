import { useState, useEffect } from 'react';
import { RotateCcw, Ban } from 'lucide-react';
import { PageLayout, DataTable, ConfirmModal, toast, useApi, SearchableSelect } from 'archon-ui';
import type { DataTableColumn, PageAction } from 'archon-ui';
import { ContractService } from '../../../services/contractService';
import { UserRoleService } from '../../../services/userRoleService';
import type { UserRole } from '../../../types/userRole';
import type { Contract } from '../../../types/contract';
import UserRoleFormModal from '../../../components/modals/UserRoleFormModal';
import { formatDate } from '../../../utils/date';

export default function UserRoles() {
  const [selectedUsuarioPerfis, setSelectedUsuarioPerfis] = useState<UserRole[]>([]);
  const [usuarioPerfis, setUsuarioPerfis] = useState<UserRole[]>([]);
  const [contratos, setContratos] = useState<Contract[]>([]);
  const [selectedContratoId, setSelectedContratoId] = useState<number | undefined>(undefined);
  const [isConfirmDeleteOpen, setIsConfirmDeleteOpen] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);

  const loadUsuarioPerfisApi = useApi({
    onSuccess: (data: UserRole[]) => {
      setUsuarioPerfis(data);
    }
  });

  const loadContratosApi = useApi({
    onSuccess: (data: Contract[]) => {
      setContratos(data);
    }
  });

  const revokeUsuarioPerfilApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: 'Sucesso',
        description: 'Vinculação inativada com sucesso',
      });
      loadUsuarioPerfis();
      setSelectedUsuarioPerfis([]);
      setIsConfirmDeleteOpen(false);
    },
    onError: () => {
      setIsConfirmDeleteOpen(false);
    }
  });

  const reactivateUsuarioPerfilApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: 'Sucesso',
        description: 'Vinculação reativada com sucesso',
      });
      loadUsuarioPerfis();
      setSelectedUsuarioPerfis([]);
    }
  });

  const loadUsuarioPerfis = async (contractId?: number) => {
    const targetContratoId = contractId || selectedContratoId;
    if (!targetContratoId) {
      setUsuarioPerfis([]);
      return;
    }
    await loadUsuarioPerfisApi.execute(() => UserRoleService.getByContrato(targetContratoId));
  };

  useEffect(() => {
    loadContratosApi.execute(() => ContractService.getActive());
  }, []);

  const handleContratoChange = (contractId: string) => {
    const id = contractId ? parseInt(contractId) : undefined;
    setSelectedContratoId(id);
    setSelectedUsuarioPerfis([]);
    if (id) {
      loadUsuarioPerfis(id);
    } else {
      setUsuarioPerfis([]);
    }
  };

  const handleAddUsuarioPerfil = () => {
    if (!selectedContratoId) {
      toast({
        variant: 'warning',
        title: 'Atenção',
        description: 'Selecione um contrato primeiro',
      });
      return;
    }
    setIsModalOpen(true);
  };

  const handleDeleteUsuarioPerfil = () => {
    if (selectedUsuarioPerfis.length === 0) {
      toast({
        variant: 'warning',
        title: 'Atenção',
        description: 'Selecione uma ou mais vinculações para remover',
      });
      return;
    }
    setIsConfirmDeleteOpen(true);
  };

  const handleConfirmDelete = async () => {
    for (const usuarioPerfil of selectedUsuarioPerfis) {
      await revokeUsuarioPerfilApi.execute(() => UserRoleService.revoke(usuarioPerfil.userId, usuarioPerfil.roleId));
    }
  };

  const handleReactivate = async () => {
    if (selectedUsuarioPerfis.length === 0) {
      toast({
        variant: 'warning',
        title: 'Atenção',
        description: 'Selecione uma ou mais vinculações para reativar',
      });
      return;
    }

    for (const usuarioPerfil of selectedUsuarioPerfis) {
      await reactivateUsuarioPerfilApi.execute(() => UserRoleService.reactivate(usuarioPerfil.userId, usuarioPerfil.roleId));
    }
  };

  const handleRefresh = () => {
    if (selectedContratoId) {
      loadUsuarioPerfis(selectedContratoId);
    }
  };

  const columns: DataTableColumn<UserRole>[] = [
    {
      key: 'username',
      title: 'Usuário',
      dataIndex: 'username',
      sortable: false,
    },
    {
      key: 'userEmail',
      title: 'E-mail',
      dataIndex: 'userEmail',
      sortable: false,
    },
    {
      key: 'roleName',
      title: 'Role',
      dataIndex: 'roleName',
      sortable: false,
    },
    {
      key: 'companyName',
      title: 'Company',
      dataIndex: 'companyName',
      render: (value: string) => value || '-'
    },
    {
      key: 'isActive',
      title: 'Status',
      dataIndex: 'isActive',
      render: (value: boolean) => value ? 'Ativo' : 'Inativo'
    },
    {
      key: 'assignedAt',
      title: 'Data Vinculação',
      dataIndex: 'assignedAt',
      render: (value: string) => formatDate(value),
      sortable: false,
    }
  ];

  const handleSelectionChange = (selected: UserRole[]) => {
    setSelectedUsuarioPerfis(selected);
  };

  const handleModalSuccess = () => {
    setSelectedUsuarioPerfis([]);
    if (selectedContratoId) {
      loadUsuarioPerfis(selectedContratoId);
    }
  };

  const handleModalClose = () => {
    setIsModalOpen(false);
  };

  const hasInactiveSelected = selectedUsuarioPerfis.some(up => !up.isActive);
  const hasActiveSelected = selectedUsuarioPerfis.some(up => up.isActive);

  const customActions: PageAction[] = [];

  if (hasActiveSelected) {
    customActions.push({
      key: 'inactivate',
      label: 'Inativar',
      icon: <Ban className="h-4 w-4" />,
      variant: 'outline',
      onClick: handleDeleteUsuarioPerfil,
      disabled: selectedUsuarioPerfis.length === 0
    });
  }

  if (hasInactiveSelected) {
    customActions.push({
      key: 'reactivate',
      label: 'Reativar',
      icon: <RotateCcw className="h-4 w-4" />,
      variant: 'outline',
      onClick: handleReactivate,
      disabled: selectedUsuarioPerfis.length === 0
    });
  }

  return (
    <PageLayout
      title="Vincular Usuários"
      subtitle="Associe usuários aos perfis disponíveis em cada contrato."
      onAdd={handleAddUsuarioPerfil}
      onRefresh={handleRefresh}
      selectedRowsCount={selectedUsuarioPerfis.length}
      actions={customActions}
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
              placeholder="Selecione um contrato para listar as vinculações"
              searchPlaceholder="Pesquisar contrato..."
              disabled={loadContratosApi.isLoading}
            />
          </div>
        </div>
      </div>

      {selectedContratoId ? (
        <DataTable
          columns={columns}
          data={usuarioPerfis}
          loading={loadUsuarioPerfisApi.isLoading || revokeUsuarioPerfilApi.isLoading || reactivateUsuarioPerfilApi.isLoading}
          rowKey="id"
          selectable
          selectedRows={selectedUsuarioPerfis}
          onSelectionChange={handleSelectionChange}
        />
      ) : (
        <div className="text-center py-12 text-muted-foreground">
          <p>Selecione um contrato para visualizar as vinculações de usuarios.</p>
        </div>
      )}

      <ConfirmModal
        open={isConfirmDeleteOpen}
        onOpenChange={(open) => setIsConfirmDeleteOpen(open)}
        onConfirm={handleConfirmDelete}
        title="Confirmar Inativação"
        description={
          selectedUsuarioPerfis.length === 1
            ? `Deseja inativar a vinculação do usuário "${selectedUsuarioPerfis[0]?.username}" com o perfil "${selectedUsuarioPerfis[0]?.roleName}"?`
            : `Deseja inativar ${selectedUsuarioPerfis.length} vinculações selecionadas?`
        }
        confirmText="Inativar"
        variant="danger"
        loading={revokeUsuarioPerfilApi.isLoading}
      />

      <UserRoleFormModal
        isOpen={isModalOpen}
        onClose={handleModalClose}
        contractId={selectedContratoId || 0}
        onSuccess={handleModalSuccess}
      />
    </PageLayout>
  );
}
