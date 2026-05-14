import { useEffect, useState } from 'react';
import { Ban, Mail, Plus, RotateCcw } from 'lucide-react';
import { Badge, Button, Card, CardContent, ConfirmModal, DataTable, TableToolbar, toast, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import { UserRoleService } from '../../../services/userRoleService';
import type { UserRole } from '../../../types/userRole';
import UserRoleFormModal from '../../../components/modals/UserRoleFormModal';
import { formatDate } from '../../../utils/date';

interface ContractPeopleTabProps {
  contractId: number;
  refreshKey: number;
  onRefresh: () => void;
}

export default function ContractPeopleTab({ contractId, refreshKey, onRefresh }: ContractPeopleTabProps) {
  const { t } = useI18n();
  const [userRoles, setUserRoles] = useState<UserRole[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [confirmAction, setConfirmAction] = useState<{ item: UserRole; type: 'revoke' | 'reactivate' } | null>(null);

  const loadApi = useApi({
    onSuccess: (data: UserRole[]) => setUserRoles(data),
  });

  const toggleApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: t('common.toast.successTitle'),
        description: t('contractDetail.people.toast.updated'),
      });
      setConfirmAction(null);
      onRefresh();
    },
    onError: () => setConfirmAction(null),
  });

  useEffect(() => {
    loadApi.execute(() => UserRoleService.getByContract(contractId));
  }, [contractId, refreshKey]);

  const handleToggle = async () => {
    if (!confirmAction) {
      return;
    }
    const { item, type } = confirmAction;
    if (type === 'revoke') {
      await toggleApi.execute(() => UserRoleService.revoke(item.userId, item.roleId));
    } else {
      await toggleApi.execute(() => UserRoleService.reactivate(item.userId, item.roleId));
    }
  };

  const filtered = userRoles.filter((ur) => {
    const search = searchTerm.trim().toLowerCase();
    return (
      !search ||
      ur.username.toLowerCase().includes(search) ||
      ur.userEmail.toLowerCase().includes(search) ||
      ur.roleName.toLowerCase().includes(search)
    );
  });

  const columns: DataTableColumn<UserRole>[] = [
    {
      key: 'user',
      title: t('common.column.username'),
      dataIndex: 'username',
      render: (value: string, record) => (
        <div>
          <div className="font-medium text-foreground">{value}</div>
          <div className="flex items-center gap-1 text-xs text-muted-foreground">
            <Mail className="h-3 w-3" /> {record.userEmail}
          </div>
        </div>
      ),
    },
    {
      key: 'role',
      title: t('clientDetail.people.roles'),
      dataIndex: 'roleName',
      render: (value: string, record) => (
        <Badge variant={record.isRoot ? 'info' : 'secondary'}>{value}</Badge>
      ),
    },
    {
      key: 'assignedAt',
      title: t('contractDetail.people.assignedAt'),
      dataIndex: 'assignedAt',
      render: (value: string) => formatDate(value),
    },
    {
      key: 'status',
      title: t('common.column.status'),
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? t('common.status.active') : t('common.status.inactive')}
        </Badge>
      ),
    },
    {
      key: 'actions',
      title: '',
      dataIndex: 'id',
      render: (_value, record) => (
        <button
          type="button"
          onClick={() =>
            setConfirmAction({ item: record, type: record.isActive ? 'revoke' : 'reactivate' })
          }
          className="inline-flex items-center gap-1 text-xs font-semibold text-primary hover:underline"
        >
          {record.isActive ? (
            <>
              <Ban className="h-3 w-3" />
              {t('common.action.revoke')}
            </>
          ) : (
            <>
              <RotateCcw className="h-3 w-3" />
              {t('common.action.reactivate')}
            </>
          )}
        </button>
      ),
    },
  ];

  if (loadApi.isLoading && userRoles.length === 0) {
    return <div className="h-32 animate-pulse rounded-lg border border-border bg-muted/30" />;
  }

  return (
    <>
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-sm font-semibold text-foreground">
              {t('contractDetail.people.heading')}
            </h3>
            <p className="text-xs text-muted-foreground">
              {t('contractDetail.people.subheading').replace('{0}', userRoles.length.toString())}
            </p>
          </div>
          <Button variant="primary" size="sm" onClick={() => setIsModalOpen(true)}>
            <Plus className="mr-2 h-4 w-4" />
            {t('contractDetail.people.assignUser')}
          </Button>
        </div>

        {userRoles.length === 0 ? (
          <Card>
            <CardContent className="py-10 text-center">
              <h3 className="text-sm font-semibold text-foreground">
                {t('contractDetail.people.empty.title')}
              </h3>
              <p className="mt-1 text-xs text-muted-foreground">
                {t('contractDetail.people.empty.description')}
              </p>
            </CardContent>
          </Card>
        ) : (
          <>
            <TableToolbar
              searchValue={searchTerm}
              onSearchChange={setSearchTerm}
              searchPlaceholder={t('contractDetail.people.searchPlaceholder')}
            />
            <DataTable columns={columns} data={filtered} rowKey="id" />
          </>
        )}
      </div>

      <UserRoleFormModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        contractId={contractId}
        onSuccess={() => {
          setIsModalOpen(false);
          onRefresh();
        }}
      />

      <ConfirmModal
        open={!!confirmAction}
        onOpenChange={(open) => !open && setConfirmAction(null)}
        onConfirm={handleToggle}
        title={
          confirmAction?.type === 'revoke'
            ? t('contractDetail.people.confirmRevokeTitle')
            : t('contractDetail.people.confirmReactivateTitle')
        }
        description={
          confirmAction
            ? t(
                confirmAction.type === 'revoke'
                  ? 'contractDetail.people.confirmRevokeDescription'
                  : 'contractDetail.people.confirmReactivateDescription',
              )
                .replace('{0}', confirmAction.item.username)
                .replace('{1}', confirmAction.item.roleName)
            : ''
        }
        confirmText={
          confirmAction?.type === 'revoke' ? t('common.action.revoke') : t('common.action.reactivate')
        }
        variant={confirmAction?.type === 'revoke' ? 'danger' : 'primary'}
        loading={toggleApi.isLoading}
      />
    </>
  );
}
