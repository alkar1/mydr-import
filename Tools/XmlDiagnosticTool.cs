using System.Xml;
using System.Text;

namespace MyDr_Import.Tools;

/// <summary>
/// Narzêdzie diagnostyczne do analizy struktury konkretnych obiektów w XML
/// </summary>
public class XmlDiagnosticTool
{
    private readonly string _xmlFilePath;

    public XmlDiagnosticTool(string xmlFilePath)
    {
        _xmlFilePath = xmlFilePath;
    }

    /// <summary>
    /// Wyœwietla pierwsze N obiektów danego modelu z pe³n¹ struktur¹ pól
    /// </summary>
    public async Task ShowObjectStructureAsync(string modelName, int count = 3)
    {
        Console.WriteLine($"??????????????????????????????????????????????????????????????????????????????");
        Console.WriteLine($"?  Analiza struktury: {modelName,-56} ?");
        Console.WriteLine($"??????????????????????????????????????????????????????????????????????????????");
        Console.WriteLine();

        var settings = new XmlReaderSettings
        {
            Async = true,
            IgnoreWhitespace = true,
            IgnoreComments = true
        };

        await using var fileStream = new FileStream(_xmlFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = XmlReader.Create(fileStream, settings);

        int found = 0;

        while (await reader.ReadAsync() && found < count)
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "object")
            {
                var model = reader.GetAttribute("model");
                if (model == modelName)
                {
                    var pk = reader.GetAttribute("pk");
                    found++;

                    Console.WriteLine($"???????????????????????????????????????????????????????????????????????????");
                    Console.WriteLine($"OBIEKT #{found} - PK: {pk}");
                    Console.WriteLine($"???????????????????????????????????????????????????????????????????????????");
                    Console.WriteLine();

                    // Czytaj wszystkie pola tego obiektu
                    var subtree = reader.ReadSubtree();
                    var fields = new List<(string name, string type, string value, string? rel, string? to)>();

                    while (await subtree.ReadAsync())
                    {
                        if (subtree.NodeType == XmlNodeType.Element && subtree.Name == "field")
                        {
                            var fieldName = subtree.GetAttribute("name");
                            var fieldType = subtree.GetAttribute("type");
                            var rel = subtree.GetAttribute("rel");
                            var to = subtree.GetAttribute("to");
                            
                            var fieldValue = subtree.ReadInnerXml();
                            
                            // Skróæ d³ugie wartoœci
                            if (fieldValue.Length > 100)
                                fieldValue = fieldValue.Substring(0, 97) + "...";

                            fields.Add((fieldName ?? "", fieldType ?? "", fieldValue, rel, to));
                        }
                    }

                    // Wyœwietl pola w czytelny sposób
                    var maxNameLength = fields.Any() ? fields.Max(f => f.name.Length) : 0;
                    var maxTypeLength = fields.Any() ? fields.Max(f => f.type.Length) : 0;

                    foreach (var (name, type, value, rel, to) in fields.OrderBy(f => f.name))
                    {
                        var displayType = rel != null ? $"{rel}->{to}" : type;
                        
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            Console.WriteLine($"  {name.PadRight(maxNameLength)} | {displayType.PadRight(maxTypeLength + 20)} | {value}");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.WriteLine($"  {name.PadRight(maxNameLength)} | {displayType.PadRight(maxTypeLength + 20)} | <NULL>");
                            Console.ResetColor();
                        }
                    }

                    Console.WriteLine();
                    Console.WriteLine($"Pól: {fields.Count} (wype³nionych: {fields.Count(f => !string.IsNullOrWhiteSpace(f.value))})");
                    Console.WriteLine();
                }
            }
        }

        if (found == 0)
        {
            Console.WriteLine($"? Nie znaleziono obiektów typu: {modelName}");
        }
        else
        {
            Console.WriteLine($"? Znaleziono i wyœwietlono {found} obiektów typu: {modelName}");
        }
    }

    /// <summary>
    /// Porównuje strukturê dwóch modeli
    /// </summary>
    public async Task CompareModelsAsync(string model1, string model2)
    {
        Console.WriteLine($"??????????????????????????????????????????????????????????????????????????????");
        Console.WriteLine($"?  Porównanie modeli: {model1} vs {model2,-40} ?");
        Console.WriteLine($"??????????????????????????????????????????????????????????????????????????????");
        Console.WriteLine();

        var fields1 = await GetModelFieldsAsync(model1);
        var fields2 = await GetModelFieldsAsync(model2);

        Console.WriteLine($"Model: {model1}");
        Console.WriteLine($"  Pola: {string.Join(", ", fields1.Take(20))}");
        if (fields1.Count > 20)
            Console.WriteLine($"  ... i {fields1.Count - 20} wiêcej");
        Console.WriteLine();

        Console.WriteLine($"Model: {model2}");
        Console.WriteLine($"  Pola: {string.Join(", ", fields2.Take(20))}");
        if (fields2.Count > 20)
            Console.WriteLine($"  ... i {fields2.Count - 20} wiêcej");
        Console.WriteLine();

        var onlyIn1 = fields1.Except(fields2).ToList();
        var onlyIn2 = fields2.Except(fields1).ToList();
        var common = fields1.Intersect(fields2).ToList();

        Console.WriteLine($"Wspólne pola ({common.Count}):");
        Console.WriteLine($"  {string.Join(", ", common.Take(20))}");
        Console.WriteLine();

        if (onlyIn1.Any())
        {
            Console.WriteLine($"Tylko w {model1} ({onlyIn1.Count}):");
            Console.WriteLine($"  {string.Join(", ", onlyIn1.Take(20))}");
            Console.WriteLine();
        }

        if (onlyIn2.Any())
        {
            Console.WriteLine($"Tylko w {model2} ({onlyIn2.Count}):");
            Console.WriteLine($"  {string.Join(", ", onlyIn2.Take(20))}");
            Console.WriteLine();
        }
    }

    private async Task<HashSet<string>> GetModelFieldsAsync(string modelName)
    {
        var fields = new HashSet<string>();

        var settings = new XmlReaderSettings
        {
            Async = true,
            IgnoreWhitespace = true,
            IgnoreComments = true
        };

        await using var fileStream = new FileStream(_xmlFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = XmlReader.Create(fileStream, settings);

        while (await reader.ReadAsync())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "object")
            {
                var model = reader.GetAttribute("model");
                if (model == modelName)
                {
                    var subtree = reader.ReadSubtree();
                    while (await subtree.ReadAsync())
                    {
                        if (subtree.NodeType == XmlNodeType.Element && subtree.Name == "field")
                        {
                            var fieldName = subtree.GetAttribute("name");
                            if (!string.IsNullOrEmpty(fieldName))
                                fields.Add(fieldName);
                        }
                    }
                    break; // Wystarczy pierwszy obiekt
                }
            }
        }

        return fields;
    }

    /// <summary>
    /// Statystyki modelu - ile obiektów, jakie pola s¹ wype³nione
    /// </summary>
    public async Task<ModelStatistics> GetModelStatisticsAsync(string modelName, int maxSample = 100)
    {
        var stats = new ModelStatistics { ModelName = modelName };
        var fieldFillCounts = new Dictionary<string, int>();

        var settings = new XmlReaderSettings
        {
            Async = true,
            IgnoreWhitespace = true,
            IgnoreComments = true
        };

        await using var fileStream = new FileStream(_xmlFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = XmlReader.Create(fileStream, settings);

        int sampled = 0;

        while (await reader.ReadAsync())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "object")
            {
                var model = reader.GetAttribute("model");
                if (model == modelName)
                {
                    stats.TotalCount++;

                    if (sampled < maxSample)
                    {
                        sampled++;
                        var subtree = reader.ReadSubtree();
                        
                        while (await subtree.ReadAsync())
                        {
                            if (subtree.NodeType == XmlNodeType.Element && subtree.Name == "field")
                            {
                                var fieldName = subtree.GetAttribute("name");
                                var fieldValue = subtree.ReadInnerXml();

                                if (!string.IsNullOrEmpty(fieldName))
                                {
                                    if (!fieldFillCounts.ContainsKey(fieldName))
                                        fieldFillCounts[fieldName] = 0;

                                    if (!string.IsNullOrWhiteSpace(fieldValue) && fieldValue != "<None></None>")
                                        fieldFillCounts[fieldName]++;
                                }
                            }
                        }
                    }
                }
            }
        }

        stats.SampledCount = sampled;
        stats.FieldFillRates = fieldFillCounts
            .Select(kvp => new FieldFillRate
            {
                FieldName = kvp.Key,
                FilledCount = kvp.Value,
                SampleSize = sampled,
                FillPercentage = (kvp.Value * 100.0) / sampled
            })
            .OrderByDescending(f => f.FillPercentage)
            .ToList();

        return stats;
    }

    public void PrintModelStatistics(ModelStatistics stats)
    {
        Console.WriteLine($"??????????????????????????????????????????????????????????????????????????????");
        Console.WriteLine($"?  Statystyki modelu: {stats.ModelName,-56} ?");
        Console.WriteLine($"??????????????????????????????????????????????????????????????????????????????");
        Console.WriteLine();
        Console.WriteLine($"?? Ca³kowita liczba obiektów: {stats.TotalCount:N0}");
        Console.WriteLine($"?? Próbka analizowana: {stats.SampledCount:N0}");
        Console.WriteLine();
        Console.WriteLine($"{"Pole",-35} | {"Wype³nionych",12} | {"Procent",8}");
        Console.WriteLine(new string('-', 60));

        foreach (var field in stats.FieldFillRates.Take(30))
        {
            var color = field.FillPercentage >= 90 ? ConsoleColor.Green :
                       field.FillPercentage >= 50 ? ConsoleColor.Yellow :
                       ConsoleColor.Red;

            Console.ForegroundColor = color;
            Console.WriteLine($"{field.FieldName,-35} | {field.FilledCount,12:N0} | {field.FillPercentage,7:F1}%");
            Console.ResetColor();
        }

        if (stats.FieldFillRates.Count > 30)
        {
            Console.WriteLine($"... i {stats.FieldFillRates.Count - 30} wiêcej pól");
        }
    }
}

public class ModelStatistics
{
    public string ModelName { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int SampledCount { get; set; }
    public List<FieldFillRate> FieldFillRates { get; set; } = new();
}

public class FieldFillRate
{
    public string FieldName { get; set; } = string.Empty;
    public int FilledCount { get; set; }
    public int SampleSize { get; set; }
    public double FillPercentage { get; set; }
}
