using MyDr_Import.Models;
using MyDr_Import.Services;

namespace MyDr_Import.Processors;

/// <summary>
/// Interfejs dla procesorow modeli - kazdy model ma osobny procesor
/// </summary>
public interface IModelProcessor
{
    /// <summary>
    /// Nazwa modelu (np. "pacjenci")
    /// </summary>
    string ModelName { get; }

    /// <summary>
    /// Nazwa pliku XML zrodlowego (np. "gabinet_patient.xml")
    /// </summary>
    string XmlFileName { get; }

    /// <summary>
    /// Przetwarza dane i generuje plik CSV
    /// </summary>
    CsvGenerationResult Process(string dataEtap1Path, string dataEtap2Path, ModelMapping mapping);
}
