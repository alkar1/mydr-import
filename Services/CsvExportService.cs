using System.Xml;
using MyDr_Import.Models.Source;
using MyDr_Import.Models.Target;
using MyDr_Import.Services.Mapping;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Diagnostics;

namespace MyDr_Import.Services;

/// <summary>
/// Serwis eksportu danych z XML MyDrEDM do CSV Optimed
/// Wykorzystuje streaming parsing dla optymalnej wydajnoœci przy du¿ych plikach
/// </summary>
public class CsvExportService
{
    private readonly string _xmlFilePath;
    private readonly string _outputDirectory;
    private readonly IProgress<ExportProgress>? _progress;
    private readonly int _batchSize;
    private readonly int _instalacjaId;

    // Lookup tables dla relacji
    private Dictionary<long, MyDrPerson> _personLookup = new();
    private Dictionary<long, MyDrAddress> _addressLookup = new();
    private Dictionary<long, MyDrPatient> _patientLookup = new();
    private Dictionary<long, MyDrCustodian> _custodianLookup = new();
    private Dictionary<long, MyDrPatientDead> _patientDeadLookup = new();
    private Dictionary<long, List<string>> _visitIcd10Lookup = new();
    private Dictionary<long, List<string>> _visitIcd9Lookup = new();

    public CsvExportService(
        string xmlFilePath, 
        string outputDirectory = "output",
        int batchSize = 1000,
        int instalacjaId = 1,
        IProgress<ExportProgress>? progress = null)
    {
        _xmlFilePath = xmlFilePath;
        _outputDirectory = outputDirectory;
        _batchSize = batchSize;
        _instalacjaId = instalacjaId;
        _progress = progress;

        Directory.CreateDirectory(_outputDirectory);
    }

    /// <summary>
    /// G³ówna metoda eksportu - wykonuje pe³ny eksport wszystkich typów danych
    /// </summary>
    public async Task<ExportResult> ExportAllAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ExportResult();

        Console.WriteLine("??????????????????????????????????????????????????????????????????????????????");
        Console.WriteLine("?              MyDr Import - Eksport do CSV (Etap 2)                         ?");
        Console.WriteLine("?                      MyDrEDM ? Optimed                                     ?");
        Console.WriteLine("??????????????????????????????????????????????????????????????????????????????");
        Console.WriteLine();
        Console.WriteLine($"?? Plik Ÿród³owy: {_xmlFilePath}");
        Console.WriteLine($"?? Folder wyjœciowy: {_outputDirectory}");
        Console.WriteLine($"?? Batch size: {_batchSize}");
        Console.WriteLine($"?? Instalacja ID: {_instalacjaId}");
        Console.WriteLine();

        try
        {
            // FAZA 1: Budowanie lookup tables (first pass)
            Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
            Console.WriteLine("FAZA 1: Budowanie lookup tables dla relacji");
            Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
            await BuildLookupTablesAsync(cancellationToken);

            Console.WriteLine();
            Console.WriteLine($"? Person lookup: {_personLookup.Count:N0} rekordów");
            Console.WriteLine($"? Address lookup: {_addressLookup.Count:N0} rekordów");
            Console.WriteLine($"? Patient lookup: {_patientLookup.Count:N0} rekordów");
            Console.WriteLine($"? Custodian lookup: {_custodianLookup.Count:N0} rekordów");
            Console.WriteLine($"? PatientDead lookup: {_patientDeadLookup.Count:N0} rekordów");
            Console.WriteLine();

            // FAZA 2: Eksport danych (second pass)
            Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
            Console.WriteLine("FAZA 2: Eksport danych do plików CSV");
            Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
            Console.WriteLine();

            // Eksportuj ka¿dy typ danych
            result.PacjenciCount = await ExportPatientsAsync(cancellationToken);
            result.PracownicyCount = await ExportEmployeesAsync(cancellationToken);
            result.WizytyCount = await ExportVisitsAsync(cancellationToken);
            result.SzczepieniaCount = await ExportVaccinationsAsync(cancellationToken);
            // result.StaleChorobyCount = await ExportChronicDiseasesAsync(cancellationToken);
            // result.StaleLekiCount = await ExportPermanentDrugsAsync(cancellationToken);

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.Success = true;

            // Podsumowanie
            Console.WriteLine();
            Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
            Console.WriteLine("? EKSPORT ZAKOÑCZONY POMYŒLNIE!");
            Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
            Console.WriteLine($"??  Czas wykonania: {result.Duration:hh\\:mm\\:ss}");
            Console.WriteLine($"?? Pacjenci: {result.PacjenciCount:N0}");
            Console.WriteLine($"?? Pracownicy: {result.PracownicyCount:N0}");
            Console.WriteLine($"?? Wizyty: {result.WizytyCount:N0}");
            Console.WriteLine($"?? Szczepienia: {result.SzczepieniaCount:N0}");
            Console.WriteLine($"?? Pliki CSV zapisane w: {_outputDirectory}");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            Console.WriteLine();
            Console.WriteLine("? B£¥D PODCZAS EKSPORTU:");
            Console.WriteLine($"   {ex.Message}");
            Console.WriteLine();
            throw;
        }

        return result;
    }

    /// <summary>
    /// Buduje lookup tables dla wszystkich powi¹zanych tabel (first pass XML)
    /// </summary>
    private async Task BuildLookupTablesAsync(CancellationToken cancellationToken)
    {
        var settings = new XmlReaderSettings
        {
            Async = true,
            IgnoreWhitespace = true,
            IgnoreComments = true
        };

        await using var fileStream = new FileStream(_xmlFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536);
        using var reader = XmlReader.Create(fileStream, settings);

        long totalObjects = 0;
        var stopwatch = Stopwatch.StartNew();

        while (await reader.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reader.NodeType == XmlNodeType.Element && reader.Name == "object")
            {
                var modelName = reader.GetAttribute("model");
                var pk = long.Parse(reader.GetAttribute("pk") ?? "0");

                totalObjects++;

                // Raportuj postêp co 10000 obiektów
                if (totalObjects % 10000 == 0)
                {
                    Console.Write($"\r? Przetwarzanie: {totalObjects:N0} obiektów... ({stopwatch.Elapsed:mm\\:ss})     ");
                }

                // Parsuj tylko potrzebne typy dla lookup
                switch (modelName)
                {
                    case "gabinet.person":
                        var person = await ParsePersonAsync(reader, pk);
                        if (person != null) _personLookup[pk] = person;
                        break;

                    case "gabinet.address":
                        var address = await ParseAddressAsync(reader, pk);
                        if (address != null) _addressLookup[pk] = address;
                        break;

                    case "gabinet.patient":
                        var patient = await ParsePatientBasicAsync(reader, pk);
                        if (patient != null) _patientLookup[pk] = patient;
                        break;

                    case "gabinet.custodian":
                    case "gabinet.incaseofemergency":
                        var custodian = await ParseCustodianAsync(reader, pk);
                        if (custodian != null) _custodianLookup[custodian.PatientPk] = custodian;
                        break;

                    case "gabinet.patientdead":
                        var dead = await ParsePatientDeadAsync(reader, pk);
                        if (dead != null) _patientDeadLookup[dead.PatientPk] = dead;
                        break;
                }
            }
        }

        Console.WriteLine($"\r? Przetworzono {totalObjects:N0} obiektów w {stopwatch.Elapsed:mm\\:ss}          ");
    }

    /// <summary>
    /// Eksportuje pacjentów do pacjenci.csv
    /// </summary>
    private async Task<int> ExportPatientsAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("?? Eksport: pacjenci.csv");
        
        var outputPath = Path.Combine(_outputDirectory, "pacjenci.csv");
        var count = 0;
        var batch = new List<OptimedPacjent>();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ",",
            Encoding = System.Text.Encoding.UTF8
        };

        await using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);
        await using var csv = new CsvWriter(writer, config);

        // Parsuj XML i znajdŸ pacjentów
        await using var fileStream = new FileStream(_xmlFilePath, FileMode.Open, FileAccess.Read);
        using var reader = XmlReader.Create(fileStream, new XmlReaderSettings { Async = true });

        while (await reader.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reader.NodeType == XmlNodeType.Element && reader.Name == "object")
            {
                var modelName = reader.GetAttribute("model");
                if (modelName == "gabinet.patient")
                {
                    var pk = long.Parse(reader.GetAttribute("pk") ?? "0");
                    var patient = await ParsePatientFullAsync(reader, pk);
                    
                    if (patient != null)
                    {
                        // DANE OSOBOWE S¥ BEZPOŒREDNIO W PATIENT - nie potrzeda Person!
                        // Za³aduj tylko adres i dead
                        patient.ResidenceAddress = patient.ResidenceAddressPk.HasValue 
                            ? _addressLookup.GetValueOrDefault(patient.ResidenceAddressPk.Value) 
                            : null;
                        patient.Dead = _patientDeadLookup.GetValueOrDefault(pk);

                        // SprawdŸ czy s¹ podstawowe dane
                        if (string.IsNullOrEmpty(patient.FirstName) || string.IsNullOrEmpty(patient.LastName))
                        {
                            // Pomiñ pacjenta bez danych osobowych
                            Console.WriteLine($"Warning: Patient {pk} missing FirstName or LastName, skipping.");
                            continue;
                        }

                        var custodian = _custodianLookup.GetValueOrDefault(pk);
                        var mapped = PatientMapper.Map(patient, _instalacjaId, custodian);
                        
                        batch.Add(mapped);
                        count++;

                        // Zapisz batch
                        if (batch.Count >= _batchSize)
                        {
                            await WriteBatchAsync(csv, batch);
                            batch.Clear();
                            Console.Write($"\r   ? {count:N0} pacjentów...     ");
                        }
                    }
                }
            }
        }

        // Zapisz pozosta³e
        if (batch.Any())
        {
            await WriteBatchAsync(csv, batch);
        }

        Console.WriteLine($"\r   ? Wyeksportowano {count:N0} pacjentów                ");
        return count;
    }

    /// <summary>
    /// Eksportuje pracowników do pracownicy.csv
    /// </summary>
    private async Task<int> ExportEmployeesAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("?? Eksport: pracownicy.csv");
        
        var outputPath = Path.Combine(_outputDirectory, "pracownicy.csv");
        var count = 0;
        var batch = new List<OptimedPracownik>();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ",",
            Encoding = System.Text.Encoding.UTF8
        };

        await using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);
        await using var csv = new CsvWriter(writer, config);

        // Eksportuj osoby które s¹ pracownikami (maj¹ NPWZ lub s¹ w systemie jako lekarze)
        foreach (var person in _personLookup.Values.Where(p => !string.IsNullOrEmpty(p.Npwz)))
        {
            var mapped = EmployeeMapper.Map(person, _instalacjaId);
            batch.Add(mapped);
            count++;

            if (batch.Count >= _batchSize)
            {
                await WriteBatchAsync(csv, batch);
                batch.Clear();
            }
        }

        if (batch.Any())
        {
            await WriteBatchAsync(csv, batch);
        }

        Console.WriteLine($"   ? Wyeksportowano {count:N0} pracowników");
        return count;
    }

    /// <summary>
    /// Eksportuje wizyty do wizyty.csv
    /// </summary>
    private async Task<int> ExportVisitsAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("?? Eksport: wizyty.csv");
        
        var outputPath = Path.Combine(_outputDirectory, "wizyty.csv");
        var count = 0;
        var batch = new List<OptimedWizyta>();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ",",
            Encoding = System.Text.Encoding.UTF8
        };

        await using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);
        await using var csv = new CsvWriter(writer, config);

        await using var fileStream = new FileStream(_xmlFilePath, FileMode.Open, FileAccess.Read);
        using var reader = XmlReader.Create(fileStream, new XmlReaderSettings { Async = true });

        while (await reader.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reader.NodeType == XmlNodeType.Element && reader.Name == "object")
            {
                var modelName = reader.GetAttribute("model");
                if (modelName == "gabinet.visit")
                {
                    var pk = long.Parse(reader.GetAttribute("pk") ?? "0");
                    var visit = await ParseVisitAsync(reader, pk);
                    
                    if (visit != null)
                    {
                        var mapped = VisitMapper.Map(visit, _instalacjaId, _personLookup, _patientLookup);
                        batch.Add(mapped);
                        count++;

                        if (batch.Count >= _batchSize)
                        {
                            await WriteBatchAsync(csv, batch);
                            batch.Clear();
                            Console.Write($"\r   ? {count:N0} wizyt...     ");
                        }
                    }
                }
            }
        }

        if (batch.Any())
        {
            await WriteBatchAsync(csv, batch);
        }

        Console.WriteLine($"\r   ? Wyeksportowano {count:N0} wizyt                ");
        return count;
    }

    /// <summary>
    /// Eksportuje szczepienia do szczepienia.csv
    /// </summary>
    private async Task<int> ExportVaccinationsAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("?? Eksport: szczepienia.csv");
        
        var outputPath = Path.Combine(_outputDirectory, "szczepienia.csv");
        var count = 0;
        var batch = new List<OptimedSzczepienie>();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ",",
            Encoding = System.Text.Encoding.UTF8
        };

        await using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);
        await using var csv = new CsvWriter(writer, config);

        await using var fileStream = new FileStream(_xmlFilePath, FileMode.Open, FileAccess.Read);
        using var reader = XmlReader.Create(fileStream, new XmlReaderSettings { Async = true });

        while (await reader.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reader.NodeType == XmlNodeType.Element && reader.Name == "object")
            {
                var modelName = reader.GetAttribute("model");
                if (modelName == "gabinet.vaccination")
                {
                    var pk = long.Parse(reader.GetAttribute("pk") ?? "0");
                    var vaccination = await ParseVaccinationAsync(reader, pk);
                    
                    if (vaccination != null)
                    {
                        var mapped = VaccinationMapper.Map(vaccination, _instalacjaId, _personLookup, _patientLookup);
                        batch.Add(mapped);
                        count++;

                        if (batch.Count >= _batchSize)
                        {
                            await WriteBatchAsync(csv, batch);
                            batch.Clear();
                            Console.Write($"\r   ? {count:N0} szczepieñ...     ");
                        }
                    }
                }
            }
        }

        if (batch.Any())
        {
            await WriteBatchAsync(csv, batch);
        }

        Console.WriteLine($"\r   ? Wyeksportowano {count:N0} szczepieñ                ");
        return count;
    }

    // ===== HELPER METHODS - Parsowanie XML =====

    // Uniwersalna metoda do odczytu wartoœci pola
    private async Task<string> ReadFieldValueAsync(XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            reader.Read(); // Przeskocz pusty element
            return string.Empty;
        }
        
        var rel = reader.GetAttribute("rel");
        if (!string.IsNullOrEmpty(rel))
        {
            return reader.ReadInnerXml();
        }
        
        try
        {
            return await reader.ReadElementContentAsStringAsync();
        }
        catch (XmlException)
        {
            // Jeœli ma dzieci, u¿yæ ReadInnerXml
            return reader.ReadInnerXml();
        }
    }

    private async Task<MyDrPerson?> ParsePersonAsync(XmlReader reader, long pk)
    {
        var person = new MyDrPerson { PrimaryKey = pk };
        var subtree = reader.ReadSubtree();
        
        while (await subtree.ReadAsync())
        {
            if (subtree.NodeType == XmlNodeType.Element && subtree.Name == "field")
            {
                var fieldName = subtree.GetAttribute("name");
                var fieldValue = await ReadFieldValueAsync(subtree);

                switch (fieldName)
                {
                    case "first_name": person.FirstName = fieldValue; break;
                    case "last_name": person.LastName = fieldValue; break;
                    case "birth_date": person.BirthDate = ParseDateSafe(fieldValue); break;
                    case "sex": person.Sex = fieldValue; break;
                    case "email": person.Email = fieldValue; break;
                    case "phone": person.Phone = fieldValue; break;
                    case "pesel": person.Pesel = fieldValue; break;
                    case "npwz": person.Npwz = fieldValue; break;
                }
            }
        }
        
        return person;
    }

    private async Task<MyDrAddress?> ParseAddressAsync(XmlReader reader, long pk)
    {
        var address = new MyDrAddress { PrimaryKey = pk };
        var subtree = reader.ReadSubtree();
        
        while (await subtree.ReadAsync())
        {
            if (subtree.NodeType == XmlNodeType.Element && subtree.Name == "field")
            {
                var fieldName = subtree.GetAttribute("name");
                var fieldValue = await ReadFieldValueAsync(subtree);

                switch (fieldName)
                {
                    case "country": address.Country = fieldValue; break;
                    case "voivodeship": address.Voivodeship = fieldValue; break;
                    case "commune_teryt": address.CommuneTeryt = fieldValue; break;
                    case "city": address.City = fieldValue; break;
                    case "city_teryt": address.CityTeryt = fieldValue; break;
                    case "postal_code": address.PostalCode = fieldValue; break;
                    case "street": address.Street = fieldValue; break;
                    case "house_number": address.HouseNumber = fieldValue; break;
                    case "apartment_number": address.ApartmentNumber = fieldValue; break;
                    case "district": address.District = fieldValue; break;
                }
            }
        }
        
        return address;
    }

    private async Task<MyDrPatient?> ParsePatientBasicAsync(XmlReader reader, long pk)
    {
        var patient = new MyDrPatient { PrimaryKey = pk };
        var subtree = reader.ReadSubtree();
        
        while (await subtree.ReadAsync())
        {
            if (subtree.NodeType == XmlNodeType.Element && subtree.Name == "field")
            {
                var fieldName = subtree.GetAttribute("name");
                var fieldValue = await ReadFieldValueAsync(subtree);

                if (fieldName == "pesel")
                    patient.Pesel = fieldValue;
            }
        }
        
        return patient;
    }

    private async Task<MyDrPatient?> ParsePatientFullAsync(XmlReader reader, long pk)
    {
        var patient = new MyDrPatient { PrimaryKey = pk };
        var subtree = reader.ReadSubtree();
        
        while (await subtree.ReadAsync())
        {
            if (subtree.NodeType == XmlNodeType.Element && subtree.Name == "field")
            {
                var fieldName = subtree.GetAttribute("name");
                var fieldValue = await ReadFieldValueAsync(subtree);

                switch (fieldName)
                {
                    // Dane osobowe które s¹ w patient!
                    case "name": patient.FirstName = fieldValue; break;
                    case "surname": patient.LastName = fieldValue; break;
                    case "date_of_birth": patient.BirthDate = ParseDateSafe(fieldValue); break;
                    case "sex": patient.Sex = fieldValue; break;
                    case "email": patient.Email = fieldValue; break;
                    case "telephone": patient.Phone = fieldValue; break;
                    case "pesel": patient.Pesel = fieldValue; break;
                    case "second_name": patient.SecondName = fieldValue; break;
                    case "maiden_name": patient.MaidenName = fieldValue; break;
                    case "identity_num": patient.IdentityNum = fieldValue; break;
                    case "country": patient.Country = fieldValue; break;
                    case "place_of_birth": patient.PlaceOfBirth = fieldValue; break;
                    case "nfz": patient.Nfz = fieldValue; break;
                    case "second_telephone": patient.SecondTelephone = fieldValue; break;
                    case "residence_address":
                        if (long.TryParse(fieldValue, out var addrPk))
                            patient.ResidenceAddressPk = addrPk;
                        break;
                }
            }
        }
        
        return patient;
    }

    private async Task<MyDrCustodian?> ParseCustodianAsync(XmlReader reader, long pk)
    {
        var custodian = new MyDrCustodian { PrimaryKey = pk };
        var subtree = reader.ReadSubtree();
        
        while (await subtree.ReadAsync())
        {
            if (subtree.NodeType == XmlNodeType.Element && subtree.Name == "field")
            {
                var fieldName = subtree.GetAttribute("name");
                var fieldValue = await ReadFieldValueAsync(subtree);

                switch (fieldName)
                {
                    case "patient":
                        if (long.TryParse(fieldValue, out var patPk))
                            custodian.PatientPk = patPk;
                        break;
                    case "first_name": custodian.FirstName = fieldValue; break;
                    case "last_name": custodian.LastName = fieldValue; break;
                    case "relationship": custodian.Relationship = fieldValue; break;
                }
            }
        }
        
        return custodian;
    }

    private async Task<MyDrPatientDead?> ParsePatientDeadAsync(XmlReader reader, long pk)
    {
        var dead = new MyDrPatientDead { PrimaryKey = pk };
        var subtree = reader.ReadSubtree();
        
        while (await subtree.ReadAsync())
        {
            if (subtree.NodeType == XmlNodeType.Element && subtree.Name == "field")
            {
                var fieldName = subtree.GetAttribute("name");
                var fieldValue = await ReadFieldValueAsync(subtree);

                switch (fieldName)
                {
                    case "patient":
                        if (long.TryParse(fieldValue, out var patPk))
                            dead.PatientPk = patPk;
                        break;
                    case "is_dead":
                        dead.IsDead = fieldValue == "True";
                        break;
                    case "death_date":
                        dead.DeathDate = ParseDateSafe(fieldValue);
                        break;
                }
            }
        }
        
        return dead;
    }

    private async Task<MyDrVisit?> ParseVisitAsync(XmlReader reader, long pk)
    {
        var visit = new MyDrVisit { PrimaryKey = pk };
        var subtree = reader.ReadSubtree();
        
        while (await subtree.ReadAsync())
        {
            if (subtree.NodeType == XmlNodeType.Element && subtree.Name == "field")
            {
                var fieldName = subtree.GetAttribute("name");
                var fieldValue = await ReadFieldValueAsync(subtree);

                switch (fieldName)
                {
                    case "patient":
                        if (long.TryParse(fieldValue, out var patPk))
                            visit.PatientPk = patPk;
                        break;
                    case "doctor":
                        if (long.TryParse(fieldValue, out var docPk))
                            visit.DoctorPk = docPk;
                        break;
                    case "office":
                        if (long.TryParse(fieldValue, out var offPk))
                            visit.OfficePk = offPk;
                        break;
                    case "date":
                        visit.Date = ParseDateSafe(fieldValue) ?? DateTime.Now;
                        break;
                    case "timeTo":
                        visit.TimeTo = ParseTimeSafe(fieldValue);
                        break;
                    case "interview":
                        visit.Interview = fieldValue;
                        break;
                }
            }
        }
        
        return visit;
    }

    private async Task<MyDrVaccination?> ParseVaccinationAsync(XmlReader reader, long pk)
    {
        var vaccination = new MyDrVaccination { PrimaryKey = pk };
        var subtree = reader.ReadSubtree();
        
        while (await subtree.ReadAsync())
        {
            if (subtree.NodeType == XmlNodeType.Element && subtree.Name == "field")
            {
                var fieldName = subtree.GetAttribute("name");
                var fieldValue = await ReadFieldValueAsync(subtree);

                switch (fieldName)
                {
                    case "patient":
                        if (long.TryParse(fieldValue, out var patPk))
                            vaccination.PatientPk = patPk;
                        break;
                    case "doctor":
                        if (long.TryParse(fieldValue, out var docPk))
                            vaccination.DoctorPk = docPk;
                        break;
                    case "name": vaccination.Name = fieldValue; break;
                    case "administration_site": vaccination.AdministrationSite = fieldValue; break;
                    case "series_number": vaccination.SeriesNumber = fieldValue; break;
                    case "administration_date":
                        vaccination.AdministrationDate = ParseDateSafe(fieldValue) ?? DateTime.Now;
                        break;
                    case "expiry_date":
                        vaccination.ExpiryDate = ParseDateSafe(fieldValue);
                        break;
                }
            }
        }
        
        return vaccination;
    }

    private async Task WriteBatchAsync<T>(CsvWriter csv, List<T> batch)
    {
        if (!batch.Any()) return;

        // CsvHelper automatycznie zapisze nag³ówek przy pierwszym zapisie
        await csv.WriteRecordsAsync(batch);
        await csv.FlushAsync();
    }

    private DateTime? ParseDateSafe(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "<None></None>")
            return null;

        if (DateTime.TryParse(value, out var date))
            return date;

        return null;
    }

    private TimeSpan? ParseTimeSafe(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "<None></None>")
            return null;

        if (TimeSpan.TryParse(value, out var time))
            return time;

        return null;
    }
}

/// <summary>
/// Postêp eksportu
/// </summary>
public class ExportProgress
{
    public string CurrentFile { get; set; } = string.Empty;
    public long RecordsProcessed { get; set; }
    public double PercentComplete { get; set; }
}

/// <summary>
/// Wynik eksportu
/// </summary>
public class ExportResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
    
    public int PacjenciCount { get; set; }
    public int PracownicyCount { get; set; }
    public int WizytyCount { get; set; }
    public int SzczepieniaCount { get; set; }
    public int StaleChorobyCount { get; set; }
    public int StaleLekiCount { get; set; }
}
