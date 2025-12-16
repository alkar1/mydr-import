using System.Diagnostics;
using System.Xml;
using MyDr_Import.Models;

namespace MyDr_Import.Services;

/// <summary>
/// Analizator struktury XML wykorzystuj¹cy strumieniowe przetwarzanie
/// Nie ³aduje ca³ego pliku do pamiêci - idealny dla du¿ych plików (10GB+)
/// </summary>
public class XmlStructureAnalyzer
{
    private readonly string _filePath;
    private readonly IProgress<AnalysisProgress>? _progress;

    public XmlStructureAnalyzer(string filePath, IProgress<AnalysisProgress>? progress = null)
    {
        _filePath = filePath;
        _progress = progress;
    }

    /// <summary>
    /// Analizuje plik XML i zwraca statystyki dla ka¿dego typu obiektu
    /// </summary>
    public async Task<Dictionary<string, XmlObjectInfo>> AnalyzeAsync(CancellationToken cancellationToken = default)
    {
        var objectInfos = new Dictionary<string, XmlObjectInfo>();
        var stopwatch = Stopwatch.StartNew();
        long totalObjects = 0;
        long fileSize = new FileInfo(_filePath).Length;
        long bytesRead = 0;

        Console.WriteLine($"?? Plik: {_filePath}");
        Console.WriteLine($"?? Rozmiar: {fileSize / (1024.0 * 1024.0 * 1024.0):F2} GB");
        Console.WriteLine($"??  Rozpoczêcie analizy: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();

        var settings = new XmlReaderSettings
        {
            Async = true,
            IgnoreWhitespace = true,
            IgnoreComments = true,
            DtdProcessing = DtdProcessing.Ignore
        };

        await using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan);
        using var reader = XmlReader.Create(fileStream, settings);

        string? currentModel = null;
        string? currentPrimaryKey = null;
        var currentFields = new Dictionary<string, (string? value, string? type, string? rel, string? relTo)>();

        while (await reader.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reader.NodeType == XmlNodeType.Element)
            {
                if (reader.Name == "object" && reader.HasAttributes)
                {
                    // Zapisz poprzedni obiekt jeœli istnieje
                    if (currentModel != null && currentPrimaryKey != null)
                    {
                        SaveObject(objectInfos, currentModel, currentPrimaryKey, currentFields);
                        currentFields.Clear();
                    }

                    // Rozpocznij nowy obiekt
                    currentModel = reader.GetAttribute("model");
                    currentPrimaryKey = reader.GetAttribute("pk");
                    totalObjects++;

                    // Raportuj postêp co 10000 obiektów
                    if (totalObjects % 10000 == 0)
                    {
                        bytesRead = fileStream.Position;
                        ReportProgress(totalObjects, bytesRead, fileSize, stopwatch.Elapsed, objectInfos);
                    }
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
                        // Odczytaj wartoœæ pola
                        var fieldValue = await reader.ReadInnerXmlAsync();
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

        // Podsumowanie koñcowe
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("? ANALIZA ZAKOÑCZONA");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"??  Czas wykonania: {stopwatch.Elapsed:hh\\:mm\\:ss}");
        Console.WriteLine($"?? Ca³kowita liczba obiektów: {totalObjects:N0}");
        Console.WriteLine($"???  Liczba typów obiektów: {objectInfos.Count}");
        Console.WriteLine($"? Prêdkoœæ: {totalObjects / stopwatch.Elapsed.TotalSeconds:F0} obiektów/s");
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

    private void ReportProgress(long totalObjects, long bytesRead, long fileSize, TimeSpan elapsed, Dictionary<string, XmlObjectInfo> objectInfos)
    {
        var percentComplete = (double)bytesRead / fileSize * 100;
        var objectsPerSecond = totalObjects / elapsed.TotalSeconds;
        var estimatedTotalTime = TimeSpan.FromSeconds(fileSize / (double)bytesRead * elapsed.TotalSeconds);
        var eta = estimatedTotalTime - elapsed;

        Console.Write($"\r? Postêp: {percentComplete:F1}% | Obiektów: {totalObjects:N0} | Prêdkoœæ: {objectsPerSecond:F0} obj/s | ETA: {eta:hh\\:mm\\:ss}    ");

        _progress?.Report(new AnalysisProgress
        {
            TotalObjects = totalObjects,
            BytesRead = bytesRead,
            FileSize = fileSize,
            PercentComplete = percentComplete,
            ObjectsPerSecond = objectsPerSecond,
            EstimatedTimeRemaining = eta,
            ObjectInfos = objectInfos
        });
    }
}

/// <summary>
/// Postêp analizy XML
/// </summary>
public class AnalysisProgress
{
    public long TotalObjects { get; set; }
    public long BytesRead { get; set; }
    public long FileSize { get; set; }
    public double PercentComplete { get; set; }
    public double ObjectsPerSecond { get; set; }
    public TimeSpan EstimatedTimeRemaining { get; set; }
    public Dictionary<string, XmlObjectInfo> ObjectInfos { get; set; } = new();
}
