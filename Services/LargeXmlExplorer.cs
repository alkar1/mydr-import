using System.Text;
using System.Xml;

namespace MyDr_Import.Services;

/// <summary>
/// Narzêdzie do eksploracji du¿ych plików XML dla LLM
/// Strumieniowe przetwarzanie - nie ³aduje ca³ego pliku do pamiêci
/// </summary>
public class LargeXmlExplorer
{
    private readonly string _filePath;

    public LargeXmlExplorer(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Plik nie istnieje: {filePath}");
        _filePath = filePath;
    }

    /// <summary>
    /// Pobiera pierwsze N rekordów z pliku XML
    /// </summary>
    public List<XmlRecord> Head(int count = 10, string? modelFilter = null)
    {
        var records = new List<XmlRecord>();
        
        foreach (var record in StreamRecords(modelFilter))
        {
            records.Add(record);
            if (records.Count >= count)
                break;
        }
        
        return records;
    }

    /// <summary>
    /// Pobiera ostatnie N rekordów z pliku XML (wymaga przejœcia przez ca³y plik)
    /// </summary>
    public List<XmlRecord> Tail(int count = 10, string? modelFilter = null)
    {
        var buffer = new Queue<XmlRecord>();
        
        foreach (var record in StreamRecords(modelFilter))
        {
            buffer.Enqueue(record);
            if (buffer.Count > count)
                buffer.Dequeue();
        }
        
        return buffer.ToList();
    }

    /// <summary>
    /// Pobiera próbkê rekordów (co N-ty rekord)
    /// </summary>
    public List<XmlRecord> Sample(int maxRecords = 10, int skipEvery = 100, string? modelFilter = null)
    {
        var records = new List<XmlRecord>();
        int counter = 0;
        
        foreach (var record in StreamRecords(modelFilter))
        {
            if (counter % skipEvery == 0)
            {
                records.Add(record);
                if (records.Count >= maxRecords)
                    break;
            }
            counter++;
        }
        
        return records;
    }

    /// <summary>
    /// Wyszukuje rekordy po wartoœci pola
    /// </summary>
    public List<XmlRecord> Search(string fieldName, string searchValue, int maxResults = 10, string? modelFilter = null, bool exactMatch = false)
    {
        var records = new List<XmlRecord>();
        
        foreach (var record in StreamRecords(modelFilter))
        {
            if (record.Fields.TryGetValue(fieldName, out var fieldValue))
            {
                bool match = exactMatch 
                    ? fieldValue.Equals(searchValue, StringComparison.OrdinalIgnoreCase)
                    : fieldValue.Contains(searchValue, StringComparison.OrdinalIgnoreCase);
                    
                if (match)
                {
                    records.Add(record);
                    if (records.Count >= maxResults)
                        break;
                }
            }
        }
        
        return records;
    }

    /// <summary>
    /// Pobiera rekord po kluczu g³ównym (pk)
    /// </summary>
    public XmlRecord? GetByPk(string pk, string? modelFilter = null)
    {
        foreach (var record in StreamRecords(modelFilter))
        {
            if (record.Pk == pk)
                return record;
        }
        return null;
    }

    /// <summary>
    /// Pobiera statystyki pliku bez ³adowania wszystkich danych
    /// </summary>
    public XmlFileStats GetStats()
    {
        var stats = new XmlFileStats
        {
            FilePath = _filePath,
            FileSizeBytes = new FileInfo(_filePath).Length
        };
        
        var modelCounts = new Dictionary<string, long>();
        var modelFields = new Dictionary<string, HashSet<string>>();
        
        foreach (var record in StreamRecords(null))
        {
            if (!modelCounts.ContainsKey(record.Model))
            {
                modelCounts[record.Model] = 0;
                modelFields[record.Model] = new HashSet<string>();
            }
            
            modelCounts[record.Model]++;
            foreach (var field in record.Fields.Keys)
            {
                modelFields[record.Model].Add(field);
            }
        }
        
        stats.ModelStats = modelCounts.Select(kv => new ModelStats
        {
            ModelName = kv.Key,
            RecordCount = kv.Value,
            Fields = modelFields[kv.Key].OrderBy(f => f).ToList()
        }).OrderByDescending(m => m.RecordCount).ToList();
        
        stats.TotalRecords = modelCounts.Values.Sum();
        
        return stats;
    }

    /// <summary>
    /// Pobiera listê dostêpnych modeli z pierwszych N rekordów
    /// </summary>
    public List<string> GetModels(int sampleSize = 10000)
    {
        var models = new HashSet<string>();
        int count = 0;
        
        foreach (var record in StreamRecords(null))
        {
            models.Add(record.Model);
            count++;
            if (count >= sampleSize)
                break;
        }
        
        return models.OrderBy(m => m).ToList();
    }

    /// <summary>
    /// Pobiera schemat pól dla danego modelu (na podstawie próbki)
    /// </summary>
    public Dictionary<string, FieldSchema> GetSchema(string model, int sampleSize = 100)
    {
        var schema = new Dictionary<string, FieldSchema>();
        int count = 0;
        
        foreach (var record in StreamRecords(model))
        {
            foreach (var (name, value) in record.Fields)
            {
                if (!schema.ContainsKey(name))
                {
                    schema[name] = new FieldSchema { Name = name };
                }
                
                schema[name].SampleValues.Add(value);
                if (schema[name].SampleValues.Count > 5)
                    schema[name].SampleValues.RemoveAt(0);
                    
                if (!string.IsNullOrEmpty(value))
                    schema[name].NonEmptyCount++;
            }
            
            count++;
            if (count >= sampleSize)
                break;
        }
        
        foreach (var field in schema.Values)
        {
            field.SampleCount = count;
        }
        
        return schema;
    }

    /// <summary>
    /// Generuje czytelny raport dla LLM
    /// </summary>
    public string GenerateReport(int headCount = 3, string? modelFilter = null)
    {
        var sb = new StringBuilder();
        var fileInfo = new FileInfo(_filePath);
        
        sb.AppendLine($"# Raport XML: {Path.GetFileName(_filePath)}");
        sb.AppendLine($"Rozmiar: {fileInfo.Length / (1024.0 * 1024.0):F2} MB");
        sb.AppendLine();
        
        if (modelFilter != null)
        {
            sb.AppendLine($"## Filtr modelu: {modelFilter}");
            sb.AppendLine();
        }
        
        var headRecords = Head(headCount, modelFilter);
        
        sb.AppendLine($"## Pierwsze {headRecords.Count} rekordy:");
        sb.AppendLine();
        
        foreach (var record in headRecords)
        {
            sb.AppendLine($"### Model: {record.Model}, PK: {record.Pk}");
            sb.AppendLine("```");
            foreach (var (name, value) in record.Fields.OrderBy(f => f.Key))
            {
                var displayValue = value.Length > 100 ? value.Substring(0, 100) + "..." : value;
                sb.AppendLine($"  {name}: {displayValue}");
            }
            sb.AppendLine("```");
            sb.AppendLine();
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Eksportuje wyniki do JSON
    /// </summary>
    public string ToJson(List<XmlRecord> records)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[");
        
        for (int i = 0; i < records.Count; i++)
        {
            var record = records[i];
            sb.AppendLine("  {");
            sb.AppendLine($"    \"model\": \"{EscapeJson(record.Model)}\",");
            sb.AppendLine($"    \"pk\": \"{EscapeJson(record.Pk)}\",");
            sb.AppendLine("    \"fields\": {");
            
            var fields = record.Fields.ToList();
            for (int j = 0; j < fields.Count; j++)
            {
                var comma = j < fields.Count - 1 ? "," : "";
                sb.AppendLine($"      \"{EscapeJson(fields[j].Key)}\": \"{EscapeJson(fields[j].Value)}\"{comma}");
            }
            
            sb.AppendLine("    }");
            sb.Append("  }");
            sb.AppendLine(i < records.Count - 1 ? "," : "");
        }
        
        sb.AppendLine("]");
        return sb.ToString();
    }

    private string EscapeJson(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    /// <summary>
    /// Strumieniowo odczytuje rekordy z pliku XML
    /// </summary>
    private IEnumerable<XmlRecord> StreamRecords(string? modelFilter)
    {
        var settings = new XmlReaderSettings
        {
            IgnoreWhitespace = true,
            DtdProcessing = DtdProcessing.Ignore
        };

        using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan);
        using var reader = XmlReader.Create(fileStream, settings);

        string? currentModel = null;
        string? currentPk = null;
        var currentFields = new Dictionary<string, string>();

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                if (reader.Name == "object" && reader.HasAttributes)
                {
                    // Zwróæ poprzedni rekord jeœli istnieje
                    if (currentModel != null && currentPk != null)
                    {
                        if (modelFilter == null || currentModel.Equals(modelFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            yield return new XmlRecord
                            {
                                Model = currentModel,
                                Pk = currentPk,
                                Fields = new Dictionary<string, string>(currentFields)
                            };
                        }
                    }

                    currentModel = reader.GetAttribute("model");
                    currentPk = reader.GetAttribute("pk");
                    currentFields.Clear();
                }
                else if (reader.Name == "field" && reader.HasAttributes && currentModel != null)
                {
                    var fieldName = reader.GetAttribute("name");
                    if (!string.IsNullOrEmpty(fieldName))
                    {
                        var fieldValue = reader.ReadInnerXml();
                        currentFields[fieldName] = fieldValue;
                    }
                }
            }
        }

        // Zwróæ ostatni rekord
        if (currentModel != null && currentPk != null)
        {
            if (modelFilter == null || currentModel.Equals(modelFilter, StringComparison.OrdinalIgnoreCase))
            {
                yield return new XmlRecord
                {
                    Model = currentModel,
                    Pk = currentPk,
                    Fields = new Dictionary<string, string>(currentFields)
                };
            }
        }
    }
}

/// <summary>
/// Pojedynczy rekord XML
/// </summary>
public class XmlRecord
{
    public string Model { get; set; } = "";
    public string Pk { get; set; } = "";
    public Dictionary<string, string> Fields { get; set; } = new();

    public override string ToString()
    {
        return $"[{Model}] pk={Pk}, fields={Fields.Count}";
    }
}

/// <summary>
/// Statystyki pliku XML
/// </summary>
public class XmlFileStats
{
    public string FilePath { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public long TotalRecords { get; set; }
    public List<ModelStats> ModelStats { get; set; } = new();
    
    public string FileSizeMB => $"{FileSizeBytes / (1024.0 * 1024.0):F2} MB";
    public string FileSizeGB => $"{FileSizeBytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
}

/// <summary>
/// Statystyki pojedynczego modelu
/// </summary>
public class ModelStats
{
    public string ModelName { get; set; } = "";
    public long RecordCount { get; set; }
    public List<string> Fields { get; set; } = new();
}

/// <summary>
/// Schemat pola
/// </summary>
public class FieldSchema
{
    public string Name { get; set; } = "";
    public int NonEmptyCount { get; set; }
    public int SampleCount { get; set; }
    public List<string> SampleValues { get; set; } = new();
    
    public double FillRate => SampleCount > 0 ? (double)NonEmptyCount / SampleCount * 100 : 0;
}
