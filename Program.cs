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

        // Komenda compare - porownanie starych i nowych wynikow
        if (args.Length > 0 && args[0].Equals("compare", StringComparison.OrdinalIgnoreCase))
        {
            return OldNewResultCompare();
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

    /// <summary>
    /// Porownuje stare wyniki CSV z nowymi wynikami
    /// </summary>
    static int OldNewResultCompare()
    {
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("POROWNANIE STARYCH I NOWYCH WYNIKOW");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine();

        // Uzyj sciezek wzglednych od katalogu domowego
        var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var oldResultPath = Path.Combine(homePath, "NC", "PROJ", "OPTIMED", "old_etap2");
        var newResultPath = Path.Combine(homePath, "NC", "PROJ", "OPTIMED", "MyDr_result");

        Console.WriteLine($"Katalog domowy: {homePath}");
        Console.WriteLine($"Stare wyniki:   {oldResultPath}");
        Console.WriteLine($"Nowe wyniki:    {newResultPath}");
        Console.WriteLine();

        if (!Directory.Exists(oldResultPath))
        {
            Console.WriteLine($"Blad: Katalog nie istnieje: {oldResultPath}");
            return 1;
        }

        if (!Directory.Exists(newResultPath))
        {
            Console.WriteLine($"Blad: Katalog nie istnieje: {newResultPath}");
            return 1;
        }

        var oldFiles = Directory.GetFiles(oldResultPath, "*.csv").Select(Path.GetFileName).ToHashSet();
        var newFiles = Directory.GetFiles(newResultPath, "*.csv").Select(Path.GetFileName).ToHashSet();

        Console.WriteLine($"Plikow CSV w old_etap2:   {oldFiles.Count}");
        Console.WriteLine($"Plikow CSV w MyDr_result: {newFiles.Count}");
        Console.WriteLine();

        // Pliki tylko w starych
        var onlyInOld = oldFiles.Except(newFiles).OrderBy(x => x).ToList();
        if (onlyInOld.Any())
        {
            Console.WriteLine($"[!] Pliki tylko w old_etap2 ({onlyInOld.Count}):");
            foreach (var f in onlyInOld)
                Console.WriteLine($"    - {f}");
            Console.WriteLine();
        }

        // Pliki tylko w nowych
        var onlyInNew = newFiles.Except(oldFiles).OrderBy(x => x).ToList();
        if (onlyInNew.Any())
        {
            Console.WriteLine($"[!] Pliki tylko w MyDr_result ({onlyInNew.Count}):");
            foreach (var f in onlyInNew)
                Console.WriteLine($"    - {f}");
            Console.WriteLine();
        }

        // Porownaj wspolne pliki
        var commonFiles = oldFiles.Intersect(newFiles).OrderBy(x => x).ToList();
        Console.WriteLine(new string('-', 80));
        Console.WriteLine($"POROWNANIE WSPOLNYCH PLIKOW ({commonFiles.Count}):");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine();

        Console.WriteLine(String.Format("{0,-35} {1,15} {2,15} {3,12} {4,10}", "Plik", "Stare wiersze", "Nowe wiersze", "Roznica", "Status"));
        Console.WriteLine(new string('-', 90));

        int differences = 0;
        foreach (var fileName in commonFiles)
        {
            var oldFilePath = Path.Combine(oldResultPath, fileName!);
            var newFilePath = Path.Combine(newResultPath, fileName!);

            var oldLines = File.ReadLines(oldFilePath, Encoding.UTF8).Count();
            var newLines = File.ReadLines(newFilePath, Encoding.UTF8).Count();

            var diff = newLines - oldLines;
            var diffStr = diff > 0 ? $"+{diff}" : diff.ToString();
            var status = diff == 0 ? "OK" : (Math.Abs(diff) > oldLines * 0.1 ? "ROZNICA!" : "roznica");

            if (diff != 0)
                differences++;

            Console.WriteLine($"{fileName,-35} {oldLines,15:N0} {newLines,15:N0} {diffStr,12} {status,10}");
        }

        Console.WriteLine(new string('-', 90));
        Console.WriteLine();

        // Porownanie rozmiaru plikow
        Console.WriteLine(new string('-', 80));
        Console.WriteLine("POROWNANIE ROZMIARU PLIKOW:");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine();

        Console.WriteLine(String.Format("{0,-35} {1,15} {2,15} {3,12}", "Plik", "Stary rozmiar", "Nowy rozmiar", "Zmiana %"));
        Console.WriteLine(new string('-', 80));

        foreach (var fileName in commonFiles)
        {
            var oldFilePath = Path.Combine(oldResultPath, fileName!);
            var newFilePath = Path.Combine(newResultPath, fileName!);

            var oldSize = new FileInfo(oldFilePath).Length;
            var newSize = new FileInfo(newFilePath).Length;

            var changePercent = oldSize > 0 ? ((double)(newSize - oldSize) / oldSize * 100) : 0;
            var changeStr = changePercent >= 0 ? $"+{changePercent:F1}%" : $"{changePercent:F1}%";

            Console.WriteLine($"{fileName,-35} {FormatBytes(oldSize),15} {FormatBytes(newSize),15} {changeStr,12}");
        }

        Console.WriteLine(new string('-', 80));
        Console.WriteLine();

        // Porownanie pol (kolumn) i ich wypelnienia - zapis do pliku dla LLM
        var reportPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "compare_report.md"));
        var report = new StringBuilder();
        
        report.AppendLine("# Raport porownania starych i nowych wynikow CSV");
        report.AppendLine($"Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();
        report.AppendLine("## Podsumowanie");
        report.AppendLine($"- Plikow w old_etap2: {oldFiles.Count}");
        report.AppendLine($"- Plikow w MyDr_result: {newFiles.Count}");
        if (onlyInOld.Any()) report.AppendLine($"- Pliki tylko w starych: {string.Join(", ", onlyInOld)}");
        if (onlyInNew.Any()) report.AppendLine($"- Pliki tylko w nowych: {string.Join(", ", onlyInNew)}");
        report.AppendLine();

        Console.WriteLine(new string('-', 80));
        Console.WriteLine("POROWNANIE POL (KOLUMN) I ICH WYPELNIENIA:");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine();

        foreach (var fileName in commonFiles)
        {
            var oldFilePath = Path.Combine(oldResultPath, fileName!);
            var newFilePath = Path.Combine(newResultPath, fileName!);

            var (oldHeaders, oldFillRates) = AnalyzeCsvFields(oldFilePath);
            var (newHeaders, newFillRates) = AnalyzeCsvFields(newFilePath);

            var onlyInOldFields = oldHeaders.Except(newHeaders).ToList();
            var onlyInNewFields = newHeaders.Except(oldHeaders).ToList();
            var commonFieldsList = oldHeaders.Intersect(newHeaders).ToList();

            // Raport do pliku - zawsze dla kazdego pliku
            report.AppendLine($"## {fileName}");
            report.AppendLine($"Stare pola: {oldHeaders.Count}, Nowe pola: {newHeaders.Count}");
            report.AppendLine();

            if (onlyInOldFields.Any())
            {
                report.AppendLine("### Usuniete pola");
                foreach (var f in onlyInOldFields)
                    report.AppendLine($"- `{f}` (wypelnienie: {oldFillRates.GetValueOrDefault(f, 0):F1}%)");
                report.AppendLine();
            }

            if (onlyInNewFields.Any())
            {
                report.AppendLine("### Nowe pola");
                foreach (var f in onlyInNewFields)
                    report.AppendLine($"- `{f}` (wypelnienie: {newFillRates.GetValueOrDefault(f, 0):F1}%)");
                report.AppendLine();
            }

            // Tabela wszystkich pol z wypelnieniem
            report.AppendLine("### Wypelnienie pol");
            report.AppendLine("| Pole | Stare % | Nowe % | Status |");
            report.AppendLine("|------|---------|--------|--------|");

            foreach (var field in commonFieldsList.OrderBy(x => x))
            {
                var oldFill = oldFillRates.GetValueOrDefault(field, 0);
                var newFill = newFillRates.GetValueOrDefault(field, 0);
                
                string status;
                if (oldFill == 0 && newFill == 0) status = "PUSTE";
                else if (newFill == 0) status = "BRAK DANYCH";
                else if (oldFill == 0) status = "NOWE DANE";
                else if (Math.Abs(oldFill - newFill) > 20) status = "DUZA ZMIANA";
                else if (Math.Abs(oldFill - newFill) > 5) status = "zmiana";
                else status = "OK";

                report.AppendLine($"| `{field}` | {oldFill:F1} | {newFill:F1} | {status} |");
            }
            report.AppendLine();

            // Konsola - pokaz tylko problemy
            bool hasFieldDiff = onlyInOldFields.Any() || onlyInNewFields.Any();
            var emptyInNew = commonFieldsList.Where(f => newFillRates.GetValueOrDefault(f, 0) == 0).ToList();
            
            if (hasFieldDiff || emptyInNew.Any())
            {
                Console.WriteLine($"[{fileName}]");
                Console.WriteLine($"  Stare pola: {oldHeaders.Count}, Nowe pola: {newHeaders.Count}");

                if (onlyInOldFields.Any())
                    Console.WriteLine($"  [-] Usuniete: {string.Join(", ", onlyInOldFields)}");
                if (onlyInNewFields.Any())
                    Console.WriteLine($"  [+] Nowe: {string.Join(", ", onlyInNewFields)}");
                if (emptyInNew.Any())
                    Console.WriteLine($"  [!] PUSTE KOLUMNY: {string.Join(", ", emptyInNew.Take(5))}{(emptyInNew.Count > 5 ? $" (+{emptyInNew.Count - 5} wiecej)" : "")}");
                Console.WriteLine();
            }
        }

        // Zapisz raport
        File.WriteAllText(reportPath, report.ToString(), Encoding.UTF8);
        Console.WriteLine($"Raport zapisany do: {reportPath}");
        Console.WriteLine();

        Console.WriteLine(new string('=', 80));
        if (differences == 0 && !onlyInOld.Any() && !onlyInNew.Any())
        {
            Console.WriteLine("POROWNANIE ZAKONCZONE - BRAK ROZNIC");
        }
        else
        {
            Console.WriteLine($"POROWNANIE ZAKONCZONE - {differences} plikow z roznicami w wierszach");
            if (onlyInOld.Any()) Console.WriteLine($"                       {onlyInOld.Count} plikow tylko w starych");
            if (onlyInNew.Any()) Console.WriteLine($"                       {onlyInNew.Count} plikow tylko w nowych");
        }
        Console.WriteLine(new string('=', 80));

        return 0;
    }

    static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:F1} {sizes[order]}";
    }

    /// <summary>
    /// Analizuje pola CSV i zwraca naglowki oraz procent wypelnienia kazdego pola
    /// </summary>
    static (List<string> Headers, Dictionary<string, double> FillRates) AnalyzeCsvFields(string csvPath)
    {
        var headers = new List<string>();
        var fillCounts = new Dictionary<string, int>();
        int totalRows = 0;

        using var reader = new StreamReader(csvPath, Encoding.UTF8);
        
        // Wczytaj naglowki
        var headerLine = reader.ReadLine();
        if (string.IsNullOrEmpty(headerLine))
            return (headers, new Dictionary<string, double>());

        headers = ParseCsvLine(headerLine);
        foreach (var h in headers)
            fillCounts[h] = 0;

        // Analizuj wiersze (max 10000 dla wydajnosci)
        const int maxRows = 10000;
        string? line;
        while ((line = reader.ReadLine()) != null && totalRows < maxRows)
        {
            totalRows++;
            var values = ParseCsvLine(line);
            
            for (int i = 0; i < Math.Min(headers.Count, values.Count); i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    fillCounts[headers[i]]++;
                }
            }
        }

        // Oblicz procent wypelnienia
        var fillRates = new Dictionary<string, double>();
        foreach (var h in headers)
        {
            fillRates[h] = totalRows > 0 ? (double)fillCounts[h] / totalRows * 100 : 0;
        }

        return (headers, fillRates);
    }

    /// <summary>
    /// Parsuje linie CSV z uwzglednieniem cudzys�ow�w
    /// </summary>
    static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ';' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());

        return result;
    }
}

