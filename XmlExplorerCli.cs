using MyDr_Import.Services;

namespace MyDr_Import;

/// <summary>
/// CLI do eksploracji du¿ych plików XML
/// U¿ycie: dotnet run -- explore [plik.xml] [komenda] [opcje]
/// </summary>
public static class XmlExplorerCli
{
    public static void Run(string[] args)
    {
        if (args.Length < 1)
        {
            PrintHelp();
            return;
        }

        var xmlPath = args[0];
        if (!File.Exists(xmlPath))
        {
            Console.WriteLine($"B³¹d: Plik nie istnieje: {xmlPath}");
            return;
        }

        var command = args.Length > 1 ? args[1].ToLower() : "help";
        var explorer = new LargeXmlExplorer(xmlPath);

        try
        {
            switch (command)
            {
                case "head":
                    RunHead(explorer, args);
                    break;
                case "tail":
                    RunTail(explorer, args);
                    break;
                case "sample":
                    RunSample(explorer, args);
                    break;
                case "search":
                    RunSearch(explorer, args);
                    break;
                case "get":
                    RunGet(explorer, args);
                    break;
                case "models":
                    RunModels(explorer, args);
                    break;
                case "schema":
                    RunSchema(explorer, args);
                    break;
                case "stats":
                    RunStats(explorer);
                    break;
                case "report":
                    RunReport(explorer, args);
                    break;
                default:
                    PrintHelp();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"B³¹d: {ex.Message}");
        }
    }

    private static void RunHead(LargeXmlExplorer explorer, string[] args)
    {
        int count = GetIntArg(args, 2, 10);
        string? model = GetStringArg(args, 3);
        
        var records = explorer.Head(count, model);
        PrintRecords(records, explorer);
    }

    private static void RunTail(LargeXmlExplorer explorer, string[] args)
    {
        int count = GetIntArg(args, 2, 10);
        string? model = GetStringArg(args, 3);
        
        Console.WriteLine("Pobieranie ostatnich rekordów (wymaga przejœcia przez ca³y plik)...");
        var records = explorer.Tail(count, model);
        PrintRecords(records, explorer);
    }

    private static void RunSample(LargeXmlExplorer explorer, string[] args)
    {
        int maxRecords = GetIntArg(args, 2, 10);
        int skipEvery = GetIntArg(args, 3, 100);
        string? model = GetStringArg(args, 4);
        
        var records = explorer.Sample(maxRecords, skipEvery, model);
        PrintRecords(records, explorer);
    }

    private static void RunSearch(LargeXmlExplorer explorer, string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("U¿ycie: search [pole] [wartoœæ] [maxWyników] [model]");
            return;
        }
        
        string field = args[2];
        string value = args[3];
        int maxResults = GetIntArg(args, 4, 10);
        string? model = GetStringArg(args, 5);
        
        Console.WriteLine($"Wyszukiwanie: {field} zawiera '{value}'...");
        var records = explorer.Search(field, value, maxResults, model);
        PrintRecords(records, explorer);
    }

    private static void RunGet(LargeXmlExplorer explorer, string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("U¿ycie: get [pk] [model]");
            return;
        }
        
        string pk = args[2];
        string? model = GetStringArg(args, 3);
        
        Console.WriteLine($"Szukanie rekordu pk={pk}...");
        var record = explorer.GetByPk(pk, model);
        
        if (record != null)
        {
            PrintRecords(new List<XmlRecord> { record }, explorer);
        }
        else
        {
            Console.WriteLine("Rekord nie znaleziony.");
        }
    }

    private static void RunModels(LargeXmlExplorer explorer, string[] args)
    {
        int sampleSize = GetIntArg(args, 2, 10000);
        
        Console.WriteLine($"Skanowanie modeli (próbka: {sampleSize} rekordów)...");
        var models = explorer.GetModels(sampleSize);
        
        Console.WriteLine($"\nZnalezione modele ({models.Count}):");
        foreach (var model in models)
        {
            Console.WriteLine($"  - {model}");
        }
    }

    private static void RunSchema(LargeXmlExplorer explorer, string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("U¿ycie: schema [model] [wielkoœæPróbki]");
            return;
        }
        
        string model = args[2];
        int sampleSize = GetIntArg(args, 3, 100);
        
        Console.WriteLine($"Analiza schematu modelu: {model}...");
        var schema = explorer.GetSchema(model, sampleSize);
        
        Console.WriteLine($"\nSchemat {model} (próbka: {schema.Values.FirstOrDefault()?.SampleCount ?? 0} rekordów):");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine($"{"Pole",-30} {"Wype³nienie",-15} {"Przyk³adowe wartoœci"}");
        Console.WriteLine(new string('-', 80));
        
        foreach (var field in schema.Values.OrderBy(f => f.Name))
        {
            var samples = string.Join(", ", field.SampleValues.Take(3).Select(v => 
                v.Length > 20 ? v.Substring(0, 20) + "..." : v));
            Console.WriteLine($"{field.Name,-30} {field.FillRate,10:F1}%    {samples}");
        }
    }

    private static void RunStats(LargeXmlExplorer explorer)
    {
        Console.WriteLine("Zbieranie statystyk (wymaga przejœcia przez ca³y plik)...");
        var stats = explorer.GetStats();
        
        Console.WriteLine($"\n=== Statystyki pliku ===");
        Console.WriteLine($"Plik: {stats.FilePath}");
        Console.WriteLine($"Rozmiar: {stats.FileSizeMB} ({stats.FileSizeGB})");
        Console.WriteLine($"£¹cznie rekordów: {stats.TotalRecords:N0}");
        Console.WriteLine($"Liczba modeli: {stats.ModelStats.Count}");
        Console.WriteLine();
        
        Console.WriteLine("Modele:");
        Console.WriteLine(new string('-', 60));
        Console.WriteLine($"{"Model",-40} {"Rekordów",-15}");
        Console.WriteLine(new string('-', 60));
        
        foreach (var model in stats.ModelStats)
        {
            Console.WriteLine($"{model.ModelName,-40} {model.RecordCount,15:N0}");
        }
    }

    private static void RunReport(LargeXmlExplorer explorer, string[] args)
    {
        int headCount = GetIntArg(args, 2, 3);
        string? model = GetStringArg(args, 3);
        
        var report = explorer.GenerateReport(headCount, model);
        Console.WriteLine(report);
    }

    private static void PrintRecords(List<XmlRecord> records, LargeXmlExplorer explorer)
    {
        if (records.Count == 0)
        {
            Console.WriteLine("Brak rekordów.");
            return;
        }
        
        Console.WriteLine($"\nZnaleziono {records.Count} rekordów:\n");
        
        foreach (var record in records)
        {
            Console.WriteLine($"=== {record.Model} [pk={record.Pk}] ===");
            foreach (var (name, value) in record.Fields.OrderBy(f => f.Key))
            {
                var displayValue = value.Length > 100 ? value.Substring(0, 100) + "..." : value;
                Console.WriteLine($"  {name}: {displayValue}");
            }
            Console.WriteLine();
        }
        
        // Opcjonalnie zapisz do JSON
        Console.WriteLine("---");
        Console.WriteLine("U¿yj --json aby wyeksportowaæ do JSON");
    }

    private static int GetIntArg(string[] args, int index, int defaultValue)
    {
        if (args.Length > index && int.TryParse(args[index], out int value))
            return value;
        return defaultValue;
    }

    private static string? GetStringArg(string[] args, int index)
    {
        return args.Length > index ? args[index] : null;
    }

    private static void PrintHelp()
    {
        Console.WriteLine(@"
=== XML Explorer CLI - Narzêdzie do eksploracji du¿ych plików XML ===

U¿ycie: dotnet run -- explore [plik.xml] [komenda] [opcje]

Komendy:
  head [n] [model]           - Pierwsze N rekordów (domyœlnie 10)
  tail [n] [model]           - Ostatnie N rekordów
  sample [n] [skip] [model]  - Co [skip]-ty rekord, max [n] wyników
  search [pole] [wartoœæ] [n] [model] - Szukaj rekordów
  get [pk] [model]           - Pobierz rekord po kluczu g³ównym
  models [próbka]            - Lista modeli w pliku
  schema [model] [próbka]    - Schemat pól dla modelu
  stats                      - Pe³ne statystyki pliku
  report [n] [model]         - Raport tekstowy dla LLM

Przyk³ady:
  dotnet run -- explore data.xml head 5
  dotnet run -- explore data.xml head 10 patients.patient
  dotnet run -- explore data.xml search name ""Jan"" 20
  dotnet run -- explore data.xml schema patients.patient 100
  dotnet run -- explore data.xml models
  dotnet run -- explore data.xml report 3 patients.patient
");
    }
}
