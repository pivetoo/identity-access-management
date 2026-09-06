import type { ComponentType, SVGProps } from 'react';

type IconProps = SVGProps<SVGSVGElement>;

// O quadrado do card e sempre branco, entao as cores da marca sao fixas e nao seguem o tema.
const ACCENT = 'hsl(186 100% 39%)';

const svgProps = { viewBox: '0 0 24 24', fill: 'none', 'aria-hidden': true } as const;

export function MainstayIcon(props: IconProps) {
  return (
    <svg {...svgProps} {...props}>
      <path fill="currentColor" d="M4 19V5h3.6l4.4 7.4L16.4 5H20v14h-3v-8.6l-3.9 6.4h-2.2L7 10.4V19H4z" />
      <path fill={ACCENT} d="M12.6 17.2H17v1.8h-4.4z" />
    </svg>
  );
}

export function IdentityManagementIcon(props: IconProps) {
  return (
    <svg {...svgProps} {...props}>
      <path
        fill="currentColor"
        fillRule="evenodd"
        d="M6 5h12a2 2 0 0 1 2 2v13a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V7a2 2 0 0 1 2-2zm6 4.2a2.4 2.4 0 1 0 0 4.8 2.4 2.4 0 0 0 0-4.8zM7.4 19c.4-2.7 2.3-4.2 4.6-4.2s4.2 1.5 4.6 4.2H7.4z"
      />
      <path fill={ACCENT} d="M9.8 2.5h4.4a.8.8 0 0 1 .8.8V6H9V3.3a.8.8 0 0 1 .8-.8z" />
    </svg>
  );
}

export function IntegrationPlatformIcon(props: IconProps) {
  return (
    <svg {...svgProps} {...props}>
      <path
        fill="currentColor"
        d="M8.5 3.5h2.5V8h2V3.5h2.5V8h1.2a1 1 0 0 1 1 1v3.2a5.7 5.7 0 0 1-4.7 5.6V19h-2v-1.2A5.7 5.7 0 0 1 6.3 12.2V9a1 1 0 0 1 1-1h1.2V3.5z"
      />
      <path fill={ACCENT} d="M11 19h2v2.3a1 1 0 0 1-2 0V19z" />
    </svg>
  );
}

export function HelpDeskIcon(props: IconProps) {
  return (
    <svg {...svgProps} {...props}>
      <path stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" d="M5 13.5V11a7 7 0 0 1 14 0v2.5" />
      <rect x="3.5" y="12" width="4.5" height="6.5" rx="1.5" fill="currentColor" />
      <rect x="16" y="12" width="4.5" height="6.5" rx="1.5" fill="currentColor" />
      <path stroke="currentColor" strokeWidth="2" strokeLinecap="round" d="M18.2 18.5v.3a2.7 2.7 0 0 1-2.7 2.7H14.5" />
      <circle cx="13" cy="21.5" r="1.7" fill={ACCENT} />
    </svg>
  );
}

// Chave e o audience do SystemApplication, que e estavel; o nome de exibicao muda.
// A comparacao ignora caixa e separadores, entao "help-desk", "helpdesk" e "HelpDesk" sao o mesmo.
const systemIconByAudience: Record<string, ComponentType<IconProps>> = {
  agencycampaign: MainstayIcon,
  identitymanagement: IdentityManagementIcon,
  integrationplatform: IntegrationPlatformIcon,
  helpdesk: HelpDeskIcon,
  mainstayhelpdesk: HelpDeskIcon
};

function normalizeAudience(audience: string) {
  return audience.toLowerCase().replace(/[^a-z0-9]/g, '');
}

function getInitials(name: string) {
  const words = name.trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) return '?';
  if (words.length === 1) return words[0].slice(0, 2).toUpperCase();
  return `${words[0][0]}${words[1][0]}`.toUpperCase();
}

interface SystemIconProps {
  audience?: string;
  name: string;
  className?: string;
}

export default function SystemIcon({ audience, name, className }: SystemIconProps) {
  const Icon = audience ? systemIconByAudience[normalizeAudience(audience)] : undefined;

  if (Icon) {
    return <Icon className={className} />;
  }

  return <span className="text-sm font-bold tracking-wider">{getInitials(name)}</span>;
}
