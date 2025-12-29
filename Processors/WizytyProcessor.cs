using System.Text;
using MyDr_Import.Models;
using MyDr_Import.Services;

namespace MyDr_Import.Processors;

/// <summary>
/// Procesor dla modelu WIZYTY
/// Zrodlo: gabinet_visit.xml
/// Cel: wizyty.csv
/// </summary>
public class WizytyProcessor : IModelProcessor
{
    public string ModelName => "wizyty";
    public string XmlFileName => "gabinet_visit.xml";

    private Dictionary<string, string>? _patientPeselCache;
    private Dictionary<string, (string npwz, string pesel)>? _personCache;
    private Dictionary<string, List<string>>? _visitIcd10Cache;  // visit_id -> list of ICD10 codes
    private Dictionary<string, List<string>>? _visitIcd9Cache;   // visit_id -> list of ICD9 codes
    private Dictionary<string, string>? _icd10CodeCache;         // icd10_pk -> code
    private Dictionary<string, string>? _icd9CodeCache;          // icd9_pk -> code

    public CsvGenerationResult Process(string dataEtap1Path, string dataEtap2Path, ModelMapping mapping)
    {
        var result = new CsvGenerationResult
        {
            ModelName = "wizyty",
            TargetTable = "wizyty"
        };

        try
        {
            LoadPatientPeselCache(dataEtap1Path);
            LoadPersonCache(dataEtap1Path);
            LoadIcd10CodeCache(dataEtap1Path);
            LoadIcd9CodeCache(dataEtap1Path);
            LoadVisitIcd10Cache(dataEtap1Path);
            LoadVisitIcd9Cache(dataEtap1Path);

            var xmlPath = Path.Combine(dataEtap1Path, "data_full", XmlFileName);
            if (!File.Exists(xmlPath))
            {
                result.Error = $"Nie znaleziono pliku XML: {XmlFileName}";
                return result;
            }

            Console.WriteLine($"  Plik zrodlowy: {XmlFileName}");
            Console.WriteLine($"  Ladowanie gabinet_visit.xml (duzy plik ~3GB)...");

            Directory.CreateDirectory(dataEtap2Path);
            var csvPath = Path.Combine(dataEtap2Path, "wizyty.csv");
            using var writer = new StreamWriter(csvPath, false, new UTF8Encoding(true));

            // Naglowek zgodny z old_etap2
            writer.WriteLine("InstalacjaId;IdImport;JednostkaId;JednostkaIdImport;PacjentId;PacjentIdImport;PacjentPesel;PracownikId;PracownikIdImport;ZasobIdImport;PracownikNPWZ;PracownikPesel;PlatnikIdImportu;JednostkaRozliczeniowaId;JednostkaRozliczeniowaIdImportu;DataUtworzenia;DataOd;DataDo;CzasOd;CzasDo;Status;NFZ;NieRozliczaj;Dodatkowy;Komentarz;TrybPrzyjecia;TrybDalszegoLeczenia;TypWizyty;KodSwiadczeniaNFZ;KodUprawnieniaPacjenta;ProceduryICD9;RozpoznaniaICD10;DokumentSkierowujacyIdImportu");

            int processedCount = 0;
            
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

                    if (record.Count > 0)
                    {
                        WriteVisitRecord(writer, record);
                        processedCount++;
                        
                        if (processedCount % 100000 == 0)
                            Console.WriteLine($"    Przetworzono {processedCount} wizyt...");
                    }
                }
            }

            result.SourceRecords = processedCount;
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

    private void WriteVisitRecord(StreamWriter writer, Dictionary<string, string> record)
    {
        var patientId = record.GetValueOrDefault("patient", "");
        var pesel = "";
        if (!string.IsNullOrEmpty(patientId) && _patientPeselCache != null)
        {
            _patientPeselCache.TryGetValue(patientId, out pesel);
            pesel ??= "";
        }

        var doctorId = record.GetValueOrDefault("doctor", "");
        var pracownikNpwz = "";
        var pracownikPesel = "";
        if (!string.IsNullOrEmpty(doctorId) && _personCache != null && _personCache.TryGetValue(doctorId, out var personData))
        {
            pracownikNpwz = personData.npwz;
            pracownikPesel = personData.pesel;
        }

        var idImport = record.GetValueOrDefault("pk", "");
        var jednostkaIdImport = record.GetValueOrDefault("facility", "");
        var dataUtworzenia = FormatDateTime(record.GetValueOrDefault("created", ""));
        var dataOd = record.GetValueOrDefault("date", "");
        var dataDo = record.GetValueOrDefault("date", "");
        var czasOd = record.GetValueOrDefault("timeFrom", "");
        var czasDo = record.GetValueOrDefault("timeTo", "");
        var status = MapVisitState(record.GetValueOrDefault("state", ""));
        var nfz = record.GetValueOrDefault("referral_needed", "") == "True" ? "1" : "0";
        var nieRozliczaj = "0";
        var dodatkowy = record.GetValueOrDefault("evisit", "") == "True" ? "1" : "0";
        var komentarz = "";
        var trybPrzyjecia = MapReceptionMode(record.GetValueOrDefault("reception_mode_choice", ""));
        var typWizyty = record.GetValueOrDefault("visit_kind", "") == "followup" ? "2" : "1";

        // Get ICD10 codes for this visit
        var icd10Codes = "";
        if (_visitIcd10Cache != null && _visitIcd10Cache.TryGetValue(idImport, out var icd10List))
            icd10Codes = string.Join(",", icd10List);
        
        // Get ICD9 codes for this visit
        var icd9Codes = "";
        if (_visitIcd9Cache != null && _visitIcd9Cache.TryGetValue(idImport, out var icd9List))
            icd9Codes = string.Join(",", icd9List);

        writer.WriteLine($";{idImport};;{jednostkaIdImport};;{patientId};{pesel};;{doctorId};;{pracownikNpwz};{pracownikPesel};;;;{dataUtworzenia};{dataOd};{dataDo};{czasOd};{czasDo};{status};{nfz};{nieRozliczaj};{dodatkowy};{Escape(komentarz)};{trybPrzyjecia};;{typWizyty};;;{icd9Codes};{icd10Codes};");
    }

    private string MapVisitState(string state)
    {
        return state switch
        {
            "done" => "9",
            "cancelled" => "8",
            "planned" => "1",
            "in_progress" => "5",
            _ => "1"
        };
    }

    private string MapReceptionMode(string mode)
    {
        return mode switch
        {
            "emergency" => "2",
            "scheduled" => "5",
            _ => "5"
        };
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

    private string Escape(string value)
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

    private void LoadIcd10CodeCache(string dataEtap1Path)
    {
        var path = Path.Combine(dataEtap1Path, "data_full", "gabinet_icd10.xml");
        _icd10CodeCache = new Dictionary<string, string>();
        if (!File.Exists(path)) return;

        Console.WriteLine("  Ladowanie gabinet_icd10.xml...");
        using var stream = File.OpenRead(path);
        using var reader = System.Xml.XmlReader.Create(stream, new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Ignore });
        
        while (reader.Read())
        {
            if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "object")
            {
                var pk = reader.GetAttribute("pk");
                if (string.IsNullOrEmpty(pk)) continue;
                string code = "";
                using var objReader = reader.ReadSubtree();
                while (objReader.Read())
                {
                    if (objReader.NodeType == System.Xml.XmlNodeType.Element && objReader.Name == "field")
                    {
                        var name = objReader.GetAttribute("name");
                        if (name == "code")
                        {
                            code = objReader.ReadElementContentAsString()?.Trim() ?? "";
                            break;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(code) && code != "None")
                    _icd10CodeCache[pk] = code;
            }
        }
        Console.WriteLine($"  Zaladowano {_icd10CodeCache.Count} kodow ICD10");
    }

    private void LoadIcd9CodeCache(string dataEtap1Path)
    {
        var path = Path.Combine(dataEtap1Path, "data_full", "gabinet_icd9.xml");
        _icd9CodeCache = new Dictionary<string, string>();
        if (!File.Exists(path)) return;

        Console.WriteLine("  Ladowanie gabinet_icd9.xml...");
        using var stream = File.OpenRead(path);
        using var reader = System.Xml.XmlReader.Create(stream, new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Ignore });
        
        while (reader.Read())
        {
            if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "object")
            {
                var pk = reader.GetAttribute("pk");
                if (string.IsNullOrEmpty(pk)) continue;
                string code = "";
                using var objReader = reader.ReadSubtree();
                while (objReader.Read())
                {
                    if (objReader.NodeType == System.Xml.XmlNodeType.Element && objReader.Name == "field")
                    {
                        var name = objReader.GetAttribute("name");
                        if (name == "code")
                        {
                            code = objReader.ReadElementContentAsString()?.Trim() ?? "";
                            break;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(code) && code != "None")
                    _icd9CodeCache[pk] = code;
            }
        }
        Console.WriteLine($"  Zaladowano {_icd9CodeCache.Count} kodow ICD9");
    }

    private void LoadVisitIcd10Cache(string dataEtap1Path)
    {
        var path = Path.Combine(dataEtap1Path, "data_full", "gabinet_recognition.xml");
        _visitIcd10Cache = new Dictionary<string, List<string>>();
        if (!File.Exists(path))
        {
            Console.WriteLine("  UWAGA: Brak pliku gabinet_recognition.xml");
            return;
        }

        Console.WriteLine("  Ladowanie gabinet_recognition.xml (rozpoznania ICD10)...");
        int totalRecords = 0;
        int matchedRecords = 0;
        
        using var stream = File.OpenRead(path);
        using var reader = System.Xml.XmlReader.Create(stream, new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Ignore });
        
        while (reader.Read())
        {
            if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "object")
            {
                totalRecords++;
                string visitId = "", icd10Id = "";
                using var objReader = reader.ReadSubtree();
                while (objReader.Read())
                {
                    if (objReader.NodeType == System.Xml.XmlNodeType.Element && objReader.Name == "field")
                    {
                        var name = objReader.GetAttribute("name");
                        if (name == "visit")
                        {
                            visitId = objReader.ReadElementContentAsString()?.Trim() ?? "";
                            if (visitId == "None" || visitId.Contains("<None")) visitId = "";
                        }
                        else if (name == "icd10")
                        {
                            icd10Id = objReader.ReadElementContentAsString()?.Trim() ?? "";
                            if (icd10Id == "None" || icd10Id.Contains("<None")) icd10Id = "";
                        }
                    }
                }
                if (!string.IsNullOrEmpty(visitId) && !string.IsNullOrEmpty(icd10Id) &&
                    _icd10CodeCache != null && _icd10CodeCache.TryGetValue(icd10Id, out var code))
                {
                    matchedRecords++;
                    if (!_visitIcd10Cache.ContainsKey(visitId))
                        _visitIcd10Cache[visitId] = new List<string>();
                    if (!_visitIcd10Cache[visitId].Contains(code))
                        _visitIcd10Cache[visitId].Add(code);
                }
            }
        }
        Console.WriteLine($"  Przetworzono {totalRecords} rozpoznan, dopasowano {matchedRecords} do wizyt");
        Console.WriteLine($"  Zaladowano rozpoznania ICD10 dla {_visitIcd10Cache.Count} wizyt");
    }

    private void LoadVisitIcd9Cache(string dataEtap1Path)
    {
        var path = Path.Combine(dataEtap1Path, "data_full", "gabinet_medicalprocedure.xml");
        _visitIcd9Cache = new Dictionary<string, List<string>>();
        if (!File.Exists(path)) return;

        Console.WriteLine("  Ladowanie gabinet_medicalprocedure.xml (procedury ICD9)...");
        using var stream = File.OpenRead(path);
        using var reader = System.Xml.XmlReader.Create(stream, new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Ignore });
        
        while (reader.Read())
        {
            if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "object")
            {
                string visitId = "", icd9Id = "";
                using var objReader = reader.ReadSubtree();
                while (objReader.Read())
                {
                    if (objReader.NodeType == System.Xml.XmlNodeType.Element && objReader.Name == "field")
                    {
                        var name = objReader.GetAttribute("name");
                        if (name == "visit")
                            visitId = objReader.ReadElementContentAsString()?.Trim() ?? "";
                        else if (name == "icd9")
                            icd9Id = objReader.ReadElementContentAsString()?.Trim() ?? "";
                    }
                }
                if (!string.IsNullOrEmpty(visitId) && !string.IsNullOrEmpty(icd9Id) && 
                    visitId != "None" && !visitId.Contains("<None") &&
                    _icd9CodeCache != null && _icd9CodeCache.TryGetValue(icd9Id, out var code))
                {
                    if (!_visitIcd9Cache.ContainsKey(visitId))
                        _visitIcd9Cache[visitId] = new List<string>();
                    if (!_visitIcd9Cache[visitId].Contains(code))
                        _visitIcd9Cache[visitId].Add(code);
                }
            }
        }
        Console.WriteLine($"  Zaladowano procedury ICD9 dla {_visitIcd9Cache.Count} wizyt");
    }
}
