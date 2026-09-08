import { Upload } from 'lucide-react';
import { ChangeEvent, DragEvent, useRef, useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api, getProblemMessage } from '../../services/api';

const allowedTypes = ['application/pdf', 'image/jpeg', 'image/png'];
const maxBytes = 25 * 1024 * 1024;

export function UploadDropzone({ folderId }: { folderId: string | null }) {
  const inputRef = useRef<HTMLInputElement | null>(null);
  const queryClient = useQueryClient();
  const [progress, setProgress] = useState<number | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const uploadMutation = useMutation({
    mutationFn: async (file: File) => {
      if (!folderId) throw new Error('Seleccione una carpeta.');
      validateFile(file);
      const form = new FormData();
      form.append('file', file);
      await api.post(`/folders/${folderId}/documents`, form, {
        headers: { 'Content-Type': 'multipart/form-data' },
        onUploadProgress: (event) => {
          if (event.total) setProgress(Math.round((event.loaded / event.total) * 100));
        }
      });
    },
    onSuccess: async () => {
      setMessage('Archivo cargado correctamente.');
      setProgress(null);
      await queryClient.invalidateQueries({ queryKey: ['documents', folderId] });
    },
    onError: (error) => {
      setProgress(null);
      setMessage(getProblemMessage(error));
    }
  });

  function pickFile(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (file) uploadMutation.mutate(file);
    event.target.value = '';
  }

  function dropFile(event: DragEvent<HTMLDivElement>) {
    event.preventDefault();
    const file = event.dataTransfer.files[0];
    if (file) uploadMutation.mutate(file);
  }

  return (
    <div className={`upload-zone ${!folderId ? 'disabled' : ''}`} onDrop={dropFile} onDragOver={(event) => event.preventDefault()}>
      <input ref={inputRef} type="file" accept=".pdf,.jpg,.jpeg,.png" hidden onChange={pickFile} />
      <Upload size={22} />
      <div>
        <strong>Subir archivo</strong>
        <span>PDF, JPG o PNG hasta 25 MB</span>
      </div>
      <button className="secondary-button" type="button" disabled={!folderId || uploadMutation.isPending} onClick={() => inputRef.current?.click()}>
        Seleccionar
      </button>
      {progress !== null && <progress value={progress} max={100} />}
      {message && <small>{message}</small>}
    </div>
  );
}

function validateFile(file: File) {
  if (!allowedTypes.includes(file.type)) {
    throw new Error('Tipo de archivo no permitido.');
  }

  if (file.size > maxBytes) {
    throw new Error('El archivo supera el tamano maximo permitido.');
  }
}
