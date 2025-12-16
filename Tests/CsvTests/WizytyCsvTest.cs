using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace MyDr_Import.Tests.CsvTests;

/// <summary>
/// Test dla pliku wizyty.csv
/// </summary>
public class WizytyCsvTest : BaseCsvTest
{
    public WizytyCsvTest(string outputDirectory) : base(outputDirectory) { }

    protected override string FileName => "wizyty.csv";
    protected override int ExpectedMinRecords => 696727;
    protected override int ExpectedMaxRecords => 700000;
    protected override int ExpectedColumns => 33;
    protected override string PrimaryKeyColumn => "IdImport";

    protected override string[] RequiredColumns => new[]
    {
        "IdImport", "PacjentIdImport", "PracownikIdImport", "DataOd",
        "DataDo", "Wywiad", "IdInstalacji"
    };

    /// <summary>
    /// Weryfikacja pól obowi¹zkowych dla wizyt
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
            ["PracownikIdImport"] = 0,
            ["DataOd"] = 0
        };

        var invalidDates = 0;
        var futureVisits = 0;

        while (await csv.ReadAsync())
        {
            foreach (var field in nullCounts.Keys.ToList())
            {
                var value = csv.GetField(field);
                if (string.IsNullOrWhiteSpace(value))
                    nullCounts[field]++;
            }

            // SprawdŸ format daty
            var dataOd = csv.GetField("DataOd");
            if (!string.IsNullOrWhiteSpace(dataOd))
            {
                if (!DateTime.TryParse(dataOd, out var date))
                {
                    invalidDates++;
                }
                else if (date > DateTime.Now)
                {
                    futureVisits++;
                }
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

        if (invalidDates > 0)
        {
            result.AddError($"Nieprawid³owy format daty w {invalidDates} rekordach");
        }
        else
        {
            Console.WriteLine($"? Wszystkie daty wizyt w poprawnym formacie");
        }

        if (futureVisits > 0)
        {
            result.AddWarning($"Wizyty w przysz³oœci: {futureVisits}");
        }
    }

    /// <summary>
    /// Dodatkowe walidacje specyficzne dla wizyt
    /// </summary>
    protected override async Task RunCustomValidationsAsync(CsvTestResult result, string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

        await csv.ReadAsync();
        csv.ReadHeader();

        var withInterviewCount = 0;
        var withIcd10Count = 0;
        var withIcd9Count = 0;
        var oldestDate = DateTime.MaxValue;
        var newestDate = DateTime.MinValue;
        var sampleSize = Math.Min(result.ActualRecords, 10000); // Próbka dla wydajnoœci

        var recordsProcessed = 0;

        while (await csv.ReadAsync() && recordsProcessed < sampleSize)
        {
            recordsProcessed++;

            // SprawdŸ wype³nienie wywiadu
            if (!string.IsNullOrWhiteSpace(csv.GetField("Wywiad"))) 
                withInterviewCount++;

            // SprawdŸ ICD-10
            if (!string.IsNullOrWhiteSpace(csv.GetField("Rozpoznania"))) 
                withIcd10Count++;

            // SprawdŸ ICD-9
            if (!string.IsNullOrWhiteSpace(csv.GetField("Procedury"))) 
                withIcd9Count++;

            // SprawdŸ zakres dat
            var dataOd = csv.GetField("DataOd");
            if (DateTime.TryParse(dataOd, out var date))
            {
                if (date < oldestDate) oldestDate = date;
                if (date > newestDate) newestDate = date;
            }
        }

        // Raportuj wyniki (proporcjonalnie do próbki)
        var ratio = (double)result.ActualRecords / recordsProcessed;
        var estimatedWithInterview = (int)(withInterviewCount * ratio);
        var estimatedWithIcd10 = (int)(withIcd10Count * ratio);
        var estimatedWithIcd9 = (int)(withIcd9Count * ratio);

        Console.WriteLine($"? Wizyty z wywiadem: ~{estimatedWithInterview:N0} ({(withInterviewCount * 100.0 / recordsProcessed):F1}%)");
        Console.WriteLine($"? Wizyty z ICD-10: ~{estimatedWithIcd10:N0} ({(withIcd10Count * 100.0 / recordsProcessed):F1}%)");
        Console.WriteLine($"? Wizyty z ICD-9: ~{estimatedWithIcd9:N0} ({(withIcd9Count * 100.0 / recordsProcessed):F1}%)");
        
        if (oldestDate != DateTime.MaxValue && newestDate != DateTime.MinValue)
        {
            Console.WriteLine($"? Zakres dat wizyt: {oldestDate:yyyy-MM-dd} do {newestDate:yyyy-MM-dd}");
        }
    }
}
