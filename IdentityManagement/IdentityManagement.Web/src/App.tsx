import { useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { AuthProvider, GlobalLoaderProvider, useGlobalLoader, useAuth, Toaster, setApiBaseURL, setIdentityManagementURL, ProtectedRoute, Callback, setGlobalLoaderContext, ThemeProvider } from 'd-rts';
import Login from './modules/Auth/Login';
import ManagementLayout from './layouts/ManagementLayout';
import Dashboard from './modules/Management/Dashboard';
import Usuarios from './modules/Management/Usuarios';
import Empresas from './modules/Management/Empresas';
import Contratos from './modules/Management/Contratos';
import Sistemas from './modules/Management/Sistemas';
import Perfis from './modules/Management/Perfis';
import UsuarioPerfis from './modules/Management/UsuarioPerfis';
import ForgotPassword from './modules/Auth/ForgotPassword';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL;
const identityManagementUrl = import.meta.env.VITE_IDENTITY_MANAGEMENT_URL || apiBaseUrl?.replace(/\/api$/, '');
if (apiBaseUrl) {
  setApiBaseURL(apiBaseUrl);
}

if (identityManagementUrl) {
  setIdentityManagementURL(identityManagementUrl);
}

function ProtectedRoutes() {
  const { isAuthenticated } = useAuth();

  return (
    <Routes>
      <Route path="/" element={<Login />} />
      <Route path="/forgot-password" element={<ForgotPassword />} />
      <Route path="/callback" element={<Callback redirectTo="/management" />} />
      <Route
        path="/management"
        element={
          <ProtectedRoute isAuthenticated={isAuthenticated}>
            <ManagementLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Dashboard />} />
        <Route path="dashboard" element={<Dashboard />} />
        <Route path="usuarios" element={<Usuarios />} />
        <Route path="empresas" element={<Empresas />} />
        <Route path="contratos" element={<Contratos />} />
        <Route path="sistemas" element={<Sistemas />} />
        <Route path="perfis" element={<Perfis />} />
        <Route path="usuario-perfis" element={<UsuarioPerfis />} />
      </Route>
    </Routes>
  );
};

function AppContent() {
  const globalLoaderContext = useGlobalLoader();

  useEffect(() => {
    setGlobalLoaderContext(globalLoaderContext);
  }, [globalLoaderContext]);

  return (
    <>
      <Toaster />
      <Router>
        <AuthProvider>
          <ProtectedRoutes />
        </AuthProvider>
      </Router>
    </>
  );
};

function App() {
  return (
    <ThemeProvider>
      <GlobalLoaderProvider>
        <AppContent />
      </GlobalLoaderProvider>
    </ThemeProvider>
  );
}

export default App
