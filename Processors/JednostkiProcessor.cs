using System.Text;
using System.Xml.Linq;
using MyDr_Import.Models;
using MyDr_Import.Services;

namespace MyDr_Import.Processors;

/// <summary>
/// Procesor dla modelu JEDNOSTKI
/// Zrodlo: gabinet_department.xml
/// Cel: jednostki.csv
/// Format wyjsciowy: IdImport;Nazwa;Aktywna;IdWewnetrzny
/// </summary>
public class JednostkiProcessor : IModelProcessor
{
    public string ModelName => "jednostki";
    public string XmlFileName => "gabinet_department.xml";

    public CsvGenerationResult Process(string dataEtap1Path, string dataEtap2Path, ModelMapping mapping)
    {
        var result = new CsvGenerationResult
        {
            ModelName = "jednostki",
            TargetTable = "jednostki"
        };

        try
        {
            // 1. Znajdz plik XML
            var xmlPath = Path.Combine(dataEtap1Path, "data_full", "gabinet_department.xml");
            if (!File.Exists(xmlPath))
            {
                result.Error = $"Nie znaleziono pliku XML: gabinet_department.xml";
                return result;
            }

            Console.WriteLine($"  Plik zrodlowy: gabinet_department.xml");

            // 2. Wczytaj dane z XML
            var records = LoadXmlRecords(xmlPath);
            result.SourceRecords = records.Count;
            Console.WriteLine($"  Rekordy zrodlowe: {records.Count}");

            if (records.Count == 0)
            {
                result.Error = "Brak rekordow w pliku zrodlowym";
                return result;
            }

            // 3. Generuj CSV
            Directory.CreateDirectory(dataEtap2Path);
            var csvPath = Path.Combine(dataEtap2Path, "jednostki.csv");
            using var writer = new StreamWriter(csvPath, false, new UTF8Encoding(true));

            // Naglowek zgodny z old_etap2
            writer.WriteLine("IdImport;Nazwa;Aktywna;IdWewnetrzny");

            // Wiersze danych
            int processedCount = 0;
            foreach (var record in records)
            {
                var idImport = record.GetValueOrDefault("facility", "");
                var nazwa = EscapeCsvField(record.GetValueOrDefault("name", ""));
                var aktywna = record.GetValueOrDefault("active", "False") == "True" ? "1" : "0";
                var idWewnetrzny = record.GetValueOrDefault("pk", "");

                writer.WriteLine($"{idImport};{nazwa};{aktywna};{idWewnetrzny}");
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

    private List<Dictionary<string, string>> LoadXmlRecords(string xmlPath)
    {
        var records = new List<Dictionary<string, string>>();
        var doc = XDocument.Load(xmlPath);
        var root = doc.Root;

        if (root == null)
            return records;

        foreach (var obj in root.Elements("object"))
        {
            var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var pk = obj.Attribute("pk")?.Value;
            if (!string.IsNullOrEmpty(pk))
            {
                record["pk"] = pk;
            }

            foreach (var field in obj.Elements("field"))
            {
                var name = field.Attribute("name")?.Value;
                if (!string.IsNullOrEmpty(name))
                {
                    var value = field.Value?.Trim() ?? "";
                    if (value == "<None></None>" || value == "None")
                        value = "";
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

    private string EscapeCsvField(string value)
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
