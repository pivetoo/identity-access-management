import { useState, useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { ArrowLeft, ShieldCheck, Eye, EyeOff, Building2, Monitor } from 'lucide-react';
import { Button, Card, CardContent, Input, useToast, useI18n } from 'archon-ui';
import { adminSetupService, type AdminInvitationInfo } from '../../../services/adminSetupService';

export default function SetupAdmin() {
  const { t } = useI18n()
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''
  const navigate = useNavigate()
  const { toast } = useToast()

  const [info, setInfo] = useState<AdminInvitationInfo | null>(null)
  const [validating, setValidating] = useState(true)
  const [tokenInvalid, setTokenInvalid] = useState(false)

  const [mode, setMode] = useState<'new' | 'existing'>('new')

  const [name, setName] = useState('')
  const [username, setUsername] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)

  const [loginIdentifier, setLoginIdentifier] = useState('')
  const [existingPassword, setExistingPassword] = useState('')
  const [showExistingPassword, setShowExistingPassword] = useState(false)

  const [isLoading, setIsLoading] = useState(false)
  const [done, setDone] = useState(false)

  useEffect(() => {
    if (!token) {
      setTokenInvalid(true)
      setValidating(false)
      return
    }

    adminSetupService.validateInvitation(token)
      .then((data) => {
        setInfo(data)
        setEmail(data.companyEmail)
      })
      .catch(() => setTokenInvalid(true))
      .finally(() => setValidating(false))
  }, [token])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (password.length < 6) {
      toast({ variant: 'destructive', title: t('common.toast.warningTitle'), description: t('authentication.login.validation.passwordMinLength') })
      return
    }

    if (password !== confirmPassword) {
      toast({ variant: 'destructive', title: t('common.toast.warningTitle'), description: t('authentication.setupAdmin.validation.passwordMismatch') })
      return
    }

    setIsLoading(true)
    try {
      await adminSetupService.setupAdmin(token, name, username, email, password)
      setDone(true)
      toast({ variant: 'success', title: t('common.toast.successTitle'), description: t('authentication.setupAdmin.toast.success') })
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : t('authentication.setupAdmin.toast.error')
      toast({ variant: 'destructive', title: t('common.toast.errorTitle'), description: message })
    } finally {
      setIsLoading(false)
    }
  }

  const handleSubmitExisting = async (e: React.FormEvent) => {
    e.preventDefault()

    setIsLoading(true)
    try {
      await adminSetupService.setupAdminExistingUser(token, loginIdentifier, existingPassword)
      setDone(true)
      toast({ variant: 'success', title: t('common.toast.successTitle'), description: t('authentication.setupAdmin.toast.success') })
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : t('authentication.setupAdmin.toast.error')
      toast({ variant: 'destructive', title: t('common.toast.errorTitle'), description: message })
    } finally {
      setIsLoading(false)
    }
  }

  if (validating) {
    return (
      <div className="flex items-center justify-center min-h-screen w-full bg-background">
        <div className="h-8 w-8 border-4 border-primary border-t-transparent rounded-full animate-spin" />
      </div>
    )
  }

  if (tokenInvalid || !token) {
    return (
      <div className="flex flex-col items-center justify-center min-h-screen w-full bg-background p-8">
        <div className="w-full max-w-[420px]">
          <Card className="border-0 shadow-md">
            <CardContent className="pt-5 flex flex-col items-center py-8 gap-4">
              <p className="text-center text-muted-foreground">{t('authentication.setupAdmin.invalidLink')}</p>
              <Button variant="ghost" icon={<ArrowLeft />} iconPosition="left" onClick={() => navigate('/')}>
                {t('authentication.forgotPassword.backToLogin')}
              </Button>
            </CardContent>
          </Card>
        </div>
      </div>
    )
  }

  return (
    <div className="flex flex-col items-center justify-center min-h-screen w-full bg-background p-8">
      <div className="w-full max-w-[480px]">
        <Card className="border-0 shadow-md">
          <CardContent className="pt-5">
            <div className="flex justify-center mb-6">
              <div className="w-20 h-20 rounded-full bg-primary/10 flex items-center justify-center">
                <ShieldCheck className="h-10 w-10 text-primary" />
              </div>
            </div>

            {!done ? (
              <>
                <h1 className="text-center text-2xl font-bold text-foreground mb-2">
                  {t('authentication.setupAdmin.title')}
                </h1>
                <p className="text-center text-sm text-muted-foreground mb-6">
                  {t('authentication.setupAdmin.subtitle')}
                </p>

                {info && (
                  <div className="flex flex-col gap-2 mb-6 p-4 rounded-lg bg-primary/5 border border-primary/10">
                    <div className="flex items-center gap-2 text-sm">
                      <Building2 className="h-4 w-4 text-primary shrink-0" />
                      <span className="text-muted-foreground">{t('authentication.setupAdmin.label.company')}:</span>
                      <span className="font-medium text-foreground">{info.companyName}</span>
                    </div>
                    <div className="flex items-start gap-2 text-sm">
                      <Monitor className="h-4 w-4 text-primary shrink-0 mt-0.5" />
                      <span className="text-muted-foreground shrink-0">{t('authentication.setupAdmin.label.system')}:</span>
                      <span className="font-medium text-foreground">
                        {info.systemApplicationNames && info.systemApplicationNames.length > 0
                          ? info.systemApplicationNames.join(', ')
                          : info.systemApplicationName}
                      </span>
                    </div>
                  </div>
                )}

                <div className="flex gap-2 mb-6">
                  <button
                    type="button"
                    onClick={() => setMode('new')}
                    className={`flex-1 py-2 px-4 rounded-md text-sm font-medium transition-colors ${mode === 'new' ? 'bg-primary text-primary-foreground' : 'bg-muted text-muted-foreground hover:bg-muted/80'}`}
                  >
                    Criar nova conta
                  </button>
                  <button
                    type="button"
                    onClick={() => setMode('existing')}
                    className={`flex-1 py-2 px-4 rounded-md text-sm font-medium transition-colors ${mode === 'existing' ? 'bg-primary text-primary-foreground' : 'bg-muted text-muted-foreground hover:bg-muted/80'}`}
                  >
                    Já tenho conta
                  </button>
                </div>

                {mode === 'new' ? (
                  <form onSubmit={handleSubmit} className="flex flex-col space-y-4">
                    <div>
                      <label className="text-sm font-medium mb-2 block text-muted-foreground">{t('authentication.setupAdmin.field.name')}</label>
                      <Input
                        type="text"
                        placeholder="João da Silva"
                        value={name}
                        onChange={(e) => setName(e.target.value)}
                        disabled={isLoading}
                        required
                      />
                    </div>

                    <div>
                      <label className="text-sm font-medium mb-2 block text-muted-foreground">{t('authentication.setupAdmin.field.username')}</label>
                      <Input
                        type="text"
                        placeholder="joaosilva"
                        value={username}
                        onChange={(e) => setUsername(e.target.value)}
                        disabled={isLoading}
                        required
                      />
                    </div>

                    <div>
                      <label className="text-sm font-medium mb-2 block text-muted-foreground">{t('authentication.setupAdmin.field.email')}</label>
                      <Input
                        type="email"
                        placeholder="admin@empresa.com"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        disabled={isLoading}
                        required
                      />
                    </div>

                    <div>
                      <label className="text-sm font-medium mb-2 block text-muted-foreground">{t('authentication.setupAdmin.field.password')}</label>
                      <div className="relative">
                        <Input
                          type={showPassword ? 'text' : 'password'}
                          placeholder="••••••••"
                          value={password}
                          onChange={(e) => setPassword(e.target.value)}
                          disabled={isLoading}
                          className="pr-10"
                          required
                        />
                        <button type="button" onClick={() => setShowPassword(v => !v)} className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground">
                          {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                        </button>
                      </div>
                    </div>

                    <div>
                      <label className="text-sm font-medium mb-2 block text-muted-foreground">{t('authentication.setupAdmin.field.confirmPassword')}</label>
                      <div className="relative">
                        <Input
                          type={showConfirm ? 'text' : 'password'}
                          placeholder="••••••••"
                          value={confirmPassword}
                          onChange={(e) => setConfirmPassword(e.target.value)}
                          disabled={isLoading}
                          className="pr-10"
                          required
                        />
                        <button type="button" onClick={() => setShowConfirm(v => !v)} className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground">
                          {showConfirm ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                        </button>
                      </div>
                    </div>

                    <div className="flex flex-col gap-3 pt-2">
                      <Button type="submit" variant="primary" className="w-full" loading={isLoading}>
                        {t('authentication.setupAdmin.submit')}
                      </Button>
                      <Button type="button" variant="ghost" className="w-full" onClick={() => navigate('/')} disabled={isLoading} icon={<ArrowLeft />} iconPosition="left">
                        {t('authentication.forgotPassword.backToLogin')}
                      </Button>
                    </div>
                  </form>
                ) : (
                  <form onSubmit={handleSubmitExisting} className="flex flex-col space-y-4">
                    <div>
                      <label className="text-sm font-medium mb-2 block text-muted-foreground">Usuário ou e-mail</label>
                      <Input
                        type="text"
                        placeholder="joaosilva ou admin@empresa.com"
                        value={loginIdentifier}
                        onChange={(e) => setLoginIdentifier(e.target.value)}
                        disabled={isLoading}
                        required
                      />
                    </div>

                    <div>
                      <label className="text-sm font-medium mb-2 block text-muted-foreground">Senha</label>
                      <div className="relative">
                        <Input
                          type={showExistingPassword ? 'text' : 'password'}
                          placeholder="••••••••"
                          value={existingPassword}
                          onChange={(e) => setExistingPassword(e.target.value)}
                          disabled={isLoading}
                          className="pr-10"
                          required
                        />
                        <button type="button" onClick={() => setShowExistingPassword(v => !v)} className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground">
                          {showExistingPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                        </button>
                      </div>
                    </div>

                    <div className="flex flex-col gap-3 pt-2">
                      <Button type="submit" variant="primary" className="w-full" loading={isLoading}>
                        Vincular minha conta
                      </Button>
                      <Button type="button" variant="ghost" className="w-full" onClick={() => navigate('/')} disabled={isLoading} icon={<ArrowLeft />} iconPosition="left">
                        {t('authentication.forgotPassword.backToLogin')}
                      </Button>
                    </div>
                  </form>
                )}
              </>
            ) : (
              <div className="flex flex-col items-center py-4 gap-4">
                <h2 className="text-2xl font-bold text-foreground">{t('authentication.setupAdmin.successTitle')}</h2>
                <p className="text-center text-sm text-muted-foreground">{t('authentication.setupAdmin.successDescription')}</p>
                <Button variant="primary" className="w-full mt-2" onClick={() => { window.location.href = 'https://agencias.mainstay.com.br' }}>
                  {t('authentication.login.submit')}
                </Button>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
