using MyDr_Import.Models;
using MyDr_Import.Services;

namespace MyDr_Import.Processors;

/// <summary>
/// Rejestr procesorow modeli
/// Automatycznie wykrywa i rejestruje wszystkie procesory
/// </summary>
public static class ProcessorRegistry
{
    private static readonly Dictionary<string, IModelProcessor> _processors = new(StringComparer.OrdinalIgnoreCase);

    static ProcessorRegistry()
    {
        // Rejestruj wszystkie znane procesory
        Register(new PacjenciProcessor());
        Register(new JednostkiProcessor());
        Register(new StaleChorobyProcessor());
        Register(new StaleLekiProcessor());
        // Dodaj kolejne procesory tutaj:
        // Register(new WizytyProcessor());
        // Register(new LekarzeProcessor());
    }

    public static void Register(IModelProcessor processor)
    {
        _processors[processor.ModelName] = processor;
    }

    public static IModelProcessor? GetProcessor(string modelName)
    {
        return _processors.TryGetValue(modelName, out var processor) ? processor : null;
    }

    public static bool HasProcessor(string modelName)
    {
        return _processors.ContainsKey(modelName);
    }

    public static IEnumerable<string> GetRegisteredModels()
    {
        return _processors.Keys.OrderBy(k => k);
    }

    /// <summary>
    /// Przetwarza model uzywajac dedykowanego procesora lub generycznego CsvGenerator
    /// </summary>
    public static CsvGenerationResult ProcessModel(
        string dataEtap1Path, 
        string dataEtap2Path, 
        ModelMapping mapping)
    {
        Console.WriteLine($"\nPrzetwarzanie: {mapping.SheetName}");

        // Sprawdz czy istnieje dedykowany procesor
        var processor = GetProcessor(mapping.SheetName);
        
        if (processor != null)
        {
            Console.WriteLine($"  Uzycie procesora: {processor.GetType().Name}");
            return processor.Process(dataEtap1Path, dataEtap2Path, mapping);
        }
        else
        {
            // Fallback do generycznego generatora
            Console.WriteLine($"  Uzycie generycznego CsvGenerator");
            var generator = new CsvGenerator(dataEtap1Path, dataEtap2Path);
            return generator.Generate(mapping);
        }
    }
}
