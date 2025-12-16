using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace MyDr_Import.Tests.CsvTests;

/// <summary>
/// Test dla pliku dokumenty_uprawniajace.csv
/// </summary>
public class DokumentyUprawniajaceCsvTest : BaseCsvTest
{
    public DokumentyUprawniajaceCsvTest(string outputDirectory) : base(outputDirectory) { }

    protected override string FileName => "dokumenty_uprawniajace.csv";
    protected override int ExpectedMinRecords => 0;
    protected override int ExpectedMaxRecords => 30000;
    protected override int ExpectedColumns => 8;
    protected override string PrimaryKeyColumn => "IdImport";

    protected override string[] RequiredColumns => new[]
    {
        "IdImport", "PacjentIdImport", "TypDokumentu"
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
            ["TypDokumentu"] = 0
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
}
