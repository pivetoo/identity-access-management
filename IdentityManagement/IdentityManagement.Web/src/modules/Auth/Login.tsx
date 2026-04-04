import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Card, CardContent, Input, useAuth, AuthService } from 'archon-ui';
import { User, Lock } from 'lucide-react';
import type { IdentifyResult, ContractType } from 'archon-ui';
import CentralSistemas from './CentralSistemas';
import logoEmpresa from '../../assets/logo-empresa.svg';
import { validateEmail } from '../../utils/validation';

export default function Login() {
  const navigate = useNavigate();
  const { login } = useAuth();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [emailError, setEmailError] = useState('');
  const [passwordError, setPasswordError] = useState('');
  const [loading, setLoading] = useState(false);
  const [contractLoading, setContractLoading] = useState(false);
  const [showContractSelection, setShowContractSelection] = useState(false);
  const [contractData, setContractData] = useState<IdentifyResult | null>(null);

  const validateForm = (): boolean => {
    setEmailError('');
    setPasswordError('');

    if (!email) {
      setEmailError('E-mail é obrigatório');
      return false;
    }

    if (!validateEmail(email)) {
      setEmailError('E-mail inválido');
      return false;
    }

    if (!password) {
      setPasswordError('Senha é obrigatória');
      return false;
    }

    if (password.length < 6) {
      setPasswordError('Senha deve ter no mínimo 6 caracteres');
      return false;
    }

    return true;
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();

    if (!validateForm()) return;

    setLoading(true);

    try {
      const data = await AuthService.identify({
        username: email,
        password: password
      });

      if (!data) {
        setLoading(false);
        return;
      }

      if ('accessToken' in data) {
        login(data);
        if (data.redirectUrl) {
          window.location.href = data.redirectUrl;
        } else {
          navigate('/management');
        }
      } else {
        setContractData(data);
        setShowContractSelection(true);
      }
    } catch (error: any) {
      setPasswordError(error.message);
    } finally {
      setLoading(false);
    }
  };

  const handleSelectContract = async (contract: ContractType) => {
    if (!contractData) return;

    setContractLoading(true);

    const data = await AuthService.loginWithContract({
      userId: contractData.userId,
      contractId: contract.contractId,
      temporaryToken: contractData.temporaryToken
    });

    login(data);
    if (data.redirectUrl) {
      window.location.href = data.redirectUrl;
    } else {
      navigate('/management');
    }

    setContractLoading(false);
  };

  const handleBackToLogin = () => {
    setShowContractSelection(false);
    setContractData(null);
  };

  const handleForgotPassword = () => {
    navigate('/forgot-password');
  };

  if (showContractSelection && contractData) {
    return (
      <div className="flex flex-col items-center justify-center min-h-screen w-full bg-gradient-to-br from-background via-background to-primary/5 p-8">
        <CentralSistemas
          userName={contractData.userName}
          userEmail={contractData.userEmail}
          contracts={contractData.availableContracts}
          onSelectContract={handleSelectContract}
          onBack={handleBackToLogin}
          loading={contractLoading}
        />
        <p className="text-center mt-8 text-xs text-muted-foreground/50">
          © {new Date().getFullYear()} Empresa de Testes. Todos os direitos reservados
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
              <div className="flex justify-center mb-2">
                <img
                  src={logoEmpresa}
                  alt="Testes"
                  className="h-40"
                />
              </div>

              <p className="text-center text-sm text-muted-foreground mb-10">
                Você está acessando<br />
                <strong>[Provedor de Identidade]</strong>
              </p>

              <form onSubmit={handleSubmit} className="flex flex-col">
                <div className="mb-2">
                  <label className="text-sm font-medium mb-1 block text-muted-foreground pl-1">Email</label>
                  <div className="relative w-full">
                    <User className="absolute left-3 top-3 h-4 w-4 text-foreground opacity-70 pointer-events-none z-10" />
                    <Input
                      type="email"
                      placeholder="Email"
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
                  <label className="text-sm font-medium mb-1 block text-muted-foreground pl-1">Senha</label>
                  <div className="relative w-full">
                    <Lock className="absolute left-3 top-3 h-4 w-4 text-foreground opacity-70 pointer-events-none z-10" />
                    <Input
                      type="password"
                      placeholder="Senha"
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
                    Entrar
                  </Button>
                </div>
              </form>

              <button
                type="button"
                onClick={handleForgotPassword}
                className="w-full text-right mt-4 text-sm text-muted-foreground hover:text-primary transition-colors"
              >
                Esqueceu a senha?
              </button>
            </CardContent>
          </Card>
        </div>
      </div>

      <p className="text-center mt-8 text-xs text-muted-foreground/50">
        © {new Date().getFullYear()} Empresa de Testes. Todos os direitos reservados
      </p>

          </div>
  );
}
