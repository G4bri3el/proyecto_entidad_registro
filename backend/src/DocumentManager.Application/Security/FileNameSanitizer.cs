using System.Text;

namespace DocumentManager.Application.Security;

public static class FileNameSanitizer
{
    private const int MaxLength = 180;

    public static string Sanitize(string fileName)
    {
        var name = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(name))
        {
            return "documento";
        }

        var builder = new StringBuilder(name.Length);
        foreach (var character in name)
        {
            if (char.IsControl(character) || Path.GetInvalidFileNameChars().Contains(character))
            {
                builder.Append('_');
                continue;
            }

            builder.Append(character);
        }

        var sanitized = builder.ToString().Trim();
        if (sanitized.Length == 0)
        {
            sanitized = "documento";
        }

        return sanitized.Length <= MaxLength ? sanitized : sanitized[..MaxLength];
    }
}
