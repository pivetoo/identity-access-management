import { useMemo } from 'react';
import { useLocation, useNavigate, Outlet } from 'react-router-dom';
import { Home, Users, MapPin, FileText, Layers, UserCheck, Link, ShieldCheck } from 'lucide-react';
import { AppLayout, useAuth, AuthService, useAppNavigation } from 'archon-ui';
import type { BreadcrumbItem } from 'archon-ui';
import logoIdentityProvider from '../assets/logo-identity-provider.svg';
import logoempresa from '../assets/logo-empresa.svg';
import { getNameInitials, resolveAvatarUrl } from '../utils/user';

export default function AdministrationLayout() {
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

  const dashboardItem = createMenuItem('dashboard', 'Dashboard', '', <Home size={20} />);

  const managementGroup = createMenuGroup('Gestão', [
    { key: 'users', label: 'Usuários', path: '/users', icon: <Users size={20} /> },
    { key: 'companies', label: 'Empresas', path: '/companies', icon: <MapPin size={20} /> },
    { key: 'system-applications', label: 'Sistemas', path: '/system-applications', icon: <Layers size={20} /> },
    { key: 'contracts', label: 'Contratos', path: '/contracts', icon: <FileText size={20} /> }
  ]);

  const systemGroup = createMenuGroup('SystemApplication', [
    { key: 'system-role-templates', label: 'Template de Perfis', path: '/system-role-templates', icon: <ShieldCheck size={20} /> },
    { key: 'roles', label: 'Perfis', path: '/roles', icon: <UserCheck size={20} /> },
    { key: 'user-roles', label: 'Vincular Usuários', path: '/user-roles', icon: <Link size={20} /> }
  ]);

  const breadcrumbs = useMemo((): BreadcrumbItem[] => {
    const path = location.pathname;
    const crumbs: BreadcrumbItem[] = [
      { label: 'Início', onClick: () => navigate('/management') }
    ];

    const routeMap: Record<string, string> = {
      '/management': 'Dashboard',
      '/management/dashboard': 'Dashboard',
      '/management/users': 'Usuários',
      '/management/companies': 'Empresas',
      '/management/contracts': 'Contratos',
      '/management/system-applications': 'Sistemas',
      '/management/system-role-templates': 'Template de Perfis',
      '/management/roles': 'Perfis',
      '/management/user-roles': 'Vincular Usuários'
    };

    const currentLabel = routeMap[path];
    if (currentLabel && currentLabel !== 'Dashboard') {
      crumbs.push({ label: currentLabel });
    }

    return crumbs;
  }, [location.pathname, navigate]);

  return (
    <AppLayout
      title="Identity Management"
      subtitle={contract?.companyName ?? ''}
      logo={
        <img
          src={logoIdentityProvider}
          alt="Identity Management"
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
