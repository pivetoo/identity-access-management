import { useEffect, useState } from 'react';
import { Eye, EyeOff, Pencil, Plus, Database, AlertTriangle, Copy, Check } from 'lucide-react';
import { Badge, Button, Card, CardContent, CardHeader, CardTitle, toast, useApi, useI18n } from 'archon-ui';
import { TenantDatabaseService } from '../../../services/tenantDatabaseService';
import type { TenantDatabase, DatabaseProvider } from '../../../types/tenantDatabase';
import { DatabaseProviderValue } from '../../../types/tenantDatabase';
import TenantDatabaseFormModal from '../../../components/modals/TenantDatabaseFormModal';

interface ContractTenantTabProps {
  contractId: number;
  refreshKey: number;
  onRefresh: () => void;
}

const providerLabels: Record<DatabaseProvider, string> = {
  [DatabaseProviderValue.PostgreSql]: 'PostgreSQL',
  [DatabaseProviderValue.SqlServer]: 'SQL Server',
  [DatabaseProviderValue.MySql]: 'MySQL',
};

export default function ContractTenantTab({ contractId, refreshKey, onRefresh }: ContractTenantTabProps) {
  const { t } = useI18n();
  const [tenant, setTenant] = useState<TenantDatabase | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [showSecret, setShowSecret] = useState(false);
  const [showConn, setShowConn] = useState(false);
  const [copiedField, setCopiedField] = useState<string | null>(null);

  const loadApi = useApi({
    onSuccess: (data: TenantDatabase | null) => setTenant(data),
  });

  useEffect(() => {
    loadApi.execute(async () => {
      const all = await TenantDatabaseService.getActive();
      return all.find((item) => item.contractId === contractId) ?? null;
    });
  }, [contractId, refreshKey]);

  const handleCopy = async (field: string, value: string) => {
    try {
      await navigator.clipboard.writeText(value);
      setCopiedField(field);
      toast({ variant: 'success', title: t('common.toast.successTitle'), description: t('contractDetail.tenant.copied') });
      setTimeout(() => setCopiedField(null), 2000);
    } catch {
      toast({ variant: 'destructive', title: t('common.toast.errorTitle'), description: t('contractDetail.tenant.copyError') });
    }
  };

  if (loadApi.isLoading && !tenant) {
    return <div className="h-32 animate-pulse rounded-lg border border-border bg-muted/30" />;
  }

  if (!tenant) {
    return (
      <>
        <Card>
          <CardContent className="flex flex-col items-center justify-center gap-3 py-12 text-center">
            <div className="rounded-full bg-warning/15 p-3">
              <AlertTriangle className="h-6 w-6 text-warning" />
            </div>
            <h3 className="text-sm font-semibold text-foreground">
              {t('contractDetail.tenant.empty.title')}
            </h3>
            <p className="max-w-md text-xs text-muted-foreground">
              {t('contractDetail.tenant.empty.description')}
            </p>
            <Button variant="primary" size="sm" onClick={() => setIsModalOpen(true)}>
              <Plus className="mr-2 h-4 w-4" />
              {t('contractDetail.tenant.configure')}
            </Button>
          </CardContent>
        </Card>

        <TenantDatabaseFormModal
          isOpen={isModalOpen}
          onClose={() => setIsModalOpen(false)}
          tenant={undefined}
          existingContractIds={[]}
          onSuccess={() => {
            setIsModalOpen(false);
            onRefresh();
          }}
        />
      </>
    );
  }

  return (
    <>
      <Card>
        <CardHeader className="flex flex-row items-start justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="rounded-md bg-primary/15 p-2 text-primary">
              <Database className="h-5 w-5" />
            </div>
            <div>
              <CardTitle>{t('contractDetail.tenant.title')}</CardTitle>
              <p className="text-sm text-muted-foreground">
                {t('contractDetail.tenant.subtitle')}
              </p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <Badge variant={tenant.isActive ? 'success' : 'destructive'}>
              {tenant.isActive ? t('common.status.active') : t('common.status.inactive')}
            </Badge>
            <Button variant="outline" size="sm" onClick={() => setIsModalOpen(true)}>
              <Pencil className="mr-2 h-4 w-4" />
              {t('common.action.edit')}
            </Button>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-x-6 gap-y-4 sm:grid-cols-2">
            <div>
              <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                {t('tenantDatabase.field.databaseProvider')}
              </div>
              <div className="mt-1 text-sm font-medium text-foreground">
                {providerLabels[tenant.databaseProvider] ?? '-'}
              </div>
            </div>
            <div>
              <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                {t('tenantDatabase.field.schemaName')}
              </div>
              <div className="mt-1 text-sm font-medium text-foreground">{tenant.schemaName}</div>
            </div>
          </div>

          <div>
            <div className="flex items-center justify-between">
              <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                {t('tenantDatabase.field.connectionString')}
              </div>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={() => setShowConn((prev) => !prev)}
                  className="text-xs text-muted-foreground hover:text-foreground"
                >
                  {showConn ? <EyeOff className="h-3.5 w-3.5" /> : <Eye className="h-3.5 w-3.5" />}
                </button>
                <button
                  type="button"
                  onClick={() => handleCopy('conn', tenant.connectionString)}
                  className="text-xs text-muted-foreground hover:text-foreground"
                >
                  {copiedField === 'conn' ? (
                    <Check className="h-3.5 w-3.5 text-success" />
                  ) : (
                    <Copy className="h-3.5 w-3.5" />
                  )}
                </button>
              </div>
            </div>
            <div className="mt-1 rounded-md border border-border/60 bg-muted/30 p-2 font-mono text-xs text-muted-foreground break-all">
              {showConn
                ? tenant.connectionString
                : '•'.repeat(Math.min(tenant.connectionString.length, 32))}
            </div>
          </div>

          <div>
            <div className="flex items-center justify-between">
              <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                {t('tenantDatabase.field.apiKey')}
              </div>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={() => setShowSecret((prev) => !prev)}
                  className="text-xs text-muted-foreground hover:text-foreground"
                >
                  {showSecret ? <EyeOff className="h-3.5 w-3.5" /> : <Eye className="h-3.5 w-3.5" />}
                </button>
                <button
                  type="button"
                  onClick={() => handleCopy('secret', tenant.apiKey)}
                  className="text-xs text-muted-foreground hover:text-foreground"
                >
                  {copiedField === 'secret' ? (
                    <Check className="h-3.5 w-3.5 text-success" />
                  ) : (
                    <Copy className="h-3.5 w-3.5" />
                  )}
                </button>
              </div>
            </div>
            <div className="mt-1 rounded-md border border-border/60 bg-muted/30 p-2 font-mono text-xs text-muted-foreground break-all">
              {showSecret
                ? tenant.apiKey
                : '•'.repeat(Math.min(tenant.apiKey.length, 32))}
            </div>
          </div>
        </CardContent>
      </Card>

      <TenantDatabaseFormModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        tenant={tenant}
        existingContractIds={[]}
        onSuccess={() => {
          setIsModalOpen(false);
          onRefresh();
        }}
      />
    </>
  );
}
