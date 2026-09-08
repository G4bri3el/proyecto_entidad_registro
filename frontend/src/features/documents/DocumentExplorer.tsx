import { useEffect, useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Download, Eye, FolderPlus, Trash2 } from 'lucide-react';
import { api, getProblemMessage } from '../../services/api';
import type { DocumentItem, Folder, PagedResult } from '../../types';
import { formatBytes, formatDate, hasRole } from '../../utils/format';
import { useAuth } from '../auth/AuthProvider';
import { FolderTree } from './FolderTree';
import { UploadDropzone } from './UploadDropzone';
import { PreviewModal } from './PreviewModal';
import { DeleteConfirmationModal } from './DeleteConfirmationModal';

export function DocumentExplorer() {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [selectedFolderId, setSelectedFolderId] = useState<string | null>(null);
  const [preview, setPreview] = useState<{ document: DocumentItem; url: string } | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<DocumentItem | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const canCreateFolder = user ? hasRole(user.roles, ['ADMINISTRATOR', 'EDITOR']) : false;
  const canUpload = canCreateFolder;
  const canDelete = user ? hasRole(user.roles, ['ADMINISTRATOR', 'EDITOR']) : false;

  const foldersQuery = useQuery({
    queryKey: ['folders'],
    queryFn: async () => (await api.get<Folder[]>('/folders/tree')).data
  });

  const selectedFolder = useMemo(() => findFolder(foldersQuery.data ?? [], selectedFolderId), [foldersQuery.data, selectedFolderId]);

  useEffect(() => {
    if (!selectedFolderId && foldersQuery.data?.length) {
      setSelectedFolderId(foldersQuery.data[0].id);
    }
  }, [foldersQuery.data, selectedFolderId]);

  const documentsQuery = useQuery({
    queryKey: ['documents', selectedFolderId],
    enabled: Boolean(selectedFolderId),
    queryFn: async () => (await api.get<PagedResult<DocumentItem>>(`/folders/${selectedFolderId}/documents`)).data
  });

  const createFolderMutation = useMutation({
    mutationFn: async () => {
      const name = window.prompt('Nombre de la carpeta');
      if (!name) return;
      await api.post('/folders', { name, parentFolderId: selectedFolderId });
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['folders'] });
    },
    onError: (error) => setMessage(getProblemMessage(error))
  });

  const deleteMutation = useMutation({
    mutationFn: async (document: DocumentItem) => api.delete(`/documents/${document.id}`, { data: { confirmation: 'ELIMINAR' } }),
    onSuccess: async () => {
      setDeleteTarget(null);
      await queryClient.invalidateQueries({ queryKey: ['documents', selectedFolderId] });
    },
    onError: (error) => setMessage(getProblemMessage(error))
  });

  async function openPreview(document: DocumentItem) {
    try {
      const response = await api.get(`/documents/${document.id}/content`, { responseType: 'blob' });
      setPreview({ document, url: URL.createObjectURL(response.data) });
    } catch (error) {
      setMessage(getProblemMessage(error));
    }
  }

  async function download(document: DocumentItem) {
    try {
      const response = await api.get(`/documents/${document.id}/download`, { responseType: 'blob' });
      const url = URL.createObjectURL(response.data);
      const link = window.document.createElement('a');
      link.href = url;
      link.download = document.originalFileName;
      link.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      setMessage(getProblemMessage(error));
    }
  }

  function closePreview() {
    if (preview?.url) URL.revokeObjectURL(preview.url);
    setPreview(null);
  }

  const breadcrumbs = selectedFolder ? buildBreadcrumbs(foldersQuery.data ?? [], selectedFolder.id) : [];

  return (
    <section className="document-workspace">
      <aside className="panel tree-panel">
        <div className="section-heading">
          <div>
            <span className="eyebrow">Repositorio</span>
            <h2>Carpetas</h2>
          </div>
          {canCreateFolder && (
            <button className="icon-button" type="button" onClick={() => createFolderMutation.mutate()} title="Nueva carpeta">
              <FolderPlus size={18} />
            </button>
          )}
        </div>
        {foldersQuery.isLoading && <div className="empty-state">Cargando carpetas...</div>}
        {!foldersQuery.isLoading && foldersQuery.data?.length === 0 && (
          <div className="empty-state">
            <p>No hay carpetas.</p>
            {canCreateFolder && <button className="primary-button" onClick={() => createFolderMutation.mutate()}>Crear raiz</button>}
          </div>
        )}
        {foldersQuery.data && <FolderTree folders={foldersQuery.data} selectedId={selectedFolderId} onSelect={(folder) => setSelectedFolderId(folder.id)} />}
      </aside>

      <div className="panel file-panel">
        <div className="section-heading">
          <div>
            <span className="eyebrow">Carpeta actual</span>
            <h2>{selectedFolder?.name ?? 'Seleccione una carpeta'}</h2>
            <div className="breadcrumbs">{breadcrumbs.map((item) => <span key={item.id}>{item.name}</span>)}</div>
          </div>
        </div>

        {canUpload && <UploadDropzone folderId={selectedFolderId} />}
        {message && <div className="alert error">{message}</div>}

        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Archivo</th>
                <th>Tipo</th>
                <th>Tamano</th>
                <th>Fecha</th>
                <th aria-label="Acciones" />
              </tr>
            </thead>
            <tbody>
              {documentsQuery.data?.items.map((document) => (
                <tr key={document.id}>
                  <td>
                    <strong>{document.originalFileName}</strong>
                    <small>{document.sha256.slice(0, 16)}...</small>
                  </td>
                  <td>{document.extension.toUpperCase()}</td>
                  <td>{formatBytes(document.size)}</td>
                  <td>{formatDate(document.uploadedAt)}</td>
                  <td className="actions-cell">
                    <button className="icon-button" onClick={() => openPreview(document)} title="Vista previa"><Eye size={17} /></button>
                    <button className="icon-button" onClick={() => download(document)} title="Descargar"><Download size={17} /></button>
                    {canDelete && <button className="icon-button danger-text" onClick={() => setDeleteTarget(document)} title="Eliminar"><Trash2 size={17} /></button>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {documentsQuery.isLoading && <div className="empty-state">Cargando documentos...</div>}
          {!documentsQuery.isLoading && selectedFolderId && documentsQuery.data?.items.length === 0 && <div className="empty-state">La carpeta no tiene documentos.</div>}
        </div>
      </div>

      <PreviewModal document={preview?.document ?? null} url={preview?.url ?? null} onClose={closePreview} />
      <DeleteConfirmationModal
        title="Eliminar documento"
        entityName={deleteTarget?.originalFileName ?? ''}
        confirmationText="ELIMINAR"
        isOpen={Boolean(deleteTarget)}
        isPending={deleteMutation.isPending}
        onCancel={() => setDeleteTarget(null)}
        onConfirm={() => deleteTarget && deleteMutation.mutate(deleteTarget)}
      />
    </section>
  );
}

function findFolder(folders: Folder[], id: string | null): Folder | null {
  if (!id) return null;
  for (const folder of folders) {
    if (folder.id === id) return folder;
    const child = findFolder(folder.children, id);
    if (child) return child;
  }
  return null;
}

function buildBreadcrumbs(folders: Folder[], id: string) {
  const path: Folder[] = [];
  function walk(items: Folder[], target: string): boolean {
    for (const folder of items) {
      path.push(folder);
      if (folder.id === target || walk(folder.children, target)) return true;
      path.pop();
    }
    return false;
  }
  walk(folders, id);
  return path;
}
