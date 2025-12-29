using System.Text;
using MyDr_Import.Models;
using MyDr_Import.Services;

namespace MyDr_Import.Processors;

/// <summary>
/// Procesor dla modelu PRACOWNICY
/// Zrodlo: gabinet_person.xml
/// Cel: pracownicy.csv
/// </summary>
public class PracownicyProcessor : IModelProcessor
{
    public string ModelName => "pracownicy";
    public string XmlFileName => "gabinet_person.xml";

    private Dictionary<string, (string firstName, string lastName, string email, bool isActive)>? _userCache;

    public CsvGenerationResult Process(string dataEtap1Path, string dataEtap2Path, ModelMapping mapping)
    {
        var result = new CsvGenerationResult
        {
            ModelName = "pracownicy",
            TargetTable = "pracownicy"
        };

        try
        {
            // Zaladuj dane z auth_user.xml (imie, nazwisko, email)
            LoadUserCache(dataEtap1Path);

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
            var csvPath = Path.Combine(dataEtap2Path, "pracownicy.csv");
            using var writer = new StreamWriter(csvPath, false, new UTF8Encoding(true));

            // Naglowek zgodny z old_etap2
            writer.WriteLine("InstalacjaId;IdImport;Imie;Nazwisko;DrugieImie;Pesel;NIP;Plec;Email;Telefon;NumerPWZ;TytulNaukowy;Specjalizacja;TypPersoneluNFZ;Login;CzyAktywny");

            int processedCount = 0;
            foreach (var record in records)
            {
                var idImport = record.GetValueOrDefault("pk", "");
                var userId = record.GetValueOrDefault("user", "");
                
                // Pobierz dane osobowe z auth_user
                var imie = "";
                var nazwisko = "";
                var email = "";
                var isActive = false;
                if (!string.IsNullOrEmpty(userId) && _userCache != null && _userCache.TryGetValue(userId, out var userData))
                {
                    imie = userData.firstName;
                    nazwisko = userData.lastName;
                    email = userData.email;
                    isActive = userData.isActive;
                }

                var drugieImie = "";
                var pesel = record.GetValueOrDefault("pesel", "");
                var nip = "";
                var plec = "";
                var telefon = record.GetValueOrDefault("telephone", "");
                var numerPwz = record.GetValueOrDefault("pwz", "");
                var tytulNaukowy = record.GetValueOrDefault("academic_degree", "");
                var specjalizacja = "";
                var typPersoneluNfz = MapProfessionCode(record.GetValueOrDefault("profession_code", ""));
                var login = "";
                var czyAktywny = isActive || record.GetValueOrDefault("confirmed", "") == "True" ? "1" : "0";

                writer.WriteLine($";{idImport};{Escape(imie)};{Escape(nazwisko)};{drugieImie};{pesel};{nip};{plec};{Escape(email)};{telefon};{numerPwz};{tytulNaukowy};{specjalizacja};{typPersoneluNfz};{login};{czyAktywny}");
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

    private void LoadUserCache(string dataEtap1Path)
    {
        var userPath = Path.Combine(dataEtap1Path, "data_full", "auth_user.xml");
        _userCache = new Dictionary<string, (string, string, string, bool)>();

        if (!File.Exists(userPath))
        {
            Console.WriteLine("  UWAGA: Brak pliku auth_user.xml - Imie/Nazwisko nie bedzie wypelnione");
            return;
        }

        using var stream = File.OpenRead(userPath);
        using var reader = System.Xml.XmlReader.Create(stream, new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Ignore });
        
        while (reader.Read())
        {
            if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "object")
            {
                var pk = reader.GetAttribute("pk");
                if (string.IsNullOrEmpty(pk)) continue;

                var firstName = "";
                var lastName = "";
                var email = "";
                var isActive = false;
                
                using var objReader = reader.ReadSubtree();
                while (objReader.Read())
                {
                    if (objReader.NodeType == System.Xml.XmlNodeType.Element && objReader.Name == "field")
                    {
                        var name = objReader.GetAttribute("name");
                        var value = objReader.ReadElementContentAsString()?.Trim() ?? "";
                        if (value == "None" || value.Contains("<None")) value = "";
                        
                        if (name == "first_name") firstName = value;
                        else if (name == "last_name") lastName = value;
                        else if (name == "email") email = value;
                        else if (name == "is_active") isActive = value == "True";
                    }
                }
                _userCache[pk] = (firstName, lastName, email, isActive);
            }
        }
        Console.WriteLine($"  Zaladowano {_userCache.Count} uzytkownikow z auth_user");
    }

    private string MapProfessionCode(string professionCode)
    {
        // Mapowanie kodow zawodow medycznych na typy personelu NFZ
        return professionCode switch
        {
            "LEK" or "L" => "1",  // Lekarz
            "PIL" or "P" => "2",  // Pielegniarka
            "POL" => "3",         // Polozna
            "FIZ" => "4",         // Fizjoterapeuta
            _ => !string.IsNullOrEmpty(professionCode) ? "1" : ""
        };
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
