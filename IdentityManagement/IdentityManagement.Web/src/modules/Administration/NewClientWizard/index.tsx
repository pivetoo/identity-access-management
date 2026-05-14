import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, ArrowRight, Building2, Check, Database, ClipboardCheck } from 'lucide-react';
import { Button, Card, CardContent, PageLayout, toast, useApi, useI18n } from 'archon-ui';
import { CompanyService } from '../../../services/companyService';
import { ContractService } from '../../../services/contractService';
import { SystemApplicationService } from '../../../services/systemApplicationService';
import { TenantDatabaseService } from '../../../services/tenantDatabaseService';
import type { SystemApplication } from '../../../types/systemApplication';
import type { DatabaseProvider } from '../../../types/tenantDatabase';
import { DatabaseProviderValue } from '../../../types/tenantDatabase';
import Step1Company from './Step1Company';
import Step2Systems from './Step2Systems';
import Step3Review from './Step3Review';

export interface CompanyData {
  legalName: string;
  tradeName: string;
  document: string;
  email: string;
  phoneNumber: string;
}

export interface SystemSelection {
  systemApplicationId: number;
  systemApplicationName: string;
  systemApplicationAudience: string;
  startDate: string;
  endDate?: string;
  connectionString: string;
  databaseProvider: DatabaseProvider;
  schemaName: string;
  integrationSecret: string;
}

const initialCompany: CompanyData = {
  legalName: '',
  tradeName: '',
  document: '',
  email: '',
  phoneNumber: '',
};

export default function NewClientWizard() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [currentStep, setCurrentStep] = useState(1);
  const [companyData, setCompanyData] = useState<CompanyData>(initialCompany);
  const [selectedSystems, setSelectedSystems] = useState<SystemSelection[]>([]);
  const [availableSystems, setAvailableSystems] = useState<SystemApplication[]>([]);
  const [submitting, setSubmitting] = useState(false);

  const loadSystemsApi = useApi({
    onSuccess: (data: SystemApplication[]) => setAvailableSystems(data),
  });

  useEffect(() => {
    loadSystemsApi.execute(() => SystemApplicationService.getActive());
  }, []);

  const steps = [
    { id: 1, label: t('wizard.steps.company'), icon: <Building2 className="h-4 w-4" /> },
    { id: 2, label: t('wizard.steps.systems'), icon: <Database className="h-4 w-4" /> },
    { id: 3, label: t('wizard.steps.review'), icon: <ClipboardCheck className="h-4 w-4" /> },
  ];

  const canProceedStep1 =
    companyData.legalName.trim().length > 0 &&
    companyData.tradeName.trim().length > 0 &&
    companyData.document.trim().length > 0 &&
    companyData.email.trim().length > 0;

  const canProceedStep2 =
    selectedSystems.length > 0 &&
    selectedSystems.every(
      (sys) =>
        sys.connectionString.trim().length > 0 &&
        sys.integrationSecret.trim().length > 0 &&
        sys.startDate.length > 0,
    );

  const handleToggleSystem = (system: SystemApplication, checked: boolean) => {
    if (checked) {
      setSelectedSystems((prev) => [
        ...prev,
        {
          systemApplicationId: system.id,
          systemApplicationName: system.name,
          systemApplicationAudience: system.audience,
          startDate: new Date().toISOString().split('T')[0],
          endDate: undefined,
          connectionString: '',
          databaseProvider: DatabaseProviderValue.PostgreSql,
          schemaName: 'public',
          integrationSecret: '',
        },
      ]);
    } else {
      setSelectedSystems((prev) => prev.filter((sys) => sys.systemApplicationId !== system.id));
    }
  };

  const handleUpdateSystem = (systemApplicationId: number, patch: Partial<SystemSelection>) => {
    setSelectedSystems((prev) =>
      prev.map((sys) =>
        sys.systemApplicationId === systemApplicationId ? { ...sys, ...patch } : sys,
      ),
    );
  };

  const handleSubmit = async () => {
    setSubmitting(true);
    try {
      const company = await CompanyService.create(companyData);

      for (const sys of selectedSystems) {
        const contract = await ContractService.create({
          companyId: company.id,
          systemApplicationId: sys.systemApplicationId,
          startDate: sys.startDate,
          endDate: sys.endDate || undefined,
        });

        await TenantDatabaseService.create({
          contractId: contract.id,
          connectionString: sys.connectionString,
          databaseProvider: sys.databaseProvider,
          schemaName: sys.schemaName || undefined,
          integrationSecret: sys.integrationSecret,
        });
      }

      toast({
        variant: 'success',
        title: t('common.toast.successTitle'),
        description: t('wizard.toast.created'),
      });

      navigate(`/management/clients/${company.id}`);
    } catch (error) {
      const message = error instanceof Error ? error.message : t('wizard.toast.error');
      toast({
        variant: 'destructive',
        title: t('common.toast.errorTitle'),
        description: message,
      });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <PageLayout
      title={t('wizard.title')}
      subtitle={t('wizard.subtitle')}
      actions={[
        {
          key: 'cancel',
          label: t('common.action.cancel'),
          icon: <ArrowLeft className="h-4 w-4" />,
          variant: 'outline',
          onClick: () => navigate('/management/companies'),
        },
      ]}
    >
      <div className="mx-auto max-w-4xl space-y-6">
        <StepIndicator steps={steps} currentStep={currentStep} />

        <Card>
          <CardContent className="p-6">
            {currentStep === 1 && (
              <Step1Company data={companyData} onChange={setCompanyData} />
            )}

            {currentStep === 2 && (
              <Step2Systems
                availableSystems={availableSystems}
                selectedSystems={selectedSystems}
                onToggleSystem={handleToggleSystem}
                onUpdateSystem={handleUpdateSystem}
              />
            )}

            {currentStep === 3 && (
              <Step3Review company={companyData} systems={selectedSystems} />
            )}
          </CardContent>
        </Card>

        <div className="flex items-center justify-between">
          <Button
            variant="outline"
            onClick={() => setCurrentStep((s) => Math.max(1, s - 1))}
            disabled={currentStep === 1 || submitting}
          >
            <ArrowLeft className="mr-2 h-4 w-4" />
            {t('wizard.action.previous')}
          </Button>

          {currentStep < 3 ? (
            <Button
              variant="primary"
              onClick={() => setCurrentStep((s) => s + 1)}
              disabled={
                (currentStep === 1 && !canProceedStep1) ||
                (currentStep === 2 && !canProceedStep2)
              }
            >
              {t('wizard.action.next')}
              <ArrowRight className="ml-2 h-4 w-4" />
            </Button>
          ) : (
            <Button variant="primary" onClick={handleSubmit} loading={submitting}>
              <Check className="mr-2 h-4 w-4" />
              {t('wizard.action.finish')}
            </Button>
          )}
        </div>
      </div>
    </PageLayout>
  );
}

function StepIndicator({
  steps,
  currentStep,
}: {
  steps: Array<{ id: number; label: string; icon: React.ReactNode }>;
  currentStep: number;
}) {
  return (
    <div className="flex items-center justify-between">
      {steps.map((step, idx) => {
        const isActive = step.id === currentStep;
        const isComplete = step.id < currentStep;
        return (
          <div key={step.id} className="flex flex-1 items-center">
            <div className="flex items-center gap-3">
              <div
                className={`flex h-9 w-9 items-center justify-center rounded-full text-sm font-semibold ${
                  isActive
                    ? 'bg-primary text-primary-foreground'
                    : isComplete
                    ? 'bg-success text-white'
                    : 'bg-muted text-muted-foreground'
                }`}
              >
                {isComplete ? <Check className="h-4 w-4" /> : step.icon}
              </div>
              <div>
                <div
                  className={`text-sm font-medium ${
                    isActive || isComplete ? 'text-foreground' : 'text-muted-foreground'
                  }`}
                >
                  {step.label}
                </div>
              </div>
            </div>
            {idx < steps.length - 1 && (
              <div
                className={`mx-3 h-px flex-1 ${
                  isComplete ? 'bg-success' : 'bg-border'
                }`}
              />
            )}
          </div>
        );
      })}
    </div>
  );
}
