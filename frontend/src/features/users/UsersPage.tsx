import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ShieldOff, UserPlus } from 'lucide-react';
import { api, getProblemMessage } from '../../services/api';
import type { PagedResult, Role, UserItem } from '../../types';
import { formatDate } from '../../utils/format';

const roles: Role[] = ['ADMINISTRATOR', 'EDITOR', 'VIEWER'];

export function UsersPage() {
  const queryClient = useQueryClient();
  const [message, setMessage] = useState<string | null>(null);
  const [draft, setDraft] = useState({
    userName: '',
    email: '',
    firstName: '',
    lastName: '',
    password: '',
    role: 'VIEWER' as Role
  });

  const usersQuery = useQuery({
    queryKey: ['users'],
    queryFn: async () => (await api.get<PagedResult<UserItem>>('/users')).data
  });

  const createMutation = useMutation({
    mutationFn: async () => api.post('/users', { ...draft, roles: [draft.role] }),
    onSuccess: async () => {
      setDraft({ userName: '', email: '', firstName: '', lastName: '', password: '', role: 'VIEWER' });
      await queryClient.invalidateQueries({ queryKey: ['users'] });
    },
    onError: (error) => setMessage(getProblemMessage(error))
  });

  const statusMutation = useMutation({
    mutationFn: async (user: UserItem) => api.patch(`/users/${user.id}/status`, { isActive: !user.isActive }),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: ['users'] }),
    onError: (error) => setMessage(getProblemMessage(error))
  });

  const roleMutation = useMutation({
    mutationFn: async ({ user, role }: { user: UserItem; role: Role }) => api.post(`/users/${user.id}/roles`, { role }),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: ['users'] }),
    onError: (error) => setMessage(getProblemMessage(error))
  });

  const reset2faMutation = useMutation({
    mutationFn: async (user: UserItem) => api.post(`/users/${user.id}/2fa/disable`),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: ['users'] }),
    onError: (error) => setMessage(getProblemMessage(error))
  });

  return (
    <section className="admin-grid">
      <div className="panel compact-panel">
        <div className="section-heading">
          <div>
            <span className="eyebrow">Administracion</span>
            <h2>Crear usuario</h2>
          </div>
          <UserPlus size={20} />
        </div>
        <div className="form-grid">
          <input placeholder="Usuario" value={draft.userName} onChange={(event) => setDraft({ ...draft, userName: event.target.value })} />
          <input placeholder="Correo" value={draft.email} onChange={(event) => setDraft({ ...draft, email: event.target.value })} />
          <input placeholder="Nombres" value={draft.firstName} onChange={(event) => setDraft({ ...draft, firstName: event.target.value })} />
          <input placeholder="Apellidos" value={draft.lastName} onChange={(event) => setDraft({ ...draft, lastName: event.target.value })} />
          <input placeholder="Contrasena temporal" type="password" value={draft.password} onChange={(event) => setDraft({ ...draft, password: event.target.value })} />
          <select value={draft.role} onChange={(event) => setDraft({ ...draft, role: event.target.value as Role })}>
            {roles.map((role) => <option key={role}>{role}</option>)}
          </select>
          <button className="primary-button" type="button" onClick={() => createMutation.mutate()} disabled={createMutation.isPending}>Crear</button>
        </div>
        {message && <div className="alert error">{message}</div>}
      </div>

      <div className="panel">
        <div className="section-heading">
          <div>
            <span className="eyebrow">Directorio</span>
            <h2>Usuarios</h2>
          </div>
        </div>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Correo</th>
                <th>Rol</th>
                <th>Estado</th>
                <th>2FA</th>
                <th>Ultimo acceso</th>
                <th aria-label="Acciones" />
              </tr>
            </thead>
            <tbody>
              {usersQuery.data?.items.map((user) => (
                <tr key={user.id}>
                  <td><strong>{user.firstName} {user.lastName}</strong><small>{user.userName}</small></td>
                  <td>{user.email}</td>
                  <td>
                    <select value={user.roles[0] ?? 'VIEWER'} onChange={(event) => roleMutation.mutate({ user, role: event.target.value as Role })}>
                      {roles.map((role) => <option key={role}>{role}</option>)}
                    </select>
                  </td>
                  <td><button className="link-button" onClick={() => statusMutation.mutate(user)}>{user.isActive ? 'Activo' : 'Desactivado'}</button></td>
                  <td>{user.twoFactorEnabled ? 'Activo' : 'No configurado'}</td>
                  <td>{formatDate(user.lastLoginAt)}</td>
                  <td>
                    <button className="icon-button" title="Reset 2FA" onClick={() => reset2faMutation.mutate(user)}>
                      <ShieldOff size={17} />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {usersQuery.isLoading && <div className="empty-state">Cargando usuarios...</div>}
        </div>
      </div>
    </section>
  );
}
