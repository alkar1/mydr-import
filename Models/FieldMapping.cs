namespace MyDr_Import.Models;

/// <summary>
/// Mapowanie pojedynczego pola zrodlowego na docelowe
/// </summary>
public class FieldMapping
{
    public string SourceField { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public string? SourceType { get; set; }
    public string? TargetType { get; set; }
    public string? TransformRule { get; set; }
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public bool IsValid { get; set; } = true;
    public string? ValidationError { get; set; }

    public override string ToString()
    {
        var arrow = IsValid ? "->" : "!>";
        return $"{SourceField} {arrow} {TargetField}";
    }
}

/// <summary>
/// Mapowanie calego modelu (arkusza) - pola zrodlowe na docelowe
/// </summary>
public class ModelMapping
{
    public string SheetName { get; set; } = string.Empty;
    public string SourceModel { get; set; } = string.Empty;
    public string TargetTable { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<FieldMapping> Fields { get; set; } = new();
    public bool IsValid => Fields.All(f => f.IsValid);
    public int ValidFieldsCount => Fields.Count(f => f.IsValid);
    public int InvalidFieldsCount => Fields.Count(f => !f.IsValid);

    public void PrintSummary()
    {
        Console.WriteLine($"\n{new string('-', 60)}");
        Console.WriteLine($"ARKUSZ: {SheetName}");
        Console.WriteLine($"Zrodlo: {SourceModel} -> Cel: {TargetTable}");
        Console.WriteLine($"Pola: {Fields.Count} (poprawne: {ValidFieldsCount}, bledy: {InvalidFieldsCount})");
        
        if (Fields.Any())
        {
            Console.WriteLine("Mapowania:");
            foreach (var field in Fields.Take(5))
            {
                var status = field.IsValid ? "[OK]" : "[ERR]";
                Console.WriteLine($"  {status} {field}");
            }
            if (Fields.Count > 5)
            {
                Console.WriteLine($"  ... i {Fields.Count - 5} wiecej");
            }
        }
    }
}

/// <summary>
/// Wynik walidacji mapowania
/// </summary>
public class MappingValidationResult
{
    public string ModelName { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int TestedRecords { get; set; }
    public int SuccessfulRecords { get; set; }
}
