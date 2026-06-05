import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Info, Plug } from 'lucide-react';
import { Badge, Button, Card, CardContent, PageLayout, Tabs, TabsBadge, TabsContent, TabsList, TabsTrigger, useApi, useI18n } from 'archon-ui';
import { SystemApplicationService } from '../../../services/systemApplicationService';
import { SystemIntegrationService } from '../../../services/systemIntegrationService';
import type { SystemApplication } from '../../../types/systemApplication';
import SystemApplicationFormModal from '../../../components/modals/SystemApplicationFormModal';
import SystemApplicationIntegrationsTab from './SystemApplicationIntegrationsTab';

export default function SystemApplicationDetail() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const systemApplicationId = Number(id);

  const [sistema, setSistema] = useState<SystemApplication | null>(null);
  const [integrationCount, setIntegrationCount] = useState(0);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const [activeTab, setActiveTab] = useState('overview');

  const loadApi = useApi({
    onSuccess: (data: { sistema: SystemApplication; integrationCount: number }) => {
      setSistema(data.sistema);
      setIntegrationCount(data.integrationCount);
    },
  });

  useEffect(() => {
    if (!Number.isFinite(systemApplicationId) || systemApplicationId <= 0) {
      return;
    }
    loadApi.execute(async () => {
      const [sistemaData, integrations] = await Promise.all([
        SystemApplicationService.getById(systemApplicationId),
        SystemIntegrationService.getBySystem(systemApplicationId),
      ]);
      return { sistema: sistemaData, integrationCount: integrations.length };
    });
  }, [systemApplicationId, refreshKey]);

  const handleRefresh = () => setRefreshKey((k) => k + 1);

  if (loadApi.isLoading && !sistema) {
    return (
      <PageLayout title="..." subtitle="">
        <div className="h-64 animate-pulse rounded-lg border border-border bg-muted/30" />
      </PageLayout>
    );
  }

  if (!sistema) {
    return (
      <PageLayout title="Sistema nao encontrado" subtitle="">
        <Card>
          <CardContent className="py-10 text-center">
            <p className="text-sm text-muted-foreground">O sistema solicitado nao foi encontrado.</p>
            <Button variant="primary" className="mt-4" onClick={() => navigate('/management/system-applications')}>
              <ArrowLeft className="mr-2 h-4 w-4" />
              Voltar para sistemas
            </Button>
          </CardContent>
        </Card>
      </PageLayout>
    );
  }

  return (
    <>
      <PageLayout
        title={sistema.name}
        subtitle={sistema.audience || 'Sistema de aplicacao'}
        showDefaultActions={false}
        actions={[
          {
            key: 'back',
            label: 'Voltar',
            onClick: () => navigate('/management/system-applications'),
            variant: 'outline',
          },
          {
            key: 'edit',
            label: t('common.action.edit'),
            onClick: () => setIsEditModalOpen(true),
            variant: 'primary',
            primary: true,
          },
        ]}
      >
        <div className="space-y-5">
          <div className="grid gap-3 sm:grid-cols-3">
            <Card>
              <CardContent className="space-y-1 p-4">
                <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Status</div>
                <div>
                  <Badge variant={sistema.isActive ? 'success' : 'destructive'}>
                    {sistema.isActive ? t('common.status.active') : t('common.status.inactive')}
                  </Badge>
                </div>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="space-y-1 p-4">
                <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Audience</div>
                <div className="text-sm font-medium font-mono text-foreground">{sistema.audience || '-'}</div>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="space-y-1 p-4">
                <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">URL base</div>
                <div className="truncate text-sm font-medium text-foreground">{sistema.baseUrl || '-'}</div>
              </CardContent>
            </Card>
          </div>

          <Tabs value={activeTab} onValueChange={setActiveTab} className="pt-2">
            <TabsList variant="underline" className="mb-6">
              <TabsTrigger value="overview">
                <Info className="h-4 w-4" />
                Visao geral
              </TabsTrigger>
              <TabsTrigger value="integrations">
                <Plug className="h-4 w-4" />
                Integracoes
                {integrationCount > 0 && <TabsBadge>{integrationCount}</TabsBadge>}
              </TabsTrigger>
            </TabsList>

            <TabsContent value="overview" className="mt-0">
              <Card>
                <CardContent className="space-y-4 p-6">
                  <div className="grid gap-4 sm:grid-cols-2">
                    <div>
                      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Nome</div>
                      <div className="mt-1 text-sm text-foreground">{sistema.name}</div>
                    </div>
                    <div>
                      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{t('systemApplication.field.audience')}</div>
                      <div className="mt-1 font-mono text-sm text-foreground">{sistema.audience || '-'}</div>
                    </div>
                    <div>
                      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{t('systemApplication.field.baseUrl')}</div>
                      <div className="mt-1 text-sm text-foreground break-all">{sistema.baseUrl || '-'}</div>
                    </div>
                    <div>
                      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{t('common.field.description')}</div>
                      <div className="mt-1 text-sm text-foreground">{sistema.description || '-'}</div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="integrations" className="mt-0">
              <SystemApplicationIntegrationsTab
                systemApplicationId={systemApplicationId}
                refreshKey={refreshKey}
                onRefresh={handleRefresh}
              />
            </TabsContent>
          </Tabs>
        </div>
      </PageLayout>

      <SystemApplicationFormModal
        isOpen={isEditModalOpen}
        onClose={() => setIsEditModalOpen(false)}
        sistema={sistema}
        onSuccess={() => {
          setIsEditModalOpen(false);
          handleRefresh();
        }}
      />
    </>
  );
}
