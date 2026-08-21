import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, Building2, CheckCircle2, MailCheck } from 'lucide-react';
import { Button, Card, CardContent, Input, useToast } from 'archon-ui';
import { signupService, formatDocument, isValidDocument, type SignupResult } from '../../../services/signupService';
import logoEmpresa from '../../../assets/logo-empresa.png';

/**
 * Cadastro publico da agencia.
 *
 * O que a tela NAO faz, de proposito: nao escolhe plano por id nem sistemas a contratar — os dois
 * sao resolvidos no servidor. Aqui o usuario so decide mensal ou anual. Senha tambem nao se define
 * aqui: quem cria o administrador e o link enviado por e-mail, que e o que prova o endereco.
 */
export default function Signup() {
  const navigate = useNavigate();
  const { toast } = useToast();

  const [legalName, setLegalName] = useState('');
  const [tradeName, setTradeName] = useState('');
  const [document, setDocument] = useState('');
  const [email, setEmail] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [annual, setAnnual] = useState(false);
  const [acceptedTerms, setAcceptedTerms] = useState(false);

  const [isLoading, setIsLoading] = useState(false);
  const [result, setResult] = useState<SignupResult | null>(null);
  const [website, setWebsite] = useState('');

  const documentTouched = document.replace(/\D/g, '').length >= 14;
  const documentInvalid = documentTouched && !isValidDocument(document);

  const canSubmit = useMemo(() => {
    return legalName.trim().length >= 3
      && tradeName.trim().length >= 2
      && isValidDocument(document)
      && /.+@.+\..+/.test(email.trim())
      && acceptedTerms
      && !isLoading;
  }, [legalName, tradeName, document, email, acceptedTerms, isLoading]);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!canSubmit) return;

    setIsLoading(true);
    try {
      const created = await signupService.signup({
        legalName: legalName.trim(),
        tradeName: tradeName.trim(),
        document,
        email: email.trim(),
        phoneNumber: phoneNumber.trim() || undefined,
        annual,
        acceptedTerms,
        website,
      });
      setResult(created);
    } catch (failure: unknown) {
      const message = failure instanceof Error ? failure.message : 'Não foi possível concluir o cadastro.';
      toast({ variant: 'destructive', title: 'Não foi possível concluir', description: message });
    } finally {
      setIsLoading(false);
    }
  };

  if (result) {
    return (
      <div className="flex flex-col items-center justify-center min-h-screen w-full bg-background p-8">
        <div className="w-full max-w-[480px]">
          <Card className="border-0 shadow-md">
            <CardContent className="pt-5 pb-8">
              <div className="flex justify-center mb-6">
                <div className="w-20 h-20 rounded-full bg-primary/10 flex items-center justify-center">
                  <MailCheck className="h-10 w-10 text-primary" />
                </div>
              </div>

              <h1 className="text-center text-2xl font-bold text-foreground mb-2">Confirme seu e-mail</h1>
              <p className="text-center text-sm text-muted-foreground">
                Enviamos um link de confirmação para <span className="font-medium text-foreground">{result.email}</span>.
                Clique nele para criar o ambiente de <span className="font-medium text-foreground">{result.companyName}</span>.
              </p>

              <div className="mt-6 rounded-lg border border-primary/10 bg-primary/5 p-4 flex flex-col gap-2 text-sm">
                <div className="flex items-center gap-2">
                  <CheckCircle2 className="h-4 w-4 text-primary shrink-0" />
                  <span className="text-muted-foreground">Plano:</span>
                  <span className="font-medium text-foreground">{result.planName}</span>
                </div>
                <div className="flex items-center gap-2">
                  <CheckCircle2 className="h-4 w-4 text-primary shrink-0" />
                  <span className="text-muted-foreground">O link vale até:</span>
                  <span className="font-medium text-foreground">
                    {new Date(result.verificationExpiresAt).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })}
                  </span>
                </div>
              </div>

              <p className="mt-4 text-center text-xs leading-relaxed text-muted-foreground">
                Até você confirmar, nada foi criado e nada será cobrado. Não recebeu? Verifique o spam
                ou faça o cadastro de novo depois que o link expirar.
              </p>

              <Button variant="ghost" className="mt-6" fullWidth icon={<ArrowLeft />} iconPosition="left" onClick={() => navigate('/login')}>
                Ir para o acesso
              </Button>
            </CardContent>
          </Card>
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col items-center justify-center min-h-screen w-full bg-background p-8">
      <div className="w-full max-w-[520px]">
        <Card className="border-0 shadow-md">
          <CardContent className="pt-5">
            <div className="flex justify-center mb-6">
              {/* Placa branca sob a marca, como no AdministrationLayout: o logo e navy sobre fundo
                  transparente e sumiria no tema escuro. */}
              <div className="w-20 h-20 rounded-full bg-white ring-1 ring-border flex items-center justify-center">
                <img src={logoEmpresa} alt="Mainstay" className="h-11 w-11 object-contain" />
              </div>
            </div>

            <h1 className="text-center text-2xl font-bold text-foreground mb-2">Criar conta da agência</h1>
            <p className="text-center text-sm text-muted-foreground mb-6">
              14 dias grátis, sem cartão de crédito. O link para criar seu acesso vai por e-mail.
            </p>

            <form onSubmit={handleSubmit} className="flex flex-col space-y-4">
              {/* Isca: pessoa nao ve, robo ingenuo preenche. Fora da tela em vez de display:none,
                  que alguns robos detectam. */}
              <div className="absolute -left-[9999px]" aria-hidden="true">
                <label>Website<input type="text" tabIndex={-1} autoComplete="off" value={website} onChange={(e) => setWebsite(e.target.value)} /></label>
              </div>

              <div className="flex flex-col gap-1.5">
                <label htmlFor="legalName" className="text-sm font-medium text-foreground">Razão social</label>
                <Input id="legalName" value={legalName} onChange={(e) => setLegalName(e.target.value)} placeholder="Agência Exemplo LTDA" autoComplete="organization" required />
              </div>

              <div className="flex flex-col gap-1.5">
                <label htmlFor="tradeName" className="text-sm font-medium text-foreground">Nome da agência</label>
                <Input id="tradeName" value={tradeName} onChange={(e) => setTradeName(e.target.value)} placeholder="Agência Exemplo" required />
              </div>

              <div className="flex flex-col gap-1.5">
                <label htmlFor="document" className="text-sm font-medium text-foreground">CNPJ</label>
                <Input
                  id="document"
                  value={document}
                  onChange={(e) => setDocument(formatDocument(e.target.value))}
                  placeholder="00.000.000/0001-00"
                  inputMode="numeric"
                  aria-invalid={documentInvalid}
                  aria-describedby={documentInvalid ? 'document-error' : undefined}
                  required
                />
                {documentInvalid && (
                  <p id="document-error" className="text-xs text-destructive">CNPJ inválido. Confira os números.</p>
                )}
              </div>

              <div className="flex flex-col gap-1.5">
                <label htmlFor="email" className="text-sm font-medium text-foreground">E-mail do administrador</label>
                <Input id="email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="voce@suaagencia.com.br" autoComplete="email" required />
                <p className="text-xs text-muted-foreground">É para onde vai o link de acesso.</p>
              </div>

              <div className="flex flex-col gap-1.5">
                <label htmlFor="phoneNumber" className="text-sm font-medium text-foreground">Telefone <span className="text-muted-foreground font-normal">(opcional)</span></label>
                <Input id="phoneNumber" value={phoneNumber} onChange={(e) => setPhoneNumber(e.target.value)} placeholder="(11) 90000-0000" autoComplete="tel" />
              </div>

              <fieldset className="flex flex-col gap-2">
                <legend className="text-sm font-medium text-foreground mb-1">Plano Completo</legend>
                <div className="grid grid-cols-2 gap-2">
                  <button
                    type="button"
                    onClick={() => setAnnual(false)}
                    aria-pressed={!annual}
                    className={`flex flex-col items-start rounded-md border px-4 py-3 text-left transition-colors ${!annual ? 'border-primary bg-primary/10' : 'border-border hover:bg-muted'}`}
                  >
                    <span className={`text-sm font-medium ${!annual ? 'text-primary' : 'text-foreground'}`}>Mensal</span>
                    <span className="text-xs text-muted-foreground">R$ 497 por mês</span>
                  </button>
                  <button
                    type="button"
                    onClick={() => setAnnual(true)}
                    aria-pressed={annual}
                    className={`flex flex-col items-start rounded-md border px-4 py-3 text-left transition-colors ${annual ? 'border-primary bg-primary/10' : 'border-border hover:bg-muted'}`}
                  >
                    <span className={`text-sm font-medium ${annual ? 'text-primary' : 'text-foreground'}`}>Anual</span>
                    <span className="text-xs text-muted-foreground">R$ 4.970 por ano (2 meses grátis)</span>
                  </button>
                </div>
                <p className="text-xs text-muted-foreground">
                  Tudo incluído, sem limite de creators. A cobrança só começa depois dos 14 dias.
                </p>
              </fieldset>

              <label className="flex items-start gap-2.5 text-sm text-muted-foreground cursor-pointer">
                <input
                  type="checkbox"
                  checked={acceptedTerms}
                  onChange={(e) => setAcceptedTerms(e.target.checked)}
                  className="mt-0.5 h-4 w-4 shrink-0 rounded border-border accent-[hsl(var(--primary))]"
                  required
                />
                <span>
                  Li e aceito os{' '}
                  <a href="https://mainstay.com.br/termos" target="_blank" rel="noopener noreferrer" className="font-medium text-primary underline underline-offset-2">termos de uso</a>
                  {' '}e a{' '}
                  <a href="https://mainstay.com.br/privacidade" target="_blank" rel="noopener noreferrer" className="font-medium text-primary underline underline-offset-2">política de privacidade</a>.
                </span>
              </label>

              <Button type="submit" fullWidth icon={<Building2 />} iconPosition="left" disabled={!canSubmit} loading={isLoading}>
                Criar conta
              </Button>
            </form>

            <div className="mt-6 border-t border-border pt-4 text-center">
              <p className="text-sm text-muted-foreground">
                Já tem conta?{' '}
                <button type="button" onClick={() => navigate('/login')} className="font-medium text-primary hover:underline">
                  Entrar
                </button>
              </p>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
