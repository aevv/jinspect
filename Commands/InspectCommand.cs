using System.ComponentModel;
using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;

namespace JInspect.Commands;

public class InspectSettings : CommandSettings
{
    [CommandArgument(0, "<FILE>")]
    [Description("Path to the JSON file to inspect")]
    public string FilePath { get; set; } = "";

    [CommandOption("-s|--sample")]
    [Description("Number of top-level array elements to sample")]
    public int SampleSize { get; set; } = 50;

    [CommandOption("-d|--max-depth")]
    [Description("Maximum nesting depth to traverse")]
    public int MaxDepth { get; set; } = 10;

    [CommandOption("--inner-sample")]
    [Description("Number of elements to sample in nested arrays")]
    public int InnerSampleSize { get; set; } = 5;

    [CommandOption("-f|--fuzzy")]
    [Description("Fuzzy-match the filename by searching the current directory recursively")]
    public bool Fuzzy { get; set; }
}

public class InspectCommand(IAnsiConsole console) : Command<InspectSettings>
{
    public InspectCommand() : this(AnsiConsole.Console) { }

    public override int Execute(CommandContext context, InspectSettings settings, CancellationToken ct) =>
        Run(settings);

    public int Run(InspectSettings settings)
    {
        var filePath = ResolveFilePath(settings);
        if (filePath is null)
        {
            console.MarkupLine($"[red]No file found matching: {Markup.Escape(settings.FilePath)}[/]");
            return 1;
        }

        console.MarkupLine($"[bold]Analyzing: {Markup.Escape(Path.GetRelativePath(Directory.GetCurrentDirectory(), filePath))}[/]");

        JsonDocument doc;
        try
        {
            using var stream = File.OpenRead(filePath);
            doc = JsonDocument.Parse(stream);
        }
        catch (JsonException ex)
        {
            console.MarkupLine($"[red]Invalid JSON: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }
        catch (IOException ex)
        {
            console.MarkupLine($"[red]Could not read file: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }

        using (doc)
        {
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                var length = root.GetArrayLength();
                console.MarkupLine($"Root: Array with [green]{length}[/] elements");
                console.MarkupLine($"Sampling [cyan]{Math.Min(settings.SampleSize, length)}[/] elements\n");

                var schema = new SchemaNode();
                var count = 0;
                foreach (var element in root.EnumerateArray())
                {
                    if (count >= settings.SampleSize) break;
                    MergeSchema(schema, element, 0, settings.MaxDepth, settings.InnerSampleSize);
                    count++;
                }

                RenderSchema(schema, "", length);
            }
            else
            {
                console.MarkupLine($"Root: {root.ValueKind}\n");
                var schema = new SchemaNode();
                MergeSchema(schema, root, 0, settings.MaxDepth, settings.InnerSampleSize);
                RenderSchema(schema, "", 1);
            }
        }

        return 0;
    }

    private string? ResolveFilePath(InspectSettings settings)
    {
        if (File.Exists(settings.FilePath))
            return Path.GetFullPath(settings.FilePath);

        if (!settings.Fuzzy)
            return null;

        var matches = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), $"{settings.FilePath}*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 1)
            return matches[0];

        if (matches.Count > 1)
        {
            console.MarkupLine($"[yellow]Multiple matches for '{Markup.Escape(settings.FilePath)}':[/]");
            foreach (var m in matches.Take(10))
                console.MarkupLine($"  {Markup.Escape(Path.GetRelativePath(Directory.GetCurrentDirectory(), m))}");
            return matches[0];
        }

        return null;
    }

    private static void MergeSchema(SchemaNode node, JsonElement element, int depth, int maxDepth, int innerSampleSize)
    {
        node.SeenCount++;

        var kind = element.ValueKind switch
        {
            JsonValueKind.String => "String",
            JsonValueKind.Number => element.TryGetInt64(out _) ? "Integer" : "Decimal",
            JsonValueKind.True or JsonValueKind.False => "Boolean",
            JsonValueKind.Null or JsonValueKind.Undefined => "Null",
            JsonValueKind.Object => "Object",
            JsonValueKind.Array => "Array",
            _ => "Unknown"
        };

        if (!node.ObservedTypes.Contains(kind))
            node.ObservedTypes.Add(kind);

        if (kind is "String")
        {
            var val = element.GetString() ?? "";
            node.SampleValues.Add(val.Length > 80 ? val[..80] + "..." : val);
        }
        else if (kind is "Integer" or "Decimal" or "Boolean")
        {
            node.SampleValues.Add(element.GetRawText());
        }

        if (depth >= maxDepth) return;

        if (kind == "Object")
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (!node.Children.TryGetValue(prop.Name, out var child))
                {
                    child = new SchemaNode();
                    node.Children[prop.Name] = child;
                }
                MergeSchema(child, prop.Value, depth + 1, maxDepth, innerSampleSize);
            }
            node.TotalObjectsSeen++;
        }
        else if (kind == "Array")
        {
            var arrayLen = element.GetArrayLength();
            node.MinArrayLength = Math.Min(node.MinArrayLength, arrayLen);
            node.MaxArrayLength = Math.Max(node.MaxArrayLength, arrayLen);

            if (!node.Children.TryGetValue("[item]", out var itemNode))
            {
                itemNode = new SchemaNode();
                node.Children["[item]"] = itemNode;
            }

            var sampled = 0;
            foreach (var item in element.EnumerateArray())
            {
                if (sampled >= innerSampleSize) break;
                MergeSchema(itemNode, item, depth + 1, maxDepth, innerSampleSize);
                sampled++;
            }
        }
    }

    private void RenderSchema(SchemaNode node, string indent, int parentCount)
    {
        if (node.Children.Count == 0) return;

        foreach (var (name, child) in node.Children.OrderByDescending(c => c.Value.SeenCount))
        {
            var types = string.Join("|", child.ObservedTypes);
            var presence = parentCount > 0 && child.SeenCount < parentCount
                ? $" [dim]({child.SeenCount}/{parentCount})[/]"
                : "";

            var arrayInfo = "";
            if (child.ObservedTypes.Contains("Array"))
                arrayInfo = $" [dim]len:{child.MinArrayLength}-{child.MaxArrayLength}[/]";

            var samples = "";
            if (child.SampleValues.Count > 0 && !child.ObservedTypes.Contains("Object") && !child.ObservedTypes.Contains("Array"))
            {
                var vals = child.SampleValues.Take(3).Select(v => Markup.Escape(v));
                samples = $" [dim]e.g. {string.Join(", ", vals)}[/]";
            }

            var color = child.ObservedTypes.Contains("Object") ? "cyan"
                : child.ObservedTypes.Contains("Array") ? "yellow"
                : "green";

            console.MarkupLine($"{indent}[{color}]{Markup.Escape(name)}[/]: [{color}]{types}[/]{presence}{arrayInfo}{samples}");

            if (child.Children.Count > 0)
            {
                RenderSchema(child, indent + "  ", child.TotalObjectsSeen > 0 ? child.TotalObjectsSeen : child.SeenCount);
            }
        }
    }

    internal class SchemaNode
    {
        public int SeenCount { get; set; }
        public int TotalObjectsSeen { get; set; }
        public List<string> ObservedTypes { get; set; } = [];
        public Dictionary<string, SchemaNode> Children { get; set; } = [];
        public HashSet<string> SampleValues { get; set; } = [];
        public int MinArrayLength { get; set; } = int.MaxValue;
        public int MaxArrayLength { get; set; }
    }
}
