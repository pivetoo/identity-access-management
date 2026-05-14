import { Pencil } from 'lucide-react';
import { Badge, Button, Card, CardContent, CardHeader, CardTitle, useI18n } from 'archon-ui';
import type { Contract } from '../../../types/contract';
import { formatDate } from '../../../utils/date';

interface ContractOverviewTabProps {
  contract: Contract;
  onEdit: () => void;
}

export default function ContractOverviewTab({ contract, onEdit }: ContractOverviewTabProps) {
  const { t } = useI18n();

  const isExpired = contract.endDate && new Date(contract.endDate) < new Date();

  return (
    <Card>
      <CardHeader className="flex flex-row items-start justify-between gap-4">
        <div>
          <CardTitle>{t('contractDetail.overview.title')}</CardTitle>
          <p className="text-sm text-muted-foreground">
            {t('contractDetail.overview.description')}
          </p>
        </div>
        <Button variant="outline" size="sm" onClick={onEdit}>
          <Pencil className="mr-2 h-4 w-4" />
          {t('common.action.edit')}
        </Button>
      </CardHeader>
      <CardContent>
        <dl className="grid gap-x-6 gap-y-4 sm:grid-cols-2">
          <Field label={t('contract.field.company')} value={contract.companyName} />
          <Field label={t('contract.field.systemApplication')} value={contract.systemApplicationName} />
          <Field label={t('common.field.startDate')} value={formatDate(contract.startDate)} />
          <Field label={t('common.field.endDate')} value={formatDate(contract.endDate)} />
          <Field
            label={t('common.column.status')}
            value={
              <div className="flex items-center gap-2">
                <Badge variant={contract.isActive ? 'success' : 'destructive'}>
                  {contract.isActive ? t('common.status.active') : t('common.status.inactive')}
                </Badge>
                {isExpired && (
                  <Badge variant="destructive">{t('clientDetail.contracts.expired')}</Badge>
                )}
              </div>
            }
          />
          <Field label="TenantId" value={contract.tenantId ?? '-'} mono />
        </dl>
      </CardContent>
    </Card>
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
