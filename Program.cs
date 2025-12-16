using MyDr_Import.Services;
using System.Text;

namespace MyDr_Import;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine();
        Console.WriteLine("??????????????????????????????????????????????????????????????????????????????");
        Console.WriteLine("?              MyDr Import - Narzêdzie Importu Danych Medycznych             ?");
        Console.WriteLine("?                      MyDrEDM (XML) ? Optimed (CSV)                         ?");
        Console.WriteLine("??????????????????????????????????????????????????????????????????????????????");
        Console.WriteLine();

        // Parsuj argumenty
        var mode = args.Length > 0 ? args[0].ToLower() : "analyze";
        var xmlFilePath = args.Length > 1 
            ? args[1] 
            : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "data", "gabinet_export_2025_12_09.xml");

        xmlFilePath = Path.GetFullPath(xmlFilePath);

        // Tryb verify nie wymaga pliku XML
        if (mode == "verify")
        {
            return await RunVerifyAsync();
        }

        // Tryb diagnose - diagnostyka struktury XML
        if (mode == "diagnose")
        {
            var objectType = args.Length > 2 ? args[2] : "gabinet.patient";
            var diagFilePath = args.Length > 1 && args[1] != objectType 
                ? args[1] 
                : xmlFilePath;
            return await RunDiagnoseAsync(diagFilePath, objectType);
        }

        // SprawdŸ czy plik istnieje
        if (!File.Exists(xmlFilePath))
        {
            Console.WriteLine($"? B³¹d: Nie znaleziono pliku: {xmlFilePath}");
            Console.WriteLine();
            Console.WriteLine("U¿ycie:");
            Console.WriteLine("  MyDr_Import.exe [mode] [œcie¿ka-do-pliku-xml]");
            Console.WriteLine();
            Console.WriteLine("Tryby (mode):");
            Console.WriteLine("  analyze  - Analiza struktury XML (Etap 1)");
            Console.WriteLine("  export   - Eksport do CSV (Etap 2)");
            Console.WriteLine("  verify   - Weryfikacja eksportowanych plików CSV");
            Console.WriteLine("  diagnose - Diagnostyka struktury obiektów XML");
            Console.WriteLine();
            Console.WriteLine("Przyk³ady:");
            Console.WriteLine("  MyDr_Import.exe analyze");
            Console.WriteLine("  MyDr_Import.exe export C:\\data\\plik.xml");
            Console.WriteLine("  MyDr_Import.exe verify");
            Console.WriteLine("  MyDr_Import.exe diagnose gabinet.patient");
            return 1;
        }

        try
        {
            if (mode == "export")
            {
                return await RunExportAsync(xmlFilePath);
            }
            else
            {
                return await RunAnalyzeAsync(xmlFilePath);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine();
            Console.WriteLine("??  Operacja przerwana przez u¿ytkownika");
            return 2;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("? B£¥D:");
            Console.WriteLine($"   {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Szczegó³y:");
            Console.WriteLine(ex.ToString());
            return 3;
        }
    }

    static async Task<int> RunAnalyzeAsync(string xmlFilePath)
    {
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
        Console.WriteLine("TRYB: ANALIZA STRUKTURY (Etap 1)");
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
        Console.WriteLine();

        // Utwórz folder wyjœciowy dla raportów
        var outputDir = Path.Combine(AppContext.BaseDirectory, "output");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine($"?? Folder raportów: {outputDir}");
        Console.WriteLine();

        // Utwórz analizator
        var analyzer = new XmlStructureAnalyzer(xmlFilePath);

        // Wykonaj analizê
        var objectInfos = await analyzer.AnalyzeAsync();

        // Wyœwietl szczegó³owe wyniki
        Console.WriteLine();
        foreach (var objectInfo in objectInfos.Values.OrderByDescending(o => o.RecordCount))
        {
            objectInfo.PrintSummary();
        }

        // Zapisz raporty do plików CSV
        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("?? ZAPISYWANIE RAPORTÓW");
        Console.WriteLine(new string('=', 80));

        foreach (var objectInfo in objectInfos.Values)
        {
            var reportFileName = $"structure_{objectInfo.ModelName.Replace(".", "_")}.csv";
            var reportFilePath = Path.Combine(outputDir, reportFileName);
            
            await File.WriteAllTextAsync(reportFilePath, objectInfo.ToCSVSummary(), Encoding.UTF8);
            Console.WriteLine($"? Zapisano: {reportFileName}");
        }

        // Zapisz zbiorczy raport
        // var summaryPath = Path.Combine(outputDir, "structure_summary.txt");
        // await WriteSummaryReport(summaryPath, objectInfos);
        // Console.WriteLine($"? Zapisano: structure_summary.txt");

        // Zapisz raport JSON dla ³atwego parsowania
        // var jsonPath = Path.Combine(outputDir, "structure_summary.json");
        // await WriteJsonReport(jsonPath, objectInfos);
        // Console.WriteLine($"? Zapisano: structure_summary.json");

        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("?? ANALIZA ZAKOÑCZONA POMYŒLNIE!");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine();
        Console.WriteLine($"?? Przeanalizowano {objectInfos.Values.Sum(o => o.RecordCount):N0} rekordów");
        Console.WriteLine($"???  Znaleziono {objectInfos.Count} typów obiektów");
        Console.WriteLine($"?? Raporty zapisane w: {outputDir}");
        Console.WriteLine();

        return 0;
    }

    static async Task<int> RunExportAsync(string xmlFilePath)
    {
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
        Console.WriteLine("TRYB: EKSPORT DO CSV (Etap 2)");
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
        Console.WriteLine();

        // Utwórz folder wyjœciowy
        var outputDir = Path.Combine(AppContext.BaseDirectory, "output_csv");
        Directory.CreateDirectory(outputDir);

        // Utwórz serwis eksportu
        var exporter = new CsvExportService(
            xmlFilePath: xmlFilePath,
            outputDirectory: outputDir,
            batchSize: 1000,
            instalacjaId: 1
        );

        // Wykonaj eksport
        var result = await exporter.ExportAllAsync();

        if (result.Success)
        {
            return 0;
        }
        else
        {
            Console.WriteLine($"? Eksport zakoñczony b³êdem: {result.ErrorMessage}");
            return 3;
        }
    }

    static async Task<int> RunVerifyAsync()
    {
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
        Console.WriteLine("TRYB: WERYFIKACJA PLIKÓW CSV");
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
        Console.WriteLine();

        var outputDir = Path.Combine(AppContext.BaseDirectory, "output_csv");
        var verifier = new Tests.CsvExportVerificationTests(outputDir);

        var result = await verifier.RunAllVerificationsAsync();

        // Generuj raport
        var reportPath = Path.Combine(AppContext.BaseDirectory, "verification_report.csv");
        await verifier.GenerateReportAsync(result, reportPath);

        return result.Success ? 0 : 1;
    }

    static async Task<int> RunDiagnoseAsync(string xmlFilePath, string objectType)
    {
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
        Console.WriteLine("TRYB: DIAGNOSTYKA STRUKTURY XML");
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????");
        Console.WriteLine();
        Console.WriteLine($"?? Plik: {xmlFilePath}");
        Console.WriteLine($"?? Typ obiektu: {objectType}");
        Console.WriteLine();

        var tool = new Tools.XmlDiagnosticTool(xmlFilePath);

        // Poka¿ 3 przyk³adowe obiekty
        await tool.ShowObjectStructureAsync(objectType, 3);

        Console.WriteLine();
        Console.WriteLine("???????????????????????????????????????????????????????????????????????????");

        // Statystyki wype³nienia pól
        var stats = await tool.GetModelStatisticsAsync(objectType, maxSample: 100);
        tool.PrintModelStatistics(stats);

        return 0;
    }
}
