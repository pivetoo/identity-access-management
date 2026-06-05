import { useMemo } from 'react';
import { useLocation, useNavigate, Outlet, matchPath } from 'react-router-dom';
import { Home, Users, Building2, Layers, ShieldCheck, KeyRound, CreditCard, Receipt } from 'lucide-react';
import { AppLayout, useAuth, useAppNavigation, useI18n } from 'archon-ui';
import type { BreadcrumbItem } from 'archon-ui';
import logoEmpresa from '../assets/logo-empresa.png';
import logoempresa from '../assets/Mainstay/logo-login.png';
import { getNameInitials, resolveAvatarUrl } from '../utils/user';

export default function AdministrationLayout() {
  const { t } = useI18n();
  const { createMenuItem, createMenuGroup } = useAppNavigation({
    basePath: '/management',
  });

  const { user: authUser, contract } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();

  const handleLogout = () => {
    window.location.href = '/logout';
  };

  const user = useMemo(() => {
    const getAvatar = () => {
      const apiBaseUrl = import.meta.env.VITE_API_BASE_URL?.replace('/api', '');
      const avatarUrl = resolveAvatarUrl(authUser!.avatarUrl, apiBaseUrl);

      if (avatarUrl) {
        return (
          <img
            src={avatarUrl}
            alt={authUser!.name}
            style={{ width: '100%', height: '100%', objectFit: 'cover' }}
          />
        );
      }
      return getNameInitials(authUser!.name);
    };

    return {
      name: authUser!.name,
      email: authUser!.email,
      role: contract?.roleName ?? '',
      avatar: getAvatar(),
    };
  }, [authUser, contract]);

  const dashboardItem = createMenuItem('dashboard', t('layout.menu.dashboard'), '', <Home size={20} />);

  const managementGroup = createMenuGroup(t('layout.menu.management'), [
    { key: 'companies', label: t('layout.menu.clients'), path: '/companies', icon: <Building2 size={20} /> },
    { key: 'system-applications', label: t('layout.menu.systemApplications'), path: '/system-applications', icon: <Layers size={20} /> },
    { key: 'users', label: t('layout.menu.users'), path: '/users', icon: <Users size={20} /> },
    { key: 'oauth-clients', label: 'OAuth Clients', path: '/oauth-clients', icon: <KeyRound size={20} /> },
    { key: 'plans', label: 'Planos', path: '/plans', icon: <CreditCard size={20} /> },
    { key: 'payments', label: 'Pagamentos', path: '/payments', icon: <Receipt size={20} /> },
  ]);

  const systemGroup = createMenuGroup(t('layout.menu.configuration'), [
    { key: 'system-role-templates', label: t('layout.menu.systemRoleTemplates'), path: '/system-role-templates', icon: <ShieldCheck size={20} /> },
  ]);

  const breadcrumbs = useMemo((): BreadcrumbItem[] => {
    const path = location.pathname;
    const crumbs: BreadcrumbItem[] = [
      { label: t('layout.breadcrumb.home'), onClick: () => navigate('/management') },
    ];

    const staticMap: Record<string, string> = {
      '/management': t('layout.menu.dashboard'),
      '/management/dashboard': t('layout.menu.dashboard'),
      '/management/users': t('layout.menu.users'),
      '/management/companies': t('layout.menu.clients'),
      '/management/system-applications': t('layout.menu.systemApplications'),
      '/management/oauth-clients': 'OAuth Clients',
      '/management/plans': 'Planos',
      '/management/payments': 'Pagamentos',
      '/management/system-role-templates': t('layout.menu.systemRoleTemplates'),
    };

    const systemAppDetailMatch = matchPath({ path: '/management/system-applications/:id', end: true }, path);

    const staticLabel = staticMap[path];
    if (staticLabel && staticLabel !== t('layout.menu.dashboard')) {
      crumbs.push({ label: staticLabel });
      return crumbs;
    }

    if (systemAppDetailMatch) {
      crumbs.push({
        label: t('layout.menu.systemApplications'),
        onClick: () => navigate('/management/system-applications'),
      });
      crumbs.push({ label: 'Detalhes' });
      return crumbs;
    }

    const clientDetailMatch = matchPath({ path: '/management/clients/:id', end: true }, path);
    const contractDetailMatch = matchPath({ path: '/management/clients/:id/contracts/:contractId', end: true }, path);

    if (clientDetailMatch || contractDetailMatch) {
      const clientId = clientDetailMatch?.params.id ?? contractDetailMatch?.params.id;
      crumbs.push({
        label: t('layout.menu.clients'),
        onClick: () => navigate('/management/companies'),
      });
      if (contractDetailMatch && clientId) {
        crumbs.push({
          label: t('layout.breadcrumb.client'),
          onClick: () => navigate(`/management/clients/${clientId}`),
        });
        crumbs.push({ label: t('layout.breadcrumb.contract') });
      } else {
        crumbs.push({ label: t('layout.breadcrumb.client') });
      }
    }

    return crumbs;
  }, [location.pathname, navigate, t]);

  return (
    <AppLayout
      title={t('layout.title')}
      subtitle="by Mainstay"
      navbarCompanyName={contract?.companyName}
      logo={
        <img
          src={logoEmpresa}
          alt="Mainstay"
          style={{ width: 28, height: 28, objectFit: 'contain' }}
        />
      }
      user={user}
      onLogout={handleLogout}
      menuItems={[dashboardItem]}
      menuGroups={[managementGroup, systemGroup]}
      initialCollapsed={true}
      breadcrumbs={breadcrumbs}
      companyLogo={logoempresa}
    >
      <Outlet />
    </AppLayout>
  );
}
