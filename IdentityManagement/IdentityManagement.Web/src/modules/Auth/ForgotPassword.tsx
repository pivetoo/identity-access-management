import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Mail, ArrowLeft, KeyRound } from 'lucide-react';
import { Button, Card, CardContent, Input, useToast } from 'd-rts';

export default function ForgotPassword() {
  const [email, setEmail] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [emailSent, setEmailSent] = useState(false);
  const navigate = useNavigate();
  const { toast } = useToast();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!email) {
      toast({
        title: 'Atenção',
        description: 'Por favor, insira seu email',
        variant: 'destructive'
      });
      return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
      toast({
        title: 'Atenção',
        description: 'Por favor, insira um email válido',
        variant: 'destructive'
      });
      return;
    }

    setIsLoading(true);

    try {
      await new Promise(resolve => setTimeout(resolve, 1500));

      setEmailSent(true);
      toast({
        title: 'Sucesso',
        description: 'Email de recuperação enviado com sucesso!'
      });
    } catch (error) {
      toast({
        title: 'Erro',
        description: 'Erro ao enviar email de recuperação',
        variant: 'destructive'
      });
    } finally {
      setIsLoading(false);
    }
  };

  const handleBackToLogin = () => {
    navigate('/');
  };

  const handleResendEmail = () => {
    setEmailSent(false);
    setEmail('');
  };

  return (
    <div className="flex flex-col items-center justify-center min-h-screen w-full bg-background p-8">
      <div className="w-full max-w-[420px]">
        <Card className="border-0 shadow-md">
          <CardContent className="pt-5">
            {!emailSent && (
              <div className="flex justify-center mb-6">
                <div className="w-20 h-20 rounded-full bg-primary/10 flex items-center justify-center">
                  <KeyRound className="h-10 w-10 text-primary" />
                </div>
              </div>
            )}

            {!emailSent ? (
              <>
                <h1 className="text-center text-2xl font-bold text-foreground mb-4">
                  Recuperar Senha
                </h1>
                <p className="text-center text-sm text-muted-foreground mb-6">
                  Digite seu email cadastrado e enviaremos as instruções para redefinir sua senha
                </p>

                <form onSubmit={handleSubmit} className="flex flex-col space-y-4">
                  <div>
                    <label className="text-sm font-medium mb-2 block text-muted-foreground">Email</label>
                    <div className="relative">
                      <Mail className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground opacity-60" />
                      <Input
                        type="email"
                        placeholder="seu.email@empresa.com"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        disabled={isLoading}
                        className="pl-10 bg-white"
                        required
                      />
                    </div>
                  </div>

                  <div className="flex flex-col gap-3 mt-6">
                    <Button
                      type="submit"
                      variant="primary"
                      className="w-full"
                      loading={isLoading}
                    >
                      Enviar Email
                    </Button>

                    <Button
                      type="button"
                      variant="ghost"
                      className="w-full"
                      onClick={handleBackToLogin}
                      disabled={isLoading}
                      icon={<ArrowLeft />}
                      iconPosition="left"
                    >
                      Voltar para o Login
                    </Button>
                  </div>
                </form>
              </>
            ) : (
              <div className="flex flex-col items-center py-6">
                <div className="w-16 h-16 rounded-full bg-primary/10 flex items-center justify-center mb-6">
                  <Mail className="h-8 w-8 text-primary" />
                </div>
                <h2 className="text-2xl font-bold text-foreground mb-4">Email Enviado!</h2>
                <p className="text-center text-sm text-muted-foreground mb-2">
                  Enviamos as instruções de recuperação de senha para o email:
                </p>
                <p className="text-center text-base font-semibold text-foreground mb-4">{email}</p>
                <p className="text-center text-sm text-muted-foreground mb-6">
                  Verifique sua caixa de entrada e siga as instruções no email para criar uma nova senha.
                  O link expira em 24 horas.
                </p>

                <div className="flex flex-col gap-3 w-full">
                  <Button
                    variant="primary"
                    className="w-full"
                    onClick={handleResendEmail}
                  >
                    Reenviar Email
                  </Button>

                  <Button
                    variant="ghost"
                    className="w-full"
                    onClick={handleBackToLogin}
                    icon={<ArrowLeft />}
                    iconPosition="left"
                  >
                    Voltar para o Login
                  </Button>
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      <p className="text-center mt-7 text-sm text-muted-foreground">
        Lembrou sua senha?{' '}
        <button
          onClick={handleBackToLogin}
          className="font-medium text-foreground hover:text-primary transition-colors"
        >
          Faça login
        </button>
      </p>
    </div>
  );
}