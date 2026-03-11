using JInspect.Interactive;
using JInspect.Query;
using Xunit;

namespace JInspect.Tests;

public class JqQueryBuilderTests
{
    private static SelectedPath Path(string jqPath, string displayName, params string[] segments) =>
        new(jqPath, displayName, segments.ToList());

    [Fact]
    public void NoSelections_RootArray_ReturnsIterator()
    {
        var query = JqQueryBuilder.Build([], rootIsArray: true);
        Assert.Equal(".[]", query);
    }

    [Fact]
    public void NoSelections_RootObject_ReturnsDot()
    {
        var query = JqQueryBuilder.Build([], rootIsArray: false);
        Assert.Equal(".", query);
    }

    [Fact]
    public void SingleField_RootArray_FiltersNulls()
    {
        var selections = new List<SelectedPath>
        {
            Path(".name", "name", "name")
        };

        var query = JqQueryBuilder.Build(selections, rootIsArray: true);
        Assert.Equal(".[] | .name // empty", query);
    }

    [Fact]
    public void SingleField_RootObject_FiltersNulls()
    {
        var selections = new List<SelectedPath>
        {
            Path(".name", "name", "name")
        };

        var query = JqQueryBuilder.Build(selections, rootIsArray: false);
        Assert.Equal(".name // empty", query);
    }

    [Fact]
    public void MultipleTopLevelFields_FiltersAllNull()
    {
        var selections = new List<SelectedPath>
        {
            Path(".id", "id", "id"),
            Path(".name", "name", "name")
        };

        var query = JqQueryBuilder.Build(selections, rootIsArray: true);
        Assert.Equal(".[] | {id, name} | select(any(.id, .name; . != null))", query);
    }

    [Fact]
    public void NestedField_UsesRenameAndFiltersNulls()
    {
        var selections = new List<SelectedPath>
        {
            Path(".name", "name", "name"),
            Path(".address.city", "address.city", "address", "city")
        };

        var query = JqQueryBuilder.Build(selections, rootIsArray: true);
        Assert.Equal(".[] | {name, city: .address.city} | select(any(.name, .city; . != null))", query);
    }

    [Fact]
    public void ArrayItemField_SingleSelection_FiltersNulls()
    {
        var selections = new List<SelectedPath>
        {
            Path(".items[].price", "items[].price", "items", "[item]", "price")
        };

        var query = JqQueryBuilder.Build(selections, rootIsArray: true);
        Assert.Equal(".[] | .items[].price // empty", query);
    }

    [Fact]
    public void MixedFieldDepths_FiltersAllNull()
    {
        var selections = new List<SelectedPath>
        {
            Path(".id", "id", "id"),
            Path(".address.city", "address.city", "address", "city"),
            Path(".items[].price", "items[].price", "items", "[item]", "price")
        };

        var query = JqQueryBuilder.Build(selections, rootIsArray: true);
        Assert.Equal(".[] | {id, city: .address.city, price: .items[].price} | select(any(.id, .city, .price; . != null))", query);
    }

    [Fact]
    public void DeeplyNestedField_SingleSelection_FiltersNulls()
    {
        var selections = new List<SelectedPath>
        {
            Path(".a.b.c.value", "a.b.c.value", "a", "b", "c", "value")
        };

        var query = JqQueryBuilder.Build(selections, rootIsArray: true);
        Assert.Equal(".[] | .a.b.c.value // empty", query);
    }

    [Fact]
    public void MultipleNestedFields_AllGetRenamedAndFiltered()
    {
        var selections = new List<SelectedPath>
        {
            Path(".user.name", "user.name", "user", "name"),
            Path(".user.email", "user.email", "user", "email")
        };

        var query = JqQueryBuilder.Build(selections, rootIsArray: true);
        Assert.Equal(".[] | {name: .user.name, email: .user.email} | select(any(.name, .email; . != null))", query);
    }

    [Fact]
    public void RootObject_MultipleFields_NoIteratorButFiltersNulls()
    {
        var selections = new List<SelectedPath>
        {
            Path(".name", "name", "name"),
            Path(".address.city", "address.city", "address", "city")
        };

        var query = JqQueryBuilder.Build(selections, rootIsArray: false);
        Assert.Equal("{name, city: .address.city} | select(any(.name, .city; . != null))", query);
    }
}
