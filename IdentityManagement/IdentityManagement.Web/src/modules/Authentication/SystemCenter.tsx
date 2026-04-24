import { useState, useMemo } from 'react';
import { Briefcase, ArrowLeft, Search } from 'lucide-react';
import { Button, Card, CardContent, Input, useI18n } from 'archon-ui';
import type { ContractType } from 'archon-ui';
import logoEmpresa from '../../assets/Mainstay/logo-login.png';

interface SystemCenterProps {
  userName: string;
  userEmail: string;
  contracts: ContractType[];
  onSelectContract: (contract: ContractType) => void;
  onBack: () => void;
  loading?: boolean;
}

export default function SystemCenter({
  userName,
  contracts,
  onSelectContract,
  onBack,
  loading = false
}: SystemCenterProps) {
  const { t } = useI18n()
  const [searchTerm, setSearchTerm] = useState('');

  const filteredContracts = useMemo(() => {
    if (!contracts || !Array.isArray(contracts)) return [];

    const orderedContracts = [...contracts].sort((left, right) => left.contractId - right.contractId);
    if (!searchTerm.trim()) return orderedContracts;

    return orderedContracts.filter(contract =>
      contract.systemApplicationName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      contract.companyName.toLowerCase().includes(searchTerm.toLowerCase())
    );
  }, [contracts, searchTerm]);

  return (
    <>
      <div className="w-full max-w-[1000px]">
        <Card className="border-0 shadow-md">
        <CardContent className="pt-6">
          <div className="mb-8 flex flex-col items-stretch">
            <div className="flex justify-start mb-6">
              <Button
                variant="outline"
                size="sm"
                icon={<ArrowLeft />}
                onClick={onBack}
                disabled={loading}
              >
                {t('common.action.back')}
              </Button>
            </div>

            <div className="flex flex-col items-center text-center w-full">
              <h1 className="text-2xl font-bold text-foreground mb-4">
                {t('authentication.systemCenter.title')}
              </h1>
              <p className="text-base text-muted-foreground leading-relaxed">
                {t('authentication.systemCenter.greeting').replace('{0}', userName)}<br />
                {t('authentication.systemCenter.subtitle')}
              </p>
            </div>
          </div>

          <div className="mb-6 max-w-[280px]">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground opacity-60" />
              <Input
                type="text"
                placeholder={t('authentication.systemCenter.searchPlaceholder')}
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10 bg-white"
              />
            </div>
          </div>

          {filteredContracts.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground text-base">
              {searchTerm.trim() ? t('authentication.systemCenter.emptyFiltered') : t('authentication.systemCenter.empty')}
            </div>
          ) : (
            <div className="flex gap-6 overflow-x-auto pb-4 scrollbar-thin">
              {filteredContracts.map((contract) => (
                <div
                  key={contract.contractId}
                  className="border border-border rounded-lg p-6 flex flex-col gap-6 transition-all duration-200 bg-card min-w-[300px] flex-shrink-0 hover:border-secondary hover:shadow-[0_4px_12px_rgba(97,121,183,0.13)] hover:translate-y-0.5"
                >
                  <div className="flex items-start gap-4 flex-1">
                    <Briefcase className="h-6 w-6 text-secondary flex-shrink-0" />
                    <div>
                      <h3 className="text-lg font-semibold text-foreground mb-1 leading-tight">
                        {contract.companyName}
                      </h3>
                      <p className="text-base text-secondary font-medium mb-1">
                        {contract.systemApplicationName}
                      </p>
                      {contract.roleName && (
                        <p className="text-sm text-muted-foreground">
                          {contract.roleName}
                        </p>
                      )}
                    </div>
                  </div>

                  <Button
                    variant="primary"
                    size="sm"
                    onClick={() => onSelectContract(contract)}
                    disabled={loading}
                    className="w-full"
                  >
                    {t('common.action.access')}
                  </Button>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>

    <a
      href="https://mainstay.com.br/"
      target="_blank"
      rel="noopener noreferrer"
      className="fixed bottom-8 left-8 hidden md:block"
    >
      <img
        src={logoEmpresa}
        alt={t('authentication.login.companyLogoAlt')}
                  className="h-16 opacity-80 hover:opacity-100 transition-opacity cursor-pointer object-contain"
      />
    </a>
  </>
  );
}
