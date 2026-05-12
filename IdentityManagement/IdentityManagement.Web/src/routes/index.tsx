import { useEffect, useMemo } from 'react';
import { BrowserRouter, Route, Routes, useNavigate } from 'react-router-dom';
import { Callback, ProtectedRoute, useAuth, AuthService, GlobalLoader } from 'archon-ui';
import AdministrationLayout from '../layouts/AdministrationLayout';
import Dashboard from '../modules/Dashboard';
import Users from '../modules/Administration/Users';
import Companies from '../modules/Administration/Companies';
import Contracts from '../modules/Administration/Contracts';
import SystemApplications from '../modules/Administration/SystemApplications';
import OAuthClients from '../modules/Administration/OAuthClients';
import Roles from '../modules/Administration/Roles';
import SystemRoleTemplates from '../modules/Administration/SystemRoleTemplates';
import UserRoles from '../modules/Administration/UserRoles';
import ForgotPassword from '../modules/Authentication/ForgotPassword';
import ResetPassword from '../modules/Authentication/ResetPassword';
import SetupAdmin from '../modules/Authentication/SetupAdmin';
import Login from '../modules/Authentication/Login';

const identityManagementUrl = (import.meta.env.VITE_IDENTITY_MANAGEMENT_URL || import.meta.env.VITE_API_BASE_URL?.replace(/\/api\/?$/, '') || '').replace(/\/+$/, '');
const oidcClientId = import.meta.env.VITE_OIDC_CLIENT_ID || 'identity-management-web';

function LoginEntry() {
  const { isAuthenticated, accessToken } = useAuth();
  const navigate = useNavigate();
  const hashParams = useMemo(() => {
    const hash = window.location.hash.startsWith('#')
      ? window.location.hash.slice(1)
      : window.location.hash;

    return new URLSearchParams(hash);
  }, []);

  const hasAuthorizationSessionToken = hashParams.has('authorizationSessionToken');
  const hasValidAccessToken = !!accessToken && !AuthService.isTokenExpiringSoon(accessToken, 0);
  const shouldShowLogin = !hasAuthorizationSessionToken && (!isAuthenticated || !accessToken || !hasValidAccessToken);

  if (!hasAuthorizationSessionToken && accessToken && !hasValidAccessToken) {
    AuthService.logout();
  }

  useEffect(() => {
    if (hasAuthorizationSessionToken) {
      window.location.replace(`/management${window.location.hash}`);
      return;
    }

    if (isAuthenticated && hasValidAccessToken) {
      navigate('/management', { replace: true });
    }
  }, [hasAuthorizationSessionToken, hasValidAccessToken, isAuthenticated, navigate]);

  if (shouldShowLogin) {
    return <Login />;
  }

  return <GlobalLoader isVisible={true} className="bg-background" />;
}

function LogoutEntry() {
  useEffect(() => {
    AuthService.logoutFromServer()
      .catch(() => {})
      .finally(() => {
        AuthService.logout();
        window.location.href = '/login';
      });
  }, []);

  return null;
}

function AppRoutes() {
  const { isAuthenticated } = useAuth();

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<LoginEntry />} />
        <Route path="/login" element={<LoginEntry />} />
        <Route path="/forgot-password" element={<ForgotPassword />} />
        <Route path="/reset-password" element={<ResetPassword />} />
        <Route path="/setup-admin" element={<SetupAdmin />} />
        <Route path="/logout" element={<LogoutEntry />} />
        <Route
          path="/callback"
          element={
            <Callback
              redirectTo="/management"
              identityManagementUrl={identityManagementUrl}
              oidcClientId={oidcClientId}
            />
          }
        />
        <Route
          path="/management"
          element={
            <ProtectedRoute
              isAuthenticated={isAuthenticated}
              externalRedirect
              redirectTo={identityManagementUrl}
              oidcClientId={oidcClientId}
            >
              <AdministrationLayout />
            </ProtectedRoute>
          }
        >
          <Route index element={<Dashboard />} />
          <Route path="dashboard" element={<Dashboard />} />
          <Route path="users" element={<Users />} />
          <Route path="companies" element={<Companies />} />
          <Route path="contracts" element={<Contracts />} />
          <Route path="system-applications" element={<SystemApplications />} />
          <Route path="oauth-clients" element={<OAuthClients />} />
          <Route path="system-role-templates" element={<SystemRoleTemplates />} />
          <Route path="roles" element={<Roles />} />
          <Route path="user-roles" element={<UserRoles />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default AppRoutes;
