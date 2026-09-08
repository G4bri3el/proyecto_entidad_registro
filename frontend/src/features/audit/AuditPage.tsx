import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api } from '../../services/api';
import type { AuditLog, PagedResult } from '../../types';
import { formatDate } from '../../utils/format';

export function AuditPage() {
  const [action, setAction] = useState('');
  const [entityType, setEntityType] = useState('');

  const auditQuery = useQuery({
    queryKey: ['audit', action, entityType],
    queryFn: async () => (await api.get<PagedResult<AuditLog>>('/audit', { params: { action: action || undefined, entityType: entityType || undefined } })).data
  });

  return (
    <section className="panel">
      <div className="section-heading">
        <div>
          <span className="eyebrow">Trazabilidad</span>
          <h2>Auditoria</h2>
        </div>
        <div className="filters">
          <input placeholder="Accion" value={action} onChange={(event) => setAction(event.target.value)} />
          <input placeholder="Entidad" value={entityType} onChange={(event) => setEntityType(event.target.value)} />
        </div>
      </div>
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Usuario</th>
              <th>Accion</th>
              <th>Entidad</th>
              <th>Descripcion</th>
              <th>IP</th>
              <th>Resultado</th>
              <th>CorrelationId</th>
            </tr>
          </thead>
          <tbody>
            {auditQuery.data?.items.map((log) => (
              <tr key={log.id}>
                <td>{formatDate(log.createdAt)}</td>
                <td>{log.username ?? '-'}</td>
                <td><code>{log.action}</code></td>
                <td>{log.entityType}</td>
                <td>{log.description}</td>
                <td>{log.ipAddress ?? '-'}</td>
                <td>{log.statusCode ?? '-'}</td>
                <td><code>{log.correlationId}</code></td>
              </tr>
            ))}
          </tbody>
        </table>
        {auditQuery.isLoading && <div className="empty-state">Cargando auditoria...</div>}
        {!auditQuery.isLoading && auditQuery.data?.items.length === 0 && <div className="empty-state">No hay eventos con esos filtros.</div>}
      </div>
    </section>
  );
}
