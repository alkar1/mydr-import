using System.Text;
using MyDr_Import.Models;
using MyDr_Import.Services;

namespace MyDr_Import;

/// <summary>
/// ETAP 1: Analiza struktury pliku XML
/// - Parsowanie strumieniowe duzych plikow XML
/// - Generowanie raportow (TXT, JSON)
/// - Zapis przykladowych rekordow do XML (data_heads)
/// - Zapis pelnych danych kazdego modelu do osobnych plikow XML (data_full)
/// </summary>
public static class Etap1
{
    public static int Run(string xmlFilePath, string outputDir)
    {
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("ETAP 1: ANALIZA STRUKTURY XML");
        Console.WriteLine(new string('=', 80));

        try
        {
            var analyzer = new XmlStructureAnalyzer(xmlFilePath);
            
            // Folder na pelne dane kazdego modelu
            var dataFullPath = Path.Combine(outputDir, "data_full");
            var objectInfos = analyzer.Analyze(dataFullPath);

            Console.WriteLine();
            foreach (var objectInfo in objectInfos.Values.OrderByDescending(o => o.RecordCount))
            {
                objectInfo.PrintSummary();
            }

            Console.WriteLine();
            Console.WriteLine(new string('=', 80));
            Console.WriteLine("ZAPISYWANIE RAPORTOW");
            Console.WriteLine(new string('=', 80));

            var summaryPath = Path.Combine(outputDir, "xml_structure_summary.txt");
            WriteSummaryReport(summaryPath, objectInfos, Path.GetFileName(xmlFilePath));
            Console.WriteLine("Zapisano: xml_structure_summary.txt");

            var jsonPath = Path.Combine(outputDir, "xml_structure_summary.json");
            WriteJsonReport(jsonPath, objectInfos);
            Console.WriteLine("Zapisano: xml_structure_summary.json");

            // Zapisz przykladowe rekordy (pierwsze 3 + ostatni) do plikow XML
            var dataHeadsPath = Path.Combine(outputDir, "data_heads");
            analyzer.SaveSampleRecordsToXml(dataHeadsPath, objectInfos);

            Console.WriteLine();
            Console.WriteLine(new string('=', 80));
            Console.WriteLine("ETAP 1 ZAKONCZONY POMYSLNIE!");
            Console.WriteLine(new string('=', 80));
            Console.WriteLine();

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("Blad podczas analizy: " + ex.Message);
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    private static void WriteSummaryReport(string filePath, Dictionary<string, XmlObjectInfo> objectInfos, string sourceFileName)
    {
        var sb = new StringBuilder();
        var displayFileName = sourceFileName.Length > 36 ? sourceFileName.Substring(0, 33) + "..." : sourceFileName;
        sb.AppendLine(new string('=', 80));
        sb.AppendLine("  RAPORT STRUKTURY XML: " + displayFileName);
        sb.AppendLine("  Data: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine(new string('=', 80));
        sb.AppendLine();

        sb.AppendLine("Calkowita liczba typow obiektow: " + objectInfos.Count);
        sb.AppendLine("Calkowita liczba rekordow: " + objectInfos.Values.Sum(o => o.RecordCount).ToString("N0"));
        sb.AppendLine();

        sb.AppendLine(new string('-', 80));
        sb.AppendLine("SZCZEGOLY OBIEKTOW (posortowane wg liczby rekordow):");
        sb.AppendLine(new string('-', 80));

        foreach (var info in objectInfos.Values.OrderByDescending(o => o.RecordCount))
        {
            sb.AppendLine();
            sb.AppendLine("Model: " + info.ModelName + "  (" + info.PolishModelName + " - " + info.PolishDescription + ")");
            sb.AppendLine("  Liczba rekordow: " + info.RecordCount.ToString("N0"));
            sb.AppendLine("  Liczba pol: " + info.Fields.Count);
            sb.AppendLine("  Pola:");

            foreach (var field in info.Fields.OrderBy(f => f.Key))
            {
                var fieldInfo = field.Value;
                var relInfo = !string.IsNullOrEmpty(fieldInfo.Relation)
                    ? " -> " + fieldInfo.RelationTo
                    : "";
                sb.AppendLine("    - " + field.Key + " (" + fieldInfo.Type + ")" + relInfo);
                sb.AppendLine("      Wypelnienie: " + info.RecordCount.ToString("N0") + " (" + (100.0 * fieldInfo.OccurrenceCount / info.RecordCount).ToString("F1") + "%)");
                if (fieldInfo.SampleValues.Any())
                {
                    var samples = string.Join(", ", fieldInfo.SampleValues.Take(3).Select(v =>
                        v.Length > 30 ? v.Substring(0, 27) + "..." : v));
                    sb.AppendLine("      Przyklady: " + samples);
                }
            }
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    private static void WriteJsonReport(string filePath, Dictionary<string, XmlObjectInfo> objectInfos)
    {
        var options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var report = new
        {
            generatedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
            totalObjectTypes = objectInfos.Count,
            totalRecords = objectInfos.Values.Sum(o => o.RecordCount),
            objects = objectInfos.Values.OrderByDescending(o => o.RecordCount).Select(info => new
            {
                model = info.ModelName,
                polishName = info.PolishModelName,
                description = info.PolishDescription,
                recordCount = info.RecordCount,
                fields = info.Fields.OrderBy(f => f.Key).Select(f => new
                {
                    name = f.Key,
                    type = f.Value.Type,
                    relation = f.Value.Relation,
                    relatedTo = f.Value.RelationTo,
                    filledCount = f.Value.OccurrenceCount,
                    fillPercentage = (100.0 * f.Value.OccurrenceCount / info.RecordCount)
                })
            })
        };

        var json = System.Text.Json.JsonSerializer.Serialize(report, options);
        File.WriteAllText(filePath, json, Encoding.UTF8);
    }
}
