import { useEffect, useMemo, useState } from 'react';
import { BrowserRouter, Route, Routes, useLocation, useNavigate } from 'react-router-dom';
import { Callback, ProtectedRoute, useAuth, AuthService, GlobalLoader } from 'archon-ui';
import AdministrationLayout from '../layouts/AdministrationLayout';
import Dashboard from '../modules/Dashboard';
import Users from '../modules/Administration/Users';
import Companies from '../modules/Administration/Companies';
import Contracts from '../modules/Administration/Contracts';
import SystemApplications from '../modules/Administration/SystemApplications';
import Roles from '../modules/Administration/Roles';
import SystemRoleTemplates from '../modules/Administration/SystemRoleTemplates';
import UserRoles from '../modules/Administration/UserRoles';
import ForgotPassword from '../modules/Authentication/ForgotPassword';
import Login from '../modules/Authentication/Login';

const getReturnUrl = (search: string) => {
  if (typeof window === 'undefined') {
    return undefined;
  }

  const rawReturnUrl = new URLSearchParams(search).get('returnUrl');
  if (!rawReturnUrl) {
    return undefined;
  }

  try {
    const parsedUrl = new URL(rawReturnUrl);
    return parsedUrl.origin === window.location.origin ? undefined : parsedUrl.toString();
  } catch {
    return undefined;
  }
};

const buildCallbackRedirectUrl = (returnUrl: string, accessToken: string, refreshToken: string) => {
  const callbackUrl = new URL(returnUrl);
  callbackUrl.searchParams.set('accessToken', accessToken);
  callbackUrl.searchParams.set('refreshToken', refreshToken);
  return callbackUrl.toString();
};

function LoginEntry() {
  const { isAuthenticated, contract, accessToken, refreshToken } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const returnUrl = useMemo(() => getReturnUrl(location.search), [location.search]);
  const [validating, setValidating] = useState(true);

  useEffect(() => {
    if (!isAuthenticated || !accessToken || !refreshToken) {
      setValidating(false);
      return;
    }

    const tokenValid = !AuthService.isTokenExpiringSoon(accessToken, 0);
    if (!tokenValid) {
      setValidating(false);
      return;
    }

    if (returnUrl) {
      window.location.href = buildCallbackRedirectUrl(returnUrl, accessToken, refreshToken);
      return;
    }

    navigate('/management', { replace: true });
  }, [accessToken, contract?.redirectUris, isAuthenticated, navigate, refreshToken, returnUrl]);

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
        <Route path="/callback" element={<Callback redirectTo="/management" />} />
        <Route
          path="/management"
          element={
            <ProtectedRoute isAuthenticated={isAuthenticated}>
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
          <Route path="system-role-templates" element={<SystemRoleTemplates />} />
          <Route path="roles" element={<Roles />} />
          <Route path="user-roles" element={<UserRoles />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default AppRoutes;
