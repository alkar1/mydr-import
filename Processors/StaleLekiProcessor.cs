using System.Text;
using System.Xml.Linq;
using MyDr_Import.Models;
using MyDr_Import.Services;

namespace MyDr_Import.Processors;

/// <summary>
/// Procesor dla modelu STALE_LEKI_PACJENTA
/// Zrodlo: gabinet_patientpermanentdrug.xml
/// Cel: stale_leki_pacjenta.csv
/// Dodaje pole PacjentPesel poprzez dolaczenie danych z gabinet_patient.xml
/// </summary>
public class StaleLekiProcessor : IModelProcessor
{
    public string ModelName => "stale_leki_pacjenta";
    public string XmlFileName => "gabinet_patientpermanentdrug.xml";

    private Dictionary<string, string>? _patientPeselCache;
    private Dictionary<string, string>? _verEanCache;

    public CsvGenerationResult Process(string dataEtap1Path, string dataEtap2Path, ModelMapping mapping)
    {
        var result = new CsvGenerationResult
        {
            ModelName = "stale_leki_pacjenta",
            TargetTable = "stale_leki_pacjenta"
        };

        try
        {
            // 1. Zaladuj cache PESEL pacjentow
            LoadPatientPeselCache(dataEtap1Path);
            
            // 2. Zaladuj cache kodow kreskowych z gabinet_ver.xml
            LoadVerEanCache(dataEtap1Path);

            // 3. Znajdz plik XML
            var xmlPath = Path.Combine(dataEtap1Path, "data_full", "gabinet_patientpermanentdrug.xml");
            if (!File.Exists(xmlPath))
            {
                result.Error = $"Nie znaleziono pliku XML: gabinet_patientpermanentdrug.xml";
                return result;
            }

            Console.WriteLine($"  Plik zrodlowy: gabinet_patientpermanentdrug.xml");

            // 4. Wczytaj dane z XML
            var records = LoadXmlRecords(xmlPath);
            result.SourceRecords = records.Count;
            Console.WriteLine($"  Rekordy zrodlowe: {records.Count}");

            if (records.Count == 0)
            {
                result.Error = "Brak rekordow w pliku zrodlowym";
                return result;
            }

            // 5. Generuj CSV
            Directory.CreateDirectory(dataEtap2Path);
            var csvPath = Path.Combine(dataEtap2Path, "stale_leki_pacjenta.csv");
            using var writer = new StreamWriter(csvPath, false, new UTF8Encoding(true));

            // Naglowek zgodny z old_etap2
            writer.WriteLine("InstalacjaId;PacjentId;PacjentIdImport;PacjentPesel;PracownikId;PracownikIdImport;KodKreskowy;DataZalecenia;DataZakonczenia;Dawkowanie;Ilosc;RodzajIlosci;KodOdplatnosci");

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

                // Pobierz kod kreskowy z gabinet_ver przez relacje drug
                var drugId = record.GetValueOrDefault("drug", "");
                var kodKreskowy = "";
                if (!string.IsNullOrEmpty(drugId) && _verEanCache != null)
                {
                    _verEanCache.TryGetValue(drugId, out kodKreskowy);
                    kodKreskowy ??= "";
                }

                var instalacjaId = "";
                var pacjentId = "";
                var pacjentIdImport = patientId;
                var pracownikId = "";
                var pracownikIdImport = "";
                // DataZalecenia nie istnieje w XML - uzyj biezacej daty jako domyslnej (zgodnie z old_etap2)
                var dataZalecenia = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var dataZakonczenia = "";
                var dawkowanie = EscapeCsvField(record.GetValueOrDefault("dosation", ""));
                var ilosc = record.GetValueOrDefault("recommendation", "");
                var rodzajIlosci = record.GetValueOrDefault("is_part_of_box", "") == "True" ? "opakowanie" : "sztuki";
                var kodOdplatnosci = record.GetValueOrDefault("payment", "");

                writer.WriteLine($"{instalacjaId};{pacjentId};{pacjentIdImport};{pesel};{pracownikId};{pracownikIdImport};{EscapeCsvField(kodKreskowy)};{dataZalecenia};{dataZakonczenia};{dawkowanie};{ilosc};{rodzajIlosci};{kodOdplatnosci}");
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

        using var stream = File.OpenRead(patientPath);
        using var reader = System.Xml.XmlReader.Create(stream, new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Ignore });
        
        while (reader.Read())
        {
            if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "object")
            {
                var pk = reader.GetAttribute("pk");
                if (string.IsNullOrEmpty(pk)) continue;

                var pesel = "";
                using var objReader = reader.ReadSubtree();
                while (objReader.Read())
                {
                    if (objReader.NodeType == System.Xml.XmlNodeType.Element && objReader.Name == "field")
                    {
                        var name = objReader.GetAttribute("name");
                        if (name == "pesel")
                        {
                            pesel = objReader.ReadElementContentAsString()?.Trim() ?? "";
                            if (pesel == "None") pesel = "";
                            break;
                        }
                    }
                }
                _patientPeselCache[pk] = pesel;
            }
        }

        Console.WriteLine($"  Zaladowano PESEL dla {_patientPeselCache.Count} pacjentow");
    }

    private void LoadVerEanCache(string dataEtap1Path)
    {
        var verPath = Path.Combine(dataEtap1Path, "data_full", "gabinet_ver.xml");
        _verEanCache = new Dictionary<string, string>();

        if (!File.Exists(verPath))
        {
            Console.WriteLine("  UWAGA: Brak pliku gabinet_ver.xml - KodKreskowy nie bedzie wypelniony");
            return;
        }

        Console.WriteLine($"  Ladowanie gabinet_ver.xml (duzy plik ~935MB)...");
        
        using var stream = File.OpenRead(verPath);
        using var reader = System.Xml.XmlReader.Create(stream, new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Ignore });
        
        while (reader.Read())
        {
            if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "object")
            {
                var pk = reader.GetAttribute("pk");
                if (string.IsNullOrEmpty(pk)) continue;

                var ean13 = "";
                using var objReader = reader.ReadSubtree();
                while (objReader.Read())
                {
                    if (objReader.NodeType == System.Xml.XmlNodeType.Element && objReader.Name == "field")
                    {
                        var name = objReader.GetAttribute("name");
                        if (name == "ean13")
                        {
                            ean13 = objReader.ReadElementContentAsString()?.Trim() ?? "";
                            if (ean13 == "None") ean13 = "";
                            break;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(ean13))
                {
                    _verEanCache[pk] = ean13;
                }
            }
        }

        Console.WriteLine($"  Zaladowano {_verEanCache.Count} kodow kreskowych");
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
                    if (value == "<None></None>" || value == "None" || value.Contains("<None"))
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
