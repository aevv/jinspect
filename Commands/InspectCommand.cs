using System.ComponentModel;
using System.Text.Json;
using JInspect.Interactive;
using JInspect.Query;
using JInspect.Schema;
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

    [CommandOption("-q|--query")]
    [Description("Interactively build a jq query from the inferred schema")]
    public bool Query { get; set; }
}

public class InspectCommand(IAnsiConsole console) : Command<InspectSettings>
{
    public InspectCommand() : this(AnsiConsole.Console) { }

    public override int Execute(CommandContext context, InspectSettings settings, CancellationToken ct) =>
        Run(settings);

    public int Run(InspectSettings settings)
    {
        var output = settings.Query
            ? AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(Console.Error) })
            : console;

        var filePath = FileResolver.Resolve(settings.FilePath, settings.Fuzzy, output);
        if (filePath is null)
        {
            output.MarkupLine($"[red]No file found matching: {Markup.Escape(settings.FilePath)}[/]");
            return 1;
        }

        output.MarkupLine($"[bold]Analyzing: {Markup.Escape(Path.GetRelativePath(Directory.GetCurrentDirectory(), filePath))}[/]");

        JsonDocument doc;
        try
        {
            using var stream = File.OpenRead(filePath);
            doc = JsonDocument.Parse(stream);
        }
        catch (JsonException ex)
        {
            output.MarkupLine($"[red]Invalid JSON: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }
        catch (IOException ex)
        {
            output.MarkupLine($"[red]Could not read file: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }

        using (doc)
        {
            var root = doc.RootElement;
            var rootIsArray = root.ValueKind == JsonValueKind.Array;
            var rootArrayLength = rootIsArray ? root.GetArrayLength() : 1;

            var schema = new SchemaNode();

            if (rootIsArray)
            {
                var length = root.GetArrayLength();
                output.MarkupLine($"Root: Array with [green]{length}[/] elements");
                output.MarkupLine($"Sampling [cyan]{Math.Min(settings.SampleSize, length)}[/] elements\n");

                var count = 0;
                foreach (var element in root.EnumerateArray())
                {
                    if (count >= settings.SampleSize) break;
                    SchemaInferrer.MergeSchema(schema, element, 0, settings.MaxDepth, settings.InnerSampleSize);
                    count++;
                }
            }
            else
            {
                output.MarkupLine($"Root: {root.ValueKind}\n");
                SchemaInferrer.MergeSchema(schema, root, 0, settings.MaxDepth, settings.InnerSampleSize);
            }

            if (settings.Query)
            {
                var navigator = new TreeNavigator(schema, rootIsArray, rootArrayLength, output);
                var selections = navigator.Run();
                var query = JqQueryBuilder.Build(selections, rootIsArray);

                var escapedQuery = query.Replace("'", "'\\''");
                var quotedPath = filePath.Contains(' ') ? $"'{filePath}'" : filePath;
                var fullCommand = $"jq '{escapedQuery}' {quotedPath}";

                Console.Out.Write(fullCommand);
            }
            else
            {
                RenderSchema(schema, "", rootIsArray ? root.GetArrayLength() : 1);
            }
        }

        return 0;
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
}
