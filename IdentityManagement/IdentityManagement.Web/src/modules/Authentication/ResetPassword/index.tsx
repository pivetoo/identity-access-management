import { useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { ArrowLeft, KeyRound, Eye, EyeOff } from 'lucide-react';
import { Button, Card, CardContent, Input, useToast, useI18n } from 'archon-ui';
import { passwordResetService } from '../../../services/passwordResetService';

export default function ResetPassword() {
  const { t } = useI18n()
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''
  const navigate = useNavigate()
  const { toast } = useToast()

  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showNew, setShowNew] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [done, setDone] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (newPassword.length < 6) {
      toast({ variant: 'destructive', title: t('common.toast.warningTitle'), description: t('authentication.login.validation.passwordMinLength') })
      return
    }

    if (newPassword !== confirmPassword) {
      toast({ variant: 'destructive', title: t('common.toast.warningTitle'), description: t('authentication.resetPassword.validation.passwordMismatch') })
      return
    }

    setIsLoading(true)
    try {
      await passwordResetService.resetPassword(token, newPassword)
      setDone(true)
      toast({ variant: 'success', title: t('common.toast.successTitle'), description: t('authentication.resetPassword.toast.success') })
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : t('authentication.resetPassword.toast.error')
      toast({ variant: 'destructive', title: t('common.toast.errorTitle'), description: message })
    } finally {
      setIsLoading(false)
    }
  }

  if (!token) {
    return (
      <div className="flex flex-col items-center justify-center min-h-screen w-full bg-background p-8">
        <div className="w-full max-w-[420px]">
          <Card className="border-0 shadow-md">
            <CardContent className="pt-5 flex flex-col items-center py-8 gap-4">
              <p className="text-center text-muted-foreground">{t('authentication.resetPassword.invalidLink')}</p>
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
      <div className="w-full max-w-[420px]">
        <Card className="border-0 shadow-md">
          <CardContent className="pt-5">
            <div className="flex justify-center mb-6">
              <div className="w-20 h-20 rounded-full bg-primary/10 flex items-center justify-center">
                <KeyRound className="h-10 w-10 text-primary" />
              </div>
            </div>

            {!done ? (
              <>
                <h1 className="text-center text-2xl font-bold text-foreground mb-2">
                  {t('authentication.resetPassword.title')}
                </h1>
                <p className="text-center text-sm text-muted-foreground mb-6">
                  {t('authentication.resetPassword.subtitle')}
                </p>

                <form onSubmit={handleSubmit} className="flex flex-col space-y-4">
                  <div>
                    <label className="text-sm font-medium mb-2 block text-muted-foreground">{t('authentication.resetPassword.newPassword')}</label>
                    <div className="relative">
                      <Input
                        type={showNew ? 'text' : 'password'}
                        placeholder="••••••••"
                        value={newPassword}
                        onChange={(e) => setNewPassword(e.target.value)}
                        disabled={isLoading}
                        className="pr-10"
                      />
                      <button type="button" onClick={() => setShowNew(v => !v)} className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground">
                        {showNew ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                      </button>
                    </div>
                  </div>

                  <div>
                    <label className="text-sm font-medium mb-2 block text-muted-foreground">{t('authentication.resetPassword.confirmPassword')}</label>
                    <div className="relative">
                      <Input
                        type={showConfirm ? 'text' : 'password'}
                        placeholder="••••••••"
                        value={confirmPassword}
                        onChange={(e) => setConfirmPassword(e.target.value)}
                        disabled={isLoading}
                        className="pr-10"
                      />
                      <button type="button" onClick={() => setShowConfirm(v => !v)} className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground">
                        {showConfirm ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                      </button>
                    </div>
                  </div>

                  <div className="flex flex-col gap-3 pt-2">
                    <Button type="submit" variant="primary" className="w-full" loading={isLoading}>
                      {t('authentication.resetPassword.submit')}
                    </Button>
                    <Button type="button" variant="ghost" className="w-full" onClick={() => navigate('/')} disabled={isLoading} icon={<ArrowLeft />} iconPosition="left">
                      {t('authentication.forgotPassword.backToLogin')}
                    </Button>
                  </div>
                </form>
              </>
            ) : (
              <div className="flex flex-col items-center py-4 gap-4">
                <h2 className="text-2xl font-bold text-foreground">{t('authentication.resetPassword.successTitle')}</h2>
                <p className="text-center text-sm text-muted-foreground">{t('authentication.resetPassword.successDescription')}</p>
                <Button variant="primary" className="w-full mt-2" onClick={() => navigate('/')}>
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
