export type Role = 'ADMINISTRATOR' | 'EDITOR' | 'VIEWER';

export interface AuthenticatedUser {
  id: string;
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  twoFactorEnabled: boolean;
  roles: Role[];
}

export interface LoginResponse {
  requiresTwoFactor: boolean;
  twoFactorToken?: string;
  accessToken?: string;
  accessTokenExpiresAt?: string;
  user?: AuthenticatedUser;
}

export interface RefreshTokenResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  user: AuthenticatedUser;
}

export interface Folder {
  id: string;
  name: string;
  parentFolderId?: string | null;
  createdAt: string;
  isDeleted: boolean;
  children: Folder[];
}

export interface DocumentItem {
  id: string;
  folderId: string;
  originalFileName: string;
  mimeType: string;
  extension: string;
  size: number;
  sha256: string;
  uploadedByUserId: string;
  uploadedAt: string;
  isDeleted: boolean;
  deletedAt?: string | null;
}

export interface UserItem {
  id: string;
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  twoFactorEnabled: boolean;
  createdAt: string;
  lastLoginAt?: string | null;
  roles: Role[];
}

export interface AuditLog {
  id: string;
  userId?: string | null;
  username?: string | null;
  action: string;
  entityType: string;
  entityId?: string | null;
  description: string;
  ipAddress?: string | null;
  userAgent?: string | null;
  httpMethod?: string | null;
  endpoint?: string | null;
  statusCode?: number | null;
  createdAt: string;
  correlationId: string;
  additionalData?: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}
