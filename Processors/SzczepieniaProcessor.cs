using System.Text;
using MyDr_Import.Models;
using MyDr_Import.Services;

namespace MyDr_Import.Processors;

/// <summary>
/// Procesor dla modelu SZCZEPIENIA
/// Zrodlo: gabinet_vaccination.xml
/// Cel: szczepienia.csv
/// </summary>
public class SzczepieniaProcessor : IModelProcessor
{
    public string ModelName => "szczepienia";
    public string XmlFileName => "gabinet_vaccination.xml";

    private Dictionary<string, string>? _patientPeselCache;
    private Dictionary<string, (string npwz, string pesel)>? _personCache;

    public CsvGenerationResult Process(string dataEtap1Path, string dataEtap2Path, ModelMapping mapping)
    {
        var result = new CsvGenerationResult
        {
            ModelName = "szczepienia",
            TargetTable = "szczepienia"
        };

        try
        {
            // 1. Zaladuj cache
            LoadPatientPeselCache(dataEtap1Path);
            LoadPersonCache(dataEtap1Path);

            // 2. Znajdz plik XML
            var xmlPath = Path.Combine(dataEtap1Path, "data_full", XmlFileName);
            if (!File.Exists(xmlPath))
            {
                result.Error = $"Nie znaleziono pliku XML: {XmlFileName}";
                return result;
            }

            Console.WriteLine($"  Plik zrodlowy: {XmlFileName}");

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
            var csvPath = Path.Combine(dataEtap2Path, "szczepienia.csv");
            using var writer = new StreamWriter(csvPath, false, new UTF8Encoding(true));

            // Naglowek zgodny z old_etap2
            writer.WriteLine("InstalacjaId;IdImport;PacjentIdImport;PacjentPesel;PracownikIdImport;PracownikNPWZ;PracownikPesel;Nazwa;MiejscePodania;NrSerii;DataPodania;DataWaznosci;DrogaPodaniaId;CzyZKalendarza;SzczepienieId;Dawka");

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

                var personId = record.GetValueOrDefault("person", "");
                var pracownikNpwz = "";
                var pracownikPesel = "";
                if (!string.IsNullOrEmpty(personId) && _personCache != null && _personCache.TryGetValue(personId, out var personData))
                {
                    pracownikNpwz = personData.npwz;
                    pracownikPesel = personData.pesel;
                }

                var idImport = record.GetValueOrDefault("pk", "");
                var nazwa = EscapeCsvField(record.GetValueOrDefault("drug", ""));
                var miejscePodania = record.GetValueOrDefault("vaccination_site", "");
                var nrSerii = record.GetValueOrDefault("vaccine_series", "");
                var dataPodania = FormatDateTime(record.GetValueOrDefault("datetime", ""));
                var dataWaznosci = FormatDateTime(record.GetValueOrDefault("expiration_date", ""));
                var czyZKalendarza = record.GetValueOrDefault("vaccination_kind", "") == "scheduled" ? "1" : "0";
                var dawka = record.GetValueOrDefault("dose", "");

                writer.WriteLine($";{idImport};{patientId};{pesel};{personId};{pracownikNpwz};{pracownikPesel};{nazwa};{miejscePodania};{nrSerii};{dataPodania};{dataWaznosci};;{czyZKalendarza};;{dawka}");
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

        if (!File.Exists(patientPath)) return;

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

    private void LoadPersonCache(string dataEtap1Path)
    {
        var personPath = Path.Combine(dataEtap1Path, "data_full", "gabinet_person.xml");
        _personCache = new Dictionary<string, (string npwz, string pesel)>();

        if (!File.Exists(personPath)) return;

        using var stream = File.OpenRead(personPath);
        using var reader = System.Xml.XmlReader.Create(stream, new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Ignore });
        
        while (reader.Read())
        {
            if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "object")
            {
                var pk = reader.GetAttribute("pk");
                if (string.IsNullOrEmpty(pk)) continue;

                var npwz = "";
                var pesel = "";
                using var objReader = reader.ReadSubtree();
                while (objReader.Read())
                {
                    if (objReader.NodeType == System.Xml.XmlNodeType.Element && objReader.Name == "field")
                    {
                        var name = objReader.GetAttribute("name");
                        if (name == "pwz")
                        {
                            npwz = objReader.ReadElementContentAsString()?.Trim() ?? "";
                            if (npwz == "None") npwz = "";
                        }
                        else if (name == "pesel")
                        {
                            pesel = objReader.ReadElementContentAsString()?.Trim() ?? "";
                            if (pesel == "None") pesel = "";
                        }
                    }
                }
                _personCache[pk] = (npwz, pesel);
            }
        }
        Console.WriteLine($"  Zaladowano {_personCache.Count} pracownikow");
    }

    private List<Dictionary<string, string>> LoadXmlRecords(string xmlPath)
    {
        var records = new List<Dictionary<string, string>>();
        
        using var stream = File.OpenRead(xmlPath);
        using var reader = System.Xml.XmlReader.Create(stream, new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Ignore });
        
        while (reader.Read())
        {
            if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "object")
            {
                var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var pk = reader.GetAttribute("pk");
                if (!string.IsNullOrEmpty(pk)) record["pk"] = pk;

                using var objReader = reader.ReadSubtree();
                while (objReader.Read())
                {
                    if (objReader.NodeType == System.Xml.XmlNodeType.Element && objReader.Name == "field")
                    {
                        var name = objReader.GetAttribute("name");
                        if (!string.IsNullOrEmpty(name))
                        {
                            var value = objReader.ReadElementContentAsString()?.Trim() ?? "";
                            if (value == "None" || value == "<None></None>" || value.Contains("<None")) value = "";
                            record[name] = value;
                        }
                    }
                }
                if (record.Count > 0) records.Add(record);
            }
        }
        return records;
    }

    private string EscapeCsvField(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains('"') || value.Contains(';') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    private string FormatDateTime(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (DateTime.TryParse(value, out var dt))
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        return value;
    }
}
