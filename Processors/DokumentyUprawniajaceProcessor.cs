using System.Text;
using MyDr_Import.Models;
using MyDr_Import.Services;

namespace MyDr_Import.Processors;

/// <summary>
/// Procesor dla modelu DOKUMENTY_UPRAWNIAJACE
/// Zrodlo: gabinet_insurancedocuments.xml
/// Cel: dokumenty_uprawniajace.csv
/// </summary>
public class DokumentyUprawniajaceProcessor : IModelProcessor
{
    public string ModelName => "dokumenty_uprawniajace";
    public string XmlFileName => "gabinet_insurancedocuments.xml";

    private Dictionary<string, string>? _patientPeselCache;
    private Dictionary<string, string>? _insurancePatientCache;

    public CsvGenerationResult Process(string dataEtap1Path, string dataEtap2Path, ModelMapping mapping)
    {
        var result = new CsvGenerationResult
        {
            ModelName = "dokumenty_uprawniajace",
            TargetTable = "dokumenty_uprawniajace"
        };

        try
        {
            LoadPatientPeselCache(dataEtap1Path);
            LoadInsurancePatientCache(dataEtap1Path);

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
            var csvPath = Path.Combine(dataEtap2Path, "dokumenty_uprawniajace.csv");
            using var writer = new StreamWriter(csvPath, false, new UTF8Encoding(true));

            // Naglowek zgodny z old_etap2
            writer.WriteLine("InstalacjaId;IdImport;KodDokumentu;KodUprawnienia;NazwaDokumentu;PacjentId;PacjentIdImport;PacjentPesel;KodOddzialuNFZ;NIP;DataOd;DataDo;DataWystawienia;KodInstytucjiWystawiajacej;NazwaInstytucjiWystawiajacej;Numer;TypOswiadczenia;PodstawaOswiadczenia;PeselOpiekuna;RodzajZezwoleniaLubOchrony;InstytucjaWystawiajaca;EwusId");

            int processedCount = 0;
            foreach (var record in records)
            {
                // Pobierz patient przez insurance relation
                var insuranceId = record.GetValueOrDefault("insurance", "");
                var patientId = "";
                if (!string.IsNullOrEmpty(insuranceId) && _insurancePatientCache != null)
                {
                    _insurancePatientCache.TryGetValue(insuranceId, out patientId);
                    patientId ??= "";
                }
                
                var pesel = "";
                if (!string.IsNullOrEmpty(patientId) && _patientPeselCache != null)
                {
                    _patientPeselCache.TryGetValue(patientId, out pesel);
                    pesel ??= "";
                }

                var idImport = record.GetValueOrDefault("pk", "");
                var kodDokumentu = record.GetValueOrDefault("document_type", "OS");
                var kodUprawnienia = record.GetValueOrDefault("entitlement_code", "U");
                var nazwaDokumentu = Escape(record.GetValueOrDefault("document_name", ""));
                var kodOddzialuNfz = record.GetValueOrDefault("nfz_branch", "");
                var nip = record.GetValueOrDefault("nip", "");
                var dataOd = record.GetValueOrDefault("valid_from", "");
                var dataDo = record.GetValueOrDefault("valid_to", "");
                var dataWystawienia = record.GetValueOrDefault("issue_date", "");
                var kodInstytucji = record.GetValueOrDefault("institution_code", "");
                var nazwaInstytucji = Escape(record.GetValueOrDefault("institution_name", ""));
                var numer = record.GetValueOrDefault("document_number", "");
                var typOswiadczenia = record.GetValueOrDefault("statement_type", "");
                var podstawaOswiadczenia = record.GetValueOrDefault("statement_basis", "1");
                var peselOpiekuna = "";
                var rodzajZezwolenia = record.GetValueOrDefault("permit_type", "");

                writer.WriteLine($";{idImport};{kodDokumentu};{kodUprawnienia};{nazwaDokumentu};;{patientId};{pesel};{kodOddzialuNfz};{nip};{dataOd};{dataDo};{dataWystawienia};{kodInstytucji};{nazwaInstytucji};{numer};{typOswiadczenia};{podstawaOswiadczenia};{peselOpiekuna};{rodzajZezwolenia};;");
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

    private void LoadInsurancePatientCache(string dataEtap1Path)
    {
        var insurancePath = Path.Combine(dataEtap1Path, "data_full", "gabinet_insurance.xml");
        _insurancePatientCache = new Dictionary<string, string>();

        if (!File.Exists(insurancePath)) return;

        using var stream = File.OpenRead(insurancePath);
        using var reader = System.Xml.XmlReader.Create(stream, new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Ignore });
        
        while (reader.Read())
        {
            if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "object")
            {
                var pk = reader.GetAttribute("pk");
                if (string.IsNullOrEmpty(pk)) continue;

                var patientId = "";
                using var objReader = reader.ReadSubtree();
                while (objReader.Read())
                {
                    if (objReader.NodeType == System.Xml.XmlNodeType.Element && objReader.Name == "field")
                    {
                        var name = objReader.GetAttribute("name");
                        if (name == "patient")
                        {
                            patientId = objReader.ReadElementContentAsString()?.Trim() ?? "";
                            if (patientId == "None" || patientId.Contains("<None")) patientId = "";
                            break;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(patientId))
                    _insurancePatientCache[pk] = patientId;
            }
        }
        Console.WriteLine($"  Zaladowano {_insurancePatientCache.Count} relacji insurance->patient");
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

    private string Escape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains('"') || value.Contains(';') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
