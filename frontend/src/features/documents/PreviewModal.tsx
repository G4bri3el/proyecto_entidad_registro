import { X } from 'lucide-react';
import { Document, Page } from 'react-pdf';
import type { DocumentItem } from '../../types';

interface PreviewModalProps {
  document: DocumentItem | null;
  url: string | null;
  onClose: () => void;
}

export function PreviewModal({ document, url, onClose }: PreviewModalProps) {
  if (!document || !url) return null;
  const isPdf = document.mimeType === 'application/pdf';
  const isImage = document.mimeType.startsWith('image/');

  return (
    <div className="modal-backdrop preview-backdrop">
      <div className="preview-modal" role="dialog" aria-modal="true">
        <header>
          <div>
            <span className="eyebrow">Vista previa</span>
            <h2>{document.originalFileName}</h2>
          </div>
          <button className="icon-button" type="button" onClick={onClose} title="Cerrar"><X size={18} /></button>
        </header>
        <div className="preview-body">
          {isPdf && (
            <Document file={url} loading={<div className="empty-state">Cargando PDF...</div>}>
              <Page pageNumber={1} width={760} />
            </Document>
          )}
          {isImage && <img src={url} alt={document.originalFileName} />}
        </div>
      </div>
    </div>
  );
}
