using JInspect.Interactive;

namespace JInspect.Query;

public static class JqQueryBuilder
{
    public static string Build(List<SelectedPath> selections, bool rootIsArray)
    {
        if (selections.Count == 0)
            return rootIsArray ? ".[]" : ".";

        var prefix = rootIsArray ? ".[] | " : "";

        if (selections.Count == 1)
        {
            var path = selections[0].JqPath;
            return $"{prefix}{path} // empty";
        }

        var fields = selections.Select(BuildFieldExpression).ToList();
        var projection = $"{{{string.Join(", ", fields)}}}";
        var nullCheck = BuildNullCheck(selections);

        return $"{prefix}{projection}{nullCheck}";
    }

    private static string BuildFieldExpression(SelectedPath selection)
    {
        var segments = selection.PathSegments;
        var leafName = segments.Last();

        if (leafName == "[item]")
            leafName = segments.Count > 1 ? segments[^2] : "items";

        var needsRename = segments.Count > 1 || leafName == "[item]";

        if (!needsRename)
            return leafName;

        return $"{leafName}: {selection.JqPath}";
    }

    private static string BuildNullCheck(List<SelectedPath> selections)
    {
        var checks = selections.Select(s =>
        {
            var leaf = s.PathSegments.Last();
            if (leaf == "[item]")
                leaf = s.PathSegments.Count > 1 ? s.PathSegments[^2] : "items";
            return $".{leaf}";
        }).ToList();

        if (checks.Count == 1)
            return $" | select({checks[0]} != null)";

        return $" | select(any({string.Join(", ", checks)}; . != null))";
    }
}
