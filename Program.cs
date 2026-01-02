using System.Text;
using MyDr_Import;
using MyDr_Import.Helpers;

class Program
{
    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine();

        // Obsluga komend
        if (args.Length > 0)
        {
            var cmd = args[0].ToLower();
            var cmdArgs = args.Skip(1).ToArray();
            
            switch (cmd)
            {
                case "explore":  XmlExplorerCli.Run(cmdArgs); return 0;
                case "copyhead": return RunCopyHead(cmdArgs);
                case "validate": return RunValidate(cmdArgs);
                case "compare":  return RunCompare();
            }
        }

        // Parsowanie argumentow
        bool startFromEtap1 = args.Contains("--etap1", StringComparer.OrdinalIgnoreCase);
        string? specificModel = GetArgValue(args, "--model");
        var pathArgs = args.Where(a => !a.StartsWith("--")).ToArray();

        string dataPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "MyDr_data"));
        string xmlFilePath;
        if (pathArgs.Length > 0)
        {
            xmlFilePath = pathArgs[0];
        }
        else
        {
            xmlFilePath = Path.Combine(dataPath, "gabinet_export.xml");
        }

        // Jesli --model bez --etap1, nie wymaga pliku XML (dane juz sa w data_etap1)
        bool requiresXmlFile = startFromEtap1 || string.IsNullOrEmpty(specificModel);
        
        if (requiresXmlFile && !File.Exists(xmlFilePath))
        {
            Console.WriteLine("Blad: Plik nie istnieje: " + xmlFilePath);
            Console.WriteLine();
            Console.WriteLine("Uzycie: MyDr_Import [--etap1] [--model=nazwa] <sciezka_do_pliku_xml>");
            Console.WriteLine("  --etap1        Wymusza rozpoczecie od etapu 1 (analiza XML)");
            Console.WriteLine("  --model=nazwa  Testuje tylko wybrany arkusz mapowania (np. --model=pacjenci)");
            Console.WriteLine("                 Bez --etap1 program zaczyna od etapu 2");
            return 1;
        }

        //folder "data_etap1" jest w katalogu z plikiem zrodlowym program.cs 
        string dataEtap1Path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data_etap1"));
        Directory.CreateDirectory(dataEtap1Path);

        // Skopiuj poczatek pliku XML do gabinet_head.xml (10 MB)
        FileHelpers.CopyXmlHead(xmlFilePath, dataEtap1Path);

        Console.WriteLine("folder \"data_etap1\" jest w katalogu dataPath: " + dataEtap1Path);
        Console.WriteLine();

        //-----------------------------------------
        //if debug - wyjscie z programu przed analiza
        bool isDebug = System.Diagnostics.Debugger.IsAttached;
        if (isDebug)
        {
            Console.WriteLine("Tryb debugowania");
        }
        //-----------------------------------------

        int result = 0;

        // ETAP 1: Analiza struktury XML (tylko jesli --etap1)
        if (startFromEtap1)
        {
            // Wyczysc folder data_full przed analiza (usuwa zablokowane pliki)
            var dataFullPath = Path.Combine(dataEtap1Path, "data_full");
            if (Directory.Exists(dataFullPath))
            {
                try
                {
                    Console.WriteLine($"Czyszczenie folderu: {dataFullPath}");
                    foreach (var file in Directory.GetFiles(dataFullPath))
                    {
                        try { File.Delete(file); }
                        catch { /* ignoruj zablokowane pliki */ }
                    }
                    Directory.Delete(dataFullPath, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ostrzezenie: {ex.Message}");
                }
            }
            
            result = Etap1.Run(xmlFilePath, dataEtap1Path);
            if (result != 0)
            {
                return result;
            }
        }
        else
        {
            Console.WriteLine("Pominieto ETAP 1 (uzyj --etap1 aby wymusic analize XML)");
            Console.WriteLine();
        }

        // ETAP 2: Przetwarzanie danych (mapowanie pol)
        result = Etap2.Run(dataEtap1Path, specificModel);

        if (result == 0)
        {
            Console.WriteLine("WSZYSTKIE ETAPY ZAKONCZONE POMYSLNIE!");
        }

        return result;
    }

    static string? GetArgValue(string[] args, string prefix)
    {
        var arg = args.FirstOrDefault(a => a.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        if (arg == null) return null;
        
        var parts = arg.Split('=', 2);
        return parts.Length > 1 ? parts[1] : null;
    }

    static int RunCopyHead(string[] args)
    {
        if (args.Length < 1) { Console.WriteLine("Uzycie: MyDr_Import copyhead <plik_xml> [folder_wyjsciowy]"); return 1; }
        var outDir = args.Length > 1 ? args[1] : Path.GetDirectoryName(args[0]) ?? ".";
        FileHelpers.CopyXmlHead(args[0], outDir);
        return 0;
    }

    static int RunValidate(string[] args)
    {
        var dataDir = args.Length > 0 ? args[0] : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data_etap1"));
        return ValidateEtap1(dataDir);
    }

    static int RunCompare() => CompareResults();

    static int ValidateEtap1(string dataEtap1Path)
    {
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("WALIDACJA DANYCH ETAP1");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine();

        var headFilePath = Path.Combine(dataEtap1Path, "gabinet_head.xml");
        var jsonFilePath = Path.Combine(dataEtap1Path, "xml_structure_summary.json");

        if (!File.Exists(headFilePath)) { Console.WriteLine($"Blad: Brak pliku {headFilePath}"); return 1; }
        if (!File.Exists(jsonFilePath)) { Console.WriteLine($"Blad: Brak pliku {jsonFilePath}\nUruchom najpierw etap1: MyDr_Import --etap1"); return 1; }

        Console.WriteLine($"Analizuje: {headFilePath}");
        var xmlContent = File.ReadAllText(headFilePath, Encoding.UTF8);
        Console.WriteLine($"Rozmiar pliku: {xmlContent.Length:N0} znakow\n");

        var modelsFromXml = new Dictionary<string, HashSet<string>>();
        var objectPattern = new System.Text.RegularExpressions.Regex(@"<object\s+model=""([^""]+)""\s+pk=""([^""]+)"">");
        var fieldPattern = new System.Text.RegularExpressions.Regex(@"<field\s+name=""([^""]+)""");

        foreach (var block in System.Text.RegularExpressions.Regex.Split(xmlContent, @"(?=<object\s+model=)"))
        {
            var modelMatch = objectPattern.Match(block);
            if (!modelMatch.Success) continue;
            var model = modelMatch.Groups[1].Value;
            if (!modelsFromXml.ContainsKey(model)) modelsFromXml[model] = new HashSet<string>();
            foreach (System.Text.RegularExpressions.Match fieldMatch in fieldPattern.Matches(block))
                modelsFromXml[model].Add(fieldMatch.Groups[1].Value);
        }

        Console.WriteLine($"Znaleziono w gabinet_head.xml:\n  Modele: {modelsFromXml.Count}\n");
        Console.WriteLine($"Porownuje z: {jsonFilePath}");

        var jsonDoc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(jsonFilePath, Encoding.UTF8));
        var objectsFromJson = new Dictionary<string, HashSet<string>>();
        foreach (var obj in jsonDoc.RootElement.GetProperty("objects").EnumerateArray())
        {
            var modelName = obj.GetProperty("model").GetString() ?? "";
            objectsFromJson[modelName] = new HashSet<string>();
            foreach (var field in obj.GetProperty("fields").EnumerateArray())
                objectsFromJson[modelName].Add(field.GetProperty("name").GetString() ?? "");
        }

        Console.WriteLine($"Znaleziono w xml_structure_summary.json:\n  Modele: {objectsFromJson.Count}\n");
        Console.WriteLine(new string('-', 80) + "\nWYNIKI POROWNANIA:\n" + new string('-', 80));

        int errors = 0;
        var modelsOnlyInXml = modelsFromXml.Keys.Except(objectsFromJson.Keys).ToList();
        if (modelsOnlyInXml.Any()) { Console.WriteLine($"\n[!] Modele w gabinet_head.xml ale NIE w etap1 ({modelsOnlyInXml.Count}):"); foreach (var m in modelsOnlyInXml.Take(10)) Console.WriteLine($"    - {m}"); errors++; }

        var modelsOnlyInJson = objectsFromJson.Keys.Except(modelsFromXml.Keys).ToList();
        if (modelsOnlyInJson.Any()) { Console.WriteLine($"\n[i] Modele w etap1 ale NIE w gabinet_head.xml ({modelsOnlyInJson.Count}):\n    (to normalne - gabinet_head.xml zawiera tylko pierwsze 10MB)"); foreach (var m in modelsOnlyInJson.Take(5)) Console.WriteLine($"    - {m}"); }

        var commonModels = modelsFromXml.Keys.Intersect(objectsFromJson.Keys).ToList();
        Console.WriteLine($"\n[OK] Wspolne modele: {commonModels.Count}");

        foreach (var model in commonModels)
        {
            var fieldsOnlyInXml = modelsFromXml[model].Except(objectsFromJson[model]).ToList();
            if (fieldsOnlyInXml.Any()) { Console.WriteLine($"\n[!] {model}: pola w XML ale nie w etap1:"); foreach (var f in fieldsOnlyInXml) Console.WriteLine($"    - {f}"); errors++; }
        }

        Console.WriteLine($"\n{new string('=', 80)}\n{(errors == 0 ? "WALIDACJA ZAKONCZONA - BRAK BLEDOW" : $"WALIDACJA ZAKONCZONA - ZNALEZIONO {errors} PROBLEMOW")}\n{new string('=', 80)}");
        return errors > 0 ? 1 : 0;
    }

    static int CompareResults()
    {
        Console.WriteLine(new string('=', 80) + "\nPOROWNANIE STARYCH I NOWYCH WYNIKOW\n" + new string('=', 80) + "\n");

        var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var oldResultPath = Path.Combine(homePath, "NC", "PROJ", "OPTIMED", "old_etap2");
        var newResultPath = Path.Combine(homePath, "NC", "PROJ", "OPTIMED", "MyDr_result");

        Console.WriteLine($"Stare wyniki: {oldResultPath}\nNowe wyniki:  {newResultPath}\n");
        if (!Directory.Exists(oldResultPath)) { Console.WriteLine($"Blad: Katalog nie istnieje: {oldResultPath}"); return 1; }
        if (!Directory.Exists(newResultPath)) { Console.WriteLine($"Blad: Katalog nie istnieje: {newResultPath}"); return 1; }

        var oldFiles = Directory.GetFiles(oldResultPath, "*.csv").Select(Path.GetFileName).ToHashSet();
        var newFiles = Directory.GetFiles(newResultPath, "*.csv").Select(Path.GetFileName).ToHashSet();
        Console.WriteLine($"Plikow CSV w old_etap2: {oldFiles.Count}\nPlikow CSV w MyDr_result: {newFiles.Count}\n");

        var onlyInOld = oldFiles.Except(newFiles).OrderBy(x => x).ToList();
        var onlyInNew = newFiles.Except(oldFiles).OrderBy(x => x).ToList();
        if (onlyInOld.Any()) { Console.WriteLine($"[!] Pliki tylko w old_etap2: {string.Join(", ", onlyInOld)}\n"); }
        if (onlyInNew.Any()) { Console.WriteLine($"[!] Pliki tylko w MyDr_result: {string.Join(", ", onlyInNew)}\n"); }

        var commonFiles = oldFiles.Intersect(newFiles).OrderBy(x => x).ToList();
        Console.WriteLine(new string('-', 80) + $"\nPOROWNANIE WSPOLNYCH PLIKOW ({commonFiles.Count}):\n" + new string('-', 80));
        Console.WriteLine($"{"Plik",-35} {"Stare",-10} {"Nowe",-10} {"Roznica",-10}");

        int differences = 0;
        foreach (var fileName in commonFiles)
        {
            var oldLines = File.ReadLines(Path.Combine(oldResultPath, fileName!), Encoding.UTF8).Count();
            var newLines = File.ReadLines(Path.Combine(newResultPath, fileName!), Encoding.UTF8).Count();
            var diff = newLines - oldLines;
            if (diff != 0) differences++;
            Console.WriteLine($"{fileName,-35} {oldLines,-10:N0} {newLines,-10:N0} {(diff > 0 ? "+" : "")}{diff,-10}");
        }

        Console.WriteLine($"\n{new string('=', 80)}\nPOROWNANIE ZAKONCZONE - {differences} plikow z roznicami\n{new string('=', 80)}");
        return 0;
    }
}