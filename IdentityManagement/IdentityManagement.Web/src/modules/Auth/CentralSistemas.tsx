import { useState, useMemo } from 'react';
import { Briefcase, ArrowLeft, Search } from 'lucide-react';
import { Button, Card, CardContent, Input } from 'd-rts';
import type { ContractType } from 'd-rts';
import logoEmpresa from '../../assets/logo-empresa.svg';

interface CentralSistemasProps {
  userName: string;
  userEmail: string;
  contracts: ContractType[];
  onSelectContract: (contract: ContractType) => void;
  onBack: () => void;
  loading?: boolean;
}

export default function CentralSistemas({
  userName,
  contracts,
  onSelectContract,
  onBack,
  loading = false
}: CentralSistemasProps) {
  const [searchTerm, setSearchTerm] = useState('');

  const filteredContracts = useMemo(() => {
    if (!contracts || !Array.isArray(contracts)) return [];
    if (!searchTerm.trim()) return contracts;

    return contracts.filter(contract =>
      contract.sistemaName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      contract.empresaName.toLowerCase().includes(searchTerm.toLowerCase())
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
                Voltar
              </Button>
            </div>

            <div className="flex flex-col items-center text-center w-full">
              <h1 className="text-2xl font-bold text-foreground mb-4">
                Central de Sistemas
              </h1>
              <p className="text-base text-muted-foreground leading-relaxed">
                Ola, <strong className="text-primary">{userName}</strong><br />
                Selecione qual sistema voce deseja acessar
              </p>
            </div>
          </div>

          <div className="mb-6 max-w-[280px]">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground opacity-60" />
              <Input
                type="text"
                placeholder="Buscar sistemas..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10 bg-white"
              />
            </div>
          </div>

          {filteredContracts.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground text-base">
              {searchTerm.trim() ? 'Nenhum sistema encontrado com esse termo.' : 'Nenhum sistema disponivel.'}
            </div>
          ) : (
            <div className="flex gap-6 overflow-x-auto pb-4 scrollbar-thin">
              {filteredContracts.map((contract) => (
                <div
                  key={contract.contratoId}
                  className="border border-border rounded-lg p-6 flex flex-col gap-6 transition-all duration-200 bg-card min-w-[300px] flex-shrink-0 hover:border-secondary hover:shadow-[0_4px_12px_rgba(97,121,183,0.13)] hover:translate-y-0.5"
                >
                  <div className="flex items-start gap-4 flex-1">
                    <Briefcase className="h-6 w-6 text-secondary flex-shrink-0" />
                    <div>
                      <h3 className="text-lg font-semibold text-foreground mb-1 leading-tight">
                        {contract.empresaName}
                      </h3>
                      <p className="text-base text-secondary font-medium mb-1">
                        {contract.sistemaName}
                      </p>
                      {contract.perfilName && (
                        <p className="text-sm text-muted-foreground">
                          {contract.perfilName}
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
                    Acessar
                  </Button>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>

    <a
      href="https://empresadetestes.com.br/"
      target="_blank"
      rel="noopener noreferrer"
      className="fixed bottom-8 left-8 hidden md:block"
    >
      <img
        src={logoEmpresa}
        alt="Empresa de Testes"
        className="h-14 opacity-80 hover:opacity-100 transition-opacity cursor-pointer object-contain"
      />
    </a>
  </>
  );
}
