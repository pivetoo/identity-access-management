import { useMemo, useState } from 'react';
import { ArrowRight, Search } from 'lucide-react';
import { Input, useI18n } from 'archon-ui';
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

// Abaixo disso a busca so atrapalha: a maioria dos usuarios tem ate tres contratos.
const SEARCH_THRESHOLD = 6;

// Cada sistema recebe uma cor pela ordem em que aparece na lista, garantindo cores distintas
// entre os sistemas de um mesmo usuario (ate esgotar a paleta).
const avatarPalette = [
  'bg-primary text-primary-foreground',
  'bg-secondary text-secondary-foreground',
  'bg-info text-info-foreground',
  'bg-success text-success-foreground',
  'bg-warning text-warning-foreground',
  'bg-violet-600 text-white'
];

function getInitials(name: string) {
  const words = name.trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) return '?';
  if (words.length === 1) return words[0].slice(0, 2).toUpperCase();
  return `${words[0][0]}${words[1][0]}`.toUpperCase();
}

export default function SystemCenter({
  userName,
  userEmail,
  contracts,
  onSelectContract,
  onBack,
  loading = false
}: SystemCenterProps) {
  const { t } = useI18n();
  const [searchTerm, setSearchTerm] = useState('');

  const orderedContracts = useMemo(
    () => (Array.isArray(contracts) ? [...contracts].sort((left, right) => left.contractId - right.contractId) : []),
    [contracts]
  );

  const avatarClassBySystem = useMemo(() => {
    const map = new Map<string, string>();
    for (const contract of orderedContracts) {
      if (!map.has(contract.systemApplicationName)) {
        map.set(contract.systemApplicationName, avatarPalette[map.size % avatarPalette.length]);
      }
    }
    return map;
  }, [orderedContracts]);

  const filteredContracts = useMemo(() => {
    const term = searchTerm.trim().toLowerCase();
    if (!term) return orderedContracts;

    return orderedContracts.filter(
      (contract) =>
        contract.systemApplicationName.toLowerCase().includes(term) ||
        contract.companyName.toLowerCase().includes(term)
    );
  }, [orderedContracts, searchTerm]);

  const showSearch = orderedContracts.length > SEARCH_THRESHOLD;

  return (
    <div className="flex w-full max-w-5xl flex-col gap-8">
      <header className="flex flex-col items-center text-center">
        <img
          src={logoEmpresa}
          alt={t('authentication.login.companyLogoAlt')}
          className="h-20 object-contain"
        />
        <h1 className="mt-6 text-2xl font-bold tracking-tight text-foreground sm:text-3xl">
          {t('authentication.systemCenter.title')}
        </h1>
        <p className="mt-3 text-base leading-relaxed text-muted-foreground">
          <span className="font-medium text-foreground">
            {t('authentication.systemCenter.greeting').replace('{0}', userName)}
          </span>
          <br />
          {t('authentication.systemCenter.subtitle')}
        </p>
        <p className="mt-3 flex flex-col items-center gap-x-2 gap-y-1 text-sm text-muted-foreground sm:flex-row">
          {userEmail && <span>{userEmail}</span>}
          {userEmail && <span aria-hidden="true" className="hidden sm:inline">&middot;</span>}
          <button
            type="button"
            onClick={onBack}
            disabled={loading}
            className="font-medium text-primary underline-offset-4 transition-colors hover:underline disabled:pointer-events-none disabled:opacity-50"
          >
            {t('authentication.systemCenter.switchAccount')}
          </button>
        </p>
      </header>

      {showSearch && (
        <div className="relative mx-auto w-full max-w-sm">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            type="search"
            placeholder={t('authentication.systemCenter.searchPlaceholder')}
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="bg-card pl-10"
          />
        </div>
      )}

      {filteredContracts.length === 0 ? (
        <p className="py-12 text-center text-base text-muted-foreground">
          {searchTerm.trim() ? t('authentication.systemCenter.emptyFiltered') : t('authentication.systemCenter.empty')}
        </p>
      ) : (
        <ul
          aria-busy={loading}
          className={`grid justify-center gap-4 grid-cols-[repeat(auto-fit,minmax(min(100%,280px),320px))] transition-opacity ${loading ? 'opacity-60' : ''}`}
        >
          {filteredContracts.map((contract) => (
            <li key={contract.contractId} data-testid="system-center-contract" className="flex">
              <button
                type="button"
                onClick={() => onSelectContract(contract)}
                disabled={loading}
                className="group flex w-full items-center gap-3 rounded-xl border border-border bg-card p-5 text-left shadow-sm transition-all duration-200 hover:-translate-y-0.5 hover:border-primary/40 hover:shadow-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background disabled:pointer-events-none"
              >
                <span
                  aria-hidden="true"
                  className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-xl text-sm font-bold tracking-wider ${avatarClassBySystem.get(contract.systemApplicationName)}`}
                >
                  {getInitials(contract.systemApplicationName)}
                </span>

                <span className="min-w-0 flex-1">
                  <span className="line-clamp-2 text-base font-semibold leading-tight text-foreground">
                    {contract.systemApplicationName}
                  </span>
                  <span className="mt-1 line-clamp-2 text-sm leading-snug text-muted-foreground">
                    {contract.companyName}
                  </span>
                  {contract.roleName && (
                    <span className="mt-2 inline-block rounded-md bg-primary/10 px-2 py-0.5 text-xs font-medium text-primary">
                      {contract.roleName}
                    </span>
                  )}
                </span>

                <ArrowRight
                  className="h-5 w-5 shrink-0 text-muted-foreground/60 transition-all group-hover:translate-x-1 group-hover:text-primary"
                  aria-hidden="true"
                />
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
