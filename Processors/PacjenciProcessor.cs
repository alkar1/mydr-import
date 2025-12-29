using System.Text;
using System.Xml.Linq;
using MyDr_Import.Models;
using MyDr_Import.Services;

namespace MyDr_Import.Processors;

/// <summary>
/// Procesor dla modelu PACJENCI
/// Zrodlo: gabinet_patient.xml + gabinet_address.xml
/// Cel: pacjenci.csv
/// </summary>
public class PacjenciProcessor : BaseModelProcessor
{
    public override string ModelName => "pacjenci";
    public override string XmlFileName => "gabinet_patient.xml";

    // Cache dla powiazanych danych
    private Dictionary<string, Dictionary<string, string>>? _addressCache;
    private Dictionary<string, Dictionary<string, string>>? _iceCache;        // In Case of Emergency (opiekun)
    private Dictionary<string, Dictionary<string, string>>? _deadCache;       // Zgon pacjenta
    private Dictionary<string, List<string>>? _notesCache;                    // Notatki pacjenta

    /// <summary>
    /// Mapowanie nazw pol z arkusza Excel na nazwy pol XML dla pacjentow
    /// </summary>
    protected override Dictionary<string, string> FieldNameMappings => new(StringComparer.OrdinalIgnoreCase)
    {
        // Identyfikator
        { "IdImport", "pk" },
        { "Pesel", "pesel" },
        
        // Dane osobowe dostepne w gabinet.patient
        { "DrugieImie", "second_name" },
        { "NazwiskoRodowe", "maiden_name" },
        { "MiejsceUrodzenia", "place_of_birth" },
        
        // Kontakt
        { "TelefonDodatkowy", "second_telephone" },
        
        // Dokumenty
        { "NumerDokumentuTozsamosci", "identity_num" },
        
        // NFZ
        { "KodOddzialuNFZ", "nfz" },
        
        // Inne
        { "NIP", "employer_nip" },
        { "Kraj", "country" },
        { "GrupaKrwi", "blood_type" },
        
        // Flagi
        { "Aktywny", "is_active" },
    };

    public override CsvGenerationResult Process(string dataEtap1Path, string dataEtap2Path, ModelMapping mapping)
    {
        var result = new CsvGenerationResult
        {
            ModelName = mapping.SheetName,
            TargetTable = mapping.TargetTable
        };

        try
        {
            // 1. Wczytaj wszystkie powiazane dane do cache
            LoadAddressCache(dataEtap1Path);
            LoadIceCache(dataEtap1Path);
            LoadDeadCache(dataEtap1Path);
            LoadNotesCache(dataEtap1Path);

            // 2. Znajdz plik XML z pacjentami
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

            // 4. Przygotuj naglowki CSV
            var validFields = mapping.Fields
                .Where(f => !string.IsNullOrEmpty(f.SourceField))
                .ToList();

            // 5. Generuj CSV
            Directory.CreateDirectory(dataEtap2Path);
            var csvPath = Path.Combine(dataEtap2Path, $"{mapping.SheetName}.csv");
            using var writer = new StreamWriter(csvPath, false, new UTF8Encoding(true));

            // Naglowek - nazwy pol z arkusza Excel
            writer.WriteLine(string.Join(";", validFields.Select(f => EscapeCsvField(f.SourceField))));

            // Wiersze danych
            int processedCount = 0;
            foreach (var record in records)
            {
                var row = new List<string>();
                foreach (var field in validFields)
                {
                    var value = ExtractFieldValueWithPesel(record, field);
                    row.Add(EscapeCsvField(value));
                }
                writer.WriteLine(string.Join(";", row));
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

    private void LoadAddressCache(string dataEtap1Path)
    {
        var addressPath = Path.Combine(dataEtap1Path, "data_full", "gabinet_address.xml");
        if (!File.Exists(addressPath))
        {
            Console.WriteLine("  UWAGA: Brak pliku gabinet_address.xml - adresy nie beda wypelnione");
            _addressCache = new Dictionary<string, Dictionary<string, string>>();
            return;
        }

        _addressCache = new Dictionary<string, Dictionary<string, string>>();
        var doc = XDocument.Load(addressPath);
        var root = doc.Root;

        if (root == null) return;

        foreach (var obj in root.Elements("object"))
        {
            var pk = obj.Attribute("pk")?.Value;
            if (string.IsNullOrEmpty(pk)) continue;

            var addr = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in obj.Elements("field"))
            {
                var name = field.Attribute("name")?.Value;
                if (!string.IsNullOrEmpty(name))
                {
                    var value = field.Value?.Trim() ?? "";
                    if (value != "<None></None>" && value != "None")
                    {
                        addr[name] = value;
                    }
                }
            }
            _addressCache[pk] = addr;
        }

        Console.WriteLine($"  Zaladowano adresow: {_addressCache.Count}");
    }

    private void LoadIceCache(string dataEtap1Path)
    {
        var icePath = Path.Combine(dataEtap1Path, "data_full", "gabinet_incaseofemergency.xml");
        _iceCache = new Dictionary<string, Dictionary<string, string>>();
        
        if (!File.Exists(icePath))
        {
            Console.WriteLine("  UWAGA: Brak pliku gabinet_incaseofemergency.xml - dane opiekunow nie beda wypelnione");
            return;
        }

        var doc = XDocument.Load(icePath);
        var root = doc.Root;
        if (root == null) return;

        foreach (var obj in root.Elements("object"))
        {
            string? patientId = null;
            var ice = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var field in obj.Elements("field"))
            {
                var name = field.Attribute("name")?.Value;
                if (string.IsNullOrEmpty(name)) continue;

                var value = field.Value?.Trim() ?? "";
                if (value == "<None></None>" || value == "None")
                    value = "";

                if (name == "patient" && !string.IsNullOrEmpty(value))
                    patientId = value;
                else
                    ice[name] = value;
            }

            // Zapisz tylko pierwszy rekord ICE dla pacjenta (glowny opiekun)
            if (!string.IsNullOrEmpty(patientId) && !_iceCache.ContainsKey(patientId))
            {
                _iceCache[patientId] = ice;
            }
        }

        Console.WriteLine($"  Zaladowano opiekunow ICE: {_iceCache.Count}");
    }

    private void LoadDeadCache(string dataEtap1Path)
    {
        var deadPath = Path.Combine(dataEtap1Path, "data_full", "gabinet_patientdead.xml");
        _deadCache = new Dictionary<string, Dictionary<string, string>>();
        
        if (!File.Exists(deadPath))
        {
            return; // Plik moze nie istniec jesli nikt nie umarl
        }

        var doc = XDocument.Load(deadPath);
        var root = doc.Root;
        if (root == null) return;

        foreach (var obj in root.Elements("object"))
        {
            var pk = obj.Attribute("pk")?.Value;
            if (string.IsNullOrEmpty(pk)) continue;

            var dead = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in obj.Elements("field"))
            {
                var name = field.Attribute("name")?.Value;
                if (!string.IsNullOrEmpty(name))
                {
                    var value = field.Value?.Trim() ?? "";
                    if (value != "<None></None>" && value != "None")
                        dead[name] = value;
                }
            }
            _deadCache[pk] = dead;
        }

        if (_deadCache.Count > 0)
            Console.WriteLine($"  Zaladowano danych o zgonach: {_deadCache.Count}");
    }

    private void LoadNotesCache(string dataEtap1Path)
    {
        var notesPath = Path.Combine(dataEtap1Path, "data_full", "gabinet_patientnote.xml");
        _notesCache = new Dictionary<string, List<string>>();
        
        if (!File.Exists(notesPath))
        {
            return;
        }

        var doc = XDocument.Load(notesPath);
        var root = doc.Root;
        if (root == null) return;

        foreach (var obj in root.Elements("object"))
        {
            string? patientId = null;
            string? title = null;
            
            foreach (var field in obj.Elements("field"))
            {
                var name = field.Attribute("name")?.Value;
                if (string.IsNullOrEmpty(name)) continue;

                var value = field.Value?.Trim() ?? "";
                if (value == "<None></None>" || value == "None")
                    value = "";

                if (name == "patient" && !string.IsNullOrEmpty(value))
                    patientId = value;
                else if (name == "title" && !string.IsNullOrEmpty(value))
                    title = value;
            }

            if (!string.IsNullOrEmpty(patientId) && !string.IsNullOrEmpty(title))
            {
                if (!_notesCache.ContainsKey(patientId))
                    _notesCache[patientId] = new List<string>();
                _notesCache[patientId].Add(title);
            }
        }

        Console.WriteLine($"  Zaladowano notatek pacjentow: {_notesCache.Values.Sum(v => v.Count)} dla {_notesCache.Count} pacjentow");
    }

    protected override List<Dictionary<string, string>> LoadXmlRecords(string xmlPath)
    {
        var records = new List<Dictionary<string, string>>();
        var doc = XDocument.Load(xmlPath);
        var root = doc.Root;

        if (root == null)
            return records;

        foreach (var obj in root.Elements("object"))
        {
            var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Dodaj pk jako pole
            var pk = obj.Attribute("pk")?.Value;
            if (!string.IsNullOrEmpty(pk))
            {
                record["pk"] = pk;
                record["id"] = pk;
            }

            string? residenceAddressId = null;

            // Pobierz wszystkie pola
            foreach (var field in obj.Elements("field"))
            {
                var name = field.Attribute("name")?.Value;
                if (string.IsNullOrEmpty(name)) continue;

                var value = field.Value?.Trim() ?? "";
                if (value == "<None></None>" || value == "None")
                {
                    value = "";
                }

                record[name] = value;

                // Zapamietaj ID adresu zamieszkania
                if (name == "residence_address" && !string.IsNullOrEmpty(value))
                {
                    residenceAddressId = value;
                }
            }

            // Dodaj dane adresowe z cache (zamieszkanie)
            if (!string.IsNullOrEmpty(residenceAddressId) && _addressCache != null && 
                _addressCache.TryGetValue(residenceAddressId, out var address))
            {
                if (address.TryGetValue("city", out var city))
                    record["address_city"] = city;
                if (address.TryGetValue("street", out var street))
                    record["address_street"] = street;
                if (address.TryGetValue("flat_number", out var flat))
                    record["address_flat"] = flat;
                if (address.TryGetValue("country", out var addrCountry))
                    record["address_country"] = addrCountry;
            }

            // Dodaj dane opiekuna (ICE) z cache
            if (!string.IsNullOrEmpty(pk) && _iceCache != null && _iceCache.TryGetValue(pk, out var ice))
            {
                if (ice.TryGetValue("last_name", out var iceName))
                    record["ice_last_name"] = iceName;
                if (ice.TryGetValue("identity_num", out var iceIdentity))
                    record["ice_identity_num"] = iceIdentity;
                if (ice.TryGetValue("city", out var iceCity))
                    record["ice_city"] = iceCity;
                if (ice.TryGetValue("street", out var iceStreet))
                    record["ice_street"] = iceStreet;
                if (ice.TryGetValue("flat_number", out var iceFlat))
                    record["ice_flat"] = iceFlat;
            }

            // Dodaj dane o zgonie z cache (pk patientdead = pk patient)
            if (!string.IsNullOrEmpty(pk) && _deadCache != null && _deadCache.TryGetValue(pk, out var dead))
            {
                record["is_dead"] = "1";
                if (dead.TryGetValue("date", out var deathDate))
                    record["death_date"] = deathDate;
                if (dead.TryGetValue("cause", out var deathCause))
                    record["death_cause"] = deathCause;
            }

            // Dodaj notatki z cache
            if (!string.IsNullOrEmpty(pk) && _notesCache != null && _notesCache.TryGetValue(pk, out var notes))
            {
                record["notes"] = string.Join("; ", notes);
            }

            if (record.Count > 0)
            {
                records.Add(record);
            }
        }

        return records;
    }

    private string ExtractFieldValueWithPesel(Dictionary<string, string> record, FieldMapping field)
    {
        if (string.IsNullOrEmpty(field.SourceField))
            return field.TransformRule ?? "";

        var sourceField = field.SourceField;

        // Specjalna obsluga pol wyliczanych z PESEL
        if (sourceField.Equals("DataUrodzenia", StringComparison.OrdinalIgnoreCase))
            return ExtractBirthDateFromPesel(record);
        if (sourceField.Equals("Plec", StringComparison.OrdinalIgnoreCase))
            return ExtractSexFromPesel(record);

        // Dane o zgonie
        if (sourceField.Equals("CzyUmarl", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("is_dead", out var isDead) && isDead == "1" ? "1" : "0";
        if (sourceField.Equals("DataZgonu", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("death_date", out var deathDate) ? deathDate : "";

        // Notatki/Uwagi
        if (sourceField.Equals("Uwagi", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("notes", out var notes) ? notes : "";

        // VIP - mapowane z takes_part_in_loyalty_program
        if (sourceField.Equals("VIP", StringComparison.OrdinalIgnoreCase))
        {
            if (record.TryGetValue("takes_part_in_loyalty_program", out var vip))
                return vip.Equals("True", StringComparison.OrdinalIgnoreCase) ? "1" : "0";
            return "0";
        }

        // Pola adresowe - zamieszkanie
        if (sourceField.Equals("MiejscowoscZamieszkanie", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("address_city", out var v1) ? v1 : "";
        if (sourceField.Equals("UlicaZamieszkanie", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("address_street", out var v2) ? v2 : "";
        if (sourceField.Equals("NrMieszkaniaZamieszkanie", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("address_flat", out var v3) ? v3 : "";
        if (sourceField.Equals("KrajZamieszkanie", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("address_country", out var v4) ? v4 : "";

        // Dane opiekuna (ICE - In Case of Emergency)
        if (sourceField.Equals("NazwiskoOpiekuna", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("ice_last_name", out var iceName) ? iceName : "";
        if (sourceField.Equals("PeselOpiekuna", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("ice_identity_num", out var iceId) ? iceId : "";
        if (sourceField.Equals("MiejscowoscOpiekuna", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("ice_city", out var iceCity) ? iceCity : "";
        if (sourceField.Equals("UlicaOpiekuna", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("ice_street", out var iceStreet) ? iceStreet : "";
        if (sourceField.Equals("NrLokaluOpiekuna", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("ice_flat", out var iceFlat) ? iceFlat : "";

        // Standardowa obsluga przez bazowa klase
        return ExtractFieldValue(record, field);
    }

    /// <summary>
    /// Wyciaga date urodzenia z numeru PESEL
    /// Format: RRMMDD gdzie MM moze byc zmodyfikowane dla roznych stuleci
    /// </summary>
    private string ExtractBirthDateFromPesel(Dictionary<string, string> record)
    {
        if (!record.TryGetValue("pesel", out var pesel) || string.IsNullOrEmpty(pesel) || pesel.Length < 6)
            return "";

        try
        {
            int year = int.Parse(pesel.Substring(0, 2));
            int month = int.Parse(pesel.Substring(2, 2));
            int day = int.Parse(pesel.Substring(4, 2));

            // Okreslenie stulecia na podstawie miesiaca
            int century;
            if (month >= 1 && month <= 12)
            {
                century = 1900;
            }
            else if (month >= 21 && month <= 32)
            {
                century = 2000;
                month -= 20;
            }
            else if (month >= 41 && month <= 52)
            {
                century = 2100;
                month -= 40;
            }
            else if (month >= 61 && month <= 72)
            {
                century = 2200;
                month -= 60;
            }
            else if (month >= 81 && month <= 92)
            {
                century = 1800;
                month -= 80;
            }
            else
            {
                return "";
            }

            year += century;

            // Walidacja daty
            if (month < 1 || month > 12 || day < 1 || day > 31)
                return "";

            return $"{year:D4}-{month:D2}-{day:D2}";
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Wyciaga plec z numeru PESEL
    /// Dziesiata cyfra: parzysta = K (kobieta), nieparzysta = M (mezczyzna)
    /// </summary>
    private string ExtractSexFromPesel(Dictionary<string, string> record)
    {
        if (!record.TryGetValue("pesel", out var pesel) || string.IsNullOrEmpty(pesel) || pesel.Length < 10)
            return "";

        try
        {
            int genderDigit = int.Parse(pesel.Substring(9, 1));
            return genderDigit % 2 == 0 ? "K" : "M";
        }
        catch
        {
            return "";
        }
    }
}
