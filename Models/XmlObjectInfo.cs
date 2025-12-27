namespace MyDr_Import.Models;

/// <summary>
/// Metadane o typie obiektu w pliku XML (np. gabinet.patientnote)
/// </summary>
public class XmlObjectInfo
{
    public string ModelName { get; set; } = string.Empty;
    public long RecordCount { get; set; }
    public Dictionary<string, FieldInfo> Fields { get; set; } = new();
    public long MinPrimaryKey { get; set; } = long.MaxValue;
    public long MaxPrimaryKey { get; set; } = long.MinValue;

    public void AddRecord(string primaryKey, Dictionary<string, (string? value, string? type, string? rel, string? relTo)> fields)
    {
        RecordCount++;

        // Aktualizuj zakres kluczy g��wnych
        if (long.TryParse(primaryKey, out long pk))
        {
            if (pk < MinPrimaryKey) MinPrimaryKey = pk;
            if (pk > MaxPrimaryKey) MaxPrimaryKey = pk;
        }

        // Aktualizuj informacje o polach
        foreach (var (fieldName, (value, type, rel, relTo)) in fields)
        {
            if (!Fields.ContainsKey(fieldName))
            {
                Fields[fieldName] = new FieldInfo 
                { 
                    Name = fieldName,
                    Type = type,
                    Relation = rel,
                    RelationTo = relTo
                };
            }

            Fields[fieldName].AddSample(value);
        }
    }

    public void PrintSummary()
    {
        Console.WriteLine($"\n{new string('=', 80)}");
        Console.WriteLine($"MODEL: {ModelName}");
        Console.WriteLine($"{new string('=', 80)}");
        Console.WriteLine($"Liczba rekord�w: {RecordCount:N0}");
        Console.WriteLine($"Primary Key Range: {MinPrimaryKey:N0} - {MaxPrimaryKey:N0}");
        Console.WriteLine($"Liczba p�l: {Fields.Count}");
        Console.WriteLine($"\nPOLA:");
        Console.WriteLine($"{new string('-', 80)}");

        foreach (var field in Fields.Values.OrderBy(f => f.Name))
        {
            Console.WriteLine($"  {field}");
            
            if (field.SampleValues.Any())
            {
                Console.WriteLine($"    Przyk�ady: {string.Join(", ", field.SampleValues.Take(3).Select(s => $"\"{s}\""))}");
            }
        }
    }

    public string ToCSVSummary()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Model,{ModelName}");
        sb.AppendLine($"RecordCount,{RecordCount}");
        sb.AppendLine($"MinPK,{MinPrimaryKey}");
        sb.AppendLine($"MaxPK,{MaxPrimaryKey}");
        sb.AppendLine($"FieldCount,{Fields.Count}");
        sb.AppendLine();
        sb.AppendLine("FieldName,Type,Relation,RelationTo,OccurrenceCount,NullCount,MaxLength");
        
        foreach (var field in Fields.Values.OrderBy(f => f.Name))
        {
            sb.AppendLine($"{field.Name},{field.Type},{field.Relation},{field.RelationTo},{field.OccurrenceCount},{field.NullCount},{field.MaxLength}");
        }

        return sb.ToString();
    }
}
