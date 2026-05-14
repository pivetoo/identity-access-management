import { Card, CardContent, useI18n } from 'archon-ui';
import { Activity } from 'lucide-react';

export default function ClientAuditTab() {
  const { t } = useI18n();

  return (
    <Card>
      <CardContent className="flex flex-col items-center justify-center gap-3 py-12 text-center">
        <div className="rounded-full bg-primary/10 p-3">
          <Activity className="h-6 w-6 text-primary" />
        </div>
        <h3 className="text-sm font-semibold text-foreground">
          {t('clientDetail.audit.empty.title')}
        </h3>
        <p className="max-w-md text-xs text-muted-foreground">
          {t('clientDetail.audit.empty.description')}
        </p>
      </CardContent>
    </Card>
  );
}
