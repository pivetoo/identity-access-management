import { useEffect, useMemo, useState } from 'react';
import { Mail } from 'lucide-react';
import { Badge, Card, CardContent, DataTable, FilterDropdown, TableToolbar, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn } from 'archon-ui';
import { ContractService } from '../../../services/contractService';
import { UserRoleService } from '../../../services/userRoleService';
import type { Contract } from '../../../types/contract';

interface ClientPeopleTabProps {
  companyId: number;
  refreshKey: number;
}

interface AggregatedUser {
  userId: number;
  username: string;
  userEmail: string;
  isActive: boolean;
  roles: Array<{
    systemApplicationName: string;
    roleName: string;
    contractId: number;
    isRoot: boolean;
  }>;
}

export default function ClientPeopleTab({ companyId, refreshKey }: ClientPeopleTabProps) {
  const { t } = useI18n();
  const [users, setUsers] = useState<AggregatedUser[]>([]);
  const [contracts, setContracts] = useState<Contract[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [contractFilter, setContractFilter] = useState<string>('all');

  const loadApi = useApi({
    onSuccess: ({ aggregated, allContracts }: { aggregated: AggregatedUser[]; allContracts: Contract[] }) => {
      setUsers(aggregated);
      setContracts(allContracts);
    },
  });

  useEffect(() => {
    loadApi.execute(async () => {
      const allContracts = await ContractService.getByCompanyId(companyId);

      const userRolesByContract = await Promise.all(
        allContracts.map(async (contract) => ({
          contract,
          userRoles: await UserRoleService.getByContract(contract.id),
        })),
      );

      const byUserId = new Map<number, AggregatedUser>();
      for (const { contract, userRoles } of userRolesByContract) {
        for (const ur of userRoles) {
          if (!ur.isActive) {
            continue;
          }

          const existing = byUserId.get(ur.userId);
          const roleEntry = {
            systemApplicationName: contract.systemApplicationName,
            roleName: ur.roleName,
            contractId: contract.id,
            isRoot: !!ur.isRoot,
          };

          if (existing) {
            existing.roles.push(roleEntry);
          } else {
            byUserId.set(ur.userId, {
              userId: ur.userId,
              username: ur.username,
              userEmail: ur.userEmail,
              isActive: true,
              roles: [roleEntry],
            });
          }
        }
      }

      return { aggregated: Array.from(byUserId.values()), allContracts };
    });
  }, [companyId, refreshKey]);

  const filteredUsers = useMemo(() => {
    const search = searchTerm.trim().toLowerCase();
    return users.filter((user) => {
      const matchesSearch =
        !search ||
        user.username.toLowerCase().includes(search) ||
        user.userEmail.toLowerCase().includes(search);
      const matchesContract =
        contractFilter === 'all' || user.roles.some((r) => r.contractId.toString() === contractFilter);
      return matchesSearch && matchesContract;
    });
  }, [users, searchTerm, contractFilter]);

  const columns: DataTableColumn<AggregatedUser>[] = [
    {
      key: 'username',
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
      key: 'roles',
      title: t('clientDetail.people.roles'),
      dataIndex: 'roles',
      render: (_value, record) => (
        <div className="flex flex-wrap gap-1">
          {record.roles.map((role, idx) => (
            <Badge
              key={`${role.contractId}-${role.roleName}-${idx}`}
              variant={role.isRoot ? 'info' : 'secondary'}
              className="text-[10px]"
            >
              {role.systemApplicationName} · {role.roleName}
            </Badge>
          ))}
        </div>
      ),
    },
  ];

  if (loadApi.isLoading && users.length === 0) {
    return <div className="h-32 animate-pulse rounded-lg border border-border bg-muted/30" />;
  }

  if (users.length === 0) {
    return (
      <Card>
        <CardContent className="py-10 text-center">
          <h3 className="text-sm font-semibold text-foreground">
            {t('clientDetail.people.empty.title')}
          </h3>
          <p className="mt-1 text-xs text-muted-foreground">
            {t('clientDetail.people.empty.description')}
          </p>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-4">
      <TableToolbar
        searchValue={searchTerm}
        onSearchChange={setSearchTerm}
        searchPlaceholder={t('clientDetail.people.searchPlaceholder')}
        rightSlot={
          contracts.length > 1 ? (
            <FilterDropdown
              label={t('clientDetail.people.filterByContract')}
              value={contractFilter}
              onChange={setContractFilter}
              options={contracts.map((c) => ({
                value: c.id.toString(),
                label: c.systemApplicationName,
              }))}
            />
          ) : undefined
        }
      />

      <DataTable
        columns={columns}
        data={filteredUsers}
        rowKey="userId"
      />
    </div>
  );
}
