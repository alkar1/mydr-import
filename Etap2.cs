using MyDr_Import.Models;
using MyDr_Import.Processors;
using MyDr_Import.Services;

namespace MyDr_Import;

/// <summary>
/// ETAP 2: Przygotowanie do migracji
/// - Wczytywanie arkuszy mapowan z Excela
/// - Walidacja mapowan wzgledem struktury XML
/// - Mozliwosc testowania pojedynczych modeli (--model)
/// </summary>
public static class Etap2
{
    // Sciezka do arkuszy mapowan
    private static readonly string MappingsFolderPath = @"C:\Users\alfred\NC\PROJ\OPTIMED\baza_plikimigracyjne";

    public static int Run(string dataEtap1Path, string? specificModel = null, bool generateCsv = false)
    {
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("ETAP 2: PRZYGOTOWANIE DO MIGRACJI");
        Console.WriteLine(new string('=', 80));

        try
        {
            // 1. Sprawdz czy istnieja wyniki etapu 1
            var summaryJsonPath = Path.Combine(dataEtap1Path, "xml_structure_summary.json");
            if (!File.Exists(summaryJsonPath))
            {
                Console.WriteLine();
                Console.WriteLine("BLAD: Brak wynikow etapu 1!");
                Console.WriteLine("Uruchom program z parametrem --etap1 aby wykonac analize XML.");
                Console.WriteLine($"Oczekiwany plik: {summaryJsonPath}");
                return 1;
            }

            Console.WriteLine($"Wyniki etapu 1: {summaryJsonPath}");
            Console.WriteLine($"Folder mapowan: {MappingsFolderPath}");
            Console.WriteLine();

            // 2. Zaladuj walidator ze struktura zrodlowa
            var validator = new MappingValidator();
            if (!validator.LoadSourceStructure(summaryJsonPath))
            {
                return 1;
            }

            // 3. Wczytaj mapowania z arkuszy Excel
            var reader = new ExcelMappingReader();
            List<ModelMapping> mappings;

            if (!string.IsNullOrEmpty(specificModel))
            {
                // Tryb pojedynczego modelu (--model)
                Console.WriteLine($"\n>>> TRYB TESTOWY: {specificModel} <<<\n");
                var mapping = reader.LoadMappingByName(MappingsFolderPath, specificModel);
                mappings = mapping != null ? new List<ModelMapping> { mapping } : new List<ModelMapping>();

                if (mapping != null)
                {
                    // Wyswietl szczegolowa strukture arkusza
                    var filePath = FindMappingFile(specificModel);
                    if (filePath != null)
                    {
                        reader.PrintSheetStructure(filePath);
                    }
                }
            }
            else
            {
                // Tryb wszystkich modeli
                mappings = reader.LoadAllMappings(MappingsFolderPath);
            }

            if (!mappings.Any())
            {
                Console.WriteLine("Nie znaleziono zadnych mapowan!");
                return 1;
            }

            // 4. Waliduj mapowania
            Console.WriteLine();
            Console.WriteLine(new string('=', 80));
            Console.WriteLine("WALIDACJA MAPOWAN");
            Console.WriteLine(new string('=', 80));

            var results = new List<MappingValidationResult>();
            foreach (var mapping in mappings)
            {
                var result = validator.Validate(mapping);
                results.Add(result);
                
                mapping.PrintSummary();
                
                if (result.Errors.Any())
                {
                    Console.WriteLine("  BLEDY:");
                    foreach (var error in result.Errors.Take(5))
                    {
                        Console.WriteLine($"    - {error}");
                    }
                    if (result.Errors.Count > 5)
                    {
                        Console.WriteLine($"    ... i {result.Errors.Count - 5} wiecej bledow");
                    }
                }
                
                if (result.Warnings.Any())
                {
                    Console.WriteLine("  OSTRZEZENIA:");
                    foreach (var warning in result.Warnings)
                    {
                        Console.WriteLine($"    - {warning}");
                    }
                }
            }

            // 5. Podsumowanie
            Console.WriteLine();
            Console.WriteLine(new string('=', 80));
            Console.WriteLine("PODSUMOWANIE ETAPU 2");
            Console.WriteLine(new string('=', 80));

            var validCount = results.Count(r => r.IsValid);
            var invalidCount = results.Count(r => !r.IsValid);
            var totalFields = mappings.Sum(m => m.Fields.Count);
            var validFields = mappings.Sum(m => m.ValidFieldsCount);

            Console.WriteLine($"Arkuszy: {mappings.Count}");
            Console.WriteLine($"  - Poprawnych: {validCount}");
            Console.WriteLine($"  - Z bledami: {invalidCount}");
            Console.WriteLine($"Pol: {totalFields}");
            Console.WriteLine($"  - Poprawnych: {validFields}");
            Console.WriteLine($"  - Z bledami: {totalFields - validFields}");

            // 6. Zapisz raport mapowan
            var mappingsOutputPath = Path.Combine(dataEtap1Path, "mappings_report.json");
            SaveMappingsReport(mappingsOutputPath, mappings, results);
            Console.WriteLine($"\nZapisano raport mapowan: {mappingsOutputPath}");

            // 7. Generuj CSV jesli wymagane (lub dla --model)
            if (generateCsv || !string.IsNullOrEmpty(specificModel))
            {
                var dataEtap2Path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "MyDr_result"));
                GenerateCsvFiles(dataEtap1Path, dataEtap2Path, mappings);
            }

            Console.WriteLine();
            Console.WriteLine(new string('=', 80));
            Console.WriteLine("ETAP 2 ZAKONCZONY!");
            Console.WriteLine(new string('=', 80));
            Console.WriteLine();

            return invalidCount > 0 ? 0 : 0; // Zawsze sukces - bledy to ostrzezenia
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("Blad podczas przetwarzania: " + ex.Message);
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    private static string? FindMappingFile(string sheetName)
    {
        var patterns = new[] { $"{sheetName}.xls", $"{sheetName}.xlsx" };
        foreach (var pattern in patterns)
        {
            var path = Path.Combine(MappingsFolderPath, pattern);
            if (File.Exists(path)) return path;
        }
        return null;
    }

    private static void GenerateCsvFiles(string dataEtap1Path, string dataEtap2Path, List<ModelMapping> mappings)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("GENEROWANIE PLIKOW CSV");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"Folder wyjsciowy: {dataEtap2Path}");
        Console.WriteLine($"Zarejestrowane procesory: {string.Join(", ", ProcessorRegistry.GetRegisteredModels())}");
        Console.WriteLine();

        var results = new List<CsvGenerationResult>();

        foreach (var mapping in mappings)
        {
            // Uzyj ProcessorRegistry - automatycznie wybiera dedykowany procesor lub generyczny
            var result = ProcessorRegistry.ProcessModel(dataEtap1Path, dataEtap2Path, mapping);
            results.Add(result);

            if (!result.IsSuccess)
            {
                Console.WriteLine($"  [BLAD] {result.Error}");
            }
        }

        // Podsumowanie
        Console.WriteLine();
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("PODSUMOWANIE GENEROWANIA CSV:");
        var successCount = results.Count(r => r.IsSuccess);
        var totalRecords = results.Sum(r => r.OutputRecords);
        Console.WriteLine($"  Plikow wygenerowanych: {successCount}/{results.Count}");
        Console.WriteLine($"  Laczna liczba rekordow: {totalRecords}");

        if (results.Any(r => !r.IsSuccess))
        {
            Console.WriteLine("  Bledy:");
            foreach (var r in results.Where(r => !r.IsSuccess))
            {
                Console.WriteLine($"    - {r.ModelName}: {r.Error}");
            }
        }
    }

    private static void SaveMappingsReport(string path, List<ModelMapping> mappings, List<MappingValidationResult> results)
    {
        var report = new
        {
            generatedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
            totalSheets = mappings.Count,
            validSheets = results.Count(r => r.IsValid),
            mappings = mappings.Select(m => new
            {
                sheetName = m.SheetName,
                sourceModel = m.SourceModel,
                targetTable = m.TargetTable,
                fieldsCount = m.Fields.Count,
                validFieldsCount = m.ValidFieldsCount,
                isValid = m.IsValid,
                fields = m.Fields.Select(f => new
                {
                    source = f.SourceField,
                    target = f.TargetField,
                    type = f.TargetType,
                    rule = f.TransformRule,
                    isValid = f.IsValid,
                    error = f.ValidationError
                })
            })
        };

        var options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var json = System.Text.Json.JsonSerializer.Serialize(report, options);
        File.WriteAllText(path, json, System.Text.Encoding.UTF8);
    }
}
