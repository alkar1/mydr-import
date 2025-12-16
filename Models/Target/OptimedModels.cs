using CsvHelper.Configuration.Attributes;

namespace MyDr_Import.Models.Target;

/// <summary>
/// Model pacjenta dla importu do Optimed (pacjenci.csv)
/// </summary>
public class OptimedPacjent
{
    [Index(0)] public int? InstalacjaId { get; set; }
    [Index(1)] public long IdImport { get; set; }
    [Index(2)] public int? UprawnieniePacjentaId { get; set; }
    [Index(3)] public int RodzajPacjenta { get; set; } = 1;
    
    [Index(4)] public string Imie { get; set; } = string.Empty;
    [Index(5)] public string Nazwisko { get; set; } = string.Empty;
    [Index(6)] public string? Pesel { get; set; }
    [Index(7)] public DateTime? DataUrodzenia { get; set; }
    
    [Index(8)] public int CzyUmarl { get; set; }
    [Index(9)] public DateTime? DataZgonu { get; set; }
    
    [Index(10)] public string? DrugieImie { get; set; }
    [Index(11)] public string? NazwiskoRodowe { get; set; }
    [Index(12)] public string? ImieOjca { get; set; }
    [Index(13)] public string? NIP { get; set; }
    [Index(14)] public string? Plec { get; set; }
    [Index(15)] public string? Email { get; set; }
    [Index(16)] public string? Telefon { get; set; }
    [Index(17)] public string? TelefonDodatkowy { get; set; }
    
    [Index(18)] public string? NumerDokumentuTozsamosci { get; set; }
    [Index(19)] public string? TypDokumentuTozsamosci { get; set; }
    [Index(20)] public string? KrajDokumentuTozsamosciKod { get; set; }
    [Index(21)] public string? NrIdentyfikacyjnyUe { get; set; }
    [Index(22)] public string? MiejsceUrodzenia { get; set; }
    [Index(23)] public string? KodOddzialuNFZ { get; set; }
    
    // Adres zameldowania
    [Index(24)] public string? KrajZameldowanie { get; set; }
    [Index(25)] public string? WojewodztwoZameldowanie { get; set; }
    [Index(26)] public string? KodTerytGminyZameldowanie { get; set; }
    [Index(27)] public string? MiejscowoscZameldowanie { get; set; }
    [Index(28)] public string? KodMiejscowosciZameldowanie { get; set; }
    [Index(29)] public string? KodPocztowyZameldowanie { get; set; }
    [Index(30)] public string? UlicaZameldowanie { get; set; }
    [Index(31)] public string? NrDomuZameldowanie { get; set; }
    [Index(32)] public string? NrMieszkaniaZameldowanie { get; set; }
    [Index(33)] public string? DzielnicaZameldowanie { get; set; }
    
    // Adres zamieszkania
    [Index(34)] public string? KrajZamieszkanie { get; set; }
    [Index(35)] public string? WojewodztwoZamieszkanie { get; set; }
    [Index(36)] public string? KodTerytGminyZamieszkanie { get; set; }
    [Index(37)] public string? MiejscowoscZamieszkanie { get; set; }
    [Index(38)] public string? KodMiejscowosciZamieszkanie { get; set; }
    [Index(39)] public string? KodPocztowyZamieszkanie { get; set; }
    [Index(40)] public string? UlicaZamieszkanie { get; set; }
    [Index(41)] public string? NrDomuZamieszkanie { get; set; }
    [Index(42)] public string? NrMieszkaniaZamieszkanie { get; set; }
    [Index(43)] public string? DzielnicaZamieszkanie { get; set; }
    
    [Index(44)] public string? Uwagi { get; set; }
    [Index(45)] public int Uchodzca { get; set; }
    [Index(46)] public int VIP { get; set; }
    [Index(47)] public string? UprawnieniePacjenta { get; set; }
    
    // Dane opiekuna / opiekuna prawnego
    [Index(48)] public string? ImieOpiekuna { get; set; }
    [Index(49)] public string? NazwiskoOpiekuna { get; set; }
    [Index(50)] public string? PlecOpiekuna { get; set; }
    [Index(51)] public DateTime? DataUrodzeniaOpiekuna { get; set; }
    [Index(52)] public string? PeselOpiekuna { get; set; }
    [Index(53)] public string? TelefonOpiekuna { get; set; }
    [Index(54)] public string? KrajOpiekuna { get; set; }
    [Index(55)] public string? WojewodztwoOpiekuna { get; set; }
    [Index(56)] public string? KodGminyOpiekuna { get; set; }
    [Index(57)] public string? MiejscowoscOpiekuna { get; set; }
    [Index(58)] public string? KodMiejscowosciOpiekuna { get; set; }
    [Index(59)] public string? KodPocztowyOpiekuna { get; set; }
    [Index(60)] public string? UlicaOpiekuna { get; set; }
    [Index(61)] public string? NrDomuOpiekuna { get; set; }
    [Index(62)] public string? NrLokaluOpiekuna { get; set; }
    [Index(63)] public string? StopienPokrewienstwaOpiekuna { get; set; }
    
    // Flagi kontrolne importu
    [Index(64)] public int SprawdzUnikalnoscIdImportu { get; set; } = 1;
    [Index(65)] public int SprawdzUnikalnoscPesel { get; set; } = 1;
    [Index(66)] public int AktualizujPoPesel { get; set; } = 0;
    [Index(67)] public string? NumerPacjenta { get; set; }
}

/// <summary>
/// Model wizyty dla importu do Optimed (wizyty.csv)
/// </summary>
public class OptimedWizyta
{
    [Index(0)] public int? InstalacjaId { get; set; }
    [Index(1)] public long IdImport { get; set; }
    
    [Index(2)] public int? JednostkaId { get; set; }
    [Index(3)] public long? JednostkaIdImport { get; set; }
    
    [Index(4)] public long? PacjentId { get; set; }
    [Index(5)] public long PacjentIdImport { get; set; }
    [Index(6)] public string? PacjentPesel { get; set; }
    
    [Index(7)] public long? PracownikId { get; set; }
    [Index(8)] public long PracownikIdImport { get; set; }
    [Index(9)] public long? ZasobIdImport { get; set; }
    [Index(10)] public string? PracownikNPWZ { get; set; }
    [Index(11)] public string? PracownikPesel { get; set; }
    
    [Index(12)] public long? PlatnikIdImportu { get; set; }
    [Index(13)] public int? JednostkaRozliczeniowaId { get; set; }
    [Index(14)] public long? JednostkaRozliczeniowaIdImportu { get; set; }
    
    [Index(15)] public DateTime? DataUtworzenia { get; set; }
    [Index(16)] public DateTime DataOd { get; set; }
    [Index(17)] public DateTime? DataDo { get; set; }
    [Index(18)] public string? CzasOd { get; set; }
    [Index(19)] public string? CzasDo { get; set; }
    
    [Index(20)] public int Status { get; set; } = 2; // Domyœlnie: odbyta
    [Index(21)] public int NFZ { get; set; } = 1;
    [Index(22)] public int NieRozliczaj { get; set; }
    [Index(23)] public int Dodatkowy { get; set; }
    
    [Index(24)] public string? Komentarz { get; set; }
    
    [Index(25)] public int? TrybPrzyjecia { get; set; }
    [Index(26)] public int? TrybDalszegoLeczenia { get; set; }
    [Index(27)] public int? TypWizyty { get; set; }
    [Index(28)] public string? KodSwiadczeniaNFZ { get; set; }
    [Index(29)] public string? KodUprawnieniaPacjenta { get; set; }
    
    [Index(30)] public string? ProceduryICD9 { get; set; }
    [Index(31)] public string? RozpoznaniaICD10 { get; set; }
    [Index(32)] public long? DokumentSkierowujacyIdImportu { get; set; }
}

/// <summary>
/// Model szczepienia dla importu do Optimed (szczepienia.csv)
/// </summary>
public class OptimedSzczepienie
{
    [Index(0)] public int? InstalacjaId { get; set; }
    [Index(1)] public long IdImport { get; set; }
    
    [Index(2)] public long PacjentIdImport { get; set; }
    [Index(3)] public string? PacjentPesel { get; set; }
    
    [Index(4)] public long? PracownikIdImport { get; set; }
    [Index(5)] public string? PracownikNPWZ { get; set; }
    [Index(6)] public string? PracownikPesel { get; set; }
    
    [Index(7)] public string Nazwa { get; set; } = string.Empty;
    [Index(8)] public string? MiejscePodania { get; set; }
    [Index(9)] public string? NrSerii { get; set; }
    [Index(10)] public DateTime DataPodania { get; set; }
    [Index(11)] public DateTime? DataWaznosci { get; set; }
    
    [Index(12)] public int? DrogaPodaniaId { get; set; }
    [Index(13)] public int CzyZKalendarza { get; set; }
    [Index(14)] public long? SzczepienieId { get; set; }
    [Index(15)] public int? Dawka { get; set; }
}

/// <summary>
/// Model sta³ej choroby pacjenta dla importu do Optimed (stale_choroby_pacjenta.csv)
/// </summary>
public class OptimedStalaChorobaPacjenta
{
    [Index(0)] public int? InstalacjaId { get; set; }
    [Index(1)] public long? PacjentId { get; set; }
    [Index(2)] public long PacjentIdImport { get; set; }
    [Index(3)] public string ICD10 { get; set; } = string.Empty;
    [Index(4)] public string? NumerChoroby { get; set; }
    [Index(5)] public string? Opis { get; set; }
}

/// <summary>
/// Model sta³ego leku pacjenta dla importu do Optimed (stale_leki_pacjenta.csv)
/// </summary>
public class OptimedStalyLekPacjenta
{
    [Index(0)] public int? InstalacjaId { get; set; }
    [Index(1)] public long? PacjentId { get; set; }
    [Index(2)] public long PacjentIdImport { get; set; }
    
    [Index(3)] public long? PracownikId { get; set; }
    [Index(4)] public long? PracownikIdImport { get; set; }
    
    [Index(5)] public string? KodKreskowy { get; set; }
    [Index(6)] public DateTime? DataZalecenia { get; set; }
    [Index(7)] public DateTime? DataZakonczenia { get; set; }
    [Index(8)] public string? Dawkowanie { get; set; }
    [Index(9)] public string? Ilosc { get; set; }
    [Index(10)] public string? RodzajIlosci { get; set; }
    [Index(11)] public string? KodOdplatnosci { get; set; }
}

/// <summary>
/// Model pracownika dla importu do Optimed (pracownicy.csv)
/// </summary>
public class OptimedPracownik
{
    [Index(0)] public int? InstalacjaId { get; set; }
    [Index(1)] public long IdImport { get; set; }
    
    [Index(2)] public string Imie { get; set; } = string.Empty;
    [Index(3)] public string Nazwisko { get; set; } = string.Empty;
    [Index(4)] public string? Pesel { get; set; }
    [Index(5)] public string? Email { get; set; }
    [Index(6)] public string? Telefon { get; set; }
    [Index(7)] public string? NPWZ { get; set; }
    
    [Index(8)] public int? TytulNaukowyId { get; set; }
    [Index(9)] public string? TytulNaukowyNazwa { get; set; }
    [Index(10)] public int? TypPersoneluId { get; set; }
    [Index(11)] public int? TypPersoneluNFZ { get; set; }
    [Index(12)] public string? SpecjalizacjeIds { get; set; }
    
    [Index(13)] public int PersonelKierujacy { get; set; }
    [Index(14)] public int Konto { get; set; }
    [Index(15)] public string? KontoLogin { get; set; }
    [Index(16)] public int NieWymagajZmianyHasla { get; set; }
    [Index(17)] public int PracownikNiemedyczny { get; set; }
    
    [Index(18)] public int SprawdzUnikalnoscPesel { get; set; } = 1;
    [Index(19)] public int SprawdzUnikalnoscNpwz { get; set; } = 1;
    [Index(20)] public int SprawdzUnikalnoscLoginu { get; set; } = 1;
    [Index(21)] public int ZachowajIdentyfikator { get; set; } = 1;
    [Index(22)] public int Usunieto { get; set; }
}
