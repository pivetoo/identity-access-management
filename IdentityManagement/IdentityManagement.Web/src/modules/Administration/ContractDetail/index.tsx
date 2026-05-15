import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Database, Info, Shield, Users } from 'lucide-react';
import { Badge, Button, Card, CardContent, PageLayout, Tabs, TabsBadge, TabsContent, TabsList, TabsTrigger, useApi, useI18n } from 'archon-ui';
import { ContractService } from '../../../services/contractService';
import { CompanyService } from '../../../services/companyService';
import { RoleService } from '../../../services/roleService';
import { UserRoleService } from '../../../services/userRoleService';
import { TenantDatabaseService } from '../../../services/tenantDatabaseService';
import type { Contract } from '../../../types/contract';
import type { Company } from '../../../types/company';
import type { TenantDatabase } from '../../../types/tenantDatabase';
import type { Role } from '../../../types/role';
import type { UserRole } from '../../../types/userRole';
import ContractFormModal from '../../../components/modals/ContractFormModal';
import ContractOverviewTab from './ContractOverviewTab';
import ContractTenantTab from './ContractTenantTab';
import ContractRolesTab from './ContractRolesTab';
import ContractPeopleTab from './ContractPeopleTab';
import { formatDate } from '../../../utils/date';

interface DetailData {
  contract: Contract;
  company: Company;
  tenant: TenantDatabase | null;
  roles: Role[];
  userRoles: UserRole[];
}

export default function ContractDetail() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { id: companyIdParam, contractId: contractIdParam } = useParams<{ id: string; contractId: string }>();
  const companyId = Number(companyIdParam);
  const contractId = Number(contractIdParam);

  const [data, setData] = useState<DetailData | null>(null);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const [activeTab, setActiveTab] = useState('overview');

  const loadApi = useApi({
    onSuccess: (loaded: DetailData) => setData(loaded),
  });

  useEffect(() => {
    if (!Number.isFinite(contractId) || contractId <= 0) {
      return;
    }
    loadApi.execute(async () => {
      const contracts = await ContractService.getByCompanyId(companyId);
      const contract = contracts.find((c) => c.id === contractId);
      if (!contract) {
        throw new Error(t('contractDetail.notFound.title'));
      }

      const [company, allTenants, roles, userRoles] = await Promise.all([
        CompanyService.getById(companyId),
        TenantDatabaseService.getActive(),
        RoleService.getByContractSummary(contractId),
        UserRoleService.getByContract(contractId),
      ]);

      return {
        contract,
        company,
        tenant: allTenants.find((tnt) => tnt.contractId === contractId) ?? null,
        roles,
        userRoles,
      };
    });
  }, [companyId, contractId, refreshKey]);

  const handleRefresh = () => setRefreshKey((k) => k + 1);

  const stats = useMemo(() => {
    if (!data) {
      return null;
    }
    const activeUsers = new Set(data.userRoles.filter((ur) => ur.isActive).map((ur) => ur.userId)).size;
    return {
      tenantConfigured: !!data.tenant && data.tenant.isActive,
      rolesCount: data.roles.length,
      activeUsers,
    };
  }, [data]);

  if (loadApi.isLoading && !data) {
    return (
      <PageLayout title="..." subtitle="">
        <div className="h-64 animate-pulse rounded-lg border border-border bg-muted/30" />
      </PageLayout>
    );
  }

  if (!data) {
    return (
      <PageLayout title={t('contractDetail.notFound.title')} subtitle="">
        <Card>
          <CardContent className="py-10 text-center">
            <p className="text-sm text-muted-foreground">{t('contractDetail.notFound.description')}</p>
            <Button variant="primary" className="mt-4" onClick={() => navigate('/management/companies')}>
              <ArrowLeft className="mr-2 h-4 w-4" />
              {t('clientDetail.backToList')}
            </Button>
          </CardContent>
        </Card>
      </PageLayout>
    );
  }

  return (
    <>
      <PageLayout
        title={data.contract.systemApplicationName}
        subtitle={`${data.company.legalName}`}
        showDefaultActions={false}
      >
        <div className="space-y-5">
          {stats && (
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
              <StatCard
                label={t('common.column.status')}
                value={
                  <Badge variant={data.contract.isActive ? 'success' : 'destructive'}>
                    {data.contract.isActive ? t('common.status.active') : t('common.status.inactive')}
                  </Badge>
                }
                icon={<Info className="h-4 w-4" />}
              />
              <StatCard
                label={t('contractDetail.metrics.tenant')}
                value={
                  <Badge variant={stats.tenantConfigured ? 'success' : 'destructive'}>
                    {stats.tenantConfigured
                      ? t('clientDetail.contracts.tenantConfigured')
                      : t('clientDetail.contracts.tenantMissing')}
                  </Badge>
                }
                icon={<Database className="h-4 w-4" />}
                tone={stats.tenantConfigured ? 'default' : 'warning'}
              />
              <StatCard
                label={t('contractDetail.metrics.roles')}
                value={stats.rolesCount}
                hint={t('contractDetail.metrics.rolesHint')}
                icon={<Shield className="h-4 w-4" />}
              />
              <StatCard
                label={t('contractDetail.metrics.activeUsers')}
                value={stats.activeUsers}
                hint={t('contractDetail.metrics.activeUsersHint')}
                icon={<Users className="h-4 w-4" />}
              />
            </div>
          )}

          <div className="text-xs text-muted-foreground">
            {t('common.field.startDate')}: <strong>{formatDate(data.contract.startDate)}</strong>
            {data.contract.endDate
              ? ` · ${t('common.field.endDate')}: ${formatDate(data.contract.endDate)}`
              : ''}
          </div>

          <Tabs value={activeTab} onValueChange={setActiveTab} className="pt-2">
            <TabsList variant="underline" className="mb-6">
              <TabsTrigger value="overview">
                <Info className="h-4 w-4" /> {t('contractDetail.tabs.overview')}
              </TabsTrigger>
              <TabsTrigger value="tenant">
                <Database className="h-4 w-4" /> {t('contractDetail.tabs.tenant')}
              </TabsTrigger>
              <TabsTrigger value="roles">
                <Shield className="h-4 w-4" /> {t('contractDetail.tabs.roles')}
                {data.roles.length ? <TabsBadge>{data.roles.length}</TabsBadge> : null}
              </TabsTrigger>
              <TabsTrigger value="people">
                <Users className="h-4 w-4" /> {t('contractDetail.tabs.people')}
                {stats?.activeUsers ? <TabsBadge>{stats.activeUsers}</TabsBadge> : null}
              </TabsTrigger>
            </TabsList>

            <TabsContent value="overview" className="mt-0">
              <ContractOverviewTab contract={data.contract} onEdit={() => setIsEditModalOpen(true)} />
            </TabsContent>

            <TabsContent value="tenant" className="mt-0">
              <ContractTenantTab contractId={contractId} refreshKey={refreshKey} onRefresh={handleRefresh} />
            </TabsContent>

            <TabsContent value="roles" className="mt-0">
              <ContractRolesTab contractId={contractId} refreshKey={refreshKey} onRefresh={handleRefresh} />
            </TabsContent>

            <TabsContent value="people" className="mt-0">
              <ContractPeopleTab contractId={contractId} refreshKey={refreshKey} onRefresh={handleRefresh} />
            </TabsContent>
          </Tabs>
        </div>
      </PageLayout>

      <ContractFormModal
        isOpen={isEditModalOpen}
        onClose={() => setIsEditModalOpen(false)}
        contrato={data.contract}
        onSuccess={() => {
          setIsEditModalOpen(false);
          handleRefresh();
        }}
      />
    </>
  );
}

function StatCard({
  label,
  value,
  hint,
  icon,
  tone,
}: {
  label: string;
  value: React.ReactNode;
  hint?: string;
  icon: React.ReactNode;
  tone?: 'default' | 'warning';
}) {
  return (
    <Card>
      <CardContent className="space-y-2 p-4">
        <div className="flex items-center justify-between gap-2">
          <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
            {label}
          </div>
          <div
            className={`rounded-md p-1.5 ${
              tone === 'warning' ? 'bg-warning/15 text-warning' : 'bg-primary/15 text-primary'
            }`}
          >
            {icon}
          </div>
        </div>
        <div className="text-2xl font-semibold text-foreground">{value}</div>
        {hint && <div className="text-xs text-muted-foreground">{hint}</div>}
      </CardContent>
    </Card>
  );
}
