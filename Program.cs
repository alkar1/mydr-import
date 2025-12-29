using System.Text;
using System.Text.RegularExpressions;
using MyDr_Import;

class Program
{
    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine();

        // Komenda explore - eksploracja duzych plikow XML
        if (args.Length > 0 && args[0].Equals("explore", StringComparison.OrdinalIgnoreCase))
        {
            XmlExplorerCli.Run(args.Skip(1).ToArray());
            return 0;
        }

        // Komenda copyhead - kopiowanie poczatku pliku XML
        if (args.Length > 0 && args[0].Equals("copyhead", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Uzycie: MyDr_Import copyhead <plik_xml> [folder_wyjsciowy]");
                return 1;
            }
            var srcPath = args[1];
            var outDir = args.Length > 2 ? args[2] : Path.GetDirectoryName(srcPath) ?? ".";
            CopyXmlHead(srcPath, outDir);
            return 0;
        }

        // Komenda validate - walidacja danych etap1 vs gabinet_head.xml
        if (args.Length > 0 && args[0].Equals("validate", StringComparison.OrdinalIgnoreCase))
        {
            var dataDir = args.Length > 1 ? args[1] : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data_etap1"));
            return ValidateEtap1Data(dataDir);
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
        CopyXmlHead(xmlFilePath, dataEtap1Path);

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

    static void CopyXmlHead(string sourceXmlPath, string outputDir)
    {
        const int headSize = 10 * 1024 * 1024; // 10 MB
        var headFilePath = Path.Combine(outputDir, "gabinet_head.xml");

        try
        {
            using var sourceStream = new FileStream(sourceXmlPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var targetStream = new FileStream(headFilePath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[headSize];
            var bytesRead = sourceStream.Read(buffer, 0, headSize);

            targetStream.Write(buffer, 0, bytesRead);

            Console.WriteLine($"Skopiowano pierwsze {bytesRead:N0} bajtow do: {headFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Blad podczas kopiowania naglowka: {ex.Message}");
        }
    }

    static int ValidateEtap1Data(string dataEtap1Path)
    {
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("WALIDACJA DANYCH ETAP1");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine();

        var headFilePath = Path.Combine(dataEtap1Path, "gabinet_head.xml");
        var jsonFilePath = Path.Combine(dataEtap1Path, "xml_structure_summary.json");

        if (!File.Exists(headFilePath))
        {
            Console.WriteLine($"Blad: Brak pliku {headFilePath}");
            return 1;
        }

        if (!File.Exists(jsonFilePath))
        {
            Console.WriteLine($"Blad: Brak pliku {jsonFilePath}");
            Console.WriteLine("Uruchom najpierw etap1: MyDr_Import --etap1");
            return 1;
        }

        // Wczytaj gabinet_head.xml jako tekst
        Console.WriteLine($"Analizuje: {headFilePath}");
        var xmlContent = File.ReadAllText(headFilePath, Encoding.UTF8);
        Console.WriteLine($"Rozmiar pliku: {xmlContent.Length:N0} znakow");
        Console.WriteLine();

        // Wyodrebnij modele i pola z XML za pomoca regex
        var modelsFromXml = new Dictionary<string, HashSet<string>>();
        var objectPattern = new Regex(@"<object\s+model=""([^""]+)""\s+pk=""([^""]+)"">", RegexOptions.Compiled);
        var fieldPattern = new Regex(@"<field\s+name=""([^""]+)""(?:\s+type=""([^""]+)"")?(?:\s+rel=""([^""]+)"")?(?:\s+to=""([^""]+)"")?", RegexOptions.Compiled);

        string? currentModel = null;
        foreach (Match objMatch in objectPattern.Matches(xmlContent))
        {
            currentModel = objMatch.Groups[1].Value;
            if (!modelsFromXml.ContainsKey(currentModel))
            {
                modelsFromXml[currentModel] = new HashSet<string>();
            }
        }

        // Znajdz pola dla kazdego modelu
        var objectBlocks = Regex.Split(xmlContent, @"(?=<object\s+model=)");
        foreach (var block in objectBlocks)
        {
            var modelMatch = objectPattern.Match(block);
            if (!modelMatch.Success) continue;

            var model = modelMatch.Groups[1].Value;
            if (!modelsFromXml.ContainsKey(model))
                modelsFromXml[model] = new HashSet<string>();

            foreach (Match fieldMatch in fieldPattern.Matches(block))
            {
                modelsFromXml[model].Add(fieldMatch.Groups[1].Value);
            }
        }

        Console.WriteLine($"Znaleziono w gabinet_head.xml:");
        Console.WriteLine($"  Modele: {modelsFromXml.Count}");
        Console.WriteLine();

        // Wczytaj xml_structure_summary.json
        Console.WriteLine($"Porownuje z: {jsonFilePath}");
        var jsonContent = File.ReadAllText(jsonFilePath, Encoding.UTF8);
        var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonContent);
        var objectsFromJson = new Dictionary<string, HashSet<string>>();

        foreach (var obj in jsonDoc.RootElement.GetProperty("objects").EnumerateArray())
        {
            var modelName = obj.GetProperty("model").GetString() ?? "";
            objectsFromJson[modelName] = new HashSet<string>();

            foreach (var field in obj.GetProperty("fields").EnumerateArray())
            {
                var fieldName = field.GetProperty("name").GetString() ?? "";
                objectsFromJson[modelName].Add(fieldName);
            }
        }

        Console.WriteLine($"Znaleziono w xml_structure_summary.json:");
        Console.WriteLine($"  Modele: {objectsFromJson.Count}");
        Console.WriteLine();

        // Porownaj
        Console.WriteLine(new string('-', 80));
        Console.WriteLine("WYNIKI POROWNANIA:");
        Console.WriteLine(new string('-', 80));

        int errors = 0;

        // Modele w XML ale nie w JSON
        var modelsOnlyInXml = modelsFromXml.Keys.Except(objectsFromJson.Keys).ToList();
        if (modelsOnlyInXml.Any())
        {
            Console.WriteLine();
            Console.WriteLine($"[!] Modele w gabinet_head.xml ale NIE w etap1 ({modelsOnlyInXml.Count}):");
            foreach (var m in modelsOnlyInXml.Take(10))
                Console.WriteLine($"    - {m}");
            if (modelsOnlyInXml.Count > 10)
                Console.WriteLine($"    ... i {modelsOnlyInXml.Count - 10} wiecej");
            errors++;
        }

        // Modele w JSON ale nie w XML (to normalne - head zawiera tylko fragment)
        var modelsOnlyInJson = objectsFromJson.Keys.Except(modelsFromXml.Keys).ToList();
        if (modelsOnlyInJson.Any())
        {
            Console.WriteLine();
            Console.WriteLine($"[i] Modele w etap1 ale NIE w gabinet_head.xml ({modelsOnlyInJson.Count}):");
            Console.WriteLine($"    (to normalne - gabinet_head.xml zawiera tylko pierwsze 10MB)");
            foreach (var m in modelsOnlyInJson.Take(5))
                Console.WriteLine($"    - {m}");
            if (modelsOnlyInJson.Count > 5)
                Console.WriteLine($"    ... i {modelsOnlyInJson.Count - 5} wiecej");
        }

        // Porownaj pola dla wspolnych modeli
        var commonModels = modelsFromXml.Keys.Intersect(objectsFromJson.Keys).ToList();
        Console.WriteLine();
        Console.WriteLine($"[OK] Wspolne modele: {commonModels.Count}");

        foreach (var model in commonModels)
        {
            var xmlFields = modelsFromXml[model];
            var jsonFields = objectsFromJson[model];

            var fieldsOnlyInXml = xmlFields.Except(jsonFields).ToList();
            if (fieldsOnlyInXml.Any())
            {
                Console.WriteLine();
                Console.WriteLine($"[!] {model}: pola w XML ale nie w etap1:");
                foreach (var f in fieldsOnlyInXml)
                    Console.WriteLine($"    - {f}");
                errors++;
            }
        }

        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        if (errors == 0)
        {
            Console.WriteLine("WALIDACJA ZAKONCZONA - BRAK BLEDOW");
        }
        else
        {
            Console.WriteLine($"WALIDACJA ZAKONCZONA - ZNALEZIONO {errors} PROBLEMOW");
        }
        Console.WriteLine(new string('=', 80));

        return errors > 0 ? 1 : 0;
    }
}
