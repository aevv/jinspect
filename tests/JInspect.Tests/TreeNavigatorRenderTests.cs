using JInspect.Interactive;
using JInspect.Schema;
using Spectre.Console;
using Xunit;

namespace JInspect.Tests;

public class TreeNavigatorRenderTests
{
    private static TreeNavigator.FlatNode MakeNode(
        string name,
        int depth = 0,
        List<string>? types = null,
        Dictionary<string, SchemaNode>? children = null,
        HashSet<string>? sampleValues = null,
        int minArrayLength = 0,
        int maxArrayLength = 0,
        TreeNavigator.FlatNode? parent = null)
    {
        var schema = new SchemaNode
        {
            ObservedTypes = types ?? ["String"],
            Children = children ?? [],
            SampleValues = sampleValues ?? [],
            MinArrayLength = minArrayLength,
            MaxArrayLength = maxArrayLength,
            SeenCount = 1
        };

        var segments = new List<string> { name };
        return new TreeNavigator.FlatNode(name, schema, depth, segments, parent);
    }

    [Fact]
    public void ArrayItemNode_ProducesValidMarkup()
    {
        var node = MakeNode("[item]", types: ["Array"],
            children: new() { ["value"] = new SchemaNode() },
            minArrayLength: 1, maxArrayLength: 10);

        var markup = TreeNavigator.BuildRowMarkup(node, isCursor: false, isSelected: false, hasChildren: true, isExpanded: false);

        new Markup(markup);
    }

    [Fact]
    public void ArrayItemNode_WithCursor_ProducesValidMarkup()
    {
        var node = MakeNode("[item]", types: ["Array"],
            children: new() { ["value"] = new SchemaNode() },
            minArrayLength: 5, maxArrayLength: 100);

        var markup = TreeNavigator.BuildRowMarkup(node, isCursor: true, isSelected: false, hasChildren: true, isExpanded: true);

        new Markup(markup);
    }

    [Fact]
    public void ArrayItemNode_SelectedWithCursor_ProducesValidMarkup()
    {
        var node = MakeNode("[item]", types: ["Array"],
            children: new() { ["value"] = new SchemaNode() },
            minArrayLength: 0, maxArrayLength: 7100);

        var markup = TreeNavigator.BuildRowMarkup(node, isCursor: true, isSelected: true, hasChildren: true, isExpanded: false);

        new Markup(markup);
    }

    [Fact]
    public void SimpleStringField_ProducesValidMarkup()
    {
        var node = MakeNode("name", types: ["String"], sampleValues: ["Alice", "Bob"]);

        var markup = TreeNavigator.BuildRowMarkup(node, isCursor: false, isSelected: false, hasChildren: false, isExpanded: false);

        new Markup(markup);
    }

    [Fact]
    public void SimpleStringField_WithCursor_ProducesValidMarkup()
    {
        var node = MakeNode("name", types: ["String"], sampleValues: ["Alice"]);

        var markup = TreeNavigator.BuildRowMarkup(node, isCursor: true, isSelected: true, hasChildren: false, isExpanded: false);

        new Markup(markup);
    }

    [Fact]
    public void ObjectField_Expanded_ProducesValidMarkup()
    {
        var node = MakeNode("address", types: ["Object"],
            children: new() { ["city"] = new SchemaNode() });

        var markup = TreeNavigator.BuildRowMarkup(node, isCursor: false, isSelected: false, hasChildren: true, isExpanded: true);

        new Markup(markup);
    }

    [Fact]
    public void NestedNode_ProducesValidMarkup()
    {
        var parentNode = MakeNode("items", depth: 0, types: ["Array"],
            children: new() { ["[item]"] = new SchemaNode() },
            minArrayLength: 1, maxArrayLength: 50);

        var childNode = MakeNode("[item]", depth: 1, types: ["Object"],
            children: new() { ["price"] = new SchemaNode() },
            parent: parentNode);

        var markup = TreeNavigator.BuildRowMarkup(childNode, isCursor: true, isSelected: false, hasChildren: true, isExpanded: false);

        new Markup(markup);
    }

    [Fact]
    public void FieldNameWithBrackets_ProducesValidMarkup()
    {
        var node = MakeNode("data[0]", types: ["String"]);

        var markup = TreeNavigator.BuildRowMarkup(node, isCursor: true, isSelected: false, hasChildren: false, isExpanded: false);

        new Markup(markup);
    }

    [Fact]
    public void MixedTypes_ProducesValidMarkup()
    {
        var node = MakeNode("value", types: ["String", "Integer", "Null"]);

        var markup = TreeNavigator.BuildRowMarkup(node, isCursor: true, isSelected: true, hasChildren: false, isExpanded: false);

        new Markup(markup);
    }

    [Fact]
    public void SampleValuesWithSpecialChars_ProducesValidMarkup()
    {
        var node = MakeNode("description", types: ["String"],
            sampleValues: ["hello [world]", "foo & bar", "a<b>c"]);

        var markup = TreeNavigator.BuildRowMarkup(node, isCursor: true, isSelected: false, hasChildren: false, isExpanded: false);

        new Markup(markup);
    }

    [Fact]
    public void ArrayItemNode_DisplaysAsBrackets()
    {
        var node = MakeNode("[item]", types: ["Object"],
            children: new() { ["id"] = new SchemaNode() });

        var markup = TreeNavigator.BuildRowMarkup(node, isCursor: false, isSelected: false, hasChildren: true, isExpanded: false);

        Assert.Contains("[[]]", markup);
        Assert.DoesNotContain("[item]", markup);
    }
}
