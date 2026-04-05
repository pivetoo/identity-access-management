import { useEffect, useState } from 'react';
import { Users, FileText, MapPin, Layers, Clock, ExternalLink, Search } from 'lucide-react';
import { Button, Input, Modal, ModalContent, ModalHeader, ModalTitle, ConfirmModal, toast, Card, CardContent, CardHeader, CardTitle, ChartContainer, PieChart } from 'archon-ui';
import dashboardService from '../../services/dashboardService';
import type { KPIs, TopSistema, ActiveSession } from '../../types/dashboard';
import { formatDateTime } from '../../utils/date';

export default function Dashboard() {
  const [kpis, setKpis] = useState<KPIs | null>(null);
  const [topSistemas, setTopSistemas] = useState<TopSistema[]>([]);
  const [activeSessions, setActiveSessions] = useState<ActiveSession[]>([]);
  const [sessionsPage, setSessionsPage] = useState(1);
  const [sessionsTotalPages, setSessionsTotalPages] = useState(1);
  const [totalSessions, setTotalSessions] = useState(0);
  const [isSessionsModalOpen, setIsSessionsModalOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [isConfirmModalOpen, setIsConfirmModalOpen] = useState(false);
  const [sessionToRevoke, setSessionToRevoke] = useState<string | null>(null);
  const [isConfirmRevokeAllOpen, setIsConfirmRevokeAllOpen] = useState(false);

  const loadDashboardData = async () => {
    const [kpisData, sistemasData, sessionsData] = await Promise.all([
      dashboardService.getKPIs(),
      dashboardService.getTopSistemas(8),
      dashboardService.getActiveSessions(1, 5)
    ]);

    setKpis(kpisData);
    setTopSistemas(sistemasData);
    setActiveSessions(sessionsData.items);
    setTotalSessions(sessionsData.totalCount);
  };

  const loadSessionsForModal = async () => {
    const sessionsData = await dashboardService.getActiveSessions(sessionsPage, 3);
    setActiveSessions(sessionsData.items);
    setSessionsTotalPages(sessionsData.totalPages);
    setTotalSessions(sessionsData.totalCount);
  };

  useEffect(() => {
    loadDashboardData();
  }, []);

  useEffect(() => {
    if (isSessionsModalOpen) {
      setSessionsPage(1);
      setSearchTerm('');
      loadSessionsForModal();
    }
  }, [isSessionsModalOpen]);

  useEffect(() => {
    if (isSessionsModalOpen) {
      loadSessionsForModal();
    }
  }, [sessionsPage]);

  const handleRevokeSession = (sessionId: string) => {
    setSessionToRevoke(sessionId);
    setIsConfirmModalOpen(true);
  };

  const confirmRevokeSession = async () => {
    if (!sessionToRevoke) return;

    await dashboardService.revokeSession(sessionToRevoke);
    toast({ variant: 'success', title: 'Sucesso', description: 'Sessão revogada com sucesso' });
    isSessionsModalOpen ? loadSessionsForModal() : loadDashboardData();
    setIsConfirmModalOpen(false);
    setSessionToRevoke(null);
  };

  const confirmRevokeAllSessions = async () => {
    await dashboardService.revokeAllSessions();
    toast({ variant: 'success', title: 'Sucesso', description: 'Todas as sessões foram revogadas com sucesso' });
    loadSessionsForModal();
    setIsConfirmRevokeAllOpen(false);
  };

  const filterSessions = (session: ActiveSession) => {
    if (!searchTerm) return true;
    const search = searchTerm.toLowerCase();
    return (
      session.userName.toLowerCase().includes(search) ||
      session.userEmail.toLowerCase().includes(search) ||
      session.companyName.toLowerCase().includes(search) ||
      session.systemApplicationName.toLowerCase().includes(search)
    );
  };

  const kpiData = [
    { icon: Users, color: 'text-primary', bgColor: 'bg-primary/10', value: kpis?.activeUsers || 0, label: 'Usuários Ativos' },
    { icon: FileText, color: 'text-secondary', bgColor: 'bg-secondary/10', value: kpis?.activeContratos || 0, label: 'Contratos Ativos' },
    { icon: MapPin, color: 'text-purple-600', bgColor: 'bg-purple-600/10', value: kpis?.empresas || 0, label: 'Empresas' },
    { icon: Layers, color: 'text-warning', bgColor: 'bg-warning/10', value: kpis?.sistemas || 0, label: 'Sistemas' }
  ];

  return (
    <div className="flex flex-col gap-6">
      <div className="border-l-4 border-primary pl-5">
        <h1 className="text-3xl font-bold text-foreground tracking-tight">
          <strong className="text-primary">Identity Access Management</strong>
        </h1>
        <p className="text-lg text-muted-foreground mt-3 leading-relaxed">
          Sistema de gerenciamento de identidade e acesso com autenticação, autorização e suporte a aplicações multi-tenant.
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {kpiData.map((kpi, index) => {
          const Icon = kpi.icon;
          return (
            <Card key={index} className="border border-border hover:shadow-md hover:-translate-y-0.5 transition-all">
              <CardContent className="p-6 flex items-center gap-4">
                <div className={`w-14 h-14 rounded-md ${kpi.bgColor} flex items-center justify-center ${kpi.color} flex-shrink-0`}>
                  <Icon size={24} />
                </div>
                <div className="flex flex-col">
                  <div className="text-3xl font-bold text-foreground leading-tight">{kpi.value}</div>
                  <div className="text-sm text-muted-foreground mt-1">{kpi.label}</div>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
        <ChartContainer
          title="Sistemas Mais Usados"

          isEmpty={topSistemas.length === 0}
          emptyMessage="Nenhum dado disponivel"
        >
          <PieChart
            data={topSistemas.map(sistema => ({
              name: sistema.name,
              value: sistema.logins
            }))}
            dataKey="value"
            labelFormatter={(entry: any) => `${entry.value}`}
          />
        </ChartContainer>

        <Card className="border border-border">
          <CardHeader className="pb-4 border-b">
            <CardTitle className="flex items-center gap-2">
              <Clock size={20} />
              Sessoes Ativas
            </CardTitle>
          </CardHeader>
          <CardContent className="p-6">
            <div className="flex flex-col items-center justify-center min-h-[400px]">
              <div className="text-7xl font-bold text-secondary leading-none">{totalSessions}</div>
              <div className="text-lg text-muted-foreground mt-4 text-center">
                Sessoes Ativas no SystemApplication
              </div>
              <Button
                variant="secondary"
                onClick={() => setIsSessionsModalOpen(true)}
                className="mt-5"
              >
                Ver Todas as Sessoes
                <ExternalLink size={18} className="ml-2" />
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>

      <Modal open={isSessionsModalOpen} onOpenChange={setIsSessionsModalOpen}>
        <ModalContent size="lg">
          <ModalHeader>
            <ModalTitle>Sessoes Ativas</ModalTitle>
          </ModalHeader>
          <div className="flex flex-col gap-4">
          <div className="flex gap-4">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Buscar por nome, email, empresa ou sistema..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10"
              />
            </div>
            <Button
              variant="primary"
              onClick={() => setIsConfirmRevokeAllOpen(true)}
              disabled={totalSessions === 0}
            >
              Revogar Todas
            </Button>
          </div>

          <div className="flex flex-col gap-2 pt-2">
            {activeSessions.filter(filterSessions).length > 0 ? (
              activeSessions.filter(filterSessions).map((session) => (
                <div
                  key={session.sessionId}
                  className="flex justify-between items-center p-4 bg-muted rounded-md hover:bg-muted/80 transition-colors"
                >
                  <div className="flex flex-col gap-1">
                    <div className="text-base font-medium text-foreground">{session.userName}</div>
                    <div className="text-sm text-muted-foreground">
                      {session.userEmail} - {session.companyName} - {session.systemApplicationName}
                    </div>
                    <div className="text-xs text-muted-foreground">
                      IP: {session.ipAddress} - Criado: {formatDateTime(session.createdAt)} - Expira: {formatDateTime(session.expiresAt)}
                    </div>
                  </div>
                  <Button
                    variant="danger"
                    size="sm"
                    onClick={() => handleRevokeSession(session.sessionId)}
                    disabled={sessionToRevoke === session.sessionId}
                  >
                    Revogar
                  </Button>
                </div>
              ))
            ) : (
              <div className="p-8 text-center text-muted-foreground text-base">
                Nenhuma sessao ativa
              </div>
            )}
          </div>

          {sessionsTotalPages > 1 && (
            <div className="flex items-center justify-center gap-4 pt-4 mt-4 border-t">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setSessionsPage(p => Math.max(1, p - 1))}
                disabled={sessionsPage === 1}
              >
                Anterior
              </Button>
              <span className="text-sm text-muted-foreground">
                Pagina {sessionsPage} de {sessionsTotalPages}
              </span>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setSessionsPage(p => Math.min(sessionsTotalPages, p + 1))}
                disabled={sessionsPage === sessionsTotalPages}
              >
                Proxima
              </Button>
            </div>
          )}
          </div>
        </ModalContent>
      </Modal>

      <ConfirmModal
        open={isConfirmModalOpen}
        onOpenChange={(open) => {
          setIsConfirmModalOpen(open);
          if (!open) setSessionToRevoke(null);
        }}
        onConfirm={confirmRevokeSession}
        title="Revogar Sessão"
        description="Tem certeza que deseja revogar esta sessao? Esta acao nao pode ser desfeita."
        confirmText="Revogar"
        cancelText="Cancelar"
        variant="danger"
      />

      <ConfirmModal
        open={isConfirmRevokeAllOpen}
        onOpenChange={setIsConfirmRevokeAllOpen}
        onConfirm={confirmRevokeAllSessions}
        title="Revogar Todas as Sessoes"
        description="Tem certeza que deseja revogar TODAS as sessões ativas do sistema? Esta acao nao pode ser desfeita e todos os usuarios serao desconectados."
        confirmText="Revogar Todas"
        cancelText="Cancelar"
        variant="danger"
      />
    </div>
  );
}
