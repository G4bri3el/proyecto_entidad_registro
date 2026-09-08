import { AlertTriangle } from 'lucide-react';
import { useState } from 'react';

interface DeleteConfirmationModalProps {
  title: string;
  entityName: string;
  confirmationText: string;
  isOpen: boolean;
  isPending?: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}

export function DeleteConfirmationModal({
  title,
  entityName,
  confirmationText,
  isOpen,
  isPending,
  onCancel,
  onConfirm
}: DeleteConfirmationModalProps) {
  const [value, setValue] = useState('');
  if (!isOpen) return null;

  const matches = value === confirmationText;

  return (
    <div className="modal-backdrop" role="presentation">
      <div className="modal" role="dialog" aria-modal="true" aria-labelledby="delete-title">
        <div className="modal-icon danger"><AlertTriangle size={22} /></div>
        <h2 id="delete-title">{title}</h2>
        <p>Esta accion enviara <strong>{entityName}</strong> a la papelera.</p>
        <label>
          Escriba {confirmationText} para continuar
          <input value={value} onChange={(event) => setValue(event.target.value)} autoFocus />
        </label>
        <div className="modal-actions">
          <button className="secondary-button" type="button" onClick={onCancel}>Cancelar</button>
          <button className="danger-button" type="button" disabled={!matches || isPending} onClick={onConfirm}>Eliminar</button>
        </div>
      </div>
    </div>
  );
}
