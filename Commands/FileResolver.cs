using Spectre.Console;

namespace JInspect.Commands;

public static class FileResolver
{
    public static string? Resolve(string filePath, bool fuzzy, IAnsiConsole console)
    {
        if (File.Exists(filePath))
            return Path.GetFullPath(filePath);

        if (!fuzzy)
            return null;

        var matches = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), $"{filePath}*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 1)
            return matches[0];

        if (matches.Count > 1)
        {
            console.MarkupLine($"[yellow]Multiple matches for '{Markup.Escape(filePath)}':[/]");
            foreach (var m in matches.Take(10))
                console.MarkupLine($"  {Markup.Escape(Path.GetRelativePath(Directory.GetCurrentDirectory(), m))}");
            return matches[0];
        }

        return null;
    }
}
