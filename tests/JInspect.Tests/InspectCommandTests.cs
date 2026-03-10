using JInspect.Commands;
using Spectre.Console.Testing;
using Xunit;

namespace JInspect.Tests;

public sealed class InspectCommandTests : IDisposable
{
    private readonly TestConsole _console = new();

    public void Dispose() => _console.Dispose();

    private int Run(string filePath, int sampleSize = 50, int maxDepth = 10, bool fuzzy = false, int innerSampleSize = 5)
    {
        var command = new InspectCommand(_console);
        var settings = new InspectSettings
        {
            FilePath = filePath,
            SampleSize = sampleSize,
            MaxDepth = maxDepth,
            Fuzzy = fuzzy,
            InnerSampleSize = innerSampleSize
        };
        return command.Run(settings);
    }

    private string FixturePath(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

    [Fact]
    public void SimpleObject_ShowsAllFields()
    {
        var exit = Run(FixturePath("simple-object.json"));

        Assert.Equal(0, exit);
        var output = _console.Output;
        Assert.Contains("Root: Object", output);
        Assert.Contains("name", output);
        Assert.Contains("age", output);
        Assert.Contains("active", output);
        Assert.Contains("String", output);
        Assert.Contains("Integer", output);
        Assert.Contains("Boolean", output);
    }

    [Fact]
    public void SimpleArray_ShowsElementCountAndFields()
    {
        var exit = Run(FixturePath("simple-array.json"));

        Assert.Equal(0, exit);
        var output = _console.Output;
        Assert.Contains("Array with", output);
        Assert.Contains("3", output);
        Assert.Contains("id", output);
        Assert.Contains("name", output);
        Assert.Contains("score", output);
        Assert.Contains("Decimal", output);
    }

    [Fact]
    public void NestedObjects_ShowsNestedProperties()
    {
        var exit = Run(FixturePath("nested-objects.json"));

        Assert.Equal(0, exit);
        var output = _console.Output;
        Assert.Contains("user", output);
        Assert.Contains("Object", output);
        Assert.Contains("address", output);
        Assert.Contains("city", output);
        Assert.Contains("postcode", output);
        Assert.Contains("tags", output);
        Assert.Contains("Array", output);
    }

    [Fact]
    public void MixedTypes_ShowsMultipleObservedTypes()
    {
        var exit = Run(FixturePath("mixed-types.json"));

        Assert.Equal(0, exit);
        var output = _console.Output;
        Assert.Contains("String", output);
        Assert.Contains("Integer", output);
        Assert.Contains("Null", output);
    }

    [Fact]
    public void SparseFields_ShowsPresenceCounts()
    {
        var exit = Run(FixturePath("sparse-fields.json"));

        Assert.Equal(0, exit);
        var output = _console.Output;
        Assert.Contains("id", output);
        Assert.Contains("name", output);
        Assert.Contains("email", output);
        Assert.Contains("phone", output);
        Assert.Contains("(2/4)", output);
    }

    [Fact]
    public void EmptyArray_ShowsZeroElements()
    {
        var exit = Run(FixturePath("empty-array.json"));

        Assert.Equal(0, exit);
        var output = _console.Output;
        Assert.Contains("Array with", output);
        Assert.Contains("0", output);
    }

    [Fact]
    public void DeeplyNested_TraversesFullDepth()
    {
        var exit = Run(FixturePath("deeply-nested.json"));

        Assert.Equal(0, exit);
        var output = _console.Output;
        Assert.Contains("level1", output);
        Assert.Contains("level2", output);
        Assert.Contains("level3", output);
        Assert.Contains("level4", output);
        Assert.Contains("level5", output);
        Assert.Contains("value", output);
    }

    [Fact]
    public void DeeplyNested_MaxDepthTruncates()
    {
        var exit = Run(FixturePath("deeply-nested.json"), maxDepth: 3);

        Assert.Equal(0, exit);
        var output = _console.Output;
        Assert.Contains("level1", output);
        Assert.Contains("level2", output);
        Assert.Contains("level3", output);
        Assert.DoesNotContain("level5", output);
    }

    [Fact]
    public void ArraysOfArrays_ShowsNestedArraySchema()
    {
        var exit = Run(FixturePath("arrays-of-arrays.json"));

        Assert.Equal(0, exit);
        var output = _console.Output;
        Assert.Contains("matrix", output);
        Assert.Contains("Array", output);
        Assert.Contains("[item]", output);
        Assert.Contains("len:", output);
    }

    [Fact]
    public void LargeArray_DefaultSampleLimitsTo50()
    {
        var exit = Run(FixturePath("large-array.json"));

        Assert.Equal(0, exit);
        var output = _console.Output;
        Assert.Contains("200", output);
        Assert.Contains("Sampling", output);
        Assert.Contains("50", output);
        Assert.Contains("id", output);
        Assert.Contains("name", output);
        Assert.Contains("active", output);
        Assert.Contains("optional_field", output);
        Assert.Contains("rare_field", output);
    }

    [Fact]
    public void LargeArray_CustomSampleSize()
    {
        var exit = Run(FixturePath("large-array.json"), sampleSize: 5);

        Assert.Equal(0, exit);
        var output = _console.Output;
        Assert.Contains("Sampling", output);
        Assert.Contains("5", output);
    }

    [Fact]
    public void ScalarRoot_HandlesNonObjectNonArray()
    {
        var exit = Run(FixturePath("scalar-root.json"));

        Assert.Equal(0, exit);
        var output = _console.Output;
        Assert.Contains("Root: String", output);
    }

    [Fact]
    public void MissingFile_ReturnsExitCode1()
    {
        var exit = Run("nonexistent-file-that-does-not-exist.json");

        Assert.Equal(1, exit);
        var output = _console.Output;
        Assert.Contains("No file found", output);
    }

    [Fact]
    public void SimpleArray_ShowsSampleValues()
    {
        var exit = Run(FixturePath("simple-array.json"));

        Assert.Equal(0, exit);
        var output = _console.Output;
        Assert.Contains("e.g.", output);
        Assert.Contains("Alice", output);
    }

    [Fact]
    public void NestedObjects_ArrayShowsLengthRange()
    {
        var exit = Run(FixturePath("nested-objects.json"));

        Assert.Equal(0, exit);
        var output = _console.Output;
        Assert.Contains("len:1-2", output);
    }

    [Fact]
    public void InvalidJson_ReturnsExitCode1WithMessage()
    {
        var path = Path.Combine(Path.GetTempPath(), $"jinspect-test-{Guid.NewGuid()}.json");
        File.WriteAllText(path, "not valid json {{{");
        try
        {
            var exit = Run(path);
            Assert.Equal(1, exit);
            Assert.Contains("Invalid JSON", _console.Output);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void MissingFileWithoutFuzzy_ReturnsExitCode1()
    {
        var exit = Run("nonexistent", fuzzy: false);

        Assert.Equal(1, exit);
        Assert.Contains("No file found", _console.Output);
    }
}
