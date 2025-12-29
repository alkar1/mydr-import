using System.Text;
using MyDr_Import.Models;
using MyDr_Import.Services;

namespace MyDr_Import.Processors;

/// <summary>
/// Procesor dla modelu DOKUMENTACJA_ZALACZNIKI
/// Zrodlo: gabinet_documents.xml
/// Cel: dokumentacja_zalaczniki.csv
/// </summary>
public class DokumentacjaZalacznikiProcessor : IModelProcessor
{
    public string ModelName => "dokumentacja_zalaczniki";
    public string XmlFileName => "gabinet_documents.xml";

    private Dictionary<string, string>? _patientPeselCache;

    public CsvGenerationResult Process(string dataEtap1Path, string dataEtap2Path, ModelMapping mapping)
    {
        var result = new CsvGenerationResult
        {
            ModelName = "dokumentacja_zalaczniki",
            TargetTable = "dokumentacja_zalaczniki"
        };

        try
        {
            LoadPatientPeselCache(dataEtap1Path);

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
            var csvPath = Path.Combine(dataEtap2Path, "dokumentacja_zalaczniki.csv");
            using var writer = new StreamWriter(csvPath, false, new UTF8Encoding(true));

            // Naglowek zgodny z old_etap2
            writer.WriteLine("IdImport;InstalacjaId;JednostkaId;JednostkaIdImport;PacjentId;PacjentIdImport;PacjentPesel;WizytaIdImport;Data;NazwaPliku;Opis;SciezkaBazowa;Sciezka;TypPliku;LimitRozmiaruMB;PacjentNumer");

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

                var idImport = record.GetValueOrDefault("pk", "");
                var wizytaIdImport = record.GetValueOrDefault("visit", "");
                var data = FormatDateTime(record.GetValueOrDefault("uploaded_date", ""));
                var nazwaPliku = Escape(record.GetValueOrDefault("original_filename", ""));
                var opis = Escape(record.GetValueOrDefault("note", ""));
                var sciezka = record.GetValueOrDefault("uploaded_file", "");
                var typPliku = Path.GetExtension(record.GetValueOrDefault("original_filename", "")).TrimStart('.').ToLower();

                writer.WriteLine($"{idImport};;;;{patientId};{pesel};{wizytaIdImport};{data};{nazwaPliku};{opis};;{sciezka};{typPliku};;");
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

    private string FormatDateTime(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (DateTime.TryParse(value, out var dt))
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        return value;
    }
}
