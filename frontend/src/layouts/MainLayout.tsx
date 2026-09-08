import { NavLink, Outlet } from 'react-router-dom';
import { ArchiveRestore, FileText, LogOut, ScrollText, Shield, Users } from 'lucide-react';
import { useAuth } from '../features/auth/AuthProvider';
import { hasRole } from '../utils/format';

export function MainLayout() {
  const { user, logout } = useAuth();
  const isAdmin = user ? hasRole(user.roles, ['ADMINISTRATOR']) : false;

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <div className="brand-mark"><Shield size={20} /></div>
          <div>
            <strong>Document Manager</strong>
            <span>Gestion segura</span>
          </div>
        </div>
        <nav className="nav-list">
          <NavLink to="/documents"><FileText size={18} /> Documentos</NavLink>
          {isAdmin && <NavLink to="/users"><Users size={18} /> Usuarios</NavLink>}
          {isAdmin && <NavLink to="/audit"><ScrollText size={18} /> Auditoria</NavLink>}
          {isAdmin && <NavLink to="/trash"><ArchiveRestore size={18} /> Papelera</NavLink>}
        </nav>
      </aside>
      <div className="main-area">
        <header className="topbar">
          <div>
            <span className="eyebrow">Sesion activa</span>
            <h1>{user?.firstName} {user?.lastName}</h1>
          </div>
          <div className="topbar-actions">
            <span className="role-pill">{user?.roles.join(' / ')}</span>
            <button className="icon-button" type="button" onClick={logout} title="Cerrar sesion">
              <LogOut size={18} />
            </button>
          </div>
        </header>
        <main className="content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
