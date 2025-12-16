using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace MyDr_Import.Tests.CsvTests;

/// <summary>
/// Test dla pliku wyniki_badan_laboratoryjnych_szczegoly.csv
/// </summary>
public class WynikiDadanLabSzczegolyTest : BaseCsvTest
{
    public WynikiDadanLabSzczegolyTest(string outputDirectory) : base(outputDirectory) { }

    protected override string FileName => "wyniki_badan_laboratoryjnych_szczegoly.csv";
    protected override int ExpectedMinRecords => 0;
    protected override int ExpectedMaxRecords => 200000;
    protected override int ExpectedColumns => 15;
    protected override string PrimaryKeyColumn => "IdImport";

    protected override string[] RequiredColumns => new[]
    {
        "IdImport", "PacjentIdImport", "NazwaBadania", "Wynik"
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
            ["NazwaBadania"] = 0
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

        var testTypes = new Dictionary<string, int>();

        while (await csv.ReadAsync())
        {
            var nazwa = csv.GetField("NazwaBadania");
            if (!string.IsNullOrWhiteSpace(nazwa))
            {
                testTypes[nazwa] = testTypes.GetValueOrDefault(nazwa) + 1;
            }
        }

        Console.WriteLine($"? Unikalnych typów badañ: {testTypes.Count}");
        
        var topTests = testTypes.OrderByDescending(x => x.Value).Take(5).ToList();
        Console.WriteLine($"  Top 5 badañ:");
        foreach (var (name, count) in topTests)
        {
            Console.WriteLine($"    - {name}: {count:N0}");
        }
    }
}
