using System.Data;
using System.Text;
using ExcelDataReader;
using MyDr_Import.Models;

namespace MyDr_Import.Services;

/// <summary>
/// Czytnik arkuszy Excel z definicjami mapowan pol
/// </summary>
public class ExcelMappingReader
{
    static ExcelMappingReader()
    {
        // Wymagane dla ExcelDataReader - rejestracja kodowania
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Wczytuje wszystkie arkusze mapowan z podanego folderu
    /// </summary>
    public List<ModelMapping> LoadAllMappings(string folderPath)
    {
        var mappings = new List<ModelMapping>();
        
        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine($"BLAD: Folder nie istnieje: {folderPath}");
            return mappings;
        }

        var excelFiles = Directory.GetFiles(folderPath, "*.xls")
            .Concat(Directory.GetFiles(folderPath, "*.xlsx"))
            .ToList();

        Console.WriteLine($"Znaleziono {excelFiles.Count} arkuszy mapowan w: {folderPath}");
        Console.WriteLine();

        foreach (var file in excelFiles)
        {
            try
            {
                var mapping = LoadMapping(file);
                if (mapping != null)
                {
                    mappings.Add(mapping);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [BLAD] {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        return mappings;
    }

    /// <summary>
    /// Wczytuje mapowanie z pojedynczego pliku Excel
    /// </summary>
    public ModelMapping? LoadMapping(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var sheetName = Path.GetFileNameWithoutExtension(filePath);

        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        
        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration
            {
                UseHeaderRow = true
            }
        });

        if (dataSet.Tables.Count == 0)
        {
            Console.WriteLine($"  [PUSTE] {fileName}");
            return null;
        }

        var table = dataSet.Tables[0];
        var mapping = new ModelMapping
        {
            SheetName = sheetName,
            SourceModel = ExtractSourceModel(table),
            TargetTable = ExtractTargetTable(table, sheetName),
            Fields = ExtractFieldMappings(table)
        };

        Console.WriteLine($"  [OK] {fileName}: {mapping.Fields.Count} pol");
        return mapping;
    }

    /// <summary>
    /// Wczytuje mapowanie dla konkretnego modelu (po nazwie arkusza)
    /// </summary>
    public ModelMapping? LoadMappingByName(string folderPath, string sheetName)
    {
        var patterns = new[] { $"{sheetName}.xls", $"{sheetName}.xlsx" };
        
        foreach (var pattern in patterns)
        {
            var filePath = Path.Combine(folderPath, pattern);
            if (File.Exists(filePath))
            {
                return LoadMapping(filePath);
            }
        }

        Console.WriteLine($"Nie znaleziono arkusza: {sheetName}");
        return null;
    }

    private string ExtractSourceModel(DataTable table)
    {
        // Probuj znalezc kolumne z nazwa modelu zrodlowego
        var possibleColumns = new[] { "source_model", "model_zrodlowy", "zrodlo", "source", "model" };
        
        foreach (var colName in possibleColumns)
        {
            if (table.Columns.Contains(colName) && table.Rows.Count > 0)
            {
                var value = table.Rows[0][colName]?.ToString();
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
        }

        return "unknown";
    }

    private string ExtractTargetTable(DataTable table, string defaultName)
    {
        // Probuj znalezc kolumne z nazwa tabeli docelowej
        var possibleColumns = new[] { "target_table", "tabela_docelowa", "cel", "target", "table" };
        
        foreach (var colName in possibleColumns)
        {
            if (table.Columns.Contains(colName) && table.Rows.Count > 0)
            {
                var value = table.Rows[0][colName]?.ToString();
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
        }

        return defaultName;
    }

    private List<FieldMapping> ExtractFieldMappings(DataTable table)
    {
        var mappings = new List<FieldMapping>();

        // Znajdz kolumny z polami zrodlowymi i docelowymi
        var sourceColIndex = FindColumnIndex(table, "source_field", "pole_zrodlowe", "zrodlo", "source", "pole_xml");
        var targetColIndex = FindColumnIndex(table, "target_field", "pole_docelowe", "cel", "target", "pole_db");
        var typeColIndex = FindColumnIndex(table, "type", "typ", "target_type", "typ_docelowy");
        var ruleColIndex = FindColumnIndex(table, "rule", "regula", "transform", "transformacja");
        var descColIndex = FindColumnIndex(table, "description", "opis", "komentarz", "uwagi");

        if (sourceColIndex < 0 || targetColIndex < 0)
        {
            // Jesli nie znaleziono nazwanych kolumn, uzyj pierwszych dwoch
            sourceColIndex = 0;
            targetColIndex = table.Columns.Count > 1 ? 1 : 0;
        }

        foreach (DataRow row in table.Rows)
        {
            var sourceField = row[sourceColIndex]?.ToString()?.Trim();
            var targetField = row[targetColIndex]?.ToString()?.Trim();

            if (string.IsNullOrEmpty(sourceField) && string.IsNullOrEmpty(targetField))
                continue;

            var mapping = new FieldMapping
            {
                SourceField = sourceField ?? "",
                TargetField = targetField ?? "",
                TargetType = typeColIndex >= 0 ? row[typeColIndex]?.ToString() : null,
                TransformRule = ruleColIndex >= 0 ? row[ruleColIndex]?.ToString() : null,
                Description = descColIndex >= 0 ? row[descColIndex]?.ToString() : null
            };

            mappings.Add(mapping);
        }

        return mappings;
    }

    private int FindColumnIndex(DataTable table, params string[] possibleNames)
    {
        foreach (var name in possibleNames)
        {
            for (int i = 0; i < table.Columns.Count; i++)
            {
                if (table.Columns[i].ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Wyswietla strukture arkusza (do debugowania)
    /// </summary>
    public void PrintSheetStructure(string filePath)
    {
        Console.WriteLine($"\nStruktura arkusza: {Path.GetFileName(filePath)}");
        Console.WriteLine(new string('-', 60));

        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        
        var dataSet = reader.AsDataSet();

        foreach (DataTable table in dataSet.Tables)
        {
            Console.WriteLine($"Zakladka: {table.TableName}");
            Console.WriteLine($"Kolumny ({table.Columns.Count}):");
            
            for (int i = 0; i < table.Columns.Count; i++)
            {
                Console.WriteLine($"  [{i}] {table.Columns[i].ColumnName}");
            }

            Console.WriteLine($"Wiersze: {table.Rows.Count}");
            
            if (table.Rows.Count > 0)
            {
                Console.WriteLine("Pierwszy wiersz:");
                var firstRow = table.Rows[0];
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    var value = firstRow[i]?.ToString() ?? "(null)";
                    if (value.Length > 50) value = value.Substring(0, 47) + "...";
                    Console.WriteLine($"  [{i}] = {value}");
                }
            }
        }
    }
}
