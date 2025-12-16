using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace MyDr_Import.Tests.CsvTests;

/// <summary>
/// Test dla pliku stale_leki_pacjenta.csv
/// </summary>
public class StaleLekiPacjentaCsvTest : BaseCsvTest
{
    public StaleLekiPacjentaCsvTest(string outputDirectory) : base(outputDirectory) { }

    protected override string FileName => "stale_leki_pacjenta.csv";
    protected override int ExpectedMinRecords => 5000;
    protected override int ExpectedMaxRecords => 20000;
    protected override int ExpectedColumns => 12;
    protected override string PrimaryKeyColumn => ""; // Brak klucza g³ównego - relacja M:N

    protected override string[] RequiredColumns => new[]
    {
        "PacjentIdImport", "KodKreskowy", "NazwaLeku"
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
            ["NazwaLeku"] = 0
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

        var drugCounts = new Dictionary<string, int>();
        var patientCounts = new Dictionary<long, int>();

        while (await csv.ReadAsync())
        {
            var nazwa = csv.GetField("NazwaLeku");
            if (!string.IsNullOrWhiteSpace(nazwa))
            {
                drugCounts[nazwa] = drugCounts.GetValueOrDefault(nazwa) + 1;
            }

            if (long.TryParse(csv.GetField("PacjentIdImport"), out var patientId))
            {
                patientCounts[patientId] = patientCounts.GetValueOrDefault(patientId) + 1;
            }
        }

        Console.WriteLine($"? Unikalnych leków: {drugCounts.Count}");
        Console.WriteLine($"? Pacjentów przyjmuj¹cych leki stale: {patientCounts.Count:N0}");
        Console.WriteLine($"? Œrednio leków na pacjenta: {(result.ActualRecords / (double)patientCounts.Count):F2}");

        var topDrugs = drugCounts.OrderByDescending(x => x.Value).Take(5).ToList();
        Console.WriteLine($"  Top 5 leków:");
        foreach (var (name, count) in topDrugs)
        {
            Console.WriteLine($"    - {name}: {count:N0}");
        }
    }
}
