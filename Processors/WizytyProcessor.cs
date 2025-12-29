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
    private Dictionary<string, (string icd10codes, string icd9codes)>? _visitDiagnosesCache;

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

        writer.WriteLine($";{idImport};;{jednostkaIdImport};;{patientId};{pesel};;{doctorId};;{pracownikNpwz};{pracownikPesel};;;;{dataUtworzenia};{dataOd};{dataDo};{czasOd};{czasDo};{status};{nfz};{nieRozliczaj};{dodatkowy};{Escape(komentarz)};{trybPrzyjecia};;{typWizyty};;;;");
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
}
