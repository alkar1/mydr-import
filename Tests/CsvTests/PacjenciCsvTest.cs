using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace MyDr_Import.Tests.CsvTests;

/// <summary>
/// Test dla pliku pacjenci.csv
/// </summary>
public class PacjenciCsvTest : BaseCsvTest
{
    public PacjenciCsvTest(string outputDirectory) : base(outputDirectory) { }

    protected override string FileName => "pacjenci.csv";
    protected override int ExpectedMinRecords => 22397;
    protected override int ExpectedMaxRecords => 22400;
    protected override int ExpectedColumns => 68;
    protected override string PrimaryKeyColumn => "IdImport";

    protected override string[] RequiredColumns => new[]
    {
        "IdImport", "Imie", "Nazwisko", "DataUrodzenia", "Plec", 
        "PESEL", "Email", "Telefon", "IdInstalacji", "Aktywny"
    };

    /// <summary>
    /// Weryfikacja pól obowi¹zkowych dla pacjentów
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
            ["DataUrodzenia"] = 0,
            ["Plec"] = 0
        };

        var invalidDates = 0;
        var invalidGenders = 0;

        while (await csv.ReadAsync())
        {
            // SprawdŸ puste wartoœci
            foreach (var field in nullCounts.Keys.ToList())
            {
                var value = csv.GetField(field);
                if (string.IsNullOrWhiteSpace(value))
                    nullCounts[field]++;
            }

            // SprawdŸ format daty urodzenia
            var birthDate = csv.GetField("DataUrodzenia");
            if (!string.IsNullOrWhiteSpace(birthDate) && !DateTime.TryParse(birthDate, out _))
            {
                invalidDates++;
            }

            // SprawdŸ p³eæ (powinna byæ K lub M)
            var plec = csv.GetField("Plec");
            if (!string.IsNullOrWhiteSpace(plec) && plec != "K" && plec != "M")
            {
                invalidGenders++;
            }
        }

        // Raportuj b³êdy
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
            Console.WriteLine($"? Wszystkie daty urodzenia w poprawnym formacie");
        }

        if (invalidGenders > 0)
        {
            result.AddError($"Nieprawid³owa p³eæ (nie K/M) w {invalidGenders} rekordach");
        }
        else
        {
            Console.WriteLine($"? Wszystkie p³cie w poprawnym formacie (K/M)");
        }
    }

    /// <summary>
    /// Dodatkowe walidacje specyficzne dla pacjentów
    /// </summary>
    protected override async Task RunCustomValidationsAsync(CsvTestResult result, string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

        await csv.ReadAsync();
        csv.ReadHeader();

        var peselSet = new HashSet<string>();
        var duplicatePesels = 0;
        var invalidPesels = 0;
        var minorsCount = 0;
        var withAddressCount = 0;
        var withEmailCount = 0;
        var withPhoneCount = 0;

        while (await csv.ReadAsync())
        {
            var pesel = csv.GetField("PESEL");
            
            // SprawdŸ duplikaty PESEL
            if (!string.IsNullOrWhiteSpace(pesel))
            {
                if (!peselSet.Add(pesel))
                {
                    duplicatePesels++;
                }

                // SprawdŸ d³ugoœæ PESEL
                if (pesel.Length != 11)
                {
                    invalidPesels++;
                }
            }

            // SprawdŸ niepe³noletnich (dla weryfikacji opiekuna)
            var birthDate = csv.GetField("DataUrodzenia");
            if (DateTime.TryParse(birthDate, out var birth))
            {
                var age = DateTime.Now.Year - birth.Year;
                if (age < 18) minorsCount++;
            }

            // Statystyki wype³nienia
            if (!string.IsNullOrWhiteSpace(csv.GetField("Ulica"))) withAddressCount++;
            if (!string.IsNullOrWhiteSpace(csv.GetField("Email"))) withEmailCount++;
            if (!string.IsNullOrWhiteSpace(csv.GetField("Telefon"))) withPhoneCount++;
        }

        // Raportuj wyniki
        if (duplicatePesels > 0)
        {
            result.AddWarning($"Duplikaty PESEL: {duplicatePesels}");
        }
        else
        {
            Console.WriteLine($"? PESEL - brak duplikatów");
        }

        if (invalidPesels > 0)
        {
            result.AddWarning($"Nieprawid³owa d³ugoœæ PESEL: {invalidPesels}");
        }

        Console.WriteLine($"? Niepe³noletni pacjenci: {minorsCount:N0}");
        Console.WriteLine($"? Pacjenci z adresem: {withAddressCount:N0} ({(withAddressCount * 100.0 / result.ActualRecords):F1}%)");
        Console.WriteLine($"? Pacjenci z emailem: {withEmailCount:N0} ({(withEmailCount * 100.0 / result.ActualRecords):F1}%)");
        Console.WriteLine($"? Pacjenci z telefonem: {withPhoneCount:N0} ({(withPhoneCount * 100.0 / result.ActualRecords):F1}%)");
    }
}
