using System.Text;

namespace MyDr_Import.Helpers;

/// <summary>
/// Pomocnicze metody do operacji na plikach
/// </summary>
public static class FileHelpers
{
    /// <summary>
    /// Kopiuje poczatek pliku XML do gabinet_head.xml (10 MB)
    /// </summary>
    public static void CopyXmlHead(string sourceXmlPath, string outputDir)
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

    /// <summary>
    /// Formatuje rozmiar bajtow do czytelnej postaci (B, KB, MB, GB)
    /// </summary>
    public static string FormatBytes(long bytes)
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
}
