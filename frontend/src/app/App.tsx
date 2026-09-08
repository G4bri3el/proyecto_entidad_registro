import { Navigate, Route, Routes } from 'react-router-dom';
import { ProtectedRoute } from './ProtectedRoute';
import { MainLayout } from '../layouts/MainLayout';
import { LoginPage } from '../features/auth/LoginPage';
import { DocumentExplorer } from '../features/documents/DocumentExplorer';
import { UsersPage } from '../features/users/UsersPage';
import { AuditPage } from '../features/audit/AuditPage';
import { TrashPage } from '../features/trash/TrashPage';

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<MainLayout />}>
          <Route index element={<Navigate to="/documents" replace />} />
          <Route path="/documents" element={<DocumentExplorer />} />
          <Route element={<ProtectedRoute roles={['ADMINISTRATOR']} />}>
            <Route path="/users" element={<UsersPage />} />
            <Route path="/audit" element={<AuditPage />} />
            <Route path="/trash" element={<TrashPage />} />
          </Route>
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/documents" replace />} />
    </Routes>
  );
}
