import { useMemo, useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Card, CardContent, GlobalLoader, Input, AuthService, useI18n } from 'archon-ui';
import { User, Lock } from 'lucide-react';
import type { IdentifyResult, ContractType } from 'archon-ui';
import SystemCenter from '../SystemCenter';
import logoEmpresa from '../../../assets/Mainstay/logo-login.png';
import { validateEmail } from '../../../utils/validation';
import { OidcService } from '../../../services/oidcService';
import { generatePkce } from '../../../utils/pkce';

const getOidcAuthorizeUrl = () => {
  if (typeof window === 'undefined') {
    return undefined;
  }

  const rawReturnUrl = new URLSearchParams(window.location.search).get('returnUrl');
  if (!rawReturnUrl) {
    return undefined;
  }

  try {
    const identityManagementUrl = import.meta.env.VITE_IDENTITY_MANAGEMENT_URL;
    if (!identityManagementUrl) {
      return undefined;
    }

    const parsedUrl = new URL(rawReturnUrl);
    const identityUrl = new URL(identityManagementUrl);
    if (parsedUrl.origin !== identityUrl.origin || parsedUrl.pathname !== '/connect/authorize') {
      return undefined;
    }

    return parsedUrl.toString();
  } catch {
    return undefined;
  }
};

export default function Login() {
  const { t } = useI18n()
  const navigate = useNavigate();

  const oidcAuthorizeUrl = useMemo(() => getOidcAuthorizeUrl(), []);
  const [pendingAuthorizeUrl, setPendingAuthorizeUrl] = useState<string | null>(null);

  const buildAuthorizeUrl = async (contractId?: number): Promise<string> => {
    const identityManagementUrl = import.meta.env.VITE_IDENTITY_MANAGEMENT_URL;
    const clientId = import.meta.env.VITE_OIDC_CLIENT_ID;
    const currentOrigin = window.location.origin;
    const callbackPath = '/callback';

    const pkce = await generatePkce();
    const redirectUri = `${currentOrigin}${callbackPath}`;

    sessionStorage.setItem('@Archon:oidc:state', pkce.state);
    sessionStorage.setItem('@Archon:oidc:nonce', pkce.nonce);
    sessionStorage.setItem('@Archon:oidc:codeVerifier', pkce.codeVerifier);
    sessionStorage.setItem('@Archon:oidc:redirectUri', redirectUri);

    const authorizeUrl = new URL('/connect/authorize', identityManagementUrl);
    authorizeUrl.searchParams.set('client_id', clientId);
    authorizeUrl.searchParams.set('redirect_uri', redirectUri);
    authorizeUrl.searchParams.set('response_type', 'code');
    authorizeUrl.searchParams.set('scope', 'openid profile email offline_access');
    authorizeUrl.searchParams.set('state', pkce.state);
    authorizeUrl.searchParams.set('nonce', pkce.nonce);
    authorizeUrl.searchParams.set('code_challenge', pkce.codeChallenge);
    authorizeUrl.searchParams.set('code_challenge_method', 'S256');

    if (typeof contractId === 'number') {
      authorizeUrl.searchParams.set('contract_id', contractId.toString());
    }

    return authorizeUrl.toString();
  };

  const withContractId = (authorizeUrl: string, contractId: number): string => {
    const url = new URL(authorizeUrl);
    url.searchParams.set('contract_id', contractId.toString());
    return url.toString();
  };

  const completeContractLogin = async (contract: ContractType, authorizationSessionToken: string) => {
    setContractLoading(true);

    try {
      const storedAuthorizeUrl = oidcAuthorizeUrl ? null : sessionStorage.getItem('@Archon:login:authorizeUrl');
      const baseAuthorizeUrl = oidcAuthorizeUrl ?? storedAuthorizeUrl ?? pendingAuthorizeUrl ?? await buildAuthorizeUrl();
      const authorizeUrl = withContractId(baseAuthorizeUrl, contract.contractId);

      setRedirecting(true);
      const response = await OidcService.completeAuthorize({
        authorizationSessionToken,
        contractId: contract.contractId,
        authorizeUrl
      });

      window.location.replace(response.redirectUrl);
    } finally {
      setContractLoading(false);
    }
  };

  const handleIdentifyResult = async (data: IdentifyResult) => {
    const contractsForOrigin = data.availableContracts;

    if (contractsForOrigin.length === 1) {
      await completeContractLogin(contractsForOrigin[0], data.authorizationSessionToken);
      return;
    }

    setContractData({
      ...data,
      availableContracts: contractsForOrigin
    });
    setShowContractSelection(true);
  };

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [emailError, setEmailError] = useState('');
  const [passwordError, setPasswordError] = useState('');
  const [loading, setLoading] = useState(false);
  const [contractLoading, setContractLoading] = useState(false);
  const [redirecting, setRedirecting] = useState(false);
  const [showContractSelection, setShowContractSelection] = useState(false);
  const [contractData, setContractData] = useState<IdentifyResult | null>(null);

  const validateForm = (): boolean => {
    setEmailError('');
    setPasswordError('');

    if (!email) {
      setEmailError(t('authentication.login.validation.emailRequired'));
      return false;
    }

    if (!validateEmail(email)) {
      setEmailError(t('authentication.login.validation.emailInvalid'));
      return false;
    }

    if (!password) {
      setPasswordError(t('authentication.login.validation.passwordRequired'));
      return false;
    }

    if (password.length < 6) {
      setPasswordError(t('authentication.login.validation.passwordMinLength'));
      return false;
    }

    return true;
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();

    if (!validateForm()) return;

    setLoading(true);

    try {
      const authorizeUrl = oidcAuthorizeUrl ?? await buildAuthorizeUrl();
      setPendingAuthorizeUrl(authorizeUrl);
      sessionStorage.setItem('@Archon:login:authorizeUrl', authorizeUrl);

      const data = await AuthService.identify({
        username: email,
        password: password,
        authorizeUrl
      });

      if (!data) {
        setLoading(false);
        return;
      }

      setPassword('');
      await handleIdentifyResult(data);
    } catch (error: any) {
      setPasswordError(error.message);
    } finally {
      setLoading(false);
    }
  };

  const handleSelectContract = async (contract: ContractType) => {
    if (!contractData) return;

    await completeContractLogin(contract, contractData.authorizationSessionToken);
  };

  const handleBackToLogin = () => {
    setShowContractSelection(false);
    setContractData(null);
  };

  const handleForgotPassword = () => {
    navigate('/forgot-password');
  };


  if (redirecting) {
    return <GlobalLoader isVisible={true} className="bg-background" />;
  }

  if (showContractSelection && contractData) {
    return (
      <div className="flex flex-col items-center justify-center min-h-screen w-full bg-gradient-to-br from-background via-background to-primary/5 p-8">
        <SystemCenter
          userName={contractData.userName}
          userEmail={contractData.userEmail}
          contracts={contractData.availableContracts}
          onSelectContract={handleSelectContract}
          onBack={handleBackToLogin}
          loading={contractLoading}
        />
        <p className="text-center mt-8 text-xs text-muted-foreground/50">
          {t('authentication.footer.copyright').replace('{0}', String(new Date().getFullYear()))}
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-col items-center justify-center min-h-screen w-full bg-gradient-to-br from-background via-background to-primary/5 p-8">
      <div className="w-full max-w-[400px]">
        <div className="relative">
          <div className="absolute -inset-1 bg-gradient-to-r from-primary/20 via-primary/10 to-primary/20 rounded-2xl blur-lg opacity-60" />
          <Card className="relative border border-border/50 shadow-2xl rounded-2xl overflow-hidden bg-background/95 backdrop-blur-sm">
            <div className="absolute top-0 left-0 right-0 h-1 bg-gradient-to-r from-primary via-primary/80 to-primary" />
            <CardContent className="pt-8 pb-10 px-8">
              <div className="flex justify-center mb-6">
                <img
                  src={logoEmpresa}
                  alt={t('authentication.login.companyLogoAlt')}
                  className="h-36 object-contain"
                />
              </div>

              <p className="text-center text-sm text-muted-foreground mb-10">
                <strong>{t('authentication.login.title')}</strong>
                <br />
                {t('authentication.login.subtitle')}
              </p>

              <form onSubmit={handleSubmit} className="flex flex-col">
                <div className="mb-2">
                  <label className="text-sm font-medium mb-1 block text-muted-foreground pl-1">{t('common.field.email')}</label>
                  <div className="relative w-full">
                    <User className="absolute left-3 top-3 h-4 w-4 text-foreground opacity-70 pointer-events-none z-10" />
                    <Input
                      type="email"
                      placeholder={t('common.field.email')}
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      error={!!emailError}
                      helperText={emailError}
                      disabled={loading}
                      className="pl-10 bg-white placeholder:text-muted-foreground/50 h-10"
                    />
                  </div>
                </div>

                <div className="mb-2">
                  <label className="text-sm font-medium mb-1 block text-muted-foreground pl-1">{t('common.field.password')}</label>
                  <div className="relative w-full">
                    <Lock className="absolute left-3 top-3 h-4 w-4 text-foreground opacity-70 pointer-events-none z-10" />
                    <Input
                      type="password"
                      placeholder={t('common.field.password')}
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                      error={!!passwordError}
                      helperText={passwordError}
                      disabled={loading}
                      className="pl-10 bg-white placeholder:text-muted-foreground/50 h-10"
                    />
                  </div>
                </div>

                <div className="mt-6">
                  <Button
                    type="submit"
                    variant="primary"
                    className="w-full py-5"
                    loading={loading}
                    disabled={loading}
                  >
                    {t('authentication.login.submit')}
                  </Button>
                </div>
              </form>

              <button
                type="button"
                onClick={handleForgotPassword}
                className="w-full text-right mt-4 text-sm text-muted-foreground hover:text-primary transition-colors"
              >
                {t('authentication.login.forgotPassword')}
              </button>
            </CardContent>
          </Card>
        </div>
      </div>

      <p className="text-center mt-8 text-xs text-muted-foreground/50">
        {t('authentication.footer.copyright').replace('{0}', String(new Date().getFullYear()))}
      </p>

          </div>
  );
}
