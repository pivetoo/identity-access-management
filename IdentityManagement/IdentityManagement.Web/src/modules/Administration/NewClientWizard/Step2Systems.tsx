import { ChevronDown } from 'lucide-react';
import { Card, CardContent, Input, useI18n } from 'archon-ui';
import type { SystemApplication } from '../../../types/systemApplication';
import type { SystemSelection } from './index';

interface Step2SystemsProps {
  availableSystems: SystemApplication[];
  selectedSystems: SystemSelection[];
  onToggleSystem: (system: SystemApplication, checked: boolean) => void;
  onUpdateSystem: (systemApplicationId: number, patch: Partial<SystemSelection>) => void;
}

export default function Step2Systems({ availableSystems, selectedSystems, onToggleSystem, onUpdateSystem }: Step2SystemsProps) {
  const { t } = useI18n();

  const provisionableSystems = availableSystems.filter((s) => s.audience !== 'identity-management');

  const isSelected = (id: number) => selectedSystems.some((s) => s.systemApplicationId === id);
  const getSelection = (id: number) => selectedSystems.find((s) => s.systemApplicationId === id);

  return (
    <div className="space-y-5">
      <div>
        <h3 className="text-base font-semibold text-foreground">{t('wizard.step2.title')}</h3>
        <p className="text-sm text-muted-foreground">{t('wizard.step2.description')}</p>
      </div>

      {provisionableSystems.length === 0 ? (
        <Card>
          <CardContent className="py-8 text-center text-sm text-muted-foreground">
            {t('wizard.step2.noSystems')}
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {provisionableSystems.map((system) => {
            const selected = isSelected(system.id);
            const data = getSelection(system.id);
            return (
              <Card key={system.id}>
                <CardContent className="p-4">
                  <label className="flex cursor-pointer items-start gap-3">
                    <input
                      type="checkbox"
                      checked={selected}
                      onChange={(e) => onToggleSystem(system, e.target.checked)}
                      className="mt-1 h-4 w-4 cursor-pointer"
                    />
                    <div className="flex-1">
                      <div className="flex items-center justify-between gap-2">
                        <div className="font-medium text-foreground">{system.name}</div>
                        <span className="rounded bg-muted px-2 py-0.5 text-[10px] font-mono text-muted-foreground">
                          {system.audience}
                        </span>
                      </div>
                      {system.description && (
                        <p className="mt-1 text-xs text-muted-foreground">{system.description}</p>
                      )}
                    </div>
                    {selected && <ChevronDown className="mt-1 h-4 w-4 text-muted-foreground" />}
                  </label>

                  {selected && data && (
                    <div className="ml-7 mt-4 space-y-4 border-t border-border/60 pt-4">
                      <div className="grid gap-4 sm:grid-cols-2">
                        <div className="flex flex-col gap-1.5">
                          <label className="text-xs font-medium text-muted-foreground">
                            {t('common.field.startDate')}
                          </label>
                          <Input
                            type="date"
                            value={data.startDate}
                            onChange={(e) => onUpdateSystem(system.id, { startDate: e.target.value })}
                          />
                        </div>
                        <div className="flex flex-col gap-1.5">
                          <label className="text-xs font-medium text-muted-foreground">
                            {t('common.field.endDate')}
                          </label>
                          <Input
                            type="date"
                            value={data.endDate ?? ''}
                            onChange={(e) => onUpdateSystem(system.id, { endDate: e.target.value || undefined })}
                          />
                        </div>
                      </div>
                    </div>
                  )}
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
