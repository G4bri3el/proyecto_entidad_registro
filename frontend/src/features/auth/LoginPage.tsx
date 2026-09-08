import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { KeyRound, ShieldCheck } from 'lucide-react';
import { z } from 'zod';
import { getProblemMessage } from '../../services/api';
import { useAuth } from './AuthProvider';

const loginSchema = z.object({
  usernameOrEmail: z.string().min(3, 'Ingrese su usuario o correo.'),
  password: z.string().min(1, 'Ingrese su contrasena.')
});

const twoFactorSchema = z.object({
  code: z.string().min(6, 'Ingrese el codigo de 6 digitos.')
});

type LoginForm = z.infer<typeof loginSchema>;
type TwoFactorForm = z.infer<typeof twoFactorSchema>;

export function LoginPage() {
  const { login, verifyTwoFactor } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [twoFactorToken, setTwoFactorToken] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const from = (location.state as { from?: { pathname?: string } } | null)?.from?.pathname ?? '/documents';

  const loginForm = useForm<LoginForm>({
    resolver: zodResolver(loginSchema),
    defaultValues: { usernameOrEmail: '', password: '' }
  });

  const twoFactorForm = useForm<TwoFactorForm>({
    resolver: zodResolver(twoFactorSchema),
    defaultValues: { code: '' }
  });

  async function submitLogin(values: LoginForm) {
    setError(null);
    try {
      const response = await login(values.usernameOrEmail, values.password);
      if (response.requiresTwoFactor && response.twoFactorToken) {
        setTwoFactorToken(response.twoFactorToken);
        return;
      }

      navigate(from, { replace: true });
    } catch (requestError) {
      setError(getProblemMessage(requestError));
    }
  }

  async function submitTwoFactor(values: TwoFactorForm) {
    if (!twoFactorToken) return;
    setError(null);
    try {
      await verifyTwoFactor(twoFactorToken, values.code);
      navigate(from, { replace: true });
    } catch (requestError) {
      setError(getProblemMessage(requestError));
    }
  }

  return (
    <div className="login-shell">
      <section className="login-panel">
        <div className="login-copy">
          <div className="brand-mark large"><ShieldCheck size={28} /></div>
          <h1>Gestion documental segura</h1>
          <p>Acceso controlado, trazabilidad completa y documentos servidos siempre por API autenticada.</p>
        </div>
        {!twoFactorToken ? (
          <form className="form-panel" onSubmit={loginForm.handleSubmit(submitLogin)}>
            <div>
              <span className="eyebrow">Acceso</span>
              <h2>Ingrese sus credenciales</h2>
            </div>
            <label>
              Usuario o correo
              <input autoComplete="username" {...loginForm.register('usernameOrEmail')} />
              <small>{loginForm.formState.errors.usernameOrEmail?.message}</small>
            </label>
            <label>
              Contrasena
              <input type="password" autoComplete="current-password" {...loginForm.register('password')} />
              <small>{loginForm.formState.errors.password?.message}</small>
            </label>
            {error && <div className="alert error">{error}</div>}
            <button className="primary-button" disabled={loginForm.formState.isSubmitting}>
              <KeyRound size={18} /> Iniciar sesion
            </button>
          </form>
        ) : (
          <form className="form-panel" onSubmit={twoFactorForm.handleSubmit(submitTwoFactor)}>
            <div>
              <span className="eyebrow">Doble factor</span>
              <h2>Codigo de autenticador</h2>
            </div>
            <label>
              Codigo
              <input inputMode="numeric" autoComplete="one-time-code" {...twoFactorForm.register('code')} />
              <small>{twoFactorForm.formState.errors.code?.message}</small>
            </label>
            {error && <div className="alert error">{error}</div>}
            <button className="primary-button" disabled={twoFactorForm.formState.isSubmitting}>
              <ShieldCheck size={18} /> Verificar
            </button>
          </form>
        )}
      </section>
    </div>
  );
}
