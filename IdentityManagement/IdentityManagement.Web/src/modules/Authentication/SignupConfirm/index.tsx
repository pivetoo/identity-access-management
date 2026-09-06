import { useEffect, useRef, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { ArrowLeft, Loader2, TriangleAlert } from 'lucide-react';
import { Button, Card, CardContent } from 'archon-ui';
import { signupService } from '../../../services/signupService';
import { trackEvent } from '../../../services/analytics';
import logoEmpresa from '../../../assets/logo-empresa.png';

/**
 * Confirmacao do cadastro publico — a etapa que efetivamente provisiona.
 *
 * O clique neste link e que autoriza criar empresa, contratos e os bancos de tenant. Por isso a tela
 * dispara sozinha ao abrir e leva alguns segundos: nao ha nada para o usuario preencher aqui, e a
 * espera e o onboarding rodando de verdade.
 *
 * Terminando bem, emenda direto no /setup-admin com o token devolvido, para a pessoa nao precisar
 * voltar ao e-mail so para definir a senha.
 */

const PASSOS = [
  'Confirmando seu e-mail...',
  'Criando o ambiente da sua agência...',
  'Preparando os módulos...',
  'Quase lá...',
];

export default function SignupConfirm() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') ?? '';
  const navigate = useNavigate();

  const [erro, setErro] = useState<string | null>(null);
  const [passo, setPasso] = useState(0);

  // O StrictMode roda o efeito duas vezes em desenvolvimento. O backend recusa a segunda chamada
  // (o cadastro ja foi consumido), o que apareceria como erro numa confirmacao bem sucedida.
  const jaDisparou = useRef(false);

  useEffect(() => {
    if (jaDisparou.current) {
      return;
    }
    jaDisparou.current = true;

    if (!token) {
      setErro('Link de confirmação inválido. Abra o link direto do e-mail que enviamos.');
      return;
    }

    signupService.confirm(token)
      .then((resultado) => {
        trackEvent('cadastro-confirmado', { plano: resultado.planName });
        navigate(`/setup-admin?token=${encodeURIComponent(resultado.setupToken)}`, { replace: true });
      })
      .catch((error) => {
        setErro(error instanceof Error ? error.message : 'Não foi possível confirmar seu e-mail.');
      });
  }, [token, navigate]);

  // Provisionar dois bancos leva alguns segundos; um spinner parado desse tempo todo passa a
  // sensacao de travado.
  useEffect(() => {
    if (erro) {
      return;
    }

    const intervalo = setInterval(() => {
      setPasso((atual) => (atual < PASSOS.length - 1 ? atual + 1 : atual));
    }, 2500);

    return () => clearInterval(intervalo);
  }, [erro]);

  return (
    <div className="flex flex-col items-center justify-center min-h-screen w-full bg-background p-8">
      <div className="w-full max-w-[480px]">
        <Card className="border-0 shadow-md">
          <CardContent className="pt-5 pb-8">
            <div className="flex justify-center mb-6">
              <div className="w-20 h-20 rounded-full bg-white ring-1 ring-border flex items-center justify-center">
                <img src={logoEmpresa} alt="Mainstay" className="h-11 w-11 object-contain" />
              </div>
            </div>

            {erro ? (
              <>
                <div className="flex justify-center mb-4">
                  <TriangleAlert className="h-8 w-8 text-warning" />
                </div>
                <h1 className="text-center text-2xl font-bold text-foreground mb-2">Não deu para confirmar</h1>
                <p className="text-center text-sm text-muted-foreground">{erro}</p>

                <div className="mt-6 flex flex-col gap-2">
                  <Button fullWidth onClick={() => navigate('/signup')}>Fazer o cadastro de novo</Button>
                  <Button variant="ghost" fullWidth icon={<ArrowLeft />} iconPosition="left" onClick={() => navigate('/login')}>
                    Ir para o acesso
                  </Button>
                </div>
              </>
            ) : (
              <>
                <div className="flex justify-center mb-4">
                  <Loader2 className="h-8 w-8 animate-spin text-primary" />
                </div>
                <h1 className="text-center text-2xl font-bold text-foreground mb-2">Criando sua conta</h1>
                <p className="text-center text-sm text-muted-foreground">{PASSOS[passo]}</p>
                <p className="mt-6 text-center text-xs text-muted-foreground">
                  Isso leva alguns segundos. Não feche esta página.
                </p>
              </>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
