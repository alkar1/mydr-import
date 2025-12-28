using System.Diagnostics;
using System.Xml;
using MyDr_Import.Models;

namespace MyDr_Import.Services;

/// <summary>
/// Analizator struktury XML wykorzystujacy strumieniowe przetwarzanie
/// Nie laduje calego pliku do pamieci - idealny dla duzych plikow (10GB+)
/// </summary>
public class XmlStructureAnalyzer
{
    private readonly string _filePath;

    public XmlStructureAnalyzer(string filePath)
    {
        _filePath = filePath;
    }

    /// <summary>
    /// Analizuje plik XML i zwraca statystyki dla kazdego typu obiektu
    /// </summary>
    public Dictionary<string, XmlObjectInfo> Analyze()
    {
        var objectInfos = new Dictionary<string, XmlObjectInfo>();
        var stopwatch = Stopwatch.StartNew();
        long totalObjects = 0;
        long fileSize = new FileInfo(_filePath).Length;

        Console.WriteLine($"Plik: {_filePath}");
        Console.WriteLine($"Rozmiar: {fileSize / (1024.0 * 1024.0 * 1024.0):F2} GB");
        Console.WriteLine($"Rozpoczecie analizy: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();

        var settings = new XmlReaderSettings
        {
            IgnoreWhitespace = true,
            IgnoreComments = true,
            DtdProcessing = DtdProcessing.Ignore
        };

        using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan);
        using var reader = XmlReader.Create(fileStream, settings);

        string? currentModel = null;
        string? currentPrimaryKey = null;
        var currentFields = new Dictionary<string, (string? value, string? type, string? rel, string? relTo)>();

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                if (reader.Name == "object" && reader.HasAttributes)
                {
                    // Zapisz poprzedni obiekt jesli istnieje
                    if (currentModel != null && currentPrimaryKey != null)
                    {
                        SaveObject(objectInfos, currentModel, currentPrimaryKey, currentFields);
                        currentFields.Clear();
                    }

                    // Rozpocznij nowy obiekt
                    currentModel = reader.GetAttribute("model");
                    currentPrimaryKey = reader.GetAttribute("pk");
                    totalObjects++;
                }
                else if (reader.Name == "field" && reader.HasAttributes && currentModel != null)
                {
                    // Parsuj pole
                    var fieldName = reader.GetAttribute("name");
                    var fieldType = reader.GetAttribute("type");
                    var fieldRel = reader.GetAttribute("rel");
                    var fieldRelTo = reader.GetAttribute("to");

                    if (!string.IsNullOrEmpty(fieldName))
                    {
                        // Odczytaj wartosc pola
                        var fieldValue = reader.ReadInnerXml();
                        currentFields[fieldName] = (fieldValue, fieldType, fieldRel, fieldRelTo);
                    }
                }
            }
        }

        // Zapisz ostatni obiekt
        if (currentModel != null && currentPrimaryKey != null)
        {
            SaveObject(objectInfos, currentModel, currentPrimaryKey, currentFields);
        }

        stopwatch.Stop();

        // Podsumowanie koncowe
        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("ANALIZA ZAKONCZONA");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"Czas wykonania: {stopwatch.Elapsed:hh\\:mm\\:ss}");
        Console.WriteLine($"Calkowita liczba obiektow: {totalObjects:N0}");
        Console.WriteLine($"Liczba typow obiektow: {objectInfos.Count}");
        Console.WriteLine($"Predkosc: {totalObjects / stopwatch.Elapsed.TotalSeconds:F0} obiektow/s");
        Console.WriteLine();

        return objectInfos;
    }

    private void SaveObject(Dictionary<string, XmlObjectInfo> objectInfos, string modelName, string primaryKey, Dictionary<string, (string? value, string? type, string? rel, string? relTo)> fields)
    {
        if (!objectInfos.ContainsKey(modelName))
        {
            objectInfos[modelName] = new XmlObjectInfo { ModelName = modelName };
        }

        objectInfos[modelName].AddRecord(primaryKey, fields);
    }
}
