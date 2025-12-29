using System.Text;
using MyDr_Import.Models;
using MyDr_Import.Services;

namespace MyDr_Import.Processors;

/// <summary>
/// Procesor dla modelu KARTY_WIZYT
/// Zrodlo: gabinet_visit.xml (dane wizyty z polami medycznymi)
/// Cel: karty_wizyt.csv
/// </summary>
public class KartyWizytProcessor : IModelProcessor
{
    public string ModelName => "karty_wizyt";
    public string XmlFileName => "gabinet_visit.xml";

    private Dictionary<string, (string npwz, string pesel)>? _personCache;
    private Dictionary<string, List<string>>? _visitIcd10Cache;
    private Dictionary<string, List<string>>? _visitIcd9Cache;
    private Dictionary<string, string>? _icd10CodeCache;
    private Dictionary<string, string>? _icd9CodeCache;

    public CsvGenerationResult Process(string dataEtap1Path, string dataEtap2Path, ModelMapping mapping)
    {
        var result = new CsvGenerationResult
        {
            ModelName = "karty_wizyt",
            TargetTable = "karty_wizyt"
        };

        try
        {
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
            var csvPath = Path.Combine(dataEtap2Path, "karty_wizyt.csv");
            using var writer = new StreamWriter(csvPath, false, new UTF8Encoding(true));

            // Naglowek zgodny z old_etap2
            writer.WriteLine("InstalacjaId;IdImport;IdImportPrefix;DataWystawienia;DataAutoryzacji;PracownikWystawiajacyIdImport;PracownikWystawiajacyNpwz;PracownikWystawiajacyPesel;WizytaIdImport;Wywiad;BadaniePrzedmiotowe;PrzyjmowaneLeki;PrzebiegLeczenia;Zalecenia;Zabiegi;Inne;NiezdolnoscOd;NiezdolnoscDo;RozpoznaniaICD10;RozpoznanieGlowneICD10;RozpoznanieWspolistniejaceICD10;RozpoznanieOpisowe;ProceduryICD9");

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

                    // Only process completed visits (Archiwalna = archived/done) - matches old system behavior
                    var state = record.GetValueOrDefault("state", "");
                    if (record.Count > 0 && (state == "Archiwalna" || state == "done"))
                    {
                        WriteVisitCardRecord(writer, record);
                        processedCount++;
                        
                        if (processedCount % 100000 == 0)
                            Console.WriteLine($"    Przetworzono {processedCount} kart wizyt...");
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

    private void WriteVisitCardRecord(StreamWriter writer, Dictionary<string, string> record)
    {
        var doctorId = record.GetValueOrDefault("doctor", "");
        var pracownikNpwz = "";
        var pracownikPesel = "";
        if (!string.IsNullOrEmpty(doctorId) && _personCache != null && _personCache.TryGetValue(doctorId, out var personData))
        {
            pracownikNpwz = personData.npwz;
            pracownikPesel = personData.pesel;
        }

        var idImport = record.GetValueOrDefault("pk", "");
        var idImportPrefix = "VISITCARD";
        var dataWystawienia = FormatDate(record.GetValueOrDefault("date", ""));
        var dataAutoryzacji = FormatDate(record.GetValueOrDefault("last_revision", ""));
        if (string.IsNullOrEmpty(dataAutoryzacji)) dataAutoryzacji = dataWystawienia;
        var wizytaIdImport = record.GetValueOrDefault("pk", "");
        var wywiad = Escape(record.GetValueOrDefault("interview", ""));
        var badaniePrzedmiotowe = Escape(record.GetValueOrDefault("examination", ""));
        var przebiegLeczenia = "";
        var zalecenia = Escape(record.GetValueOrDefault("recommendation", ""));
        var inne = Escape(record.GetValueOrDefault("note", ""));
        var rozpoznanieOpisowe = Escape(record.GetValueOrDefault("recognition_description", ""));

        // Get ICD10 codes for this visit
        var icd10Codes = "";
        var icd10Main = "";
        var icd10Other = "";
        if (_visitIcd10Cache != null && _visitIcd10Cache.TryGetValue(idImport, out var icd10List) && icd10List.Count > 0)
        {
            icd10Codes = string.Join(",", icd10List);
            icd10Main = icd10List[0];
            if (icd10List.Count > 1) icd10Other = string.Join(",", icd10List.Skip(1));
        }
        
        // Get ICD9 codes for this visit
        var icd9Codes = "";
        if (_visitIcd9Cache != null && _visitIcd9Cache.TryGetValue(idImport, out var icd9List))
            icd9Codes = string.Join(",", icd9List);

        writer.WriteLine($";{idImport};{idImportPrefix};{dataWystawienia};{dataAutoryzacji};{doctorId};{pracownikNpwz};{pracownikPesel};{wizytaIdImport};{wywiad};{badaniePrzedmiotowe};;{przebiegLeczenia};{zalecenia};;{inne};;;{icd10Codes};{icd10Main};{icd10Other};{rozpoznanieOpisowe};{icd9Codes}");
        
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

    private string FormatDate(string value)
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
                        if (name == "code") { code = objReader.ReadElementContentAsString()?.Trim() ?? ""; break; }
                    }
                }
                if (!string.IsNullOrEmpty(code) && code != "None") _icd10CodeCache[pk] = code;
            }
        }
    }

    private void LoadIcd9CodeCache(string dataEtap1Path)
    {
        var path = Path.Combine(dataEtap1Path, "data_full", "gabinet_icd9.xml");
        _icd9CodeCache = new Dictionary<string, string>();
        if (!File.Exists(path)) return;

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
                        if (name == "code") { code = objReader.ReadElementContentAsString()?.Trim() ?? ""; break; }
                    }
                }
                if (!string.IsNullOrEmpty(code) && code != "None") _icd9CodeCache[pk] = code;
            }
        }
    }

    private void LoadVisitIcd10Cache(string dataEtap1Path)
    {
        var path = Path.Combine(dataEtap1Path, "data_full", "gabinet_recognition.xml");
        _visitIcd10Cache = new Dictionary<string, List<string>>();
        if (!File.Exists(path)) return;

        using var stream = File.OpenRead(path);
        using var reader = System.Xml.XmlReader.Create(stream, new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Ignore });
        
        while (reader.Read())
        {
            if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "object")
            {
                string visitId = "", icd10Id = "";
                using var objReader = reader.ReadSubtree();
                while (objReader.Read())
                {
                    if (objReader.NodeType == System.Xml.XmlNodeType.Element && objReader.Name == "field")
                    {
                        var name = objReader.GetAttribute("name");
                        if (name == "visit") visitId = objReader.ReadElementContentAsString()?.Trim() ?? "";
                        else if (name == "icd10") icd10Id = objReader.ReadElementContentAsString()?.Trim() ?? "";
                    }
                }
                if (!string.IsNullOrEmpty(visitId) && !string.IsNullOrEmpty(icd10Id) && 
                    visitId != "None" && !visitId.Contains("<None") &&
                    _icd10CodeCache != null && _icd10CodeCache.TryGetValue(icd10Id, out var code))
                {
                    if (!_visitIcd10Cache.ContainsKey(visitId)) _visitIcd10Cache[visitId] = new List<string>();
                    if (!_visitIcd10Cache[visitId].Contains(code)) _visitIcd10Cache[visitId].Add(code);
                }
            }
        }
    }

    private void LoadVisitIcd9Cache(string dataEtap1Path)
    {
        var path = Path.Combine(dataEtap1Path, "data_full", "gabinet_medicalprocedure.xml");
        _visitIcd9Cache = new Dictionary<string, List<string>>();
        if (!File.Exists(path)) return;

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
                        if (name == "visit") visitId = objReader.ReadElementContentAsString()?.Trim() ?? "";
                        else if (name == "icd9") icd9Id = objReader.ReadElementContentAsString()?.Trim() ?? "";
                    }
                }
                if (!string.IsNullOrEmpty(visitId) && !string.IsNullOrEmpty(icd9Id) && 
                    visitId != "None" && !visitId.Contains("<None") &&
                    _icd9CodeCache != null && _icd9CodeCache.TryGetValue(icd9Id, out var code))
                {
                    if (!_visitIcd9Cache.ContainsKey(visitId)) _visitIcd9Cache[visitId] = new List<string>();
                    if (!_visitIcd9Cache[visitId].Contains(code)) _visitIcd9Cache[visitId].Add(code);
                }
            }
        }
    }
}
