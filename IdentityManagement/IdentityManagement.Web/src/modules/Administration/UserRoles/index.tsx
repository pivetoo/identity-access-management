import { useState, useEffect } from 'react';
import { RotateCcw, Ban } from 'lucide-react';
import { PageLayout, DataTable, ConfirmModal, toast, useApi, SearchableSelect, useI18n } from 'archon-ui';
import type { DataTableColumn, PageAction } from 'archon-ui';
import { ContractService } from '../../../services/contractService';
import { UserRoleService } from '../../../services/userRoleService';
import type { UserRole } from '../../../types/userRole';
import type { Contract } from '../../../types/contract';
import UserRoleFormModal from '../../../components/modals/UserRoleFormModal';
import { formatDate } from '../../../utils/date';

export default function UserRoles() {
  const { t } = useI18n()
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
        title: t('common.toast.successTitle'),
        description: t('userRole.list.toast.inactivated'),
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
        title: t('common.toast.successTitle'),
        description: t('userRole.list.toast.reactivated'),
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
        title: t('common.toast.warningTitle'),
        description: t('userRole.list.validation.selectContractFirst'),
      });
      return;
    }
    setIsModalOpen(true);
  };

  const handleDeleteUsuarioPerfil = () => {
    if (selectedUsuarioPerfis.length === 0) {
      toast({
        variant: 'warning',
        title: t('common.toast.warningTitle'),
        description: t('userRole.list.validation.selectToInactivate'),
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
        title: t('common.toast.warningTitle'),
        description: t('userRole.list.validation.selectToReactivate'),
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
      title: t('userRole.field.user'),
      dataIndex: 'username',
      sortable: false,
    },
    {
      key: 'userEmail',
      title: t('common.field.email'),
      dataIndex: 'userEmail',
      sortable: false,
    },
    {
      key: 'roleName',
      title: t('userRole.field.role'),
      dataIndex: 'roleName',
      sortable: false,
    },
    {
      key: 'companyName',
      title: t('contract.field.company'),
      dataIndex: 'companyName',
      render: (value: string) => value || t('common.value.notAvailable')
    },
    {
      key: 'isActive',
      title: t('common.column.status'),
      dataIndex: 'isActive',
      render: (value: boolean) => value ? t('common.status.active') : t('common.status.inactive')
    },
    {
      key: 'assignedAt',
      title: t('userRole.field.assignedAt'),
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
      label: t('common.action.inactivate'),
      icon: <Ban className="h-4 w-4" />,
      variant: 'outline',
      onClick: handleDeleteUsuarioPerfil,
      disabled: selectedUsuarioPerfis.length === 0
    });
  }

  if (hasInactiveSelected) {
    customActions.push({
      key: 'reactivate',
      label: t('common.action.reactivate'),
      icon: <RotateCcw className="h-4 w-4" />,
      variant: 'outline',
      onClick: handleReactivate,
      disabled: selectedUsuarioPerfis.length === 0
    });
  }

  return (
    <PageLayout
      title={t('userRole.list.title')}
      subtitle={t('userRole.list.subtitle')}
      onAdd={handleAddUsuarioPerfil}
      onRefresh={handleRefresh}
      selectedRowsCount={selectedUsuarioPerfis.length}
      actions={customActions}
    >
      <div className="mb-6 p-4 bg-card rounded-lg border">
        <div className="flex gap-4 items-end">
          <div className="flex-1 flex flex-col gap-2">
            <label className="text-sm font-medium">
              {t('userRole.list.contractLabel')}
            </label>
            <SearchableSelect
              options={contratos.map((contrato) => ({
                label: `${contrato.companyName} - ${contrato.systemApplicationName}`,
                value: contrato.id.toString()
              }))}
              value={selectedContratoId?.toString()}
              onValueChange={handleContratoChange}
              placeholder={t('userRole.list.contractPlaceholder')}
              searchPlaceholder={t('userRole.list.contractSearchPlaceholder')}
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
          <p>{t('userRole.list.emptyWithoutContract')}</p>
        </div>
      )}

      <ConfirmModal
        open={isConfirmDeleteOpen}
        onOpenChange={(open) => setIsConfirmDeleteOpen(open)}
        onConfirm={handleConfirmDelete}
        title={t('userRole.list.confirmInactivateTitle')}
        description={
          selectedUsuarioPerfis.length === 1
            ? t('userRole.list.confirmInactivateSingle')
              .replace('{0}', selectedUsuarioPerfis[0]?.username ?? '')
              .replace('{1}', selectedUsuarioPerfis[0]?.roleName ?? '')
            : t('userRole.list.confirmInactivateMultiple').replace('{0}', String(selectedUsuarioPerfis.length))
        }
        confirmText={t('common.action.inactivate')}
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
