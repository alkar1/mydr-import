using System.Diagnostics;

namespace MyDr_Import.Services;

/// <summary>
/// Klasa do raportowania postêpu operacji z wizualizacj¹ w konsoli
/// </summary>
public class ProgressReporter
{
    private readonly Stopwatch _stopwatch = new();
    private long _lastReportedItems = 0;
    private DateTime _lastReportTime = DateTime.Now;

    public void Start()
    {
        _stopwatch.Start();
        _lastReportTime = DateTime.Now;
    }

    public void Stop()
    {
        _stopwatch.Stop();
    }

    public void Report(long currentItems, long totalItems, string itemName = "items")
    {
        var elapsed = _stopwatch.Elapsed;
        var itemsPerSecond = currentItems / elapsed.TotalSeconds;
        var percentComplete = (double)currentItems / totalItems * 100;
        
        var estimatedTotalTime = TimeSpan.FromSeconds(totalItems / itemsPerSecond);
        var eta = estimatedTotalTime - elapsed;

        // Oblicz aktualn¹ prêdkoœæ (od ostatniego raportu)
        var timeSinceLastReport = (DateTime.Now - _lastReportTime).TotalSeconds;
        var itemsSinceLastReport = currentItems - _lastReportedItems;
        var currentSpeed = timeSinceLastReport > 0 ? itemsSinceLastReport / timeSinceLastReport : 0;

        Console.Write($"\r? {percentComplete:F1}% | {currentItems:N0}/{totalItems:N0} {itemName} | ");
        Console.Write($"? {itemsPerSecond:F0}/s (teraz: {currentSpeed:F0}/s) | ");
        Console.Write($"?? ETA: {FormatTimeSpan(eta)}     ");

        _lastReportedItems = currentItems;
        _lastReportTime = DateTime.Now;
    }

    public void ReportWithMemory(long currentItems, long totalItems, string itemName = "items")
    {
        Report(currentItems, totalItems, itemName);
        
        var memoryMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
        Console.Write($"| ?? {memoryMB:F0} MB     ");
    }

    public void Complete(long totalItems, string itemName = "items")
    {
        Stop();
        Console.WriteLine();
        Console.WriteLine($"? Zakoñczono: {totalItems:N0} {itemName}");
        Console.WriteLine($"??  Ca³kowity czas: {FormatTimeSpan(_stopwatch.Elapsed)}");
        Console.WriteLine($"? Œrednia prêdkoœæ: {totalItems / _stopwatch.Elapsed.TotalSeconds:F0} {itemName}/s");
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
        if (ts.TotalMinutes >= 1)
            return $"{ts.Minutes}m {ts.Seconds}s";
        return $"{ts.Seconds}s";
    }

    public static void PrintHeader(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"  {title}");
        Console.WriteLine(new string('=', 80));
    }

    public static void PrintSection(string sectionName)
    {
        Console.WriteLine();
        Console.WriteLine($"--- {sectionName} ---");
    }

    public static void PrintSuccess(string message)
    {
        Console.WriteLine($"? {message}");
    }

    public static void PrintError(string message)
    {
        Console.WriteLine($"? {message}");
    }

    public static void PrintWarning(string message)
    {
        Console.WriteLine($"??  {message}");
    }

    public static void PrintInfo(string message)
    {
        Console.WriteLine($"??  {message}");
    }
}
