import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowRight, Database, Plus, Calendar, AlertTriangle, CheckCircle2 } from 'lucide-react';
import { Badge, Button, Card, CardContent, useApi, useI18n } from 'archon-ui';
import { ContractService } from '../../../services/contractService';
import { TenantDatabaseService } from '../../../services/tenantDatabaseService';
import { UserRoleService } from '../../../services/userRoleService';
import type { Contract } from '../../../types/contract';
import type { TenantDatabase } from '../../../types/tenantDatabase';
import { formatDate } from '../../../utils/date';

interface ClientContractsTabProps {
  companyId: number;
  onCreateContract: () => void;
  refreshKey: number;
}

interface ContractCardData {
  contract: Contract;
  tenant?: TenantDatabase;
  userCount: number;
}

export default function ClientContractsTab({ companyId, onCreateContract, refreshKey }: ClientContractsTabProps) {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [items, setItems] = useState<ContractCardData[]>([]);

  const loadApi = useApi({
    onSuccess: (data: ContractCardData[]) => setItems(data),
  });

  useEffect(() => {
    loadApi.execute(async () => {
      const [contracts, tenants] = await Promise.all([
        ContractService.getByCompanyId(companyId),
        TenantDatabaseService.getActive(),
      ]);

      const tenantsByContract = new Map(tenants.map((t) => [t.contractId, t]));

      const data: ContractCardData[] = await Promise.all(
        contracts.map(async (contract) => {
          const userRoles = await UserRoleService.getByContract(contract.id);
          const uniqueUsers = new Set(userRoles.filter((ur) => ur.isActive).map((ur) => ur.userId));
          return {
            contract,
            tenant: tenantsByContract.get(contract.id),
            userCount: uniqueUsers.size,
          };
        }),
      );

      return data;
    });
  }, [companyId, refreshKey]);

  if (loadApi.isLoading && items.length === 0) {
    return <Skeleton />;
  }

  if (items.length === 0) {
    return (
      <EmptyState
        title={t('clientDetail.contracts.empty.title')}
        description={t('clientDetail.contracts.empty.description')}
        actionLabel={t('clientDetail.contracts.addContract')}
        onAction={onCreateContract}
      />
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-sm font-semibold text-foreground">
            {t('clientDetail.contracts.heading')}
          </h3>
          <p className="text-xs text-muted-foreground">
            {t('clientDetail.contracts.subheading').replace('{0}', items.length.toString())}
          </p>
        </div>
        <Button variant="primary" size="sm" onClick={onCreateContract}>
          <Plus className="mr-2 h-4 w-4" />
          {t('clientDetail.contracts.addContract')}
        </Button>
      </div>

      <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
        {items.map(({ contract, tenant, userCount }) => (
          <ContractCard
            key={contract.id}
            contract={contract}
            tenant={tenant}
            userCount={userCount}
            onOpen={() => navigate(`/management/clients/${companyId}/contracts/${contract.id}`)}
          />
        ))}
      </div>
    </div>
  );
}

function ContractCard({
  contract,
  tenant,
  userCount,
  onOpen,
}: {
  contract: Contract;
  tenant?: TenantDatabase;
  userCount: number;
  onOpen: () => void;
}) {
  const { t } = useI18n();

  const isExpired = contract.endDate && new Date(contract.endDate) < new Date();
  const tenantConfigured = !!tenant && tenant.isActive;

  return (
    <Card
      className="cursor-pointer transition-shadow hover:shadow-md"
      onClick={onOpen}
    >
      <CardContent className="space-y-4 p-5">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-primary/70">
              {contract.systemApplicationName}
            </div>
            <div className="mt-1 truncate text-base font-semibold text-foreground">
              {t('clientDetail.contracts.contractCardTitle').replace('{0}', contract.id.toString())}
            </div>
          </div>
          <Badge variant={contract.isActive ? 'success' : 'destructive'}>
            {contract.isActive ? t('common.status.active') : t('common.status.inactive')}
          </Badge>
        </div>

        <div className="space-y-2 border-t border-border/60 pt-3 text-xs text-muted-foreground">
          <div className="flex items-center gap-2">
            <Calendar className="h-3.5 w-3.5" />
            <span>
              {formatDate(contract.startDate)}
              {contract.endDate ? ` → ${formatDate(contract.endDate)}` : ''}
            </span>
            {isExpired && (
              <Badge variant="destructive" className="ml-auto">
                {t('clientDetail.contracts.expired')}
              </Badge>
            )}
          </div>
          <div className="flex items-center gap-2">
            <Database className="h-3.5 w-3.5" />
            {tenantConfigured ? (
              <span className="flex items-center gap-1 text-success">
                <CheckCircle2 className="h-3 w-3" />
                {t('clientDetail.contracts.tenantConfigured')}
              </span>
            ) : (
              <span className="flex items-center gap-1 text-warning">
                <AlertTriangle className="h-3 w-3" />
                {t('clientDetail.contracts.tenantMissing')}
              </span>
            )}
          </div>
          <div className="flex items-center gap-2">
            <span>{t('clientDetail.contracts.userCount').replace('{0}', userCount.toString())}</span>
          </div>
        </div>

        <div className="flex items-center justify-end pt-1 text-xs font-medium text-primary">
          {t('clientDetail.contracts.open')}
          <ArrowRight className="ml-1 h-3.5 w-3.5" />
        </div>
      </CardContent>
    </Card>
  );
}

function Skeleton() {
  return (
    <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
      {[1, 2, 3].map((i) => (
        <div key={i} className="h-40 animate-pulse rounded-lg border border-border bg-muted/30" />
      ))}
    </div>
  );
}

function EmptyState({
  title,
  description,
  actionLabel,
  onAction,
}: {
  title: string;
  description: string;
  actionLabel: string;
  onAction: () => void;
}) {
  return (
    <div className="rounded-lg border border-dashed border-border bg-muted/20 p-10 text-center">
      <h3 className="text-sm font-semibold text-foreground">{title}</h3>
      <p className="mt-1 text-xs text-muted-foreground">{description}</p>
      <Button variant="primary" size="sm" className="mt-4" onClick={onAction}>
        <Plus className="mr-2 h-4 w-4" />
        {actionLabel}
      </Button>
    </div>
  );
}
