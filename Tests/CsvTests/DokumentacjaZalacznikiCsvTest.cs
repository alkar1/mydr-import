using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace MyDr_Import.Tests.CsvTests;

/// <summary>
/// Test dla pliku dokumentacja_zalaczniki.csv
/// </summary>
public class DokumentacjaZalacznikiCsvTest : BaseCsvTest
{
    public DokumentacjaZalacznikiCsvTest(string outputDirectory) : base(outputDirectory) { }

    protected override string FileName => "dokumentacja_zalaczniki.csv";
    protected override int ExpectedMinRecords => 0;
    protected override int ExpectedMaxRecords => 50000;
    protected override int ExpectedColumns => 10;
    protected override string PrimaryKeyColumn => "IdImport";

    protected override string[] RequiredColumns => new[]
    {
        "IdImport", "PacjentIdImport", "NazwaPliku"
    };

    protected override async Task VerifyRequiredFieldsAsync(CsvTestResult result, string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

        await csv.ReadAsync();
        csv.ReadHeader();

        var nullCounts = new Dictionary<string, int>
        {
            ["PacjentIdImport"] = 0,
            ["NazwaPliku"] = 0
        };

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
                result.AddError($"Pole '{field}' puste w {count:N0} rekordach");
            }
            else
            {
                Console.WriteLine($"? Pole '{field}' - wszystkie rekordy wype³nione");
            }
        }
    }

    protected override async Task RunCustomValidationsAsync(CsvTestResult result, string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

        await csv.ReadAsync();
        csv.ReadHeader();

        var fileTypes = new Dictionary<string, int>();
        var patientCounts = new Dictionary<long, int>();

        while (await csv.ReadAsync())
        {
            var nazwa = csv.GetField("NazwaPliku");
            if (!string.IsNullOrWhiteSpace(nazwa))
            {
                var ext = Path.GetExtension(nazwa).ToLower();
                fileTypes[ext] = fileTypes.GetValueOrDefault(ext) + 1;
            }

            if (long.TryParse(csv.GetField("PacjentIdImport"), out var patientId))
            {
                patientCounts[patientId] = patientCounts.GetValueOrDefault(patientId) + 1;
            }
        }

        Console.WriteLine($"? Pacjentów z za³¹cznikami: {patientCounts.Count:N0}");
        Console.WriteLine($"? Œrednio za³¹czników na pacjenta: {(result.ActualRecords / (double)Math.Max(patientCounts.Count, 1)):F2}");
        
        if (fileTypes.Any())
        {
            Console.WriteLine($"  Typy plików:");
            foreach (var (ext, count) in fileTypes.OrderByDescending(x => x.Value))
            {
                Console.WriteLine($"    - {ext}: {count:N0}");
            }
        }
    }
}
