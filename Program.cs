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
            Console.WriteLine();
            Console.WriteLine("Przyk³ady:");
            Console.WriteLine("  MyDr_Import.exe analyze");
            Console.WriteLine("  MyDr_Import.exe export C:\\data\\plik.xml");
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
        var summaryPath = Path.Combine(outputDir, "structure_summary.txt");
        await WriteSummaryReport(summaryPath, objectInfos);
        Console.WriteLine($"? Zapisano: structure_summary.txt");

        // Zapisz raport JSON dla ³atwego parsowania
        var jsonPath = Path.Combine(outputDir, "structure_summary.json");
        await WriteJsonReport(jsonPath, objectInfos);
        Console.WriteLine($"? Zapisano: structure_summary.json");

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

    static async Task WriteSummaryReport(string filePath, Dictionary<string, Models.XmlObjectInfo> objectInfos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("??????????????????????????????????????????????????????????????????????????????");
        sb.AppendLine("?              RAPORT ANALIZY STRUKTURY - gabinet_export_2025_12_09.xml      ?");
        sb.AppendLine("??????????????????????????????????????????????????????????????????????????????");
        sb.AppendLine();
        sb.AppendLine($"Data wygenerowania: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine(new string('=', 80));
        sb.AppendLine("PODSUMOWANIE");
        sb.AppendLine(new string('=', 80));
        sb.AppendLine();
        sb.AppendLine($"Ca³kowita liczba rekordów: {objectInfos.Values.Sum(o => o.RecordCount):N0}");
        sb.AppendLine($"Liczba typów obiektów: {objectInfos.Count}");
        sb.AppendLine();

        foreach (var objectInfo in objectInfos.Values.OrderByDescending(o => o.RecordCount))
        {
            sb.AppendLine(new string('-', 80));
            sb.AppendLine($"Typ: {objectInfo.ModelName}");
            sb.AppendLine($"Rekordów: {objectInfo.RecordCount:N0}");
            sb.AppendLine($"Zakres PK: {objectInfo.MinPrimaryKey:N0} - {objectInfo.MaxPrimaryKey:N0}");
            sb.AppendLine($"Liczba pól: {objectInfo.Fields.Count}");
            sb.AppendLine();
            sb.AppendLine("Pola:");
            foreach (var field in objectInfo.Fields.Values.OrderBy(f => f.Name))
            {
                sb.AppendLine($"  - {field.Name}");
                sb.AppendLine($"    Typ: {field.Type ?? "N/A"}");
                if (!string.IsNullOrEmpty(field.Relation))
                    sb.AppendLine($"    Relacja: {field.Relation} -> {field.RelationTo}");
                sb.AppendLine($"    Wystêpuje: {field.OccurrenceCount:N0} razy");
                if (field.NullCount > 0)
                    sb.AppendLine($"    Wartoœci NULL: {field.NullCount:N0}");
                if (field.MaxLength > 0)
                    sb.AppendLine($"    Maks. d³ugoœæ: {field.MaxLength:N0}");
                sb.AppendLine();
            }
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }

    static async Task WriteJsonReport(string filePath, Dictionary<string, Models.XmlObjectInfo> objectInfos)
    {
        var json = new StringBuilder();
        json.AppendLine("{");
        json.AppendLine($"  \"generatedAt\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",");
        json.AppendLine($"  \"totalRecords\": {objectInfos.Values.Sum(o => o.RecordCount)},");
        json.AppendLine($"  \"totalTypes\": {objectInfos.Count},");
        json.AppendLine("  \"objects\": [");

        var isFirst = true;
        foreach (var objectInfo in objectInfos.Values.OrderByDescending(o => o.RecordCount))
        {
            if (!isFirst) json.AppendLine(",");
            isFirst = false;

            json.AppendLine("    {");
            json.AppendLine($"      \"modelName\": \"{objectInfo.ModelName}\",");
            json.AppendLine($"      \"recordCount\": {objectInfo.RecordCount},");
            json.AppendLine($"      \"minPrimaryKey\": {objectInfo.MinPrimaryKey},");
            json.AppendLine($"      \"maxPrimaryKey\": {objectInfo.MaxPrimaryKey},");
            json.AppendLine($"      \"fieldCount\": {objectInfo.Fields.Count},");
            json.AppendLine("      \"fields\": [");

            var isFirstField = true;
            foreach (var field in objectInfo.Fields.Values.OrderBy(f => f.Name))
            {
                if (!isFirstField) json.AppendLine(",");
                isFirstField = false;

                json.AppendLine("        {");
                json.AppendLine($"          \"name\": \"{field.Name}\",");
                json.AppendLine($"          \"type\": \"{field.Type ?? "N/A"}\",");
                json.AppendLine($"          \"relation\": \"{field.Relation ?? ""}\",");
                json.AppendLine($"          \"relationTo\": \"{field.RelationTo ?? ""}\",");
                json.AppendLine($"          \"occurrenceCount\": {field.OccurrenceCount},");
                json.AppendLine($"          \"nullCount\": {field.NullCount},");
                json.AppendLine($"          \"maxLength\": {field.MaxLength}");
                json.Append("        }");
            }

            json.AppendLine();
            json.AppendLine("      ]");
            json.Append("    }");
        }

        json.AppendLine();
        json.AppendLine("  ]");
        json.AppendLine("}");

        await File.WriteAllTextAsync(filePath, json.ToString(), Encoding.UTF8);
    }
}
