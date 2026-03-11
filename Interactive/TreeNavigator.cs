using JInspect.Schema;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace JInspect.Interactive;

public class TreeNavigator
{
    private readonly IAnsiConsole _console;
    private readonly SchemaNode _root;
    private readonly bool _rootIsArray;
    private readonly int _rootArrayLength;
    private readonly List<FlatNode> _visible = [];
    private readonly HashSet<FlatNode> _selected = [];
    private readonly HashSet<FlatNode> _expanded = [];
    private int _cursor;
    private int _scrollOffset;

    public TreeNavigator(SchemaNode root, bool rootIsArray, int rootArrayLength, IAnsiConsole? console = null)
    {
        _root = root;
        _rootIsArray = rootIsArray;
        _rootArrayLength = rootArrayLength;
        _console = console ?? AnsiConsole.Console;
    }

    public List<SelectedPath> Run()
    {
        RebuildVisible();

        _console.Live(Render())
            .AutoClear(true)
            .Overflow(VerticalOverflow.Ellipsis)
            .Start(ctx =>
            {
                ctx.UpdateTarget(Render());

                while (true)
                {
                    var key = Console.ReadKey(intercept: true);

                    switch (key.Key)
                    {
                        case ConsoleKey.UpArrow or ConsoleKey.K:
                            if (_cursor > 0) _cursor--;
                            break;

                        case ConsoleKey.DownArrow or ConsoleKey.J:
                            if (_cursor < _visible.Count - 1) _cursor++;
                            break;

                        case ConsoleKey.RightArrow or ConsoleKey.L:
                            ExpandCurrent();
                            break;

                        case ConsoleKey.LeftArrow or ConsoleKey.H:
                            CollapseCurrent();
                            break;

                        case ConsoleKey.Spacebar:
                            ToggleSelect();
                            break;

                        case ConsoleKey.Enter when key.Modifiers == 0:
                            if (HasChildren(Current))
                                ExpandCurrent();
                            else
                                ToggleSelect();
                            break;

                        case ConsoleKey.A:
                            SelectAll();
                            break;

                        case ConsoleKey.N:
                            ClearAll();
                            break;

                        case ConsoleKey.Escape or ConsoleKey.Q:
                            return;
                    }

                    EnsureCursorVisible();
                    ctx.UpdateTarget(Render());
                }
            });

        return _selected
            .Select(n => n.ToSelectedPath(_rootIsArray))
            .OrderBy(p => p.JqPath)
            .ToList();
    }

    private FlatNode Current => _visible[_cursor];

    private void ExpandCurrent()
    {
        var node = Current;
        if (HasChildren(node) && _expanded.Add(node))
            RebuildVisible();
    }

    private void CollapseCurrent()
    {
        var node = Current;
        if (_expanded.Remove(node))
        {
            RebuildVisible();
        }
        else if (node.Parent is not null)
        {
            _expanded.Remove(node.Parent);
            RebuildVisible();
            _cursor = _visible.IndexOf(node.Parent);
            if (_cursor < 0) _cursor = 0;
        }
    }

    private void ToggleSelect()
    {
        var node = Current;
        if (!_selected.Remove(node))
            _selected.Add(node);
    }

    private void SelectAll()
    {
        foreach (var node in _visible.Where(n => !HasChildren(n)))
            _selected.Add(node);
    }

    private void ClearAll()
    {
        _selected.Clear();
    }

    private static bool HasChildren(FlatNode node) =>
        node.Schema.Children.Count > 0;

    private void RebuildVisible()
    {
        var oldCurrent = _visible.Count > 0 && _cursor < _visible.Count ? _visible[_cursor] : null;
        _visible.Clear();
        Flatten(_root, 0, [], null);

        if (oldCurrent is not null)
        {
            var idx = _visible.IndexOf(oldCurrent);
            if (idx >= 0) _cursor = idx;
        }

        if (_cursor >= _visible.Count)
            _cursor = Math.Max(0, _visible.Count - 1);
    }

    private void Flatten(SchemaNode node, int depth, List<string> pathSegments, FlatNode? parent)
    {
        foreach (var (name, child) in node.Children.OrderByDescending(c => c.Value.SeenCount))
        {
            var segments = new List<string>(pathSegments) { name };
            var flat = new FlatNode(name, child, depth, segments, parent);
            _visible.Add(flat);

            if (_expanded.Contains(flat) && child.Children.Count > 0)
                Flatten(child, depth + 1, segments, flat);
        }
    }

    private void EnsureCursorVisible()
    {
        var maxVisible = Console.WindowHeight - 8;
        if (maxVisible < 5) maxVisible = 5;

        if (_cursor < _scrollOffset)
            _scrollOffset = _cursor;
        else if (_cursor >= _scrollOffset + maxVisible)
            _scrollOffset = _cursor - maxVisible + 1;
    }

    private IRenderable Render()
    {
        var maxVisible = Console.WindowHeight - 8;
        if (maxVisible < 5) maxVisible = 5;

        var rows = new List<IRenderable>();

        rows.Add(new Markup("[dim]↑↓ navigate  →/Enter expand  ← collapse  Space select  a all  n none  Esc done[/]"));
        rows.Add(Text.Empty);

        var end = Math.Min(_scrollOffset + maxVisible, _visible.Count);
        for (var i = _scrollOffset; i < end; i++)
        {
            var node = _visible[i];
            var isCursor = i == _cursor;
            var isSelected = _selected.Contains(node);
            var hasKids = HasChildren(node);
            var isExpanded = _expanded.Contains(node);

            var indent = new string(' ', node.Depth * 2);
            var checkbox = isSelected ? "[green]■[/]" : "[dim]□[/]";
            var expandIcon = hasKids
                ? (isExpanded ? "[dim]▼[/]" : "[dim]►[/]")
                : " ";

            var types = string.Join("|", node.Schema.ObservedTypes);
            var color = node.Schema.ObservedTypes.Contains("Object") ? "cyan"
                : node.Schema.ObservedTypes.Contains("Array") ? "yellow"
                : "green";

            var meta = BuildMeta(node);
            var cursorMark = isCursor ? "[bold white on blue]" : "";
            var cursorEnd = isCursor ? "[/]" : "";

            var displayName = node.Name == "[item]" ? "[]" : Markup.Escape(node.Name);

            rows.Add(new Markup(
                $"{indent}{checkbox} {expandIcon} {cursorMark}[{color}]{displayName}[/]: [{color}]{types}[/]{meta}{cursorEnd}"));
        }

        if (_visible.Count > maxVisible)
        {
            rows.Add(Text.Empty);
            rows.Add(new Markup($"[dim]({_scrollOffset + 1}-{end} of {_visible.Count})[/]"));
        }

        rows.Add(Text.Empty);

        var selectedNames = _selected
            .Select(n => n.ToSelectedPath(_rootIsArray).DisplayName)
            .OrderBy(n => n)
            .ToList();

        if (selectedNames.Count > 0)
            rows.Add(new Markup($"[bold]selected:[/] [green]{Markup.Escape(string.Join(", ", selectedNames))}[/]"));
        else
            rows.Add(new Markup("[dim]no fields selected[/]"));

        return new Rows(rows);
    }

    private static string BuildMeta(FlatNode node)
    {
        var parts = new List<string>();

        if (node.Schema.ObservedTypes.Contains("Array"))
            parts.Add($"len:{node.Schema.MinArrayLength}-{node.Schema.MaxArrayLength}");

        if (node.Schema.SampleValues.Count > 0
            && !node.Schema.ObservedTypes.Contains("Object")
            && !node.Schema.ObservedTypes.Contains("Array"))
        {
            var vals = node.Schema.SampleValues.Take(3).Select(Markup.Escape);
            parts.Add($"e.g. {string.Join(", ", vals)}");
        }

        return parts.Count > 0 ? $" [dim]{string.Join("  ", parts)}[/]" : "";
    }

    internal sealed class FlatNode : IEquatable<FlatNode>
    {
        public string Name { get; }
        public SchemaNode Schema { get; }
        public int Depth { get; }
        public List<string> PathSegments { get; }
        public FlatNode? Parent { get; }
        private readonly string _identity;

        public FlatNode(string name, SchemaNode schema, int depth, List<string> pathSegments, FlatNode? parent)
        {
            Name = name;
            Schema = schema;
            Depth = depth;
            PathSegments = pathSegments;
            Parent = parent;
            _identity = string.Join(".", pathSegments);
        }

        public SelectedPath ToSelectedPath(bool rootIsArray)
        {
            var jqParts = new List<string>();
            var displayParts = new List<string>();

            foreach (var seg in PathSegments)
            {
                if (seg == "[item]")
                {
                    jqParts.Add("[]");
                    displayParts.Add("[]");
                }
                else
                {
                    jqParts.Add($".{seg}");
                    displayParts.Add(seg);
                }
            }

            var jqPath = string.Join("", jqParts);
            var displayName = string.Join(".", displayParts).Replace(".[]", "[]");

            return new SelectedPath(jqPath, displayName, PathSegments);
        }

        public bool Equals(FlatNode? other) => other is not null && _identity == other._identity;
        public override bool Equals(object? obj) => obj is FlatNode other && Equals(other);
        public override int GetHashCode() => _identity.GetHashCode();
    }
}

public record SelectedPath(string JqPath, string DisplayName, List<string> PathSegments);
