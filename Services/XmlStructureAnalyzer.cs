using System.Diagnostics;
using System.Text;
using System.Xml;
using MyDr_Import.Models;

namespace MyDr_Import.Services;

/// <summary>
/// Analizator struktury XML wykorzystujacy strumieniowe przetwarzanie
/// Nie laduje calego pliku do pamieci - idealny dla duzych plikow (10GB+)
/// </summary>
public class XmlStructureAnalyzer
{
    private readonly string _filePath;
    
    // Przechowuje surowe rekordy XML (pierwsze 3 + ostatni) dla kazdego modelu
    public Dictionary<string, List<string>> SampleRecords { get; } = new();
    
    public XmlStructureAnalyzer(string filePath)
    {
        _filePath = filePath;
    }

    /// <summary>
    /// Analizuje plik XML i zwraca statystyki dla kazdego typu obiektu
    /// </summary>
    public Dictionary<string, XmlObjectInfo> Analyze()
    {
        var objectInfos = new Dictionary<string, XmlObjectInfo>();
        var stopwatch = Stopwatch.StartNew();
        var lastReportTime = TimeSpan.Zero;
        var reportInterval = TimeSpan.FromSeconds(30);
        long totalObjects = 0;
        long fileSize = new FileInfo(_filePath).Length;

        Console.WriteLine($"Plik: {_filePath}");
        Console.WriteLine($"Rozmiar: {fileSize / (1024.0 * 1024.0 * 1024.0):F2} GB");
        Console.WriteLine($"Rozpoczecie analizy: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();

        var settings = new XmlReaderSettings
        {
            IgnoreWhitespace = true,
            DtdProcessing = DtdProcessing.Ignore
        };

        using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan);
        using var reader = XmlReader.Create(fileStream, settings);

        string? currentModel = null;
        string? currentPrimaryKey = null;
        var currentFields = new Dictionary<string, (string? value, string? type, string? rel, string? relTo)>();

        // Tymczasowe przechowywanie ostatniego rekordu dla kazdego modelu
        var lastRecordXml = new Dictionary<string, string>();

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                if (reader.Name == "object" && reader.HasAttributes)
                {
                    // Zapisz poprzedni obiekt jesli istnieje
                    if (currentModel != null && currentPrimaryKey != null)
                    {
                        SaveObject(objectInfos, currentModel, currentPrimaryKey, currentFields);
                        
                        // Zapisz surowy XML rekordu
                        var recordXml = BuildRecordXml(currentModel, currentPrimaryKey, currentFields);
                        SaveSampleRecord(currentModel, recordXml, objectInfos[currentModel].RecordCount, lastRecordXml);
                        
                        currentFields.Clear();
                    }

                    // Raport postepu co okreslony czas
                    if (stopwatch.Elapsed - lastReportTime >= reportInterval)
                    {
                        lastReportTime = stopwatch.Elapsed;
                        double progress = (double)fileStream.Position / fileSize * 100;
                        double avgSpeed = totalObjects / stopwatch.Elapsed.TotalSeconds;

                        Console.WriteLine($"[{stopwatch.Elapsed:hh\\:mm\\:ss}] " +
                            $"Postep: {progress:F2}% | " +
                            $"Obiektow: {totalObjects:N0} | " +
                            $"Predkosc: {avgSpeed:F0} obj/s");
                    }

                    // Rozpocznij nowy obiekt
                    currentModel = reader.GetAttribute("model");
                    currentPrimaryKey = reader.GetAttribute("pk");
                    totalObjects++;
                }
                else if (reader.Name == "field" && reader.HasAttributes && currentModel != null)
                {
                    // Parsuj pole
                    var fieldName = reader.GetAttribute("name");
                    var fieldType = reader.GetAttribute("type");
                    var fieldRel = reader.GetAttribute("rel");
                    var fieldRelTo = reader.GetAttribute("to");

                    if (!string.IsNullOrEmpty(fieldName))
                    {
                        // Odczytaj wartosc pola
                        var fieldValue = reader.ReadInnerXml();
                        currentFields[fieldName] = (fieldValue, fieldType, fieldRel, fieldRelTo);
                    }
                }
            }
        }

        // Zapisz ostatni obiekt
        if (currentModel != null && currentPrimaryKey != null)
        {
            SaveObject(objectInfos, currentModel, currentPrimaryKey, currentFields);
            
            var recordXml = BuildRecordXml(currentModel, currentPrimaryKey, currentFields);
            SaveSampleRecord(currentModel, recordXml, objectInfos[currentModel].RecordCount, lastRecordXml);
        }

        // Dodaj ostatnie rekordy do SampleRecords
        foreach (var (model, lastXml) in lastRecordXml)
        {
            if (SampleRecords.ContainsKey(model) && objectInfos[model].RecordCount > 3)
            {
                // Dodaj ostatni rekord tylko jesli jest inny niz juz zapisane
                if (!SampleRecords[model].Contains(lastXml))
                {
                    SampleRecords[model].Add(lastXml);
                }
            }
        }

        stopwatch.Stop();

        // Podsumowanie koncowe
        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("ANALIZA ZAKONCZONA");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"Czas wykonania: {stopwatch.Elapsed:hh\\:mm\\:ss}");
        Console.WriteLine($"Calkowita liczba obiektow: {totalObjects:N0}");
        Console.WriteLine($"Liczba typow obiektow: {objectInfos.Count}");
        Console.WriteLine($"Predkosc: {totalObjects / stopwatch.Elapsed.TotalSeconds:F0} obiektow/s");
        Console.WriteLine();

        return objectInfos;
    }

    private string BuildRecordXml(string model, string pk, Dictionary<string, (string? value, string? type, string? rel, string? relTo)> fields)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"  <object model=\"{model}\" pk=\"{pk}\">");
        
        foreach (var (fieldName, (value, type, rel, relTo)) in fields.OrderBy(f => f.Key))
        {
            var typeAttr = !string.IsNullOrEmpty(type) ? $" type=\"{type}\"" : "";
            var relAttr = !string.IsNullOrEmpty(rel) ? $" rel=\"{rel}\"" : "";
            var relToAttr = !string.IsNullOrEmpty(relTo) ? $" to=\"{relTo}\"" : "";
            
            sb.AppendLine($"    <field name=\"{fieldName}\"{typeAttr}{relAttr}{relToAttr}>{EscapeXml(value)}</field>");
        }
        
        sb.AppendLine("  </object>");
        return sb.ToString();
    }

    private string EscapeXml(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    private void SaveSampleRecord(string model, string recordXml, long recordCount, Dictionary<string, string> lastRecordXml)
    {
        if (!SampleRecords.ContainsKey(model))
        {
            SampleRecords[model] = new List<string>();
        }

        // Zapisz pierwsze 3 rekordy
        if (recordCount <= 3)
        {
            SampleRecords[model].Add(recordXml);
        }

        // Zawsze aktualizuj ostatni rekord
        lastRecordXml[model] = recordXml;
    }

    private void SaveObject(Dictionary<string, XmlObjectInfo> objectInfos, string modelName, string primaryKey, Dictionary<string, (string? value, string? type, string? rel, string? relTo)> fields)
    {
        if (!objectInfos.ContainsKey(modelName))
        {
            objectInfos[modelName] = new XmlObjectInfo { ModelName = modelName };
        }

        objectInfos[modelName].AddRecord(primaryKey, fields);
    }

    /// <summary>
    /// Zapisuje przykladowe rekordy (pierwsze 3 + ostatni) do plikow XML
    /// </summary>
    public void SaveSampleRecordsToXml(string outputDir, Dictionary<string, XmlObjectInfo> objectInfos)
    {
        Directory.CreateDirectory(outputDir);

        foreach (var (model, records) in SampleRecords)
        {
            var safeModelName = model.Replace(".", "_");
            var filePath = Path.Combine(outputDir, $"{safeModelName}_head.xml");

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine($"<!-- Model: {model} -->");
            sb.AppendLine($"<!-- Calkowita liczba rekordow: {objectInfos[model].RecordCount} -->");
            sb.AppendLine($"<!-- Pierwsze 3 rekordy + ostatni rekord -->");
            sb.AppendLine("<objects>");

            foreach (var record in records)
            {
                sb.Append(record);
            }

            sb.AppendLine("</objects>");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        Console.WriteLine($"Zapisano {SampleRecords.Count} plikow XML z przykladowymi rekordami do: {outputDir}");
    }
}