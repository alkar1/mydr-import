using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace MyDr_Import.Tests.CsvTests;

/// <summary>
/// Test dla pliku pracownicy.csv
/// </summary>
public class PracownicyCsvTest : BaseCsvTest
{
    public PracownicyCsvTest(string outputDirectory) : base(outputDirectory) { }

    protected override string FileName => "pracownicy.csv";
    protected override int ExpectedMinRecords => 150;
    protected override int ExpectedMaxRecords => 200;
    protected override int ExpectedColumns => 23;
    protected override string PrimaryKeyColumn => "IdImport";

    protected override string[] RequiredColumns => new[]
    {
        "IdImport", "Imie", "Nazwisko", "Email", "Telefon",
        "NPWZ", "IdInstalacji", "Aktywny"
    };

    /// <summary>
    /// Weryfikacja pól obowi¹zkowych dla pracowników
    /// </summary>
    protected override async Task VerifyRequiredFieldsAsync(CsvTestResult result, string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

        await csv.ReadAsync();
        csv.ReadHeader();

        var nullCounts = new Dictionary<string, int>
        {
            ["Imie"] = 0,
            ["Nazwisko"] = 0,
            ["NPWZ"] = 0
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

    /// <summary>
    /// Dodatkowe walidacje specyficzne dla pracowników
    /// </summary>
    protected override async Task RunCustomValidationsAsync(CsvTestResult result, string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

        await csv.ReadAsync();
        csv.ReadHeader();

        var npwzSet = new HashSet<string>();
        var duplicateNpwz = 0;
        var invalidNpwzLength = 0;
        var withEmailCount = 0;
        var withPhoneCount = 0;

        while (await csv.ReadAsync())
        {
            var npwz = csv.GetField("NPWZ");
            
            // SprawdŸ duplikaty NPWZ
            if (!string.IsNullOrWhiteSpace(npwz))
            {
                if (!npwzSet.Add(npwz))
                {
                    duplicateNpwz++;
                }

                // NPWZ powinien mieæ 7 cyfr
                if (npwz.Length != 7 || !npwz.All(char.IsDigit))
                {
                    invalidNpwzLength++;
                }
            }

            // Statystyki wype³nienia
            if (!string.IsNullOrWhiteSpace(csv.GetField("Email"))) withEmailCount++;
            if (!string.IsNullOrWhiteSpace(csv.GetField("Telefon"))) withPhoneCount++;
        }

        // Raportuj wyniki
        if (duplicateNpwz > 0)
        {
            result.AddError($"Duplikaty NPWZ: {duplicateNpwz}");
        }
        else
        {
            Console.WriteLine($"? NPWZ - brak duplikatów");
        }

        if (invalidNpwzLength > 0)
        {
            result.AddWarning($"Nieprawid³owy format NPWZ (nie 7 cyfr): {invalidNpwzLength}");
        }
        else
        {
            Console.WriteLine($"? Wszystkie NPWZ w poprawnym formacie (7 cyfr)");
        }

        Console.WriteLine($"? Pracownicy z emailem: {withEmailCount:N0} ({(withEmailCount * 100.0 / result.ActualRecords):F1}%)");
        Console.WriteLine($"? Pracownicy z telefonem: {withPhoneCount:N0} ({(withPhoneCount * 100.0 / result.ActualRecords):F1}%)");
    }
}
