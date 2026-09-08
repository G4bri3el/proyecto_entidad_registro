import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ArchiveRestore, Trash2 } from 'lucide-react';
import { api, getProblemMessage } from '../../services/api';
import type { DocumentItem, Folder, PagedResult } from '../../types';
import { formatBytes, formatDate } from '../../utils/format';
import { DeleteConfirmationModal } from '../documents/DeleteConfirmationModal';

export function TrashPage() {
  const queryClient = useQueryClient();
  const [target, setTarget] = useState<DocumentItem | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const documentsQuery = useQuery({
    queryKey: ['trash-documents'],
    queryFn: async () => (await api.get<PagedResult<DocumentItem>>('/trash/documents')).data
  });

  const foldersQuery = useQuery({
    queryKey: ['trash-folders'],
    queryFn: async () => (await api.get<PagedResult<Folder>>('/trash/folders')).data
  });

  const restoreMutation = useMutation({
    mutationFn: async (document: DocumentItem) => api.post(`/documents/${document.id}/restore`),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['trash-documents'] });
      await queryClient.invalidateQueries({ queryKey: ['documents'] });
    },
    onError: (error) => setMessage(getProblemMessage(error))
  });

  const permanentMutation = useMutation({
    mutationFn: async (document: DocumentItem) => api.delete(`/documents/${document.id}/permanent`, { data: { confirmation: 'ELIMINAR DEFINITIVAMENTE' } }),
    onSuccess: async () => {
      setTarget(null);
      await queryClient.invalidateQueries({ queryKey: ['trash-documents'] });
    },
    onError: (error) => setMessage(getProblemMessage(error))
  });

  return (
    <section className="admin-grid">
      <div className="panel">
        <div className="section-heading">
          <div>
            <span className="eyebrow">Papelera</span>
            <h2>Documentos eliminados</h2>
          </div>
        </div>
        {message && <div className="alert error">{message}</div>}
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Archivo</th>
                <th>Tamano</th>
                <th>Eliminado</th>
                <th aria-label="Acciones" />
              </tr>
            </thead>
            <tbody>
              {documentsQuery.data?.items.map((document) => (
                <tr key={document.id}>
                  <td><strong>{document.originalFileName}</strong></td>
                  <td>{formatBytes(document.size)}</td>
                  <td>{formatDate(document.deletedAt)}</td>
                  <td className="actions-cell">
                    <button className="icon-button" title="Restaurar" onClick={() => restoreMutation.mutate(document)}><ArchiveRestore size={17} /></button>
                    <button className="icon-button danger-text" title="Eliminar definitivamente" onClick={() => setTarget(document)}><Trash2 size={17} /></button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {!documentsQuery.isLoading && documentsQuery.data?.items.length === 0 && <div className="empty-state">No hay documentos en papelera.</div>}
        </div>
      </div>
      <div className="panel compact-panel">
        <div className="section-heading">
          <div>
            <span className="eyebrow">Carpetas</span>
            <h2>Eliminadas</h2>
          </div>
        </div>
        <div className="folder-list">
          {foldersQuery.data?.items.map((folder) => (
            <div key={folder.id} className="folder-row">
              <strong>{folder.name}</strong>
              <span>{formatDate(folder.createdAt)}</span>
            </div>
          ))}
          {!foldersQuery.isLoading && foldersQuery.data?.items.length === 0 && <div className="empty-state">No hay carpetas eliminadas.</div>}
        </div>
      </div>
      <DeleteConfirmationModal
        title="Eliminar definitivamente"
        entityName={target?.originalFileName ?? ''}
        confirmationText="ELIMINAR DEFINITIVAMENTE"
        isOpen={Boolean(target)}
        isPending={permanentMutation.isPending}
        onCancel={() => setTarget(null)}
        onConfirm={() => target && permanentMutation.mutate(target)}
      />
    </section>
  );
}
