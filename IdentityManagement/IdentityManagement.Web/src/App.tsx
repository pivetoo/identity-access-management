import { useEffect } from 'react';
import { AuthProvider, GlobalLoaderProvider, I18nProvider, useGlobalLoader, Toaster, setApiBaseURL, setIdentityManagementURL, setGlobalLoaderContext, ThemeProvider } from 'archon-ui';
import AppRoutes from './routes';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL;
const identityManagementUrl = import.meta.env.VITE_IDENTITY_MANAGEMENT_URL || apiBaseUrl;
if (apiBaseUrl) {
  setApiBaseURL(apiBaseUrl);
}

if (identityManagementUrl) {
  setIdentityManagementURL(identityManagementUrl);
}

function AppContent() {
  const globalLoaderContext = useGlobalLoader();

  useEffect(() => {
    setGlobalLoaderContext(globalLoaderContext);
  }, [globalLoaderContext]);

  return (
    <>
      <Toaster />
      <AuthProvider>
        <AppRoutes />
      </AuthProvider>
    </>
  );
}

function App() {
  return (
    <ThemeProvider>
      <I18nProvider initialCulture="pt-BR">
        <GlobalLoaderProvider>
          <AppContent />
        </GlobalLoaderProvider>
      </I18nProvider>
    </ThemeProvider>
  );
}

export default App
