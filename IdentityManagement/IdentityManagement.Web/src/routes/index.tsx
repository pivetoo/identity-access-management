import { useEffect, useState } from 'react';
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
import Login from '../modules/Authentication/Login';

const identityManagementUrl = (import.meta.env.VITE_IDENTITY_MANAGEMENT_URL || import.meta.env.VITE_API_BASE_URL?.replace(/\/api\/?$/, '') || '').replace(/\/+$/, '');
const oidcClientId = import.meta.env.VITE_OIDC_CLIENT_ID || 'identity-management-web';

function LoginEntry() {
  const { isAuthenticated, accessToken } = useAuth();
  const navigate = useNavigate();
  const [validating, setValidating] = useState(true);

  useEffect(() => {
    const storedAccessToken = localStorage.getItem('@Archon:accessToken');
    const storedRefreshToken = localStorage.getItem('@Archon:refreshToken');
    const hasStoredTokens = !!storedAccessToken && !!storedRefreshToken;

    if (!isAuthenticated || !accessToken) {
      if (hasStoredTokens) {
        return;
      }

      setValidating(false);
      return;
    }

    const tokenValid = !AuthService.isTokenExpiringSoon(accessToken, 0);
    if (!tokenValid) {
      setValidating(false);
      return;
    }

    navigate('/management', { replace: true });
  }, [accessToken, isAuthenticated, navigate]);

  if (validating) {
    return <GlobalLoader isVisible={true} className="bg-background" />;
  }

  return <Login />;
}

function LogoutEntry() {
  useEffect(() => {
    AuthService.logout();
    window.location.href = '/';
  }, []);

  return null;
}

function AppRoutes() {
  const { isAuthenticated } = useAuth();

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<LoginEntry />} />
        <Route path="/forgot-password" element={<ForgotPassword />} />
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
