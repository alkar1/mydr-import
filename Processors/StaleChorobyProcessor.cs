using System.Text;
using System.Xml.Linq;
using MyDr_Import.Models;
using MyDr_Import.Services;

namespace MyDr_Import.Processors;

/// <summary>
/// Procesor dla modelu STALE_CHOROBY_PACJENTA
/// Zrodlo: gabinet_recognition.xml
/// Cel: stale_choroby_pacjenta.csv
/// Dodaje pole PacjentPesel poprzez dolaczenie danych z gabinet_patient.xml
/// </summary>
public class StaleChorobyProcessor : IModelProcessor
{
    public string ModelName => "stale_choroby_pacjenta";
    public string XmlFileName => "gabinet_recognition.xml";

    private Dictionary<string, string>? _patientPeselCache;

    public CsvGenerationResult Process(string dataEtap1Path, string dataEtap2Path, ModelMapping mapping)
    {
        var result = new CsvGenerationResult
        {
            ModelName = "stale_choroby_pacjenta",
            TargetTable = "stale_choroby_pacjenta"
        };

        try
        {
            // 1. Zaladuj cache PESEL pacjentow
            LoadPatientPeselCache(dataEtap1Path);

            // 2. Znajdz plik XML
            var xmlPath = Path.Combine(dataEtap1Path, "data_full", "gabinet_recognition.xml");
            if (!File.Exists(xmlPath))
            {
                result.Error = $"Nie znaleziono pliku XML: gabinet_recognition.xml";
                return result;
            }

            Console.WriteLine($"  Plik zrodlowy: gabinet_recognition.xml");

            // 3. Wczytaj dane z XML
            var records = LoadXmlRecords(xmlPath);
            result.SourceRecords = records.Count;
            Console.WriteLine($"  Rekordy zrodlowe: {records.Count}");

            if (records.Count == 0)
            {
                result.Error = "Brak rekordow w pliku zrodlowym";
                return result;
            }

            // 4. Generuj CSV
            Directory.CreateDirectory(dataEtap2Path);
            var csvPath = Path.Combine(dataEtap2Path, "stale_choroby_pacjenta.csv");
            using var writer = new StreamWriter(csvPath, false, new UTF8Encoding(true));

            // Naglowek zgodny z old_etap2
            writer.WriteLine("InstalacjaId;PacjentId;PacjentIdImport;PacjentPesel;ICD10;NumerChoroby;Opis");

            // Wiersze danych
            int processedCount = 0;
            foreach (var record in records)
            {
                var patientId = record.GetValueOrDefault("patient", "");
                var pesel = "";
                if (!string.IsNullOrEmpty(patientId) && _patientPeselCache != null)
                {
                    _patientPeselCache.TryGetValue(patientId, out pesel);
                    pesel ??= "";
                }

                var instalacjaId = "";
                var pacjentId = "";
                var pacjentIdImport = patientId;
                var icd10 = EscapeCsvField(record.GetValueOrDefault("code", ""));
                var numerChoroby = record.GetValueOrDefault("pk", "");
                var opis = EscapeCsvField(record.GetValueOrDefault("name", ""));

                writer.WriteLine($"{instalacjaId};{pacjentId};{pacjentIdImport};{pesel};{icd10};{numerChoroby};{opis}");
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

    private void LoadPatientPeselCache(string dataEtap1Path)
    {
        var patientPath = Path.Combine(dataEtap1Path, "data_full", "gabinet_patient.xml");
        _patientPeselCache = new Dictionary<string, string>();

        if (!File.Exists(patientPath))
        {
            Console.WriteLine("  UWAGA: Brak pliku gabinet_patient.xml - PESEL nie bedzie wypelniony");
            return;
        }

        var doc = XDocument.Load(patientPath);
        var root = doc.Root;
        if (root == null) return;

        foreach (var obj in root.Elements("object"))
        {
            var pk = obj.Attribute("pk")?.Value;
            if (string.IsNullOrEmpty(pk)) continue;

            var pesel = "";
            foreach (var field in obj.Elements("field"))
            {
                var name = field.Attribute("name")?.Value;
                if (name == "pesel")
                {
                    pesel = field.Value?.Trim() ?? "";
                    if (pesel == "<None></None>" || pesel == "None")
                        pesel = "";
                    break;
                }
            }
            _patientPeselCache[pk] = pesel;
        }

        Console.WriteLine($"  Zaladowano PESEL dla {_patientPeselCache.Count} pacjentow");
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
