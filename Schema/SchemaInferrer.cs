using System.Text.Json;

namespace JInspect.Schema;

public static class SchemaInferrer
{
    public static void MergeSchema(SchemaNode node, JsonElement element, int depth, int maxDepth, int innerSampleSize)
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
}
