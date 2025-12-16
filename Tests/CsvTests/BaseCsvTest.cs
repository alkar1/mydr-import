using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace MyDr_Import.Tests.CsvTests;

/// <summary>
/// Klasa bazowa dla testów CSV - wspólne metody weryfikacji
/// </summary>
public abstract class BaseCsvTest
{
    protected string OutputDirectory { get; }
    protected abstract string FileName { get; }
    protected abstract int ExpectedMinRecords { get; }
    protected abstract int ExpectedMaxRecords { get; }
    protected abstract int ExpectedColumns { get; }
    protected abstract string[] RequiredColumns { get; }
    protected abstract string PrimaryKeyColumn { get; }

    protected BaseCsvTest(string outputDirectory)
    {
        OutputDirectory = outputDirectory;
    }

    /// <summary>
    /// G³ówna metoda testowa - uruchamia wszystkie weryfikacje
    /// </summary>
    public async Task<CsvTestResult> RunTestAsync()
    {
        var result = new CsvTestResult
        {
            FileName = FileName,
            StartTime = DateTime.Now
        };

        Console.WriteLine($"???????????????????????????????????????????????????????????????????????????????");
        Console.WriteLine($"TEST: {FileName}");
        Console.WriteLine($"???????????????????????????????????????????????????????????????????????????????");

        var filePath = Path.Combine(OutputDirectory, FileName);

        // Test 1: Plik istnieje
        if (!File.Exists(filePath))
        {
            result.AddError("Plik nie istnieje");
            result.Passed = false;
            return result;
        }

        var fileInfo = new FileInfo(filePath);
        result.FileSizeBytes = fileInfo.Length;
        Console.WriteLine($"? Plik istnieje ({FormatFileSize(fileInfo.Length)})");

        try
        {
            // Test 2: Struktura CSV
            await VerifyStructureAsync(result, filePath);

            // Test 3: Liczba rekordów
            VerifyRecordCount(result);

            // Test 4: Klucze g³ówne
            await VerifyPrimaryKeysAsync(result, filePath);

            // Test 5: Pola wymagane
            await VerifyRequiredFieldsAsync(result, filePath);

            // Test 6: Testy specyficzne dla typu
            await RunCustomValidationsAsync(result, filePath);

            result.EndTime = DateTime.Now;
            result.Duration = result.EndTime - result.StartTime;
            result.Passed = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            result.AddError($"B³¹d podczas testowania: {ex.Message}");
            result.Passed = false;
        }

        PrintTestSummary(result);
        return result;
    }

    /// <summary>
    /// Weryfikacja struktury CSV
    /// </summary>
    private async Task VerifyStructureAsync(CsvTestResult result, string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

        await csv.ReadAsync();
        csv.ReadHeader();

        result.ActualColumns = csv.HeaderRecord?.Length ?? 0;

        // SprawdŸ liczbê kolumn
        if (result.ActualColumns != ExpectedColumns)
        {
            result.AddWarning($"Oczekiwano {ExpectedColumns} kolumn, znaleziono {result.ActualColumns}");
        }
        else
        {
            Console.WriteLine($"? Liczba kolumn: {result.ActualColumns}");
        }

        // SprawdŸ wymagane kolumny
        var missingColumns = RequiredColumns.Where(c => !csv.HeaderRecord!.Contains(c)).ToList();
        if (missingColumns.Any())
        {
            result.AddError($"Brakuj¹ce kolumny: {string.Join(", ", missingColumns)}");
        }
        else
        {
            Console.WriteLine($"? Wszystkie wymagane kolumny obecne");
        }

        // Zlicz rekordy
        var recordCount = 0;
        while (await csv.ReadAsync())
        {
            recordCount++;
        }
        result.ActualRecords = recordCount;
    }

    /// <summary>
    /// Weryfikacja liczby rekordów
    /// </summary>
    private void VerifyRecordCount(CsvTestResult result)
    {
        if (result.ActualRecords < ExpectedMinRecords)
        {
            result.AddError($"Za ma³o rekordów: {result.ActualRecords:N0} (oczekiwano min. {ExpectedMinRecords:N0})");
        }
        else if (result.ActualRecords > ExpectedMaxRecords)
        {
            result.AddWarning($"Wiêcej rekordów ni¿ oczekiwano: {result.ActualRecords:N0} (max {ExpectedMaxRecords:N0})");
        }
        else
        {
            Console.WriteLine($"? Liczba rekordów: {result.ActualRecords:N0} (zakres: {ExpectedMinRecords:N0}-{ExpectedMaxRecords:N0})");
        }
    }

    /// <summary>
    /// Weryfikacja unikalnoœci kluczy g³ównych
    /// </summary>
    private async Task VerifyPrimaryKeysAsync(CsvTestResult result, string filePath)
    {
        if (string.IsNullOrEmpty(PrimaryKeyColumn))
            return;

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

        await csv.ReadAsync();
        csv.ReadHeader();

        var pkSet = new HashSet<string>();
        var duplicates = 0;
        var nullKeys = 0;

        while (await csv.ReadAsync())
        {
            var pk = csv.GetField(PrimaryKeyColumn);
            if (string.IsNullOrWhiteSpace(pk))
            {
                nullKeys++;
            }
            else if (!pkSet.Add(pk))
            {
                duplicates++;
            }
        }

        result.UniquePrimaryKeys = pkSet.Count;
        result.DuplicatePrimaryKeys = duplicates;

        if (nullKeys > 0)
        {
            result.AddError($"Klucz g³ówny pusty w {nullKeys} rekordach");
        }

        if (duplicates > 0)
        {
            result.AddError($"Duplikaty kluczy g³ównych: {duplicates}");
        }
        else
        {
            Console.WriteLine($"? Klucze g³ówne unikalne: {pkSet.Count:N0}");
        }
    }

    /// <summary>
    /// Weryfikacja pól wymaganych - do nadpisania w klasach pochodnych
    /// </summary>
    protected virtual async Task VerifyRequiredFieldsAsync(CsvTestResult result, string filePath)
    {
        // Domyœlnie nie weryfikujemy
        await Task.CompletedTask;
    }

    /// <summary>
    /// Dodatkowe walidacje specyficzne dla typu - do nadpisania w klasach pochodnych
    /// </summary>
    protected virtual async Task RunCustomValidationsAsync(CsvTestResult result, string filePath)
    {
        // Domyœlnie brak dodatkowych walidacji
        await Task.CompletedTask;
    }

    /// <summary>
    /// Pomocnicza metoda do odczytu wszystkich rekordów z CSV
    /// </summary>
    protected async Task<List<Dictionary<string, string>>> ReadAllRecordsAsync(string filePath)
    {
        var records = new List<Dictionary<string, string>>();

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord!;

        while (await csv.ReadAsync())
        {
            var record = new Dictionary<string, string>();
            foreach (var header in headers)
            {
                record[header] = csv.GetField(header) ?? string.Empty;
            }
            records.Add(record);
        }

        return records;
    }

    /// <summary>
    /// Wyœwietla podsumowanie testu
    /// </summary>
    private void PrintTestSummary(CsvTestResult result)
    {
        Console.WriteLine();
        Console.WriteLine($"???????????????????????????????????????????????????????????????????????????????");
        
        if (result.Passed)
        {
            Console.WriteLine($"? TEST PASSED - {FileName}");
        }
        else
        {
            Console.WriteLine($"? TEST FAILED - {FileName}");
        }

        Console.WriteLine($"  Czas: {result.Duration.TotalSeconds:F2}s");
        Console.WriteLine($"  B³êdy: {result.Errors.Count}");
        Console.WriteLine($"  Ostrze¿enia: {result.Warnings.Count}");

        if (result.Errors.Any())
        {
            Console.WriteLine();
            Console.WriteLine("  B£ÊDY:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"    ? {error}");
            }
        }

        if (result.Warnings.Any())
        {
            Console.WriteLine();
            Console.WriteLine("  OSTRZE¯ENIA:");
            foreach (var warning in result.Warnings)
            {
                Console.WriteLine($"    ? {warning}");
            }
        }

        Console.WriteLine($"???????????????????????????????????????????????????????????????????????????????");
        Console.WriteLine();
    }

    private string FormatFileSize(long bytes)
    {
        var kb = bytes / 1024.0;
        var mb = kb / 1024.0;
        return mb >= 1 ? $"{mb:F2} MB" : $"{kb:F2} KB";
    }
}

/// <summary>
/// Wynik testu CSV
/// </summary>
public class CsvTestResult
{
    public string FileName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public long FileSizeBytes { get; set; }
    public int ActualRecords { get; set; }
    public int ActualColumns { get; set; }
    public int UniquePrimaryKeys { get; set; }
    public int DuplicatePrimaryKeys { get; set; }
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();

    public void AddError(string error) => Errors.Add(error);
    public void AddWarning(string warning) => Warnings.Add(warning);
}
