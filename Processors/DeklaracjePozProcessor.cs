using System.Text;
using MyDr_Import.Models;
using MyDr_Import.Services;

namespace MyDr_Import.Processors;

/// <summary>
/// Procesor dla modelu DEKLARACJE_POZ
/// Zrodlo: gabinet_nfzdeclaration.xml
/// Cel: deklaracje_poz.csv
/// </summary>
public class DeklaracjePozProcessor : IModelProcessor
{
    public string ModelName => "deklaracje_poz";
    public string XmlFileName => "gabinet_nfzdeclaration.xml";

    private Dictionary<string, string>? _patientPeselCache;
    private Dictionary<string, (string npwz, string pesel)>? _personCache;

    public CsvGenerationResult Process(string dataEtap1Path, string dataEtap2Path, ModelMapping mapping)
    {
        var result = new CsvGenerationResult
        {
            ModelName = "deklaracje_poz",
            TargetTable = "deklaracje_poz"
        };

        try
        {
            LoadPatientPeselCache(dataEtap1Path);
            LoadPersonCache(dataEtap1Path);

            var xmlPath = Path.Combine(dataEtap1Path, "data_full", XmlFileName);
            if (!File.Exists(xmlPath))
            {
                result.Error = $"Nie znaleziono pliku XML: {XmlFileName}";
                return result;
            }

            Console.WriteLine($"  Plik zrodlowy: {XmlFileName}");

            var records = LoadXmlRecords(xmlPath);
            result.SourceRecords = records.Count;
            Console.WriteLine($"  Rekordy zrodlowe: {records.Count}");

            if (records.Count == 0)
            {
                result.Error = "Brak rekordow w pliku zrodlowym";
                return result;
            }

            Directory.CreateDirectory(dataEtap2Path);
            var csvPath = Path.Combine(dataEtap2Path, "deklaracje_poz.csv");
            using var writer = new StreamWriter(csvPath, false, new UTF8Encoding(true));

            // Naglowek zgodny z old_etap2
            writer.WriteLine("InstalacjaId;IdImport;TypDeklaracjiPOZ;DataZlozenia;DataWygasniecia;JednostkaId;JednostkaIdImport;PacjentId;PacjentIdImport;PacjentPesel;TypPacjentaId;PracownikId;PracownikIdImport;PracownikNPWZ;PeselOpiekuna;PeselOpiekuna2;KodTypuPodopiecznego;TypSzkolyId;KodRodzajuSzkoly;NazwaSzkoly;PatronSzkoly;RegonSzkoly;UlicaSzkoly;KodPocztowySzkoly;KodGminySzkoly;MiejscowoscSzkoly;NrDomuSzkoly;NrTelefonuSzkoly;NrUmowyUbezpieczeniowej;NIP;ProfilaktykaFluorkowa;Komentarz;TypDeklaracjiPOZId");

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

                var personId = record.GetValueOrDefault("personnel", "");
                var pracownikNpwz = "";
                if (!string.IsNullOrEmpty(personId) && _personCache != null && _personCache.TryGetValue(personId, out var personData))
                {
                    pracownikNpwz = personData.npwz;
                }

                var idImport = record.GetValueOrDefault("pk", "");
                var typDeklaracji = record.GetValueOrDefault("type", "");
                var dataZlozenia = FormatDateTime(record.GetValueOrDefault("creation_date", ""));
                var dataWygasniecia = FormatDateTime(record.GetValueOrDefault("deletion_date", ""));
                var jednostkaIdImport = record.GetValueOrDefault("department", "");
                var typPacjentaId = record.GetValueOrDefault("ward_type", "") != "" ? "1" : "1";
                var profFluor = record.GetValueOrDefault("prof_fluor", "") == "True" ? "1" : "0";
                var komentarz = EscapeCsvField(record.GetValueOrDefault("note", ""));

                writer.WriteLine($";{idImport};{typDeklaracji};{dataZlozenia};{dataWygasniecia};;{jednostkaIdImport};;{patientId};{pesel};{typPacjentaId};;{personId};{pracownikNpwz};;;;;;;;;;;;;;;;;;{profFluor};{komentarz};");
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
                        if (name == "pwz") npwz = objReader.ReadElementContentAsString()?.Trim() ?? "";
                        else if (name == "pesel") pesel = objReader.ReadElementContentAsString()?.Trim() ?? "";
                    }
                }
                if (npwz == "None") npwz = "";
                if (pesel == "None") pesel = "";
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

    private string Clean(string value)
    {
        if (string.IsNullOrEmpty(value) || value == "None" || value.Contains("<None")) return "";
        return value;
    }
}
