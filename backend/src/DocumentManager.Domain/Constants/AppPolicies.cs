namespace DocumentManager.Domain.Constants;

public static class AppPolicies
{
    public const string CanManageUsers = nameof(CanManageUsers);
    public const string CanManageRoles = nameof(CanManageRoles);
    public const string CanCreateFolders = nameof(CanCreateFolders);
    public const string CanRenameFolders = nameof(CanRenameFolders);
    public const string CanMoveFolders = nameof(CanMoveFolders);
    public const string CanDeleteFolders = nameof(CanDeleteFolders);
    public const string CanUploadDocuments = nameof(CanUploadDocuments);
    public const string CanViewDocuments = nameof(CanViewDocuments);
    public const string CanDownloadDocuments = nameof(CanDownloadDocuments);
    public const string CanDeleteDocuments = nameof(CanDeleteDocuments);
    public const string CanRestoreDocuments = nameof(CanRestoreDocuments);
    public const string CanPermanentlyDeleteDocuments = nameof(CanPermanentlyDeleteDocuments);
    public const string CanViewAudit = nameof(CanViewAudit);
}
