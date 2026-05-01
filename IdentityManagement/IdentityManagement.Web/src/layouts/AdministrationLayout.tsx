import { useMemo } from 'react';
import { useLocation, useNavigate, Outlet } from 'react-router-dom';
import { Home, Users, MapPin, FileText, Layers, UserCheck, Link, ShieldCheck, KeyRound } from 'lucide-react';
import { AppLayout, useAuth, AuthService, useAppNavigation, useI18n } from 'archon-ui';
import type { BreadcrumbItem } from 'archon-ui';
import logoIdentityProvider from '../assets/logo-identity-provider.png';
import logoempresa from '../assets/Mainstay/logo-login.png';
import { getNameInitials, resolveAvatarUrl } from '../utils/user';

export default function AdministrationLayout() {
  const { t } = useI18n()
  const { createMenuItem, createMenuGroup } = useAppNavigation({
    basePath: '/management'
  });

  const { user: authUser, contract, logout } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await AuthService.logoutFromServer();
    logout();
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
      avatar: getAvatar()
    };
  }, [authUser, contract]);

  const dashboardItem = createMenuItem('dashboard', t('layout.menu.dashboard'), '', <Home size={20} />);

  const managementGroup = createMenuGroup(t('layout.menu.management'), [
    { key: 'users', label: t('layout.menu.users'), path: '/users', icon: <Users size={20} /> },
    { key: 'companies', label: t('layout.menu.companies'), path: '/companies', icon: <MapPin size={20} /> },
    { key: 'system-applications', label: t('layout.menu.systemApplications'), path: '/system-applications', icon: <Layers size={20} /> },
    { key: 'oauth-clients', label: 'OAuth Clients', path: '/oauth-clients', icon: <KeyRound size={20} /> },
    { key: 'contracts', label: t('layout.menu.contracts'), path: '/contracts', icon: <FileText size={20} /> }
  ]);

  const systemGroup = createMenuGroup(t('layout.menu.accessControl'), [
    { key: 'system-role-templates', label: t('layout.menu.systemRoleTemplates'), path: '/system-role-templates', icon: <ShieldCheck size={20} /> },
    { key: 'roles', label: t('layout.menu.roles'), path: '/roles', icon: <UserCheck size={20} /> },
    { key: 'user-roles', label: t('layout.menu.userRoles'), path: '/user-roles', icon: <Link size={20} /> }
  ]);

  const breadcrumbs = useMemo((): BreadcrumbItem[] => {
    const path = location.pathname;
    const crumbs: BreadcrumbItem[] = [
      { label: t('layout.breadcrumb.home'), onClick: () => navigate('/management') }
    ];

    const routeMap: Record<string, string> = {
      '/management': t('layout.menu.dashboard'),
      '/management/dashboard': t('layout.menu.dashboard'),
      '/management/users': t('layout.menu.users'),
      '/management/companies': t('layout.menu.companies'),
      '/management/contracts': t('layout.menu.contracts'),
      '/management/system-applications': t('layout.menu.systemApplications'),
      '/management/oauth-clients': 'OAuth Clients',
      '/management/system-role-templates': t('layout.menu.systemRoleTemplates'),
      '/management/roles': t('layout.menu.roles'),
      '/management/user-roles': t('layout.menu.userRoles')
    };

    const currentLabel = routeMap[path];
    if (currentLabel && currentLabel !== t('layout.menu.dashboard')) {
      crumbs.push({ label: currentLabel });
    }

    return crumbs;
  }, [location.pathname, navigate, t]);

  return (
    <AppLayout
      title={t('layout.title')}
      titleStyle={{ fontSize: '14px' }}
      subtitle={contract?.companyName ?? ''}
      logo={
        <img
          src={logoIdentityProvider}
          alt={t('layout.title')}
          style={{ width: '32px', height: '32px', objectFit: 'contain' }}
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
