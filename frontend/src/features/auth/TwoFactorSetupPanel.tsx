import { useState } from 'react';
import { QRCodeSVG } from 'qrcode.react';
import { useMutation } from '@tanstack/react-query';
import { ShieldCheck } from 'lucide-react';
import { api, getProblemMessage } from '../../services/api';

interface SetupResponse {
  manualEntryKey: string;
  otpAuthUri: string;
}

export function TwoFactorSetupPanel({ enabled }: { enabled: boolean }) {
  const [setup, setSetup] = useState<SetupResponse | null>(null);
  const [code, setCode] = useState('');
  const [message, setMessage] = useState<string | null>(null);

  const setupMutation = useMutation({
    mutationFn: async () => (await api.post<SetupResponse>('/auth/2fa/setup')).data,
    onSuccess: setSetup,
    onError: (error) => setMessage(getProblemMessage(error))
  });

  const confirmMutation = useMutation({
    mutationFn: async () => api.post('/auth/2fa/confirm', { code }),
    onSuccess: () => setMessage('2FA activado correctamente.'),
    onError: (error) => setMessage(getProblemMessage(error))
  });

  return (
    <section className="panel compact-panel">
      <div className="section-heading">
        <div>
          <span className="eyebrow">Cuenta</span>
          <h2>Doble factor</h2>
        </div>
        <span className={enabled ? 'status active' : 'status'}>{enabled ? 'Activo' : 'Pendiente'}</span>
      </div>
      {!setup ? (
        <button className="secondary-button" type="button" onClick={() => setupMutation.mutate()} disabled={setupMutation.isPending || enabled}>
          <ShieldCheck size={16} /> Configurar 2FA
        </button>
      ) : (
        <div className="two-factor-grid">
          <QRCodeSVG value={setup.otpAuthUri} size={132} />
          <div>
            <p className="muted">Clave manual</p>
            <code>{setup.manualEntryKey}</code>
            <label>
              Codigo
              <input value={code} onChange={(event) => setCode(event.target.value)} />
            </label>
            <button className="primary-button" type="button" onClick={() => confirmMutation.mutate()} disabled={confirmMutation.isPending || code.length < 6}>
              Confirmar
            </button>
          </div>
        </div>
      )}
      {message && <div className="alert">{message}</div>}
    </section>
  );
}
