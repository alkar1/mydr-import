using System.Text;
using System.Xml.Linq;
using MyDr_Import.Models;
using MyDr_Import.Services;

namespace MyDr_Import.Processors;

/// <summary>
/// Bazowa klasa dla procesorow modeli
/// Zawiera wspolna logike wczytywania XML i generowania CSV
/// </summary>
public abstract class BaseModelProcessor : IModelProcessor
{
    public abstract string ModelName { get; }
    public abstract string XmlFileName { get; }

    /// <summary>
    /// Mapowanie nazw pol z arkusza Excel na nazwy pol XML
    /// Kazdy procesor moze nadpisac to wlasnym mapowaniem
    /// </summary>
    protected virtual Dictionary<string, string> FieldNameMappings => new();

    public virtual CsvGenerationResult Process(string dataEtap1Path, string dataEtap2Path, ModelMapping mapping)
    {
        var result = new CsvGenerationResult
        {
            ModelName = mapping.SheetName,
            TargetTable = mapping.TargetTable
        };

        try
        {
            // 1. Znajdz plik XML
            var xmlPath = Path.Combine(dataEtap1Path, "data_full", XmlFileName);
            if (!File.Exists(xmlPath))
            {
                result.Error = $"Nie znaleziono pliku XML: {XmlFileName}";
                return result;
            }

            Console.WriteLine($"  Plik zrodlowy: {XmlFileName}");

            // 2. Wczytaj dane z XML
            var records = LoadXmlRecords(xmlPath);
            result.SourceRecords = records.Count;
            Console.WriteLine($"  Rekordy zrodlowe: {records.Count}");

            if (records.Count == 0)
            {
                result.Error = "Brak rekordow w pliku zrodlowym";
                return result;
            }

            // 3. Przygotuj naglowki CSV
            var validFields = mapping.Fields
                .Where(f => !string.IsNullOrEmpty(f.SourceField))
                .ToList();

            // 4. Generuj CSV
            Directory.CreateDirectory(dataEtap2Path);
            var csvPath = Path.Combine(dataEtap2Path, $"{mapping.SheetName}.csv");
            using var writer = new StreamWriter(csvPath, false, new UTF8Encoding(true));

            // Naglowek
            writer.WriteLine(string.Join(";", validFields.Select(f => EscapeCsvField(f.SourceField))));

            // Wiersze danych
            int processedCount = 0;
            foreach (var record in records)
            {
                var row = new List<string>();
                foreach (var field in validFields)
                {
                    var value = ExtractFieldValue(record, field);
                    row.Add(EscapeCsvField(value));
                }
                writer.WriteLine(string.Join(";", row));
                processedCount++;
            }

            result.OutputRecords = processedCount;
            result.OutputPath = csvPath;
            result.IsSuccess = true;

            Console.WriteLine($"  Wygenerowano: {csvPath}");
            Console.WriteLine($"  Rekordy wyjsciowe: {processedCount}");
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }

        return result;
    }

    protected virtual List<Dictionary<string, string>> LoadXmlRecords(string xmlPath)
    {
        var records = new List<Dictionary<string, string>>();
        var doc = XDocument.Load(xmlPath);
        var root = doc.Root;

        if (root == null)
            return records;

        foreach (var obj in root.Elements("object"))
        {
            var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Dodaj pk jako pole
            var pk = obj.Attribute("pk")?.Value;
            if (!string.IsNullOrEmpty(pk))
            {
                record["pk"] = pk;
                record["id"] = pk;
            }

            // Pobierz wszystkie pola
            foreach (var field in obj.Elements("field"))
            {
                var name = field.Attribute("name")?.Value;
                if (!string.IsNullOrEmpty(name))
                {
                    var value = field.Value?.Trim() ?? "";
                    // Wyczysc wartosci NULL z XML (reprezentowane jako <None></None>)
                    if (value == "<None></None>" || value == "None")
                    {
                        value = "";
                    }
                    record[name] = value;
                }
            }

            if (record.Count > 0)
            {
                records.Add(record);
            }
        }

        return records;
    }

    protected virtual string ExtractFieldValue(Dictionary<string, string> record, FieldMapping field)
    {
        if (string.IsNullOrEmpty(field.SourceField))
            return field.TransformRule ?? "";

        // Probuj rozne warianty nazwy pola
        var fieldVariants = new List<string>
        {
            field.SourceField,
            field.SourceField.ToLower(),
            ToSnakeCase(field.SourceField)
        };

        // Dodaj mapowanie ze slownika
        if (FieldNameMappings.TryGetValue(field.SourceField, out var mappedName))
        {
            fieldVariants.Insert(0, mappedName);
        }

        foreach (var variant in fieldVariants)
        {
            if (record.TryGetValue(variant, out var value))
            {
                return ApplyTransform(value, field.TransformRule);
            }
        }

        return "";
    }

    protected string ApplyTransform(string value, string? rule)
    {
        if (string.IsNullOrEmpty(rule))
            return value;

        return rule.ToLower() switch
        {
            "upper" => value.ToUpper(),
            "lower" => value.ToLower(),
            "trim" => value.Trim(),
            _ => value
        };
    }

    protected string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (char.IsUpper(c) && i > 0)
                sb.Append('_');
            sb.Append(char.ToLower(c));
        }
        return sb.ToString();
    }

    protected string EscapeCsvField(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains('"') || value.Contains(';') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }
}
