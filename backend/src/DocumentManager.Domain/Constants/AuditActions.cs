namespace DocumentManager.Domain.Constants;

public static class AuditActions
{
    public const string LoginSuccess = "LOGIN_SUCCESS";
    public const string LoginFailed = "LOGIN_FAILED";
    public const string Logout = "LOGOUT";
    public const string TokenRefresh = "TOKEN_REFRESH";
    public const string TokenRevoked = "TOKEN_REVOKED";
    public const string TwoFactorSetup = "2FA_SETUP";
    public const string TwoFactorSuccess = "2FA_SUCCESS";
    public const string TwoFactorFailed = "2FA_FAILED";
    public const string TwoFactorEnabled = "2FA_ENABLED";
    public const string TwoFactorDisabled = "2FA_DISABLED";
    public const string UserCreated = "USER_CREATED";
    public const string UserUpdated = "USER_UPDATED";
    public const string UserDisabled = "USER_DISABLED";
    public const string RoleAssigned = "ROLE_ASSIGNED";
    public const string RoleRemoved = "ROLE_REMOVED";
    public const string FolderCreated = "FOLDER_CREATED";
    public const string FolderRenamed = "FOLDER_RENAMED";
    public const string FolderMoved = "FOLDER_MOVED";
    public const string FolderDeleted = "FOLDER_DELETED";
    public const string FileUploaded = "FILE_UPLOADED";
    public const string FileViewed = "FILE_VIEWED";
    public const string FileDownloaded = "FILE_DOWNLOADED";
    public const string FileDeleted = "FILE_DELETED";
    public const string FileRestored = "FILE_RESTORED";
    public const string FilePermanentlyDeleted = "FILE_PERMANENTLY_DELETED";
}
