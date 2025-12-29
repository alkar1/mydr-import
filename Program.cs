using System.Text;
using MyDr_Import;

class Program
{
    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine();

        // Parsowanie argumentow
        bool startFromEtap1 = args.Contains("--etap1", StringComparer.OrdinalIgnoreCase);
        var pathArgs = args.Where(a => !a.StartsWith("--")).ToArray();

        string dataPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "MyDr_data"));
        string xmlFilePath;
        if (pathArgs.Length > 0)
        {
            xmlFilePath = pathArgs[0];
        }
        else
        {
            xmlFilePath = Path.Combine(dataPath, "gabinet_export.xml");
        }
        if (!File.Exists(xmlFilePath))
        {
            Console.WriteLine("Blad: Plik nie istnieje: " + xmlFilePath);
            Console.WriteLine();
            Console.WriteLine("Uzycie: MyDr_Import [--etap1] <sciezka_do_pliku_xml>");
            Console.WriteLine("  --etap1  Wymusza rozpoczecie od etapu 1 (analiza XML)");
            Console.WriteLine("           Bez tej flagi program zaczyna od etapu 2");
            Console.WriteLine("Nacisnij Enter, aby zakonczyc...");
            Console.ReadLine();

            return 1;
        }

        //folder "data_etap1" jest w katalogu z plikiem zrodlowym program.cs 
        string dataEtap1Path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data_etap1"));
        Directory.CreateDirectory(dataEtap1Path);

        Console.WriteLine("folder \"data_etap1\" jest w katalogu dataPath: " + dataEtap1Path);
        Console.WriteLine();

        //-----------------------------------------
        //if debug - wyjscie z programu przed analiza
        bool isDebug = System.Diagnostics.Debugger.IsAttached;
        if (isDebug)
        {
            Console.WriteLine("Tryb debugowania");
        }
        //-----------------------------------------

        int result = 0;

        // ETAP 1: Analiza struktury XML (tylko jesli --etap1)
        if (startFromEtap1)
        {
            result = Etap1.Run(xmlFilePath, dataEtap1Path);
            if (result != 0)
            {
                Console.WriteLine("Nacisnij Enter, aby zakonczyc...");
                Console.ReadLine();
                return result;
            }
        }
        else
        {
            Console.WriteLine("Pominieto ETAP 1 (uzyj --etap1 aby wymusic analize XML)");
            Console.WriteLine();
        }

        // ETAP 2: Przetwarzanie danych
        result = Etap2.Run(dataEtap1Path);

        if (result == 0)
        {
            Console.WriteLine("WSZYSTKIE ETAPY ZAKONCZONE POMYSLNIE!");
        }

        Console.WriteLine("Nacisnij Enter, aby zakonczyc...");
        Console.ReadLine();

        return result;
    }
}
