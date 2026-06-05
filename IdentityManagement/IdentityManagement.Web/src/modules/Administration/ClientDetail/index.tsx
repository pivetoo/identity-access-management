import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, FileText, Users, Activity, Info, Database, CreditCard } from 'lucide-react';
import { Badge, Button, Card, CardContent, PageLayout, Tabs, TabsBadge, TabsContent, TabsList, TabsTrigger, useApi, useI18n } from 'archon-ui';
import { CompanyService } from '../../../services/companyService';
import { ContractService } from '../../../services/contractService';
import { TenantDatabaseService } from '../../../services/tenantDatabaseService';
import type { Company } from '../../../types/company';
import type { Contract } from '../../../types/contract';
import type { TenantDatabase } from '../../../types/tenantDatabase';
import CompanyFormModal from '../../../components/modals/CompanyFormModal';
import ContractFormModal from '../../../components/modals/ContractFormModal';
import ClientOverviewTab from './ClientOverviewTab';
import ClientContractsTab from './ClientContractsTab';
import ClientPeopleTab from './ClientPeopleTab';
import ClientSubscriptionTab from './ClientSubscriptionTab';

export default function ClientDetail() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const companyId = Number(id);

  const [company, setCompany] = useState<Company | null>(null);
  const [contracts, setContracts] = useState<Contract[]>([]);
  const [tenants, setTenants] = useState<TenantDatabase[]>([]);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isContractModalOpen, setIsContractModalOpen] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const [activeTab, setActiveTab] = useState('overview');

  const loadCompanyApi = useApi({
    onSuccess: (data: { company: Company; contracts: Contract[]; tenants: TenantDatabase[] }) => {
      setCompany(data.company);
      setContracts(data.contracts);
      setTenants(data.tenants);
    },
  });

  const loadEverything = () => {
    if (!Number.isFinite(companyId) || companyId <= 0) {
      return;
    }
    loadCompanyApi.execute(async () => {
      const [companyData, contractList, allTenants] = await Promise.all([
        CompanyService.getById(companyId),
        ContractService.getByCompanyId(companyId),
        TenantDatabaseService.getActive(),
      ]);
      return {
        company: companyData,
        contracts: contractList,
        tenants: allTenants,
      };
    });
  };

  useEffect(() => {
    loadEverything();
  }, [companyId, refreshKey]);

  const metrics = useMemo(() => {
    if (!company) {
      return null;
    }
    const activeContracts = contracts.filter((c) => c.isActive).length;
    const tenantsByContract = new Map(tenants.map((tnt) => [tnt.contractId, tnt]));
    const configuredTenants = contracts.filter((c) => tenantsByContract.get(c.id)?.isActive).length;
    const expiringSoon = contracts.filter((c) => {
      if (!c.endDate) {
        return false;
      }
      const end = new Date(c.endDate).getTime();
      const now = Date.now();
      const days30 = 30 * 24 * 60 * 60 * 1000;
      return end > now && end - now < days30;
    }).length;

    return {
      activeContracts,
      totalContracts: contracts.length,
      configuredTenants,
      expiringSoon,
    };
  }, [company, contracts, tenants]);

  if (loadCompanyApi.isLoading && !company) {
    return (
      <PageLayout title="..." subtitle="">
        <div className="h-64 animate-pulse rounded-lg border border-border bg-muted/30" />
      </PageLayout>
    );
  }

  if (!company) {
    return (
      <PageLayout title={t('clientDetail.notFound.title')} subtitle="">
        <Card>
          <CardContent className="py-10 text-center">
            <p className="text-sm text-muted-foreground">{t('clientDetail.notFound.description')}</p>
            <Button variant="primary" className="mt-4" onClick={() => navigate('/management/companies')}>
              <ArrowLeft className="mr-2 h-4 w-4" />
              {t('clientDetail.backToList')}
            </Button>
          </CardContent>
        </Card>
      </PageLayout>
    );
  }

  const subtitle = company.tradeName && company.tradeName !== company.legalName ? company.tradeName : '';

  return (
    <>
      <PageLayout
        title={company.legalName}
        subtitle={subtitle}
        showDefaultActions={false}
      >
        <div className="space-y-5">
          {metrics && (
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
              <MetricCard
                label={t('clientDetail.metrics.activeContracts')}
                value={metrics.activeContracts}
                hint={t('clientDetail.metrics.totalContracts').replace('{0}', metrics.totalContracts.toString())}
                icon={<FileText className="h-4 w-4" />}
              />
              <MetricCard
                label={t('clientDetail.metrics.configuredTenants')}
                value={metrics.configuredTenants}
                hint={t('clientDetail.metrics.configuredTenantsHint')}
                icon={<Database className="h-4 w-4" />}
              />
              <MetricCard
                label={t('clientDetail.metrics.expiringSoon')}
                value={metrics.expiringSoon}
                hint={t('clientDetail.metrics.expiringSoonHint')}
                icon={<Activity className="h-4 w-4" />}
                tone={metrics.expiringSoon > 0 ? 'warning' : 'default'}
              />
              <MetricCard
                label={t('common.column.status')}
                value={
                  <Badge variant={company.isActive ? 'success' : 'destructive'}>
                    {company.isActive ? t('common.status.active') : t('common.status.inactive')}
                  </Badge>
                }
                hint={t('clientDetail.metrics.statusHint')}
                icon={<Info className="h-4 w-4" />}
              />
            </div>
          )}

          <Tabs value={activeTab} onValueChange={setActiveTab} className="pt-2">
            <TabsList variant="underline" className="mb-6">
              <TabsTrigger value="overview">
                <Info className="h-4 w-4" /> {t('clientDetail.tabs.overview')}
              </TabsTrigger>
              <TabsTrigger value="contracts">
                <FileText className="h-4 w-4" /> {t('clientDetail.tabs.contracts')}
                {contracts.length ? <TabsBadge>{contracts.length}</TabsBadge> : null}
              </TabsTrigger>
              <TabsTrigger value="people">
                <Users className="h-4 w-4" /> {t('clientDetail.tabs.people')}
              </TabsTrigger>
              <TabsTrigger value="subscription">
                <CreditCard className="h-4 w-4" /> Assinatura
              </TabsTrigger>
            </TabsList>

            <TabsContent value="overview" className="mt-0">
              <ClientOverviewTab company={company} onEdit={() => setIsEditModalOpen(true)} />
            </TabsContent>

            <TabsContent value="contracts" className="mt-0">
              <ClientContractsTab
                companyId={companyId}
                onCreateContract={() => setIsContractModalOpen(true)}
                refreshKey={refreshKey}
              />
            </TabsContent>

            <TabsContent value="people" className="mt-0">
              <ClientPeopleTab companyId={companyId} refreshKey={refreshKey} />
            </TabsContent>

            <TabsContent value="subscription" className="mt-0">
              <ClientSubscriptionTab companyId={companyId} refreshKey={refreshKey} />
            </TabsContent>
          </Tabs>
        </div>
      </PageLayout>

      <CompanyFormModal
        isOpen={isEditModalOpen}
        onClose={() => setIsEditModalOpen(false)}
        company={company}
        onSuccess={() => {
          setIsEditModalOpen(false);
          setRefreshKey((k) => k + 1);
        }}
      />

      <ContractFormModal
        isOpen={isContractModalOpen}
        onClose={() => setIsContractModalOpen(false)}
        contrato={undefined}
        onSuccess={() => {
          setIsContractModalOpen(false);
          setRefreshKey((k) => k + 1);
        }}
      />
    </>
  );
}

function MetricCard({
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
