import { Badge, Card, CardContent, useI18n } from 'archon-ui';
import { Building2, CheckCircle2, Database } from 'lucide-react';
import type { CompanyData, SystemSelection } from './index';
import type { OnboardClientResponse } from '../../../services/clientService';

interface Step3ReviewProps {
  company: CompanyData;
  systems: SystemSelection[];
  result?: OnboardClientResponse;
}

export default function Step3Review({ company, systems, result }: Step3ReviewProps) {
  const { t } = useI18n();

  if (result) {
    return (
      <div className="space-y-6">
        <div className="flex flex-col items-center gap-3 py-4 text-center">
          <div className="rounded-full bg-success/15 p-4 text-success">
            <CheckCircle2 className="h-10 w-10" />
          </div>
          <div>
            <h3 className="text-lg font-semibold text-foreground">Provisionamento concluido</h3>
            <p className="mt-1 text-sm text-muted-foreground">
              O tenant foi provisionado com sucesso. Um convite de configuracao foi enviado para{' '}
              <span className="font-medium text-foreground">{company.email}</span>.
            </p>
          </div>
        </div>

        <Card>
          <CardContent className="p-4">
            <div className="mb-3 flex items-center gap-2">
              <Database className="h-4 w-4 text-primary" />
              <span className="text-sm font-semibold text-foreground">Bancos de dados provisionados</span>
              <Badge variant="secondary">{result.databaseNames.length}</Badge>
            </div>
            <div className="space-y-1.5">
              {result.databaseNames.map((name) => (
                <div key={name} className="rounded bg-muted px-3 py-1.5 font-mono text-xs text-foreground">
                  {name}
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        <div className="rounded-lg border border-success/30 bg-success/5 p-4 text-sm text-foreground">
          <strong>Proximo passo:</strong>
          <p className="mt-1 text-xs text-muted-foreground">
            O administrador receberao um link de configuracao no e-mail{' '}
            <span className="font-medium">{company.email}</span> para concluir a configuracao inicial
            do tenant.
          </p>
        </div>
      </div>
    );
  }

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
              value={company.phoneNumber ? company.phoneNumber.replace(/^(\d{2})(\d{5})(\d{4})$/, '($1) $2-$3') : '-'}
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
                </div>
                <dl className="mt-4 grid gap-x-6 gap-y-3 sm:grid-cols-2">
                  <Field label={t('common.field.startDate')} value={sys.startDate} />
                  <Field label={t('common.field.endDate')} value={sys.endDate ?? '-'} />
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
