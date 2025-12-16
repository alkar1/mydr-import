using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace MyDr_Import.Tests;

/// <summary>
/// Testy weryfikacyjne eksportu CSV - sprawdzajπ kompletnoúÊ i poprawnoúÊ danych
/// </summary>
public class CsvExportVerificationTests
{
    private readonly string _outputDirectory;
    private readonly Dictionary<string, ExpectedCsvStats> _expectedStats;

    public CsvExportVerificationTests(string outputDirectory = "output_csv")
    {
        _outputDirectory = outputDirectory;
        
        // Oczekiwane statystyki na podstawie analizy XML (Etap 1)
        _expectedStats = new Dictionary<string, ExpectedCsvStats>
        {
            ["pacjenci.csv"] = new ExpectedCsvStats 
            { 
                MinRecords = 22397,
                MaxRecords = 22400,
                RequiredColumns = 68,
                KeyColumns = new[] { "IdImport", "Imie", "Nazwisko", "DataUrodzenia" }
            },
            ["pracownicy.csv"] = new ExpectedCsvStats 
            { 
                MinRecords = 150,
                MaxRecords = 200,
                RequiredColumns = 23,
                KeyColumns = new[] { "IdImport", "Imie", "Nazwisko" }
            },
            ["wizyty.csv"] = new ExpectedCsvStats 
            { 
                MinRecords = 696727,
                MaxRecords = 700000,
                RequiredColumns = 33,
                KeyColumns = new[] { "IdImport", "PacjentIdImport", "PracownikIdImport", "DataOd" }
            },
            ["szczepienia.csv"] = new ExpectedCsvStats 
            { 
                MinRecords = 48000,
                MaxRecords = 52000,
                RequiredColumns = 16,
                KeyColumns = new[] { "IdImport", "PacjentIdImport", "Nazwa", "DataPodania" }
            },
            ["stale_choroby_pacjenta.csv"] = new ExpectedCsvStats 
            { 
                MinRecords = 40000,
                MaxRecords = 100000,
                RequiredColumns = 6,
                KeyColumns = new[] { "PacjentIdImport", "ICD10" }
            },
            ["stale_leki_pacjenta.csv"] = new ExpectedCsvStats 
            { 
                MinRecords = 5000,
                MaxRecords = 20000,
                RequiredColumns = 12,
                KeyColumns = new[] { "PacjentIdImport", "KodKreskowy" }
            }
        };
    }

    /// <summary>
    /// G≥Ûwny test weryfikacyjny wszystkich plikÛw CSV
    /// </summary>
    public async Task<VerificationResult> RunAllVerificationsAsync()
    {
        var result = new VerificationResult
        {
            StartTime = DateTime.Now
        };

        Console.WriteLine("??????????????????????????????????????????????????????????????????????????????");
        Console.WriteLine("?           Test Weryfikacji Eksportu CSV - KompletnoúÊ Danych              ?");
        Console.WriteLine("??????????????????????????????????????????????????????????????????????????????");
        Console.WriteLine();
        Console.WriteLine($"?? Folder: {_outputDirectory}");
        Console.WriteLine($"?? Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();

        // Sprawdü czy folder istnieje
        if (!Directory.Exists(_outputDirectory))
        {
            result.AddError($"Folder wyjúciowy nie istnieje: {_outputDirectory}");
            result.Success = false;
            return result;
        }

        // Test 1: Weryfikacja istnienia plikÛw
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
        Console.WriteLine("TEST 1: Weryfikacja istnienia plikÛw CSV");
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
        await VerifyFilesExistAsync(result);
        Console.WriteLine();

        // Test 2: Weryfikacja struktury i liczby rekordÛw
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
        Console.WriteLine("TEST 2: Weryfikacja struktury i liczby rekordÛw");
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
        await VerifyFileStructuresAsync(result);
        Console.WriteLine();

        // Test 3: Weryfikacja kluczy g≥Ûwnych (unikalne, brak duplikatÛw)
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
        Console.WriteLine("TEST 3: Weryfikacja unikalnoúci kluczy g≥Ûwnych");
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
        await VerifyPrimaryKeysAsync(result);
        Console.WriteLine();

        // Test 4: Weryfikacja relacji miÍdzy tabelami
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
        Console.WriteLine("TEST 4: Weryfikacja spÛjnoúci relacji");
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
        await VerifyRelationsAsync(result);
        Console.WriteLine();

        // Test 5: Weryfikacja danych obowiπzkowych
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
        Console.WriteLine("TEST 5: Weryfikacja pÛl obowiπzkowych");
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
        await VerifyRequiredFieldsAsync(result);
        Console.WriteLine();

        result.EndTime = DateTime.Now;
        result.Duration = result.EndTime - result.StartTime;
        result.Success = result.Errors.Count == 0 && result.Warnings.Count == 0;

        // Podsumowanie
        PrintSummary(result);

        return result;
    }

    private async Task VerifyFilesExistAsync(VerificationResult result)
    {
        foreach (var (fileName, stats) in _expectedStats)
        {
            var filePath = Path.Combine(_outputDirectory, fileName);
            var fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists)
            {
                result.AddError($"? {fileName} - Plik nie istnieje");
                result.FileResults[fileName] = new FileVerificationResult 
                { 
                    FileName = fileName, 
                    Exists = false 
                };
            }
            else
            {
                var fileResult = new FileVerificationResult
                {
                    FileName = fileName,
                    Exists = true,
                    FileSizeBytes = fileInfo.Length
                };

                // Szybkie zliczenie linii
                var lineCount = 0;
                using (var reader = new StreamReader(filePath))
                {
                    while (await reader.ReadLineAsync() != null)
                        lineCount++;
                }

                fileResult.ActualRecords = lineCount - 1; // Minus nag≥Ûwek
                result.FileResults[fileName] = fileResult;

                var sizeKB = fileInfo.Length / 1024.0;
                var sizeMB = sizeKB / 1024.0;
                var sizeStr = sizeMB >= 1 ? $"{sizeMB:F2} MB" : $"{sizeKB:F2} KB";

                Console.WriteLine($"? {fileName,-35} | {fileResult.ActualRecords,8:N0} rekordÛw | {sizeStr,10}");
            }
        }
    }

    private async Task VerifyFileStructuresAsync(VerificationResult result)
    {
        foreach (var (fileName, expectedStats) in _expectedStats)
        {
            if (!result.FileResults.TryGetValue(fileName, out var fileResult) || !fileResult.Exists)
                continue;

            var filePath = Path.Combine(_outputDirectory, fileName);

            try
            {
                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

                await csv.ReadAsync();
                csv.ReadHeader();

                var actualColumns = csv.HeaderRecord?.Length ?? 0;
                fileResult.ActualColumns = actualColumns;

                // Weryfikacja liczby kolumn
                if (actualColumns != expectedStats.RequiredColumns)
                {
                    result.AddWarning($"??  {fileName}: Oczekiwano {expectedStats.RequiredColumns} kolumn, znaleziono {actualColumns}");
                }
                else
                {
                    Console.WriteLine($"? {fileName,-35} | Kolumn: {actualColumns}/{expectedStats.RequiredColumns}");
                }

                // Weryfikacja liczby rekordÛw
                var recordCount = fileResult.ActualRecords;
                if (recordCount < expectedStats.MinRecords)
                {
                    result.AddError($"? {fileName}: Za ma≥o rekordÛw ({recordCount:N0} < {expectedStats.MinRecords:N0})");
                }
                else if (recordCount > expectedStats.MaxRecords)
                {
                    result.AddWarning($"??  {fileName}: WiÍcej rekordÛw niø oczekiwano ({recordCount:N0} > {expectedStats.MaxRecords:N0})");
                }
                else
                {
                    Console.WriteLine($"? {fileName,-35} | RekordÛw: {recordCount:N0} (zakres: {expectedStats.MinRecords:N0}-{expectedStats.MaxRecords:N0})");
                }

                // Weryfikacja kluczowych kolumn
                var missingColumns = expectedStats.KeyColumns.Where(c => !csv.HeaderRecord!.Contains(c)).ToList();
                if (missingColumns.Any())
                {
                    result.AddError($"? {fileName}: Brakujπce kolumny: {string.Join(", ", missingColumns)}");
                }
            }
            catch (Exception ex)
            {
                result.AddError($"? {fileName}: B≥πd odczytu - {ex.Message}");
            }
        }
    }

    private async Task VerifyPrimaryKeysAsync(VerificationResult result)
    {
        var filesWithPK = new[] { "pacjenci.csv", "pracownicy.csv", "wizyty.csv", "szczepienia.csv" };

        foreach (var fileName in filesWithPK)
        {
            if (!result.FileResults.TryGetValue(fileName, out var fileResult) || !fileResult.Exists)
                continue;

            var filePath = Path.Combine(_outputDirectory, fileName);

            try
            {
                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

                var pkSet = new HashSet<long>();
                var duplicates = 0;

                await csv.ReadAsync();
                csv.ReadHeader();

                while (await csv.ReadAsync())
                {
                    var idImport = csv.GetField<long>("IdImport");
                    if (!pkSet.Add(idImport))
                    {
                        duplicates++;
                    }
                }

                fileResult.UniquePrimaryKeys = pkSet.Count;
                fileResult.DuplicatePrimaryKeys = duplicates;

                if (duplicates > 0)
                {
                    result.AddError($"? {fileName}: Znaleziono {duplicates} duplikatÛw kluczy g≥Ûwnych!");
                }
                else
                {
                    Console.WriteLine($"? {fileName,-35} | Unikalne klucze: {pkSet.Count:N0}");
                }
            }
            catch (Exception ex)
            {
                result.AddWarning($"??  {fileName}: Nie moøna zweryfikowaÊ kluczy - {ex.Message}");
            }
        }
    }

    private async Task VerifyRelationsAsync(VerificationResult result)
    {
        // Za≥aduj wszystkie IdImport z pacjentÛw i pracownikÛw
        var patientIds = await LoadIdsFromCsvAsync("pacjenci.csv", "IdImport");
        var employeeIds = await LoadIdsFromCsvAsync("pracownicy.csv", "IdImport");

        Console.WriteLine($"?? Za≥adowano {patientIds.Count:N0} ID pacjentÛw");
        Console.WriteLine($"?? Za≥adowano {employeeIds.Count:N0} ID pracownikÛw");
        Console.WriteLine();

        // Weryfikuj wizyty
        if (result.FileResults.TryGetValue("wizyty.csv", out var visitResult) && visitResult.Exists)
        {
            var orphanedPatients = 0;
            var orphanedEmployees = 0;
            var validVisits = 0;

            var filePath = Path.Combine(_outputDirectory, "wizyty.csv");
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                var pacjentId = csv.GetField<long>("PacjentIdImport");
                var pracownikId = csv.GetField<long>("PracownikIdImport");

                var patientExists = patientIds.Contains(pacjentId);
                var employeeExists = employeeIds.Contains(pracownikId);

                if (!patientExists) orphanedPatients++;
                if (!employeeExists) orphanedEmployees++;
                if (patientExists && employeeExists) validVisits++;
            }

            if (orphanedPatients > 0)
            {
                result.AddWarning($"??  wizyty.csv: {orphanedPatients:N0} wizyt z nieistniejπcym pacjentem");
            }
            if (orphanedEmployees > 0)
            {
                result.AddWarning($"??  wizyty.csv: {orphanedEmployees:N0} wizyt z nieistniejπcym pracownikiem");
            }

            Console.WriteLine($"? wizyty.csv: {validVisits:N0} wizyt z poprawnymi relacjami");
        }

        // Weryfikuj szczepienia
        if (result.FileResults.TryGetValue("szczepienia.csv", out var vaccResult) && vaccResult.Exists)
        {
            var orphanedPatients = 0;
            var validVaccinations = 0;

            var filePath = Path.Combine(_outputDirectory, "szczepienia.csv");
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                var pacjentId = csv.GetField<long>("PacjentIdImport");

                if (patientIds.Contains(pacjentId))
                    validVaccinations++;
                else
                    orphanedPatients++;
            }

            if (orphanedPatients > 0)
            {
                result.AddWarning($"??  szczepienia.csv: {orphanedPatients:N0} szczepieÒ z nieistniejπcym pacjentem");
            }

            Console.WriteLine($"? szczepienia.csv: {validVaccinations:N0} szczepieÒ z poprawnymi relacjami");
        }
    }

    private async Task VerifyRequiredFieldsAsync(VerificationResult result)
    {
        // Weryfikuj pacjenci.csv - pola obowiπzkowe
        if (result.FileResults.TryGetValue("pacjenci.csv", out var patResult) && patResult.Exists)
        {
            var nullCounts = new Dictionary<string, int>
            {
                ["Imie"] = 0,
                ["Nazwisko"] = 0,
                ["DataUrodzenia"] = 0
            };

            var filePath = Path.Combine(_outputDirectory, "pacjenci.csv");
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                foreach (var field in nullCounts.Keys.ToList())
                {
                    var value = csv.GetField(field);
                    if (string.IsNullOrWhiteSpace(value))
                        nullCounts[field]++;
                }
            }

            foreach (var (field, count) in nullCounts)
            {
                if (count > 0)
                {
                    result.AddError($"? pacjenci.csv: Pole '{field}' puste w {count} rekordach");
                }
                else
                {
                    Console.WriteLine($"? pacjenci.csv: Pole '{field}' - wszystkie rekordy wype≥nione");
                }
            }
        }

        // Weryfikuj wizyty.csv
        if (result.FileResults.TryGetValue("wizyty.csv", out var visResult) && visResult.Exists)
        {
            var nullCount = 0;
            var filePath = Path.Combine(_outputDirectory, "wizyty.csv");
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                var dataOd = csv.GetField("DataOd");
                if (string.IsNullOrWhiteSpace(dataOd))
                    nullCount++;
            }

            if (nullCount > 0)
            {
                result.AddError($"? wizyty.csv: Pole 'DataOd' puste w {nullCount} rekordach");
            }
            else
            {
                Console.WriteLine($"? wizyty.csv: Pole 'DataOd' - wszystkie rekordy wype≥nione");
            }
        }
    }

    private async Task<HashSet<long>> LoadIdsFromCsvAsync(string fileName, string columnName)
    {
        var ids = new HashSet<long>();
        var filePath = Path.Combine(_outputDirectory, fileName);

        if (!File.Exists(filePath))
            return ids;

        try
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                var id = csv.GetField<long>(columnName);
                ids.Add(id);
            }
        }
        catch
        {
            // Ignoruj b≥Ídy
        }

        return ids;
    }

    private void PrintSummary(VerificationResult result)
    {
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
        Console.WriteLine("PODSUMOWANIE TEST”W WERYFIKACYJNYCH");
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
        Console.WriteLine();
        Console.WriteLine($"??  Czas wykonania: {result.Duration:mm\\:ss}");
        Console.WriteLine($"?? PlikÛw zweryfikowanych: {result.FileResults.Count}");
        Console.WriteLine($"? B≥Ídy: {result.Errors.Count}");
        Console.WriteLine($"??  Ostrzeøenia: {result.Warnings.Count}");
        Console.WriteLine();

        if (result.Errors.Any())
        {
            Console.WriteLine("B£ DY:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"  {error}");
            }
            Console.WriteLine();
        }

        if (result.Warnings.Any())
        {
            Console.WriteLine("OSTRZEØENIA:");
            foreach (var warning in result.Warnings)
            {
                Console.WriteLine($"  {warning}");
            }
            Console.WriteLine();
        }

        // Tabela wynikÛw
        Console.WriteLine("SZCZEG”£Y PLIK”W:");
        Console.WriteLine($"{"Plik",-35} | {"RekordÛw",10} | {"Kolumn",8} | {"Rozmiar",12} | Status");
        Console.WriteLine(new string('-', 80));

        foreach (var fileResult in result.FileResults.Values.OrderBy(f => f.FileName))
        {
            var sizeKB = fileResult.FileSizeBytes / 1024.0;
            var sizeMB = sizeKB / 1024.0;
            var sizeStr = sizeMB >= 1 ? $"{sizeMB:F2} MB" : $"{sizeKB:F2} KB";

            var status = fileResult.Exists ? "?" : "?";
            Console.WriteLine($"{fileResult.FileName,-35} | {fileResult.ActualRecords,10:N0} | {fileResult.ActualColumns,8} | {sizeStr,12} | {status}");
        }

        Console.WriteLine();
        
        if (result.Success)
        {
            Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
            Console.WriteLine("? WSZYSTKIE TESTY ZAKO—CZONE SUKCESEM!");
            Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
        }
        else
        {
            Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
            Console.WriteLine("? TESTY WYKRY£Y PROBLEMY - SPRAWDè B£ DY I OSTRZEØENIA POWYØEJ");
            Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
        }
    }

    // Generuj raport CSV
    public async Task GenerateReportAsync(VerificationResult result, string reportPath = "verification_report.csv")
    {
        using var writer = new StreamWriter(reportPath);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        await csv.WriteRecordsAsync(result.FileResults.Values);
        Console.WriteLine($"?? Raport zapisany: {reportPath}");
    }
}

// Klasy pomocnicze

public class ExpectedCsvStats
{
    public int MinRecords { get; set; }
    public int MaxRecords { get; set; }
    public int RequiredColumns { get; set; }
    public string[] KeyColumns { get; set; } = Array.Empty<string>();
}

public class VerificationResult
{
    public bool Success { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    public Dictionary<string, FileVerificationResult> FileResults { get; } = new();

    public void AddError(string error) => Errors.Add(error);
    public void AddWarning(string warning) => Warnings.Add(warning);
}

public class FileVerificationResult
{
    public string FileName { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public long FileSizeBytes { get; set; }
    public int ActualRecords { get; set; }
    public int ActualColumns { get; set; }
    public int UniquePrimaryKeys { get; set; }
    public int DuplicatePrimaryKeys { get; set; }
}
