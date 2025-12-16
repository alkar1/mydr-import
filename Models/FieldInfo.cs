namespace MyDr_Import.Models;

/// <summary>
/// Informacje o pojedynczym polu w obiekcie XML
/// </summary>
public class FieldInfo
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Relation { get; set; }
    public string? RelationTo { get; set; }
    public int OccurrenceCount { get; set; }
    public HashSet<string> SampleValues { get; set; } = new();
    public int NullCount { get; set; }
    public int MaxLength { get; set; }

    public void AddSample(string? value)
    {
        if (value == null || value == "<None></None>" || string.IsNullOrWhiteSpace(value))
        {
            NullCount++;
            return;
        }

        OccurrenceCount++;
        
        if (value.Length > MaxLength)
        {
            MaxLength = value.Length;
        }

        // Przechowuj tylko pierwsze 10 przyk³adowych wartoœci
        if (SampleValues.Count < 10)
        {
            SampleValues.Add(value.Length > 100 ? value.Substring(0, 100) + "..." : value);
        }
    }

    public override string ToString()
    {
        var parts = new List<string> { Name };
        
        if (!string.IsNullOrEmpty(Type))
            parts.Add($"Type: {Type}");
        
        if (!string.IsNullOrEmpty(Relation))
            parts.Add($"Rel: {Relation} -> {RelationTo}");
        
        parts.Add($"Count: {OccurrenceCount}");
        
        if (NullCount > 0)
            parts.Add($"Nulls: {NullCount}");
        
        if (MaxLength > 0)
            parts.Add($"MaxLen: {MaxLength}");

        return string.Join(" | ", parts);
    }
}
