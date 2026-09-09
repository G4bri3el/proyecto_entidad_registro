namespace DocumentManager.Application.Configuration;

public static class DotEnvLoader
{
    public static void LoadNearest(params string[] startDirectories)
    {
        foreach (var path in FindNearestDotEnvFiles(startDirectories))
        {
            LoadFile(path);
        }
    }

    private static IEnumerable<string> FindNearestDotEnvFiles(IEnumerable<string> startDirectories)
    {
        var visitedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var yieldedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = startDirectories
            .Append(Directory.GetCurrentDirectory())
            .Append(AppContext.BaseDirectory);

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var fullCandidate = Path.GetFullPath(candidate);
            var directory = File.Exists(fullCandidate)
                ? Path.GetDirectoryName(fullCandidate)
                : fullCandidate;

            while (!string.IsNullOrWhiteSpace(directory) && visitedDirectories.Add(directory))
            {
                var envPath = Path.Combine(directory, ".env");
                if (File.Exists(envPath) && yieldedFiles.Add(envPath))
                {
                    yield return envPath;
                }

                directory = Directory.GetParent(directory)?.FullName;
            }
        }
    }

    private static void LoadFile(string path)
    {
        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith("export ", StringComparison.Ordinal))
            {
                line = line[7..].TrimStart();
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            if (!IsValidKey(key) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                continue;
            }

            var value = Unquote(line[(separatorIndex + 1)..].Trim());
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static string Unquote(string value)
    {
        if (value.Length < 2)
        {
            return value;
        }

        var quote = value[0];
        if ((quote != '\'' && quote != '"') || value[^1] != quote)
        {
            return value;
        }

        value = value[1..^1];
        return quote == '"'
            ? value.Replace("\\n", "\n", StringComparison.Ordinal)
                .Replace("\\r", "\r", StringComparison.Ordinal)
                .Replace("\\t", "\t", StringComparison.Ordinal)
                .Replace("\\\"", "\"", StringComparison.Ordinal)
                .Replace("\\\\", "\\", StringComparison.Ordinal)
            : value;
    }

    private static bool IsValidKey(string key) =>
        key.Length > 0 && key.All(character => char.IsLetterOrDigit(character) || character is '_' or ':');
}