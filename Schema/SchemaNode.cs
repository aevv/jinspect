namespace JInspect.Schema;

public class SchemaNode
{
    public int SeenCount { get; set; }
    public int TotalObjectsSeen { get; set; }
    public List<string> ObservedTypes { get; set; } = [];
    public Dictionary<string, SchemaNode> Children { get; set; } = [];
    public HashSet<string> SampleValues { get; set; } = [];
    public int MinArrayLength { get; set; } = int.MaxValue;
    public int MaxArrayLength { get; set; }
}
