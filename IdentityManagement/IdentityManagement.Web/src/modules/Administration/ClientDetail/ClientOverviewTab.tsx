import { Pencil } from 'lucide-react';
import { Badge, Button, Card, CardContent, CardHeader, CardTitle, useI18n } from 'archon-ui';
import type { Company } from '../../../types/company';

interface ClientOverviewTabProps {
  company: Company;
  onEdit: () => void;
}

export default function ClientOverviewTab({ company, onEdit }: ClientOverviewTabProps) {
  const { t } = useI18n();

  const document = company.document
    ? company.document.replace(/^(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})$/, '$1.$2.$3/$4-$5')
    : t('common.value.notAvailable');

  const phone = company.phoneNumber
    ? company.phoneNumber.replace(/^(\d{2})(\d{5})(\d{4})$/, '($1) $2-$3')
    : t('common.value.notAvailable');

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader className="flex flex-row items-start justify-between gap-4">
          <div>
            <CardTitle>{t('clientDetail.overview.registrationTitle')}</CardTitle>
            <p className="text-sm text-muted-foreground">
              {t('clientDetail.overview.registrationDescription')}
            </p>
          </div>
          <Button variant="outline" size="sm" onClick={onEdit}>
            <Pencil className="mr-2 h-4 w-4" />
            {t('common.action.edit')}
          </Button>
        </CardHeader>
        <CardContent>
          <dl className="grid gap-x-6 gap-y-4 sm:grid-cols-2">
            <Field label={t('company.field.legalName')} value={company.legalName} />
            <Field label={t('company.field.tradeName')} value={company.tradeName} />
            <Field label={t('company.field.document')} value={document} mono />
            <Field
              label={t('common.column.status')}
              value={
                <Badge variant={company.isActive ? 'success' : 'destructive'}>
                  {company.isActive ? t('common.status.active') : t('common.status.inactive')}
                </Badge>
              }
            />
          </dl>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('clientDetail.overview.contactTitle')}</CardTitle>
          <p className="text-sm text-muted-foreground">
            {t('clientDetail.overview.contactDescription')}
          </p>
        </CardHeader>
        <CardContent>
          <dl className="grid gap-x-6 gap-y-4 sm:grid-cols-2">
            <Field label={t('common.field.email')} value={company.email} />
            <Field label={t('common.field.phoneNumber')} value={phone} />
          </dl>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('clientDetail.overview.tenantTitle')}</CardTitle>
          <p className="text-sm text-muted-foreground">
            {t('clientDetail.overview.tenantDescription')}
          </p>
        </CardHeader>
        <CardContent>
          <dl className="grid gap-x-6 gap-y-4 sm:grid-cols-2">
            <Field label="TenantId" value={company.tenantId ?? '-'} mono />
          </dl>
        </CardContent>
      </Card>
    </div>
  );
}

function Field({
  label,
  value,
  mono,
}: {
  label: string;
  value: React.ReactNode;
  mono?: boolean;
}) {
  return (
    <div>
      <dt className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
        {label}
      </dt>
      <dd
        className={`mt-1 text-sm text-foreground ${mono ? 'font-mono break-all text-xs' : ''}`}
      >
        {value}
      </dd>
    </div>
  );
}
