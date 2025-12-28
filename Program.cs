using System.Text;
using MyDr_Import.Models;
using MyDr_Import.Services;

class Program
{
    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine();

        string xmlFilePath;
        //folder "data_etap1" jest w katalogu z plikiem �r�d�owym program.cs 
        string dataPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data_etap1"));
        Console.WriteLine("folder \"data_etap1\" jest w katalogu dataPath: " + dataPath);
        Console.WriteLine();
        

        if (args.Length > 0)
        {
            xmlFilePath = args[0];
        }
        else
        {
            xmlFilePath = Path.Combine(dataPath, "gabinet.xml");
        }

        if (!File.Exists(xmlFilePath))
        {
            Console.WriteLine("Blad: Plik nie istnieje: " + xmlFilePath);
            Console.WriteLine();
            Console.WriteLine("Uzycie: MyDr_Import <sciezka_do_pliku_xml>");
            return 1;
        }

        CopyXmlHead(xmlFilePath, dataPath);
        Console.WriteLine("Nacisnij dowolny klawisz, aby kontynuowac...");
        Console.ReadKey(true);
        Console.WriteLine();

        Console.WriteLine(new string('=', 80));
        Console.WriteLine("ETAP 1: ANALIZA STRUKTURY XML");
        Console.WriteLine(new string('=', 80));

        try
        {
            var outputDir = dataPath;
            Directory.CreateDirectory(outputDir);

            var analyzer = new XmlStructureAnalyzer(xmlFilePath);
            var objectInfos = analyzer.Analyze();

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

            Console.WriteLine();
            Console.WriteLine(new string('=', 80));
            Console.WriteLine("ANALIZA ZAKONCZONA POMYSLNIE!");
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

    static void CopyXmlHead(string sourceXmlPath, string outputDir)
    {
        const int headSize = 10 * 1024;
        Directory.CreateDirectory(outputDir);
        var headFilePath = Path.Combine(outputDir, "gabinet_head.xml");

        try
        {
            using var sourceStream = new FileStream(sourceXmlPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var targetStream = new FileStream(headFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            
            var buffer = new byte[headSize];
            var bytesRead = sourceStream.Read(buffer, 0, headSize);
            
            targetStream.Write(buffer, 0, bytesRead);
            
            Console.WriteLine("Skopiowano pierwsze " + bytesRead.ToString("N0") + " bajtow do: " + headFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Blad podczas kopiowania naglowka: " + ex.Message);
        }
    }
    static void WriteSummaryReport(string filePath, Dictionary<string, XmlObjectInfo> objectInfos, string sourceFileName)
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
            sb.AppendLine("Model: " + info.ModelName);
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
                sb.AppendLine("      Wypelnienie: " + fieldInfo.OccurrenceCount.ToString("N0") + "/" + info.RecordCount.ToString("N0") + " (" + (100.0 * fieldInfo.OccurrenceCount / info.RecordCount).ToString("F1") + "%)");
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

    static void WriteJsonReport(string filePath, Dictionary<string, XmlObjectInfo> objectInfos)
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






