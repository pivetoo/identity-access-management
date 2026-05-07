import { useEffect, useMemo, useState } from 'react';
import { Activity, BarChart3, LineChart as LineChartIcon, PieChart as PieChartIcon, ShieldCheck } from 'lucide-react';
import { AreaChart, BarChart, Card, CardContent, CardHeader, CardTitle, ChartContainer, GlobalLoader, LineChart, PieChart, useI18n } from 'archon-ui';
import dashboardService from '../../services/dashboardService';
import type { DashboardOverview } from '../../types/dashboard';

const chartColors = ['#6366f1', '#22c55e', '#f59e0b', '#ec4899', '#06b6d4', '#8b5cf6'];

function formatSystemLabel(value: string) {
  const parts = value.trim().split(/\s+/);
  return parts.length > 1 ? `${parts[0]}...` : value;
}

export default function Dashboard() {
  const { t } = useI18n();
  const [overview, setOverview] = useState<DashboardOverview | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;

    dashboardService
      .getOverview()
      .then((response) => {
        if (isMounted) {
          setOverview(response);
        }
      })
      .finally(() => {
        if (isMounted) {
          setIsLoading(false);
        }
      });

    return () => {
      isMounted = false;
    };
  }, []);

  const loginTrendData = useMemo(
    () => overview?.loginTrend.map((item) => ({ name: item.label, logins: item.logins, falhas: item.failures })) ?? [],
    [overview]
  );

  const contractHealthData = useMemo(
    () => overview
      ? [
          { name: 'Ativos', value: overview.contractHealth.active },
          { name: 'Expirando em breve', value: overview.contractHealth.expiringSoon },
          { name: 'Suspensos', value: overview.contractHealth.suspended },
          { name: 'Sem OAuth client', value: overview.contractHealth.withoutOAuthClient }
        ]
      : [],
    [overview]
  );

  const sessionFlowData = useMemo(
    () => overview?.sessionsByHour.map((item) => ({ name: item.label, sessões: item.sessions })) ?? [],
    [overview]
  );

  const systemsData = useMemo(
    () => overview?.topSystems.map((item) => ({ name: formatSystemLabel(item.name), acessos: item.accesses })) ?? [],
    [overview]
  );

  const securityPulseData = useMemo(
    () => overview
      ? [
          { name: 'MFA', value: overview.securityPulse.mfaCoverage },
          { name: 'Sessões válidas', value: overview.securityPulse.validSessions },
          { name: 'Tokens rotacionados', value: overview.securityPulse.rotatedTokens },
          { name: 'Acessos revisados', value: overview.securityPulse.reviewedAccesses }
        ]
      : [],
    [overview]
  );

  if (isLoading) {
    return <GlobalLoader isVisible={true} className="bg-background" />;
  }

  return (
    <div className="flex flex-col gap-5">
      <div className="border-l-4 border-primary pl-5">
        <h1 className="text-3xl font-bold text-foreground tracking-tight">
          <strong className="text-primary">{t('dashboard.title')}</strong>
        </h1>
        <p className="text-lg text-muted-foreground mt-3 leading-relaxed">
          {t('dashboard.subtitle')}
        </p>
      </div>

      <div className="grid gap-4 xl:grid-cols-[1.4fr_0.9fr]">
        <Card className="overflow-hidden border border-border/70 shadow-sm">
          <CardHeader className="border-b bg-muted/20 pb-4">
            <CardTitle className="flex items-center gap-2 text-base">
              <LineChartIcon className="h-5 w-5 text-primary" />
              Logins da semana
            </CardTitle>
          </CardHeader>
          <CardContent className="p-5">
            <ChartContainer title="Sucesso x falha" height={290}>
              <AreaChart
                data={loginTrendData}
                dataKeys={['logins', 'falhas']}
                colors={['#6366f1', '#ef4444']}
                height={230}
                showLegend={false}
                fillOpacity={0.18}
              />
            </ChartContainer>
          </CardContent>
        </Card>

        <Card className="overflow-hidden border border-border/70 shadow-sm">
          <CardHeader className="border-b bg-muted/20 pb-4">
            <CardTitle className="flex items-center gap-2 text-base">
              <PieChartIcon className="h-5 w-5 text-violet-600" />
              Saúde dos contratos
            </CardTitle>
          </CardHeader>
          <CardContent className="p-5">
            <ChartContainer title="Status operacional" height={290}>
              <PieChart
                data={contractHealthData}
                colors={['#22c55e', '#f59e0b', '#ef4444', '#6366f1']}
                height={230}
                innerRadius={62}
                showLabels={false}
              />
            </ChartContainer>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <Card className="border border-border/70 shadow-sm">
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2 text-sm font-semibold">
              <Activity className="h-4 w-4 text-sky-600" />
              Sessões por horário
            </CardTitle>
          </CardHeader>
          <CardContent className="px-4 pb-4">
            <LineChart
              data={sessionFlowData}
              dataKeys={['sessões']}
              colors={['#06b6d4']}
              height={170}
              showLegend={false}
              showDots={false}
              enableArea
              areaOpacity={0.14}
            />
          </CardContent>
        </Card>

        <Card className="border border-border/70 shadow-sm">
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2 text-sm font-semibold">
              <BarChart3 className="h-4 w-4 text-amber-600" />
              Sistemas mais acessados
            </CardTitle>
          </CardHeader>
          <CardContent className="px-4 pb-4">
            <BarChart
              data={systemsData}
              dataKeys={['acessos']}
              colors={['#f59e0b']}
              height={170}
              showLegend={false}
              showGrid={false}
              layout="horizontal"
            />
          </CardContent>
        </Card>

        <Card className="border border-border/70 shadow-sm">
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2 text-sm font-semibold">
              <ShieldCheck className="h-4 w-4 text-emerald-600" />
              Pulso de segurança
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-3 px-4 pb-4">
            {securityPulseData.map((item, index) => (
              <div key={item.name} className="space-y-1.5">
                <div className="flex items-center justify-between text-xs">
                  <span className="font-medium text-foreground">{item.name}</span>
                  <span className="font-semibold text-muted-foreground">{item.value}%</span>
                </div>
                <div className="h-2 overflow-hidden rounded-full bg-muted">
                  <div
                    className="h-full rounded-full"
                    style={{ width: `${item.value}%`, backgroundColor: chartColors[index % chartColors.length] }}
                  />
                </div>
              </div>
            ))}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
