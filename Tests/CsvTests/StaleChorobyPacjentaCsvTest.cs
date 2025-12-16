using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace MyDr_Import.Tests.CsvTests;

/// <summary>
/// Test dla pliku stale_choroby_pacjenta.csv
/// </summary>
public class StaleChorobyPacjentaCsvTest : BaseCsvTest
{
    public StaleChorobyPacjentaCsvTest(string outputDirectory) : base(outputDirectory) { }

    protected override string FileName => "stale_choroby_pacjenta.csv";
    protected override int ExpectedMinRecords => 10000;
    protected override int ExpectedMaxRecords => 100000;
    protected override int ExpectedColumns => 6;
    protected override string PrimaryKeyColumn => ""; // Brak klucza g³ównego - relacja M:N

    protected override string[] RequiredColumns => new[]
    {
        "PacjentIdImport", "ICD10", "DataRozpoznania"
    };

    /// <summary>
    /// Weryfikacja pól obowi¹zkowych dla sta³ych chorób
    /// </summary>
    protected override async Task VerifyRequiredFieldsAsync(CsvTestResult result, string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

        await csv.ReadAsync();
        csv.ReadHeader();

        var nullCounts = new Dictionary<string, int>
        {
            ["PacjentIdImport"] = 0,
            ["ICD10"] = 0
        };

        var invalidIcd10 = 0;
        var invalidDates = 0;

        while (await csv.ReadAsync())
        {
            foreach (var field in nullCounts.Keys.ToList())
            {
                var value = csv.GetField(field);
                if (string.IsNullOrWhiteSpace(value))
                    nullCounts[field]++;
            }

            // SprawdŸ format ICD-10 (powinien byæ A00-Z99)
            var icd10 = csv.GetField("ICD10");
            if (!string.IsNullOrWhiteSpace(icd10))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(icd10, @"^[A-Z]\d{2}"))
                {
                    invalidIcd10++;
                }
            }

            // SprawdŸ format daty
            var dataRozpoznania = csv.GetField("DataRozpoznania");
            if (!string.IsNullOrWhiteSpace(dataRozpoznania) && !DateTime.TryParse(dataRozpoznania, out _))
            {
                invalidDates++;
            }
        }

        foreach (var (field, count) in nullCounts)
        {
            if (count > 0)
            {
                result.AddError($"Pole '{field}' puste w {count:N0} rekordach");
            }
            else
            {
                Console.WriteLine($"? Pole '{field}' - wszystkie rekordy wype³nione");
            }
        }

        if (invalidIcd10 > 0)
        {
            result.AddWarning($"Nieprawid³owy format ICD-10 w {invalidIcd10} rekordach");
        }
        else
        {
            Console.WriteLine($"? Wszystkie kody ICD-10 w poprawnym formacie");
        }

        if (invalidDates > 0)
        {
            result.AddWarning($"Nieprawid³owy format daty w {invalidDates} rekordach");
        }
    }

    /// <summary>
    /// Dodatkowe walidacje specyficzne dla sta³ych chorób
    /// </summary>
    protected override async Task RunCustomValidationsAsync(CsvTestResult result, string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

        await csv.ReadAsync();
        csv.ReadHeader();

        var icd10Counts = new Dictionary<string, int>();
        var patientCounts = new Dictionary<long, int>();

        while (await csv.ReadAsync())
        {
            // Zlicz rozpoznania
            var icd10 = csv.GetField("ICD10");
            if (!string.IsNullOrWhiteSpace(icd10))
            {
                icd10Counts[icd10] = icd10Counts.GetValueOrDefault(icd10) + 1;
            }

            // Zlicz choroby na pacjenta
            if (long.TryParse(csv.GetField("PacjentIdImport"), out var patientId))
            {
                patientCounts[patientId] = patientCounts.GetValueOrDefault(patientId) + 1;
            }
        }

        // Raportuj wyniki
        Console.WriteLine($"? Unikalnych kodów ICD-10: {icd10Counts.Count}");
        Console.WriteLine($"? Pacjentów z chorobami przewlek³ymi: {patientCounts.Count:N0}");
        Console.WriteLine($"? Œrednio chorób na pacjenta: {(result.ActualRecords / (double)patientCounts.Count):F2}");

        var topDiseases = icd10Counts.OrderByDescending(x => x.Value).Take(5).ToList();
        Console.WriteLine($"  Top 5 rozpoznañ:");
        foreach (var (code, count) in topDiseases)
        {
            Console.WriteLine($"    - {code}: {count:N0} ({(count * 100.0 / result.ActualRecords):F1}%)");
        }

        // SprawdŸ czy s¹ pacjenci z du¿¹ liczb¹ chorób
        var maxDiseases = patientCounts.Values.Max();
        if (maxDiseases > 20)
        {
            result.AddWarning($"Znaleziono pacjenta z {maxDiseases} chorobami przewlek³ymi");
        }
    }
}
