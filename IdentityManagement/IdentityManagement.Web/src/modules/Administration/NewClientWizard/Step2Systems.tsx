import { useState } from 'react';
import { ChevronDown, Eye, EyeOff } from 'lucide-react';
import { Card, CardContent, Input, SearchableSelect, useI18n } from 'archon-ui';
import type { SystemApplication } from '../../../types/systemApplication';
import type { SystemSelection } from './index';
import { DatabaseProviderValue } from '../../../types/tenantDatabase';

interface Step2SystemsProps {
  availableSystems: SystemApplication[];
  selectedSystems: SystemSelection[];
  onToggleSystem: (system: SystemApplication, checked: boolean) => void;
  onUpdateSystem: (systemApplicationId: number, patch: Partial<SystemSelection>) => void;
}

export default function Step2Systems({
  availableSystems,
  selectedSystems,
  onToggleSystem,
  onUpdateSystem,
}: Step2SystemsProps) {
  const { t } = useI18n();
  const [showSecretIds, setShowSecretIds] = useState<Set<number>>(new Set());

  const isSelected = (id: number) => selectedSystems.some((s) => s.systemApplicationId === id);
  const getSelection = (id: number) =>
    selectedSystems.find((s) => s.systemApplicationId === id);

  const toggleShowSecret = (id: number) => {
    setShowSecretIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };

  const providerOptions = [
    { label: 'PostgreSQL', value: DatabaseProviderValue.PostgreSql.toString() },
    { label: 'SQL Server', value: DatabaseProviderValue.SqlServer.toString() },
    { label: 'MySQL', value: DatabaseProviderValue.MySql.toString() },
  ];

  return (
    <div className="space-y-5">
      <div>
        <h3 className="text-base font-semibold text-foreground">{t('wizard.step2.title')}</h3>
        <p className="text-sm text-muted-foreground">{t('wizard.step2.description')}</p>
      </div>

      {availableSystems.length === 0 ? (
        <Card>
          <CardContent className="py-8 text-center text-sm text-muted-foreground">
            {t('wizard.step2.noSystems')}
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {availableSystems.map((system) => {
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
                    {selected && (
                      <ChevronDown className="mt-1 h-4 w-4 text-muted-foreground" />
                    )}
                  </label>

                  {selected && data && (
                    <div className="ml-7 mt-4 space-y-4 border-t border-border/60 pt-4">
                      <div className="grid gap-4 sm:grid-cols-3">
                        <div className="flex flex-col gap-1.5">
                          <label className="text-xs font-medium text-muted-foreground">
                            {t('common.field.startDate')}
                          </label>
                          <Input
                            type="date"
                            value={data.startDate}
                            onChange={(e) =>
                              onUpdateSystem(system.id, { startDate: e.target.value })
                            }
                          />
                        </div>
                        <div className="flex flex-col gap-1.5">
                          <label className="text-xs font-medium text-muted-foreground">
                            {t('common.field.endDate')}
                          </label>
                          <Input
                            type="date"
                            value={data.endDate ?? ''}
                            onChange={(e) =>
                              onUpdateSystem(system.id, { endDate: e.target.value || undefined })
                            }
                          />
                        </div>
                        <div className="flex flex-col gap-1.5">
                          <label className="text-xs font-medium text-muted-foreground">
                            {t('tenantDatabase.field.databaseProvider')}
                          </label>
                          <SearchableSelect
                            options={providerOptions}
                            value={data.databaseProvider.toString()}
                            onValueChange={(value) =>
                              onUpdateSystem(system.id, {
                                databaseProvider: Number(value) as SystemSelection['databaseProvider'],
                              })
                            }
                          />
                        </div>
                      </div>

                      <div className="grid gap-4 sm:grid-cols-2">
                        <div className="flex flex-col gap-1.5">
                          <label className="text-xs font-medium text-muted-foreground">
                            {t('tenantDatabase.field.schemaName')}
                          </label>
                          <Input
                            value={data.schemaName}
                            onChange={(e) =>
                              onUpdateSystem(system.id, { schemaName: e.target.value })
                            }
                            placeholder="public"
                          />
                        </div>
                        <div className="flex flex-col gap-1.5">
                          <label className="text-xs font-medium text-muted-foreground">
                            {t('tenantDatabase.field.integrationSecret')}{' '}
                            <span className="text-destructive">*</span>
                          </label>
                          <div className="relative">
                            <Input
                              type={showSecretIds.has(system.id) ? 'text' : 'password'}
                              value={data.integrationSecret}
                              onChange={(e) =>
                                onUpdateSystem(system.id, { integrationSecret: e.target.value })
                              }
                              className="pr-9"
                            />
                            <button
                              type="button"
                              onClick={() => toggleShowSecret(system.id)}
                              className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                            >
                              {showSecretIds.has(system.id) ? (
                                <EyeOff className="h-3.5 w-3.5" />
                              ) : (
                                <Eye className="h-3.5 w-3.5" />
                              )}
                            </button>
                          </div>
                        </div>
                      </div>

                      <div className="flex flex-col gap-1.5">
                        <label className="text-xs font-medium text-muted-foreground">
                          {t('tenantDatabase.field.connectionString')}{' '}
                          <span className="text-destructive">*</span>
                        </label>
                        <Input
                          value={data.connectionString}
                          onChange={(e) =>
                            onUpdateSystem(system.id, { connectionString: e.target.value })
                          }
                          placeholder="Host=...;Port=5432;Database=...;Username=...;Password=...;"
                        />
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
