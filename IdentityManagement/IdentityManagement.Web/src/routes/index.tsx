import { useEffect, useMemo } from 'react';
import { BrowserRouter, Route, Routes, useLocation, useNavigate } from 'react-router-dom';
import { Callback, ProtectedRoute, useAuth } from 'archon-ui';
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

function LoginEntry() {
  const { isAuthenticated } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const returnUrl = useMemo(() => getReturnUrl(location.search), [location.search]);

  useEffect(() => {
    if (!isAuthenticated) {
      return;
    }

    if (returnUrl) {
      window.location.href = returnUrl;
      return;
    }

    navigate('/management', { replace: true });
  }, [isAuthenticated, navigate, returnUrl]);

  if (isAuthenticated) {
    return null;
  }

  return <Login />;
}

function AppRoutes() {
  const { isAuthenticated } = useAuth();

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<LoginEntry />} />
        <Route path="/forgot-password" element={<ForgotPassword />} />
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
