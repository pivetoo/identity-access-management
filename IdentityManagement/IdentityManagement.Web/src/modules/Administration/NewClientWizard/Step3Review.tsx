import { Badge, Card, CardContent, useI18n } from 'archon-ui';
import { Building2, Database } from 'lucide-react';
import type { CompanyData, SystemSelection } from './index';
import { DatabaseProviderValue } from '../../../types/tenantDatabase';
import type { DatabaseProvider } from '../../../types/tenantDatabase';

interface Step3ReviewProps {
  company: CompanyData;
  systems: SystemSelection[];
}

const providerLabels: Record<DatabaseProvider, string> = {
  [DatabaseProviderValue.PostgreSql]: 'PostgreSQL',
  [DatabaseProviderValue.SqlServer]: 'SQL Server',
  [DatabaseProviderValue.MySql]: 'MySQL',
};

const maskSecret = (value: string): string => {
  if (!value) {
    return '';
  }
  if (value.length <= 8) {
    return '••••••••';
  }
  return `${value.slice(0, 4)}••••${value.slice(-4)}`;
};

export default function Step3Review({ company, systems }: Step3ReviewProps) {
  const { t } = useI18n();

  return (
    <div className="space-y-6">
      <div>
        <h3 className="text-base font-semibold text-foreground">{t('wizard.step3.title')}</h3>
        <p className="text-sm text-muted-foreground">{t('wizard.step3.description')}</p>
      </div>

      <Card>
        <CardContent className="p-4">
          <div className="flex items-center gap-3 border-b border-border/60 pb-3">
            <div className="rounded-md bg-primary/15 p-2 text-primary">
              <Building2 className="h-5 w-5" />
            </div>
            <div>
              <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                {t('wizard.step3.companySection')}
              </div>
              <div className="text-base font-semibold text-foreground">{company.legalName}</div>
            </div>
          </div>
          <dl className="mt-4 grid gap-x-6 gap-y-3 sm:grid-cols-2">
            <Field label={t('company.field.tradeName')} value={company.tradeName} />
            <Field
              label={t('company.field.document')}
              value={company.document.replace(/^(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})$/, '$1.$2.$3/$4-$5')}
              mono
            />
            <Field label={t('common.field.email')} value={company.email} />
            <Field
              label={t('common.field.phoneNumber')}
              value={company.phoneNumber
                ? company.phoneNumber.replace(/^(\d{2})(\d{5})(\d{4})$/, '($1) $2-$3')
                : '-'}
            />
          </dl>
        </CardContent>
      </Card>

      <div>
        <div className="mb-3 flex items-center justify-between">
          <h4 className="text-sm font-semibold text-foreground">
            {t('wizard.step3.systemsSection')}
          </h4>
          <Badge variant="secondary">{systems.length}</Badge>
        </div>

        <div className="space-y-3">
          {systems.map((sys) => (
            <Card key={sys.systemApplicationId}>
              <CardContent className="p-4">
                <div className="flex items-center gap-3 border-b border-border/60 pb-3">
                  <div className="rounded-md bg-primary/15 p-2 text-primary">
                    <Database className="h-5 w-5" />
                  </div>
                  <div className="flex-1">
                    <div className="text-base font-semibold text-foreground">
                      {sys.systemApplicationName}
                    </div>
                    <div className="text-xs font-mono text-muted-foreground">
                      {sys.systemApplicationAudience}
                    </div>
                  </div>
                  <Badge variant="primary">{providerLabels[sys.databaseProvider]}</Badge>
                </div>
                <dl className="mt-4 grid gap-x-6 gap-y-3 sm:grid-cols-2">
                  <Field label={t('common.field.startDate')} value={sys.startDate} />
                  <Field label={t('common.field.endDate')} value={sys.endDate ?? '-'} />
                  <Field label={t('tenantDatabase.field.schemaName')} value={sys.schemaName} />
                  <Field
                    label={t('tenantDatabase.field.integrationSecret')}
                    value={maskSecret(sys.integrationSecret)}
                    mono
                  />
                  <div className="sm:col-span-2">
                    <Field
                      label={t('tenantDatabase.field.connectionString')}
                      value={sys.connectionString}
                      mono
                    />
                  </div>
                </dl>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>

      <div className="rounded-lg border border-primary/30 bg-primary/5 p-4 text-sm text-foreground">
        <strong>{t('wizard.step3.confirmTitle')}</strong>
        <p className="mt-1 text-xs text-muted-foreground">{t('wizard.step3.confirmDescription')}</p>
      </div>
    </div>
  );
}

function Field({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div>
      <dt className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
        {label}
      </dt>
      <dd className={`mt-1 text-sm text-foreground ${mono ? 'font-mono break-all text-xs' : ''}`}>
        {value}
      </dd>
    </div>
  );
}
