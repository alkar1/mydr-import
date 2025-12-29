using System.Text;
using System.Xml.Linq;
using MyDr_Import.Models;
using MyDr_Import.Services;

namespace MyDr_Import.Processors;

/// <summary>
/// Procesor dla modelu PACJENCI
/// Zrodlo: gabinet_patient.xml + gabinet_address.xml + gabinet_incaseofemergency.xml + gabinet_patientdead.xml + gabinet_patientnote.xml
/// Cel: pacjenci.csv
/// 
/// UWAGA: Pola Imie i Nazwisko NIE SA dostepne w eksporcie XML (dane osobowe).
/// Dostepne pola w gabinet.patient wg xml_structure_summary.json:
/// - pesel (88.6%), second_name (8.7%), maiden_name (6.4%), place_of_birth (6.7%)
/// - nfz (99.6%), identity_num (6.1%), country (100%), blood_type (100%)
/// - is_active (100%), second_telephone (1.5%), takes_part_in_loyalty_program (100%)
/// - residence_address -> gabinet.address (city, street, flat_number, country)
/// - employer_nip (0.2%), dead -> gabinet.patientdead
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

    // Slownik mapowania typu dokumentu tozsamosci (5.4)
    private static readonly Dictionary<string, string> DocTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "id_card", "D" },
        { "passport", "P" },
        { "driving_license", "PR" }
    };

    /// <summary>
    /// Mapowanie nazw pol z arkusza Excel na nazwy pol XML dla pacjentow
    /// Zrodlo mapowan: data_etap1/xml_structure_summary.json
    /// </summary>
    protected override Dictionary<string, string> FieldNameMappings => new(StringComparer.OrdinalIgnoreCase)
    {
        // Identyfikator
        { "IdImport", "pk" },
        { "Pesel", "pesel" },
        { "Imie", "name" },
        { "Nazwisko", "surname" },
        
        // Dane osobowe dostepne w gabinet.patient
        // UWAGA: Imie i Nazwisko NIE SA dostepne w eksporcie XML!
        { "DrugieImie", "second_name" },
        { "NazwiskoRodowe", "maiden_name" },
        { "MiejsceUrodzenia", "place_of_birth" },
        
        // Kontakt - UWAGA: telephone i email NIE SA dostepne w gabinet.patient!
        // second_telephone jest dostepny ale tylko 1.5% wypelnione
        { "Telefon", "telephone" },
        { "Email", "email" },
        { "TelefonDodatkowy", "second_telephone" },
        
        // Dokumenty
        { "NumerDokumentuTozsamosci", "identity_num" },
        
        // NFZ - 99.6% wypelnione
        { "KodOddzialuNFZ", "nfz" },
        
        // Inne
        { "NIP", "employer_nip" },
        { "Kraj", "country" },
        { "GrupaKrwi", "blood_type" },
        
        // Flagi
        { "Aktywny", "is_active" },
        
        // Numer pacjenta
        { "NumerPacjenta", "user" },
    };

    // Naglowek CSV zgodny z old_etap2
    private static readonly string[] CsvHeader = new[]
    {
        "InstalacjaId", "IdImport", "UprawnieniePacjentaId", "RodzajPacjenta", "Imie", "Nazwisko",
        "Pesel", "DataUrodzenia", "CzyUmarl", "DataZgonu", "DrugieImie", "NazwiskoRodowe", "ImieOjca",
        "NIP", "Plec", "Email", "Telefon", "TelefonDodatkowy", "NumerDokumentuTozsamosci",
        "TypDokumentuTozsamosci", "KrajDokumentuTozsamosciKod", "NrIdentyfikacyjnyUe", "MiejsceUrodzenia",
        "KodOddzialuNFZ", "KrajZameldowanie", "WojewodztwoZameldowanie", "KodTerytGminyZameldowanie",
        "MiejscowoscZameldowanie", "KodMiejscowosciZameldowanie", "KodPocztowyZameldowanie",
        "UlicaZameldowanie", "NrDomuZameldowanie", "NrMieszkaniaZameldowanie", "DzielnicaZameldowanie",
        "KrajZamieszkanie", "WojewodztwoZamieszkanie", "KodTerytGminyZamieszkanie", "MiejscowoscZamieszkanie",
        "KodMiejscowosciZamieszkanie", "KodPocztowyZamieszkanie", "UlicaZamieszkanie", "NrDomuZamieszkanie",
        "NrMieszkaniaZamieszkanie", "DzielnicaZamieszkanie", "Uwagi", "Uchodzca", "VIP", "UprawnieniePacjenta",
        "ImieOpiekuna", "NazwiskoOpiekuna", "PlecOpiekuna", "DataUrodzeniaOpiekuna", "PeselOpiekuna",
        "TelefonOpiekuna", "KrajOpiekuna", "WojewodztwoOpiekuna", "KodGminyOpiekuna", "MiejscowoscOpiekuna",
        "KodMiejscowosciOpiekuna", "KodPocztowyOpiekuna", "UlicaOpiekuna", "NrDomuOpiekuna", "NrLokaluOpiekuna",
        "StopienPokrewienstwaOpiekuna", "SprawdzUnikalnoscIdImportu", "SprawdzUnikalnoscPesel",
        "AktualizujPoPesel", "NumerPacjenta"
    };

    public override CsvGenerationResult Process(string dataEtap1Path, string dataEtap2Path, ModelMapping mapping)
    {
        var result = new CsvGenerationResult
        {
            ModelName = "pacjenci",
            TargetTable = "pacjenci"
        };

        try
        {
            // OSTRZEZENIE: Pola niedostepne w eksporcie XML
            Console.WriteLine();
            Console.WriteLine("  [INFO] Pola niedostepne w eksporcie gabinet.patient XML:");
            Console.WriteLine("         - Imie, Nazwisko (dane osobowe nie eksportowane)");
            Console.WriteLine("         - Telefon, Email (brak w gabinet.patient)");
            Console.WriteLine("         Dane beda puste w CSV dla tych kolumn.");
            Console.WriteLine();

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

            // 4. Generuj CSV z hardcoded naglowkiem (zgodnie z old_etap2)
            Directory.CreateDirectory(dataEtap2Path);
            var csvPath = Path.Combine(dataEtap2Path, "pacjenci.csv");
            using var writer = new StreamWriter(csvPath, false, new UTF8Encoding(true));

            // Naglowek
            writer.WriteLine(string.Join(";", CsvHeader));

            // Wiersze danych
            int processedCount = 0;
            foreach (var record in records)
            {
                var row = new List<string>();
                foreach (var fieldName in CsvHeader)
                {
                    var value = GetFieldValue(record, fieldName);
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

    private string GetFieldValue(Dictionary<string, string> record, string fieldName)
    {
        return fieldName switch
        {
            "InstalacjaId" => "",
            "IdImport" => record.GetValueOrDefault("pk", ""),
            "UprawnieniePacjentaId" => "",
            "RodzajPacjenta" => "1",
            "Imie" => record.GetValueOrDefault("name", ""),
            "Nazwisko" => record.GetValueOrDefault("surname", ""),
            "Pesel" => record.GetValueOrDefault("pesel", ""),
            "DataUrodzenia" => ExtractBirthDateFromPesel(record),
            "CzyUmarl" => record.TryGetValue("is_dead", out var isDead) && isDead == "1" ? "1" : "0",
            "DataZgonu" => record.GetValueOrDefault("death_date", ""),
            "DrugieImie" => record.GetValueOrDefault("second_name", ""),
            "NazwiskoRodowe" => record.GetValueOrDefault("maiden_name", ""),
            "ImieOjca" => "",
            "NIP" => record.GetValueOrDefault("employer_nip", ""),
            "Plec" => ExtractSexFromPesel(record),
            "Email" => record.GetValueOrDefault("email", ""),
            "Telefon" => record.GetValueOrDefault("telephone", ""),
            "TelefonDodatkowy" => record.GetValueOrDefault("second_telephone", ""),
            "NumerDokumentuTozsamosci" => record.GetValueOrDefault("identity_num", ""),
            "TypDokumentuTozsamosci" => MapDocType(record.GetValueOrDefault("identity_type", "")),
            "KrajDokumentuTozsamosciKod" => "PL",
            "NrIdentyfikacyjnyUe" => "",
            "MiejsceUrodzenia" => record.GetValueOrDefault("place_of_birth", ""),
            "KodOddzialuNFZ" => record.GetValueOrDefault("nfz", ""),
            "KrajZameldowanie" => MapCountryCode(record.GetValueOrDefault("reg_country", "")),
            "WojewodztwoZameldowanie" => record.GetValueOrDefault("reg_province", ""),
            "KodTerytGminyZameldowanie" => record.GetValueOrDefault("reg_teryt", ""),
            "MiejscowoscZameldowanie" => record.GetValueOrDefault("reg_city", ""),
            "KodMiejscowosciZameldowanie" => "",
            "KodPocztowyZameldowanie" => record.GetValueOrDefault("reg_postal_code", ""),
            "UlicaZameldowanie" => record.GetValueOrDefault("reg_street", ""),
            "NrDomuZameldowanie" => record.GetValueOrDefault("reg_street_number", ""),
            "NrMieszkaniaZameldowanie" => record.GetValueOrDefault("reg_flat_number", ""),
            "DzielnicaZameldowanie" => "",
            "KrajZamieszkanie" => MapCountryCode(record.GetValueOrDefault("res_country", "")),
            "WojewodztwoZamieszkanie" => record.GetValueOrDefault("res_province", ""),
            "KodTerytGminyZamieszkanie" => record.GetValueOrDefault("res_teryt", ""),
            "MiejscowoscZamieszkanie" => record.GetValueOrDefault("res_city", ""),
            "KodMiejscowosciZamieszkanie" => "",
            "KodPocztowyZamieszkanie" => record.GetValueOrDefault("res_postal_code", ""),
            "UlicaZamieszkanie" => record.GetValueOrDefault("res_street", ""),
            "NrDomuZamieszkanie" => record.GetValueOrDefault("res_street_number", ""),
            "NrMieszkaniaZamieszkanie" => record.GetValueOrDefault("res_flat_number", ""),
            "DzielnicaZamieszkanie" => "",
            "Uwagi" => record.GetValueOrDefault("notes", ""),
            "Uchodzca" => record.TryGetValue("is_refugee", out var ref1) && ref1.Equals("True", StringComparison.OrdinalIgnoreCase) ? "1" : "0",
            "VIP" => record.TryGetValue("vip", out var vip) && vip.Equals("True", StringComparison.OrdinalIgnoreCase) ? "1" : "0",
            "UprawnieniePacjenta" => record.TryGetValue("rights", out var rights) ? rights.Replace("X", "").Replace("x", "").Trim() : "",
            "ImieOpiekuna" => record.GetValueOrDefault("ice_first_name", ""),
            "NazwiskoOpiekuna" => record.GetValueOrDefault("ice_last_name", ""),
            "PlecOpiekuna" => "",
            "DataUrodzeniaOpiekuna" => "",
            "PeselOpiekuna" => record.GetValueOrDefault("ice_identity_num", ""),
            "TelefonOpiekuna" => "",
            "KrajOpiekuna" => "",
            "WojewodztwoOpiekuna" => "",
            "KodGminyOpiekuna" => "",
            "MiejscowoscOpiekuna" => record.GetValueOrDefault("ice_city", ""),
            "KodMiejscowosciOpiekuna" => "",
            "KodPocztowyOpiekuna" => "",
            "UlicaOpiekuna" => record.GetValueOrDefault("ice_street", ""),
            "NrDomuOpiekuna" => record.GetValueOrDefault("ice_house", ""),
            "NrLokaluOpiekuna" => record.GetValueOrDefault("ice_flat", ""),
            "StopienPokrewienstwaOpiekuna" => "",
            "SprawdzUnikalnoscIdImportu" => "1",
            "SprawdzUnikalnoscPesel" => "0",
            "AktualizujPoPesel" => "0",
            "NumerPacjenta" => record.GetValueOrDefault("user", ""),
            _ => ""
        };
    }

    private string MapDocType(string idType)
    {
        if (string.IsNullOrEmpty(idType)) return "";
        return DocTypeMap.TryGetValue(idType, out var mapped) ? mapped : idType;
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
            string? registrationAddressId = null;

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
                // Zapamietaj ID adresu zameldowania
                if (name == "registration_address" && !string.IsNullOrEmpty(value))
                {
                    registrationAddressId = value;
                }
            }

            // Dodaj dane adresowe z cache (zameldowanie - registration_address)
            if (!string.IsNullOrEmpty(registrationAddressId) && _addressCache != null && 
                _addressCache.TryGetValue(registrationAddressId, out var regAddress))
            {
                ExtractAddressFields(regAddress, record, "reg_");
            }

            // Dodaj dane adresowe z cache (zamieszkanie - residence_address)
            if (!string.IsNullOrEmpty(residenceAddressId) && _addressCache != null && 
                _addressCache.TryGetValue(residenceAddressId, out var resAddress))
            {
                ExtractAddressFields(resAddress, record, "res_");
            }
            // Fallback: jesli brak adresu zamieszkania, uzyj adresu zameldowania
            else if (!string.IsNullOrEmpty(registrationAddressId) && _addressCache != null && 
                _addressCache.TryGetValue(registrationAddressId, out var fallbackAddress))
            {
                ExtractAddressFields(fallbackAddress, record, "res_");
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
                {
                    var (parsedStreet, parsedHouse) = ParseStreetAndNumber(iceStreet);
                    record["ice_street"] = parsedStreet;
                    if (!string.IsNullOrEmpty(parsedHouse))
                        record["ice_house"] = parsedHouse;
                }
                // UWAGA: W danych zrodlowych flat_number czesto zawiera numer DOMU
                if (ice.TryGetValue("flat_number", out var iceFlat) && !string.IsNullOrEmpty(iceFlat))
                {
                    if (string.IsNullOrEmpty(record.GetValueOrDefault("ice_house")))
                        record["ice_house"] = iceFlat;
                    else
                        record["ice_flat"] = iceFlat;
                }
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
            if (record.TryGetValue("vip", out var vip))
                return vip.Equals("True", StringComparison.OrdinalIgnoreCase) ? "1" : "0";
            return "0";
        }

        // Uchodzca - z pola is_refugee
        if (sourceField.Equals("Uchodzca", StringComparison.OrdinalIgnoreCase))
        {
            if (record.TryGetValue("is_refugee", out var refugee))
                return refugee.Equals("True", StringComparison.OrdinalIgnoreCase) ? "1" : "0";
            return "0";
        }

        // Wartosci domyslne wg dokumentacji (sekcja 4.3)
        if (sourceField.Equals("RodzajPacjenta", StringComparison.OrdinalIgnoreCase))
            return "1"; // osoba fizyczna
        if (sourceField.Equals("KrajDokumentuTozsamosciKod", StringComparison.OrdinalIgnoreCase))
            return "PL";
        if (sourceField.Equals("SprawdzUnikalnoscIdImportu", StringComparison.OrdinalIgnoreCase))
            return "1";
        if (sourceField.Equals("SprawdzUnikalnoscPesel", StringComparison.OrdinalIgnoreCase))
            return "0";
        if (sourceField.Equals("AktualizujPoPesel", StringComparison.OrdinalIgnoreCase))
            return "0";

        // UprawnieniePacjenta - z pola rights z usunieciem "X"
        if (sourceField.Equals("UprawnieniePacjenta", StringComparison.OrdinalIgnoreCase))
        {
            if (record.TryGetValue("rights", out var rights) && !string.IsNullOrEmpty(rights))
            {
                var cleaned = rights.Replace("X", "").Replace("x", "").Trim();
                return cleaned;
            }
            return "";
        }

        // TypDokumentuTozsamosci - mapowanie z identity_type
        if (sourceField.Equals("TypDokumentuTozsamosci", StringComparison.OrdinalIgnoreCase))
        {
            if (record.TryGetValue("identity_type", out var idType) && !string.IsNullOrEmpty(idType))
            {
                return DocTypeMap.TryGetValue(idType, out var mapped) ? mapped : idType;
            }
            return "";
        }

        // Pola adresowe - zameldowanie (registration_address)
        if (sourceField.Equals("KrajZameldowanie", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("reg_country", out var rc) ? MapCountryCode(rc) : "PL";
        if (sourceField.Equals("WojewodztwoZameldowanie", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("reg_province", out var rp) ? rp : "";
        if (sourceField.Equals("KodTerytGminyZameldowanie", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("reg_teryt", out var rt) ? rt : "";
        if (sourceField.Equals("MiejscowoscZameldowanie", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("reg_city", out var rci) ? rci : "";
        if (sourceField.Equals("KodPocztowyZameldowanie", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("reg_postal_code", out var rpc) ? rpc : "";
        if (sourceField.Equals("UlicaZameldowanie", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("reg_street", out var rs) ? rs : "";
        if (sourceField.Equals("NrDomuZameldowanie", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("reg_street_number", out var rsn) ? rsn : "";
        if (sourceField.Equals("NrMieszkaniaZameldowanie", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("reg_flat_number", out var rfn) ? rfn : "";

        // Pola adresowe - zamieszkanie (residence_address)
        if (sourceField.Equals("KrajZamieszkanie", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("res_country", out var zc) ? MapCountryCode(zc) : "PL";
        if (sourceField.Equals("WojewodztwoZamieszkanie", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("res_province", out var zp) ? zp : "";
        if (sourceField.Equals("KodTerytGminyZamieszkanie", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("res_teryt", out var zt) ? zt : "";
        if (sourceField.Equals("MiejscowoscZamieszkanie", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("res_city", out var zci) ? zci : "";
        if (sourceField.Equals("KodPocztowyZamieszkanie", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("res_postal_code", out var zpc) ? zpc : "";
        if (sourceField.Equals("UlicaZamieszkanie", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("res_street", out var zs) ? zs : "";
        if (sourceField.Equals("NrDomuZamieszkanie", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("res_street_number", out var zsn) ? zsn : "";
        if (sourceField.Equals("NrMieszkaniaZamieszkanie", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("res_flat_number", out var zfn) ? zfn : "";

        // Dane opiekuna (ICE - In Case of Emergency)
        if (sourceField.Equals("NazwiskoOpiekuna", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("ice_last_name", out var iceName) ? iceName : "";
        if (sourceField.Equals("PeselOpiekuna", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("ice_identity_num", out var iceId) ? iceId : "";
        if (sourceField.Equals("MiejscowoscOpiekuna", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("ice_city", out var iceCity) ? iceCity : "";
        if (sourceField.Equals("UlicaOpiekuna", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("ice_street", out var iceStreet) ? iceStreet : "";
        if (sourceField.Equals("NrDomuOpiekuna", StringComparison.OrdinalIgnoreCase))
            return record.TryGetValue("ice_house", out var iceHouse) ? iceHouse : "";
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

    /// <summary>
    /// Wyodrebnia pola adresowe i dodaje do rekordu z podanym prefiksem
    /// </summary>
    private void ExtractAddressFields(Dictionary<string, string> address, Dictionary<string, string> record, string prefix)
    {
        if (address.TryGetValue("city", out var city))
            record[$"{prefix}city"] = city;
        if (address.TryGetValue("street", out var street))
            record[$"{prefix}street"] = street;
        if (address.TryGetValue("street_number", out var streetNum))
            record[$"{prefix}street_number"] = streetNum;
        if (address.TryGetValue("flat_number", out var flatNum))
            record[$"{prefix}flat_number"] = flatNum;
        if (address.TryGetValue("postal_code", out var postalCode))
            record[$"{prefix}postal_code"] = postalCode;
        if (address.TryGetValue("province", out var province))
            record[$"{prefix}province"] = province;
        if (address.TryGetValue("teryt", out var teryt))
            record[$"{prefix}teryt"] = teryt;
        if (address.TryGetValue("country", out var country))
            record[$"{prefix}country"] = country;
    }

    /// <summary>
    /// Mapuje kod kraju na format wymagany przez system docelowy
    /// </summary>
    private string MapCountryCode(string country)
    {
        if (string.IsNullOrEmpty(country))
            return "PL";
        
        // Jesli juz jest kodem ISO, zwroc uppercase
        if (country.Length == 2)
            return country.ToUpper();
        
        // Mapowanie pelnych nazw na kody
        return country.ToLower() switch
        {
            "polska" => "PL",
            "poland" => "PL",
            "niemcy" => "DE",
            "germany" => "DE",
            "ukraina" => "UA",
            "ukraine" => "UA",
            _ => country.ToUpper()
        };
    }

    /// <summary>
    /// Parsuje ulice i wyodrebnia numer domu jesli jest na koncu
    /// np. "Wielicka 15" -> ("Wielicka", "15")
    /// np. "Osiedlowa" -> ("Osiedlowa", "")
    /// </summary>
    private (string street, string houseNumber) ParseStreetAndNumber(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return ("", "");

        input = input.Trim();
        
        // Szukaj numeru na koncu (cyfry, opcjonalnie z litera)
        var match = System.Text.RegularExpressions.Regex.Match(input, @"^(.+?)\s+(\d+[a-zA-Z]?)$");
        if (match.Success)
        {
            return (match.Groups[1].Value.Trim(), match.Groups[2].Value);
        }

        return (input, "");
    }
}
