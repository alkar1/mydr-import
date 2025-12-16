using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace MyDr_Import.Tests.CsvTests;

/// <summary>
/// Test dla pliku szczepienia.csv
/// </summary>
public class SzczepienCsvTest : BaseCsvTest
{
    public SzczepienCsvTest(string outputDirectory) : base(outputDirectory) { }

    protected override string FileName => "szczepienia.csv";
    protected override int ExpectedMinRecords => 48000;
    protected override int ExpectedMaxRecords => 52000;
    protected override int ExpectedColumns => 16;
    protected override string PrimaryKeyColumn => "IdImport";

    protected override string[] RequiredColumns => new[]
    {
        "IdImport", "PacjentIdImport", "PracownikIdImport", "Nazwa",
        "DataPodania", "IdInstalacji"
    };

    /// <summary>
    /// Weryfikacja pól obowi¹zkowych dla szczepieñ
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
            ["Nazwa"] = 0,
            ["DataPodania"] = 0
        };

        var invalidDates = 0;
        var futureVaccinations = 0;

        while (await csv.ReadAsync())
        {
            foreach (var field in nullCounts.Keys.ToList())
            {
                var value = csv.GetField(field);
                if (string.IsNullOrWhiteSpace(value))
                    nullCounts[field]++;
            }

            // SprawdŸ format daty podania
            var dataPodania = csv.GetField("DataPodania");
            if (!string.IsNullOrWhiteSpace(dataPodania))
            {
                if (!DateTime.TryParse(dataPodania, out var date))
                {
                    invalidDates++;
                }
                else if (date > DateTime.Now)
                {
                    futureVaccinations++;
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
            Console.WriteLine($"? Wszystkie daty szczepieñ w poprawnym formacie");
        }

        if (futureVaccinations > 0)
        {
            result.AddWarning($"Szczepienia w przysz³oœci: {futureVaccinations}");
        }
    }

    /// <summary>
    /// Dodatkowe walidacje specyficzne dla szczepieñ
    /// </summary>
    protected override async Task RunCustomValidationsAsync(CsvTestResult result, string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

        await csv.ReadAsync();
        csv.ReadHeader();

        var vaccineTypes = new Dictionary<string, int>();
        var withSeriesNumber = 0;
        var withExpiryDate = 0;
        var withAdministrationSite = 0;
        var oldestDate = DateTime.MaxValue;
        var newestDate = DateTime.MinValue;

        while (await csv.ReadAsync())
        {
            // Zlicz typy szczepionek
            var nazwa = csv.GetField("Nazwa");
            if (!string.IsNullOrWhiteSpace(nazwa))
            {
                vaccineTypes[nazwa] = vaccineTypes.GetValueOrDefault(nazwa) + 1;
            }

            // Statystyki wype³nienia
            if (!string.IsNullOrWhiteSpace(csv.GetField("NumerSerii"))) 
                withSeriesNumber++;
            if (!string.IsNullOrWhiteSpace(csv.GetField("DataWaznosci"))) 
                withExpiryDate++;
            if (!string.IsNullOrWhiteSpace(csv.GetField("MiejscePodania"))) 
                withAdministrationSite++;

            // Zakres dat
            var dataPodania = csv.GetField("DataPodania");
            if (DateTime.TryParse(dataPodania, out var date))
            {
                if (date < oldestDate) oldestDate = date;
                if (date > newestDate) newestDate = date;
            }
        }

        // Raportuj wyniki
        Console.WriteLine($"? Unikalnych typów szczepionek: {vaccineTypes.Count}");
        
        var topVaccines = vaccineTypes.OrderByDescending(x => x.Value).Take(5).ToList();
        Console.WriteLine($"  Top 5 szczepionek:");
        foreach (var (name, count) in topVaccines)
        {
            Console.WriteLine($"    - {name}: {count:N0} ({(count * 100.0 / result.ActualRecords):F1}%)");
        }

        Console.WriteLine($"? Szczepienia z numerem serii: {withSeriesNumber:N0} ({(withSeriesNumber * 100.0 / result.ActualRecords):F1}%)");
        Console.WriteLine($"? Szczepienia z dat¹ wa¿noœci: {withExpiryDate:N0} ({(withExpiryDate * 100.0 / result.ActualRecords):F1}%)");
        Console.WriteLine($"? Szczepienia z miejscem podania: {withAdministrationSite:N0} ({(withAdministrationSite * 100.0 / result.ActualRecords):F1}%)");
        
        if (oldestDate != DateTime.MaxValue && newestDate != DateTime.MinValue)
        {
            Console.WriteLine($"? Zakres dat szczepieñ: {oldestDate:yyyy-MM-dd} do {newestDate:yyyy-MM-dd}");
        }

        // Ostrze¿enie jeœli brak danych opcjonalnych
        if (withSeriesNumber == 0)
        {
            result.AddWarning("Brak numerów serii dla szczepieñ");
        }
    }
}
