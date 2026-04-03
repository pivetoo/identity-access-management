import { useMemo } from 'react';
import { useLocation, useNavigate, Outlet } from 'react-router-dom';
import { Home, Users, MapPin, FileText, Layers, UserCheck, Link } from 'lucide-react';
import { AppLayout, useAuth, AuthService, useAppNavigation } from 'd-rts';
import type { BreadcrumbItem } from 'd-rts';
import logoIdentityProvider from '../assets/logo-identity-provider.svg';
import logoempresa from '../assets/logo-empresa.svg';

export default function ManagementLayout() {
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

  const getInitials = (name: string) => {
    const parts = name.split(' ');
    if (parts.length >= 2) {
      return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
  };

  const user = useMemo(() => {
    const getAvatar = () => {
      if (authUser!.avatarUrl) {
        const apiBaseUrl = import.meta.env.VITE_API_BASE_URL?.replace('/api', '');
        const avatarUrl = authUser!.avatarUrl.startsWith('http')
          ? authUser!.avatarUrl
          : `${apiBaseUrl}${authUser!.avatarUrl}`;

        return (
          <img
            src={avatarUrl}
            alt={authUser!.name}
            style={{ width: '100%', height: '100%', objectFit: 'cover' }}
          />
        );
      }
      return getInitials(authUser!.name);
    };

    return {
      name: authUser!.name,
      email: authUser!.email,
      role: contract!.perfilName!,
      avatar: getAvatar()
    };
  }, [authUser, contract]);

  const dashboardItem = createMenuItem('dashboard', 'Dashboard', '', <Home size={20} />);

  const managementGroup = createMenuGroup('Gestão', [
    { key: 'usuarios', label: 'Usuários', path: '/usuarios', icon: <Users size={20} /> },
    { key: 'empresas', label: 'Empresas', path: '/empresas', icon: <MapPin size={20} /> },
    { key: 'sistemas', label: 'Sistemas', path: '/sistemas', icon: <Layers size={20} /> },
    { key: 'contratos', label: 'Contratos', path: '/contratos', icon: <FileText size={20} /> }
  ]);

  const systemGroup = createMenuGroup('Sistema', [
    { key: 'perfis', label: 'Perfis', path: '/perfis', icon: <UserCheck size={20} /> },
    { key: 'usuario-perfis', label: 'Vincular Usuários', path: '/usuario-perfis', icon: <Link size={20} /> }
  ]);

  const breadcrumbs = useMemo((): BreadcrumbItem[] => {
    const path = location.pathname;
    const crumbs: BreadcrumbItem[] = [
      { label: 'Início', onClick: () => navigate('/management') }
    ];

    const routeMap: Record<string, string> = {
      '/management': 'Dashboard',
      '/management/dashboard': 'Dashboard',
      '/management/usuarios': 'Usuários',
      '/management/empresas': 'Empresas',
      '/management/contratos': 'Contratos',
      '/management/sistemas': 'Sistemas',
      '/management/perfis': 'Perfis',
      '/management/usuario-perfis': 'Vincular Usuários'
    };

    const currentLabel = routeMap[path];
    if (currentLabel && currentLabel !== 'Dashboard') {
      crumbs.push({ label: currentLabel });
    }

    return crumbs;
  }, [location.pathname, navigate]);

  return (
    <AppLayout
      title="Identity Provider"
      subtitle={contract!.empresaName}
      logo={
        <img
          src={logoIdentityProvider}
          alt="Identity Provider"
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
