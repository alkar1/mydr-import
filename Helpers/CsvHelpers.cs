using System.Text;

namespace MyDr_Import.Helpers;

/// <summary>
/// Pomocnicze metody do operacji na plikach CSV
/// </summary>
public static class CsvHelpers
{
    /// <summary>
    /// Analizuje pola CSV i zwraca naglowki oraz procent wypelnienia kazdego pola
    /// </summary>
    public static (List<string> Headers, Dictionary<string, double> FillRates) AnalyzeCsvFields(string csvPath)
    {
        var headers = new List<string>();
        var fillCounts = new Dictionary<string, int>();
        int totalRows = 0;

        using var reader = new StreamReader(csvPath, Encoding.UTF8);
        
        // Wczytaj naglowki
        var headerLine = reader.ReadLine();
        if (string.IsNullOrEmpty(headerLine))
            return (headers, new Dictionary<string, double>());

        headers = ParseCsvLine(headerLine);
        foreach (var h in headers)
            fillCounts[h] = 0;

        // Analizuj wiersze (max 10000 dla wydajnosci)
        const int maxRows = 10000;
        string? line;
        while ((line = reader.ReadLine()) != null && totalRows < maxRows)
        {
            totalRows++;
            var values = ParseCsvLine(line);
            
            for (int i = 0; i < Math.Min(headers.Count, values.Count); i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    fillCounts[headers[i]]++;
                }
            }
        }

        // Oblicz procent wypelnienia
        var fillRates = new Dictionary<string, double>();
        foreach (var h in headers)
        {
            fillRates[h] = totalRows > 0 ? (double)fillCounts[h] / totalRows * 100 : 0;
        }

        return (headers, fillRates);
    }

    /// <summary>
    /// Parsuje linie CSV z uwzglednieniem cudzys³owów
    /// </summary>
    public static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ';' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());

        return result;
    }
}
