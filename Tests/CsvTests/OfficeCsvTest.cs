using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace MyDr_Import.Tests.CsvTests;

/// <summary>
/// Test dla pliku office.csv (gabinety/poradnie)
/// </summary>
public class OfficeCsvTest : BaseCsvTest
{
    public OfficeCsvTest(string outputDirectory) : base(outputDirectory) { }

    protected override string FileName => "office.csv";
    protected override int ExpectedMinRecords => 1;
    protected override int ExpectedMaxRecords => 100;
    protected override int ExpectedColumns => 10;
    protected override string PrimaryKeyColumn => "IdImport";

    protected override string[] RequiredColumns => new[]
    {
        "IdImport", "Nazwa", "IdInstalacji"
    };

    protected override async Task VerifyRequiredFieldsAsync(CsvTestResult result, string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

        await csv.ReadAsync();
        csv.ReadHeader();

        var nullCounts = new Dictionary<string, int>
        {
            ["Nazwa"] = 0
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

        var offices = new List<string>();

        while (await csv.ReadAsync())
        {
            var nazwa = csv.GetField("Nazwa");
            if (!string.IsNullOrWhiteSpace(nazwa))
            {
                offices.Add(nazwa);
            }
        }

        Console.WriteLine($"? Gabinety:");
        foreach (var office in offices)
        {
            Console.WriteLine($"    - {office}");
        }
    }
}
