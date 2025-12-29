using System.Text;
using System.Xml.Linq;
using MyDr_Import.Models;

namespace MyDr_Import.Services;

/// <summary>
/// Generator plikow CSV na podstawie mapowan i danych zrodlowych XML
/// </summary>
public class CsvGenerator
{
    private readonly string _xmlDataPath;
    private readonly string _outputPath;

    // Mapowanie nazw pol z arkusza Excel na nazwy pol XML
    private static readonly Dictionary<string, string> FieldNameMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // Pacjenci
        { "Imie", "first_name" },
        { "Nazwisko", "last_name" },
        { "DrugieImie", "second_name" },
        { "NazwiskoRodowe", "maiden_name" },
        { "DataUrodzenia", "birth_date" },
        { "DataZgonu", "death_date" },
        { "CzyUmarl", "dead" },
        { "Plec", "sex" },
        { "Email", "email" },
        { "Telefon", "telephone" },
        { "TelefonDodatkowy", "second_telephone" },
        { "NumerDokumentuTozsamosci", "identity_num" },
        { "MiejsceUrodzenia", "place_of_birth" },
        { "KodOddzialuNFZ", "nfz" },
        { "Uwagi", "notes" },
        { "VIP", "is_vip" },
        { "NIP", "employer_nip" },
        { "Kraj", "country" },
        { "GrupaKrwi", "blood_type" },
        { "IdImport", "pk" },
        // Adresy (z relacji)
        { "KrajZamieszkanie", "country" },
        { "KrajZameldowanie", "country" },
        // Pracownicy/Lekarze
        { "Login", "username" },
        { "Haslo", "password" },
        { "DataZatrudnienia", "hire_date" },
        { "DataZwolnienia", "termination_date" },
        { "NumerPWZ", "pwz" },
        { "Specjalizacja", "specialty" },
    };

    public CsvGenerator(string xmlDataPath, string outputPath)
    {
        _xmlDataPath = xmlDataPath;
        _outputPath = outputPath;

        if (!Directory.Exists(_outputPath))
        {
            Directory.CreateDirectory(_outputPath);
        }
    }

    /// <summary>
    /// Generuje plik CSV dla podanego mapowania
    /// </summary>
    public CsvGenerationResult Generate(ModelMapping mapping)
    {
        var result = new CsvGenerationResult
        {
            ModelName = mapping.SheetName,
            TargetTable = mapping.TargetTable
        };

        try
        {
            // 1. Znajdz plik XML z danymi zrodlowymi
            var xmlFile = FindXmlDataFile(mapping.SourceModel, mapping.SheetName);
            if (xmlFile == null)
            {
                result.Error = $"Nie znaleziono pliku XML dla modelu: {mapping.SourceModel} / {mapping.SheetName}";
                return result;
            }

            Console.WriteLine($"  Plik zrodlowy: {Path.GetFileName(xmlFile)}");

            // 2. Wczytaj dane z XML
            var records = LoadXmlRecords(xmlFile);
            result.SourceRecords = records.Count;
            Console.WriteLine($"  Rekordy zrodlowe: {records.Count}");

            if (records.Count == 0)
            {
                result.Error = "Brak rekordow w pliku zrodlowym";
                return result;
            }

            // 3. Przygotuj naglowki CSV (nazwy pol zrodlowych jako naglowki docelowe)
            var validFields = mapping.Fields
                .Where(f => !string.IsNullOrEmpty(f.SourceField))
                .ToList();

            // 4. Generuj CSV
            var csvPath = Path.Combine(_outputPath, $"{mapping.SheetName}.csv");
            using var writer = new StreamWriter(csvPath, false, new UTF8Encoding(true));

            // Naglowek - nazwy pol zrodlowych
            writer.WriteLine(string.Join(";", validFields.Select(f => EscapeCsvField(f.SourceField))));

            // Wiersze danych
            int processedCount = 0;
            foreach (var record in records)
            {
                var row = new List<string>();
                foreach (var field in validFields)
                {
                    var value = ExtractFieldValue(record, field);
                    row.Add(EscapeCsvField(value));
                }
                writer.WriteLine(string.Join(";", row));
                processedCount++;
            }

            result.OutputRecords = processedCount;
            result.OutputPath = csvPath;
            result.IsSuccess = true;

            Console.WriteLine($"  Wygenerowano: {csvPath}");
            Console.WriteLine($"  Rekordy wyjsciowe: {processedCount}");
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }

        return result;
    }

    private string? FindXmlDataFile(string sourceModel, string sheetName)
    {
        // Szukaj w folderze data_full
        var dataFullPath = Path.Combine(_xmlDataPath, "data_full");
        
        // Probuj rozne wzorce nazw plikow
        var patterns = new List<string>
        {
            $"gabinet_{sheetName}.xml",
            $"gabinet.{sheetName}.xml",
            $"{sheetName}.xml",
            $"gabinet_{sheetName.ToLower()}.xml",
            $"gabinet.{sheetName.ToLower()}.xml"
        };

        // Jesli sourceModel jest znany, dodaj wzorce na jego podstawie
        if (!string.IsNullOrEmpty(sourceModel) && sourceModel != "unknown")
        {
            var modelName = sourceModel.Replace(".", "_");
            patterns.Insert(0, $"{modelName}.xml");
            patterns.Insert(0, $"{sourceModel.Replace(".", "_")}.xml");
        }

        // Specjalne mapowanie dla znanych modeli
        var modelMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "pacjenci", "gabinet_patient.xml" },
            { "patient", "gabinet_patient.xml" },
            { "gabinet.patient", "gabinet_patient.xml" },
            { "wizyty", "gabinet_visit.xml" },
            { "visit", "gabinet_visit.xml" },
            { "lekarze", "gabinet_doctor.xml" },
            { "pracownicy", "gabinet_employee.xml" }
        };

        if (modelMappings.TryGetValue(sheetName, out var mappedFile))
        {
            patterns.Insert(0, mappedFile);
        }

        foreach (var pattern in patterns)
        {
            var fullPath = Path.Combine(dataFullPath, pattern);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        // Szukaj po czesciowym dopasowaniu
        if (Directory.Exists(dataFullPath))
        {
            var files = Directory.GetFiles(dataFullPath, "*.xml");
            var match = files.FirstOrDefault(f => 
                Path.GetFileNameWithoutExtension(f).Contains(sheetName, StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileNameWithoutExtension(f).Contains(sheetName.Replace("_", ""), StringComparison.OrdinalIgnoreCase));
            
            if (match != null)
                return match;
        }

        return null;
    }

    private List<Dictionary<string, string>> LoadXmlRecords(string xmlPath)
    {
        var records = new List<Dictionary<string, string>>();

        var doc = XDocument.Load(xmlPath);
        var root = doc.Root;

        if (root == null)
            return records;

        // Struktura Django: <django-objects><object model="..."><field name="...">value</field></object></django-objects>
        var objects = root.Elements("object");

        foreach (var obj in objects)
        {
            var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Dodaj pk jako pole
            var pk = obj.Attribute("pk")?.Value;
            if (!string.IsNullOrEmpty(pk))
            {
                record["pk"] = pk;
                record["id"] = pk;
            }

            // Pobierz wszystkie pola
            foreach (var field in obj.Elements("field"))
            {
                var name = field.Attribute("name")?.Value;
                if (!string.IsNullOrEmpty(name))
                {
                    var value = field.Value?.Trim() ?? "";
                    
                    // Sprawdz czy to relacja (rel atrybut)
                    var rel = field.Attribute("rel")?.Value;
                    if (!string.IsNullOrEmpty(rel))
                    {
                        // Dla relacji, wartosc moze byc w atrybucie to lub w tresci
                        var toAttr = field.Attribute("to")?.Value;
                        if (!string.IsNullOrEmpty(value))
                        {
                            record[name] = value;
                        }
                    }
                    else
                    {
                        record[name] = value;
                    }
                }
            }

            if (record.Count > 0)
            {
                records.Add(record);
            }
        }

        return records;
    }

    private string ExtractFieldValue(Dictionary<string, string> record, FieldMapping field)
    {
        if (string.IsNullOrEmpty(field.SourceField))
        {
            // Pole bez zrodla - moze byc stala lub puste
            return field.TransformRule ?? "";
        }

        // Probuj rozne warianty nazwy pola
        var fieldVariants = new List<string>
        {
            field.SourceField,
            field.SourceField.ToLower(),
            ToSnakeCase(field.SourceField),
            ToCamelCase(field.SourceField)
        };

        // Dodaj mapowanie ze slownika jesli istnieje
        if (FieldNameMappings.TryGetValue(field.SourceField, out var mappedName))
        {
            fieldVariants.Insert(0, mappedName);
            fieldVariants.Add(mappedName.ToLower());
        }

        foreach (var variant in fieldVariants)
        {
            if (record.TryGetValue(variant, out var value))
            {
                return ApplyTransform(value, field.TransformRule);
            }
        }

        return "";
    }

    private string ApplyTransform(string value, string? rule)
    {
        if (string.IsNullOrEmpty(rule))
            return value;

        // Proste transformacje
        return rule.ToLower() switch
        {
            "upper" => value.ToUpper(),
            "lower" => value.ToLower(),
            "trim" => value.Trim(),
            _ => value
        };
    }

    private string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (char.IsUpper(c) && i > 0)
            {
                sb.Append('_');
            }
            sb.Append(char.ToLower(c));
        }
        return sb.ToString();
    }

    private string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var parts = input.Split('_');
        if (parts.Length == 1)
            return char.ToLower(input[0]) + input.Substring(1);

        return string.Concat(parts.Select((p, i) => 
            i == 0 ? p.ToLower() : char.ToUpper(p[0]) + p.Substring(1).ToLower()));
    }

    private string EscapeCsvField(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        // Escapuj cudzyslowy i dodaj cudzyslowy jesli potrzeba
        if (value.Contains('"') || value.Contains(';') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }
}

/// <summary>
/// Wynik generowania CSV
/// </summary>
public class CsvGenerationResult
{
    public string ModelName { get; set; } = string.Empty;
    public string TargetTable { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
    public int SourceRecords { get; set; }
    public int OutputRecords { get; set; }
    public string? OutputPath { get; set; }
}
