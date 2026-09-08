namespace DocumentManager.Domain.Constants;

public static class AppRoles
{
    public const string Administrator = "ADMINISTRATOR";
    public const string Editor = "EDITOR";
    public const string Viewer = "VIEWER";

    public static readonly string[] All = [Administrator, Editor, Viewer];
}
