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
        Console.WriteLine("?              MyDr Import - Analiza Struktury XML (Etap 1)                  ?");
        Console.WriteLine("??????????????????????????????????????????????????????????????????????????????");
        Console.WriteLine();

        // Œcie¿ka do pliku XML
        var dataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            @"NC\PROJ\OPTIMED\MyDr_data");

        var xmlFilePath = args.Length > 0 
            ? args[0] 
            : Path.Combine(dataPath, "gabinet_export.xml");

        xmlFilePath = Path.GetFullPath(xmlFilePath);

        // SprawdŸ czy plik istnieje
        if (!File.Exists(xmlFilePath))
        {
            Console.WriteLine($"? B³¹d: Nie znaleziono pliku: {xmlFilePath}");
            Console.WriteLine();
            Console.WriteLine("U¿ycie:");
            Console.WriteLine("  MyDr_Import.exe [œcie¿ka-do-pliku-xml]");
            Console.WriteLine();
            Console.WriteLine($"Lub umieœæ plik gabinet_export.xml w folderze: {dataPath}");
            return 1;
        }

        Console.WriteLine($"? Analiza pliku: {Path.GetFileName(xmlFilePath)}");

        try
        {
			// Utwórz folder wyjœciowy dla raportów w katalogu 
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
            var summaryPath = Path.Combine(outputDir, "xml_structure_summary.txt");
            await WriteSummaryReport(summaryPath, objectInfos, Path.GetFileName(xmlFilePath));
            Console.WriteLine($"? Zapisano: xml_structure_summary.txt");

            // Zapisz raport JSON dla ³atwego parsowania
            var jsonPath = Path.Combine(outputDir, "xml_structure_summary.json");
            await WriteJsonReport(jsonPath, objectInfos);
            Console.WriteLine($"? Zapisano: xml_structure_summary.json");

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
        catch (OperationCanceledException)
        {
            Console.WriteLine();
            Console.WriteLine("??  Operacja przerwana przez u¿ytkownika");
            return 2;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("? B£¥D PODCZAS ANALIZY:");
            Console.WriteLine($"   {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Szczegó³y:");
            Console.WriteLine(ex.ToString());
            return 3;
        }
    }

    static async Task WriteSummaryReport(string filePath, Dictionary<string, Models.XmlObjectInfo> objectInfos, string sourceFileName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("??????????????????????????????????????????????????????????????????????????????");
        sb.AppendLine($"?              RAPORT ANALIZY STRUKTURY - {sourceFileName,-50}?");
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
