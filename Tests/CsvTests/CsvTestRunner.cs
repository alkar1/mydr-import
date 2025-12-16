using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace MyDr_Import.Tests.CsvTests;

/// <summary>
/// Runner wszystkich testów CSV - uruchamia testy dla ka¿dego pliku osobno
/// </summary>
public class CsvTestRunner
{
    private readonly string _outputDirectory;
    private readonly List<BaseCsvTest> _tests;

    public CsvTestRunner(string outputDirectory = "output_csv")
    {
        _outputDirectory = outputDirectory;
        
        // Rejestruj wszystkie testy - 15 plików zgodnie z definicjami XLS
        _tests = new List<BaseCsvTest>
        {
            // G³ówne pliki
            new PacjenciCsvTest(outputDirectory),
            new PracownicyCsvTest(outputDirectory),
            new WizytyCsvTest(outputDirectory),
            new SzczepienCsvTest(outputDirectory),
            
            // Dodatkowe dane pacjentów
            new StaleChorobyPacjentaCsvTest(outputDirectory),
            new StaleLekiPacjentaCsvTest(outputDirectory),
            new DokumentyUprawniajaceCsvTest(outputDirectory),
            new DeklaracjePozCsvTest(outputDirectory),
            
            // Dokumentacja i badania
            new DokumentacjaZalacznikiCsvTest(outputDirectory),
            new SkierowaniaWystawioneCsvTest(outputDirectory),
            new KartyWizytCsvTest(outputDirectory),
            new WynikiDadanLabSzczegolyTest(outputDirectory),
            
            // Struktura organizacyjna
            new DepartmentsCsvTest(outputDirectory),
            new OfficeCsvTest(outputDirectory)
        };
    }

    /// <summary>
    /// Uruchom wszystkie testy
    /// </summary>
    public async Task<TestRunResult> RunAllTestsAsync()
    {
        Console.OutputEncoding = Encoding.UTF8;
        
        var runResult = new TestRunResult
        {
            StartTime = DateTime.Now
        };

        Console.WriteLine();
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????????");
        Console.WriteLine("?         TESTY AUTOMATYCZNE EKSPORTU CSV - PE£NA WERYFIKACJA                ?");
        Console.WriteLine("?                   MyDr Import - Ka¿dy plik osobno                          ?");
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????????");
        Console.WriteLine();
        Console.WriteLine($"?? Folder: {_outputDirectory}");
        Console.WriteLine($"?? Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"?? Liczba testów: {_tests.Count}");
        Console.WriteLine();

        // SprawdŸ czy folder istnieje
        if (!Directory.Exists(_outputDirectory))
        {
            Console.WriteLine($"? B£¥D: Folder wyjœciowy nie istnieje: {_outputDirectory}");
            Console.WriteLine();
            Console.WriteLine("U¿yj najpierw trybu 'export' aby wygenerowaæ pliki CSV:");
            Console.WriteLine($"  MyDr_Import.exe export [plik-xml]");
            Console.WriteLine();
            return runResult;
        }

        Console.WriteLine("???????????????????????????????????????????????????????????????????????????????");
        Console.WriteLine("URUCHAMIANIE TESTÓW");
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????????");
        Console.WriteLine();

        // Uruchom ka¿dy test
        foreach (var test in _tests)
        {
            try
            {
                var result = await test.RunTestAsync();
                runResult.TestResults.Add(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Krytyczny b³¹d podczas testu: {ex.Message}");
                Console.WriteLine();
            }
        }

        runResult.EndTime = DateTime.Now;
        runResult.Duration = runResult.EndTime - runResult.StartTime;

        // Wyœwietl podsumowanie
        PrintSummary(runResult);

        // Zapisz raport
        await GenerateReportsAsync(runResult);

        return runResult;
    }

    /// <summary>
    /// Wyœwietl podsumowanie wszystkich testów
    /// </summary>
    private void PrintSummary(TestRunResult runResult)
    {
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????????");
        Console.WriteLine("PODSUMOWANIE TESTÓW");
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????????");
        Console.WriteLine();

        var passed = runResult.TestResults.Count(t => t.Passed);
        var failed = runResult.TestResults.Count(t => !t.Passed);
        var totalErrors = runResult.TestResults.Sum(t => t.Errors.Count);
        var totalWarnings = runResult.TestResults.Sum(t => t.Warnings.Count);

        Console.WriteLine($"?? Czas wykonania: {runResult.Duration.TotalSeconds:F2}s");
        Console.WriteLine($"?? Testy: {runResult.TestResults.Count}");
        Console.WriteLine($"? Zaliczone: {passed}");
        Console.WriteLine($"? Niezaliczone: {failed}");
        Console.WriteLine($"??  Ostrze¿enia: {totalWarnings}");
        Console.WriteLine($"?? B³êdy: {totalErrors}");
        Console.WriteLine();

        // Tabela szczegó³owa
        Console.WriteLine("SZCZEGÓ£Y WYNIKÓW:");
        Console.WriteLine();
        Console.WriteLine($"{"Plik",-30} | {"Rekordów",12} | {"Status",10} | {"B³êdy",7} | {"Ostrz.",7}");
        Console.WriteLine(new string('?', 80));

        foreach (var result in runResult.TestResults)
        {
            var status = result.Passed ? "? PASSED" : "? FAILED";
            var fileName = result.FileName.Length > 30 ? result.FileName.Substring(0, 27) + "..." : result.FileName;
            
            Console.WriteLine($"{fileName,-30} | {result.ActualRecords,12:N0} | {status,-10} | {result.Errors.Count,7} | {result.Warnings.Count,7}");
        }

        Console.WriteLine();

        // Lista wszystkich b³êdów
        if (totalErrors > 0)
        {
            Console.WriteLine("???????????????????????????????????????????????????????????????????????????????");
            Console.WriteLine("WSZYSTKIE B£ÊDY:");
            Console.WriteLine("???????????????????????????????????????????????????????????????????????????????");
            foreach (var result in runResult.TestResults.Where(r => r.Errors.Any()))
            {
                Console.WriteLine();
                Console.WriteLine($"? {result.FileName}:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"   • {error}");
                }
            }
            Console.WriteLine();
        }

        // Lista wszystkich ostrze¿eñ
        if (totalWarnings > 0)
        {
            Console.WriteLine("???????????????????????????????????????????????????????????????????????????????");
            Console.WriteLine("WSZYSTKIE OSTRZE¯ENIA:");
            Console.WriteLine("???????????????????????????????????????????????????????????????????????????????");
            foreach (var result in runResult.TestResults.Where(r => r.Warnings.Any()))
            {
                Console.WriteLine();
                Console.WriteLine($"??  {result.FileName}:");
                foreach (var warning in result.Warnings)
                {
                    Console.WriteLine($"   • {warning}");
                }
            }
            Console.WriteLine();
        }

        // Koñcowy status
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????????");
        if (failed == 0 && totalErrors == 0)
        {
            Console.WriteLine("?? WSZYSTKIE TESTY ZALICZONE!");
            Console.WriteLine("   Eksport CSV dzia³a poprawnie dla wszystkich typów danych.");
        }
        else
        {
            Console.WriteLine("??  TESTY WYKRY£Y PROBLEMY");
            Console.WriteLine($"   {failed} z {runResult.TestResults.Count} testów niezaliczonych");
            Console.WriteLine($"   {totalErrors} b³êdów wymaga naprawy");
            Console.WriteLine();
            Console.WriteLine("   Przejrzyj szczegó³y powy¿ej i napraw wykryte problemy.");
        }
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????????");
        Console.WriteLine();
    }

    /// <summary>
    /// Generuj raporty testów
    /// </summary>
    private async Task GenerateReportsAsync(TestRunResult runResult)
    {
        var reportDir = Path.Combine(_outputDirectory, "test_reports");
        Directory.CreateDirectory(reportDir);

        // Raport CSV ze szczegó³ami
        var csvReportPath = Path.Combine(reportDir, $"test_results_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        await GenerateCsvReportAsync(runResult, csvReportPath);

        // Raport tekstowy
        var txtReportPath = Path.Combine(reportDir, $"test_summary_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        await GenerateTextReportAsync(runResult, txtReportPath);

        Console.WriteLine($"?? Raporty zapisane w: {reportDir}");
        Console.WriteLine($"   • {Path.GetFileName(csvReportPath)}");
        Console.WriteLine($"   • {Path.GetFileName(txtReportPath)}");
        Console.WriteLine();
    }

    /// <summary>
    /// Generuj raport CSV
    /// </summary>
    private async Task GenerateCsvReportAsync(TestRunResult runResult, string filePath)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        // Zapisz nag³ówki
        csv.WriteField("FileName");
        csv.WriteField("Passed");
        csv.WriteField("ActualRecords");
        csv.WriteField("ActualColumns");
        csv.WriteField("UniquePrimaryKeys");
        csv.WriteField("DuplicatePrimaryKeys");
        csv.WriteField("ErrorCount");
        csv.WriteField("WarningCount");
        csv.WriteField("Duration_Seconds");
        csv.WriteField("FileSizeBytes");
        await csv.NextRecordAsync();

        // Zapisz wyniki
        foreach (var result in runResult.TestResults)
        {
            csv.WriteField(result.FileName);
            csv.WriteField(result.Passed);
            csv.WriteField(result.ActualRecords);
            csv.WriteField(result.ActualColumns);
            csv.WriteField(result.UniquePrimaryKeys);
            csv.WriteField(result.DuplicatePrimaryKeys);
            csv.WriteField(result.Errors.Count);
            csv.WriteField(result.Warnings.Count);
            csv.WriteField(result.Duration.TotalSeconds);
            csv.WriteField(result.FileSizeBytes);
            await csv.NextRecordAsync();
        }
    }

    /// <summary>
    /// Generuj raport tekstowy
    /// </summary>
    private async Task GenerateTextReportAsync(TestRunResult runResult, string filePath)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

        await writer.WriteLineAsync("???????????????????????????????????????????????????????????????????????????????");
        await writer.WriteLineAsync("RAPORT TESTÓW AUTOMATYCZNYCH CSV");
        await writer.WriteLineAsync("???????????????????????????????????????????????????????????????????????????????");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync($"Data: {runResult.StartTime:yyyy-MM-dd HH:mm:ss}");
        await writer.WriteLineAsync($"Czas wykonania: {runResult.Duration.TotalSeconds:F2}s");
        await writer.WriteLineAsync($"Folder: {_outputDirectory}");
        await writer.WriteLineAsync();

        var passed = runResult.TestResults.Count(t => t.Passed);
        var failed = runResult.TestResults.Count(t => !t.Passed);

        await writer.WriteLineAsync("PODSUMOWANIE:");
        await writer.WriteLineAsync($"  Testów ogó³em: {runResult.TestResults.Count}");
        await writer.WriteLineAsync($"  Zaliczonych: {passed}");
        await writer.WriteLineAsync($"  Niezaliczonych: {failed}");
        await writer.WriteLineAsync();

        foreach (var result in runResult.TestResults)
        {
            await writer.WriteLineAsync("?????????????????????????????????????????????????????????????????????????????");
            await writer.WriteLineAsync($"PLIK: {result.FileName}");
            await writer.WriteLineAsync($"Status: {(result.Passed ? "PASSED" : "FAILED")}");
            await writer.WriteLineAsync($"Rekordów: {result.ActualRecords:N0}");
            await writer.WriteLineAsync($"Kolumn: {result.ActualColumns}");
            await writer.WriteLineAsync($"Rozmiar: {FormatFileSize(result.FileSizeBytes)}");
            await writer.WriteLineAsync($"Czas: {result.Duration.TotalSeconds:F2}s");
            await writer.WriteLineAsync();

            if (result.Errors.Any())
            {
                await writer.WriteLineAsync("B£ÊDY:");
                foreach (var error in result.Errors)
                {
                    await writer.WriteLineAsync($"  • {error}");
                }
                await writer.WriteLineAsync();
            }

            if (result.Warnings.Any())
            {
                await writer.WriteLineAsync("OSTRZE¯ENIA:");
                foreach (var warning in result.Warnings)
                {
                    await writer.WriteLineAsync($"  • {warning}");
                }
                await writer.WriteLineAsync();
            }
        }

        await writer.WriteLineAsync("???????????????????????????????????????????????????????????????????????????????");
    }

    private string FormatFileSize(long bytes)
    {
        var kb = bytes / 1024.0;
        var mb = kb / 1024.0;
        return mb >= 1 ? $"{mb:F2} MB" : $"{kb:F2} KB";
    }
}

/// <summary>
/// Wynik ca³ego uruchomienia testów
/// </summary>
public class TestRunResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public List<CsvTestResult> TestResults { get; } = new();
}
