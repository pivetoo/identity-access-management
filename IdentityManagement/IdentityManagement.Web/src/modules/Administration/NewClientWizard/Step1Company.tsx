import { Input, useI18n } from 'archon-ui';
import type { CompanyData } from './index';

interface Step1CompanyProps {
  data: CompanyData;
  onChange: (data: CompanyData) => void;
}

export default function Step1Company({ data, onChange }: Step1CompanyProps) {
  const { t } = useI18n();

  const update = (field: keyof CompanyData, value: string) => {
    onChange({ ...data, [field]: value });
  };

  return (
    <div className="space-y-5">
      <div>
        <h3 className="text-base font-semibold text-foreground">{t('wizard.step1.title')}</h3>
        <p className="text-sm text-muted-foreground">{t('wizard.step1.description')}</p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-2 sm:col-span-2">
          <label className="text-sm font-medium">
            {t('company.field.legalName')} <span className="text-destructive">*</span>
          </label>
          <Input
            value={data.legalName}
            onChange={(e) => update('legalName', e.target.value)}
            placeholder={t('wizard.step1.legalNamePlaceholder')}
          />
        </div>

        <div className="flex flex-col gap-2">
          <label className="text-sm font-medium">
            {t('company.field.tradeName')} <span className="text-destructive">*</span>
          </label>
          <Input
            value={data.tradeName}
            onChange={(e) => update('tradeName', e.target.value)}
            placeholder={t('wizard.step1.tradeNamePlaceholder')}
          />
        </div>

        <div className="flex flex-col gap-2">
          <label className="text-sm font-medium">
            {t('company.field.document')} <span className="text-destructive">*</span>
          </label>
          <Input
            value={data.document}
            onChange={(e) => update('document', e.target.value.replace(/\D/g, ''))}
            placeholder="00000000000000"
            maxLength={14}
          />
        </div>

        <div className="flex flex-col gap-2">
          <label className="text-sm font-medium">
            {t('common.field.email')} <span className="text-destructive">*</span>
          </label>
          <Input
            type="email"
            value={data.email}
            onChange={(e) => update('email', e.target.value)}
            placeholder="contato@empresa.com"
          />
        </div>

        <div className="flex flex-col gap-2">
          <label className="text-sm font-medium">{t('common.field.phoneNumber')}</label>
          <Input
            value={data.phoneNumber}
            onChange={(e) => update('phoneNumber', e.target.value.replace(/\D/g, ''))}
            placeholder="11999999999"
            maxLength={11}
          />
        </div>
      </div>
    </div>
  );
}
