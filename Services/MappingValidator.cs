using System.Text.Json;
using MyDr_Import.Models;

namespace MyDr_Import.Services;

/// <summary>
/// Walidator mapowan - sprawdza czy pola zrodlowe istnieja w strukturze XML
/// </summary>
public class MappingValidator
{
    private readonly Dictionary<string, HashSet<string>> _sourceFields = new();

    /// <summary>
    /// Laduje strukture pol zrodlowych z raportu JSON etapu 1
    /// </summary>
    public bool LoadSourceStructure(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            Console.WriteLine($"BLAD: Brak pliku struktury: {jsonPath}");
            return false;
        }

        try
        {
            var json = File.ReadAllText(jsonPath);
            using var doc = JsonDocument.Parse(json);
            
            var objects = doc.RootElement.GetProperty("objects");
            
            foreach (var obj in objects.EnumerateArray())
            {
                var modelName = obj.GetProperty("model").GetString() ?? "";
                var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var field in obj.GetProperty("fields").EnumerateArray())
                {
                    var fieldName = field.GetProperty("name").GetString();
                    if (!string.IsNullOrEmpty(fieldName))
                    {
                        fields.Add(fieldName);
                    }
                }

                _sourceFields[modelName] = fields;
            }

            Console.WriteLine($"Zaladowano strukture: {_sourceFields.Count} modeli");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"BLAD: Nie mozna wczytac struktury: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Waliduje mapowanie sprawdzajac czy pola zrodlowe istnieja
    /// </summary>
    public MappingValidationResult Validate(ModelMapping mapping)
    {
        var result = new MappingValidationResult
        {
            ModelName = mapping.SheetName,
            IsValid = true
        };

        // Znajdz pasujacy model zrodlowy
        var matchingModels = _sourceFields.Keys
            .Where(k => k.Contains(mapping.SourceModel, StringComparison.OrdinalIgnoreCase) ||
                       mapping.SourceModel.Contains(k.Split('.').Last(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!matchingModels.Any() && mapping.SourceModel != "unknown")
        {
            result.Warnings.Add($"Nie znaleziono modelu zrodlowego: {mapping.SourceModel}");
        }

        HashSet<string>? sourceFieldSet = null;
        if (matchingModels.Count == 1)
        {
            sourceFieldSet = _sourceFields[matchingModels[0]];
        }
        else if (matchingModels.Count > 1)
        {
            // Polacz wszystkie pasujace modele
            sourceFieldSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var model in matchingModels)
            {
                foreach (var field in _sourceFields[model])
                {
                    sourceFieldSet.Add(field);
                }
            }
        }

        foreach (var field in mapping.Fields)
        {
            ValidateField(field, sourceFieldSet, result);
        }

        result.IsValid = !result.Errors.Any();
        return result;
    }

    private void ValidateField(FieldMapping field, HashSet<string>? sourceFields, MappingValidationResult result)
    {
        // Puste mapowanie
        if (string.IsNullOrEmpty(field.SourceField) && string.IsNullOrEmpty(field.TargetField))
        {
            field.IsValid = false;
            field.ValidationError = "Puste mapowanie";
            return;
        }

        // Tylko pole docelowe (stala wartosc lub generowane)
        if (string.IsNullOrEmpty(field.SourceField))
        {
            field.IsValid = true; // Moze byc stala lub generowane
            return;
        }

        // Sprawdz czy pole zrodlowe istnieje
        if (sourceFields != null && !sourceFields.Contains(field.SourceField))
        {
            // Sprawdz czy to nie jest wyrazenie (np. "field1 + field2")
            if (!field.SourceField.Contains('+') && 
                !field.SourceField.Contains('(') &&
                !field.SourceField.StartsWith("$"))
            {
                field.IsValid = false;
                field.ValidationError = $"Pole zrodlowe nie istnieje: {field.SourceField}";
                result.Errors.Add(field.ValidationError);
            }
        }
    }

    /// <summary>
    /// Wyswietla dostepne modele zrodlowe
    /// </summary>
    public void PrintAvailableModels()
    {
        Console.WriteLine($"\nDostepne modele zrodlowe ({_sourceFields.Count}):");
        foreach (var model in _sourceFields.Keys.OrderBy(k => k))
        {
            Console.WriteLine($"  {model}: {_sourceFields[model].Count} pol");
        }
    }

    /// <summary>
    /// Sprawdza czy pole istnieje w modelu
    /// </summary>
    public bool FieldExists(string modelName, string fieldName)
    {
        if (_sourceFields.TryGetValue(modelName, out var fields))
        {
            return fields.Contains(fieldName);
        }
        return false;
    }

    /// <summary>
    /// Zwraca liste pol dla modelu
    /// </summary>
    public IEnumerable<string> GetFieldsForModel(string modelName)
    {
        if (_sourceFields.TryGetValue(modelName, out var fields))
        {
            return fields.OrderBy(f => f);
        }
        return Enumerable.Empty<string>();
    }
}
