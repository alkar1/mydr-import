using MyDr_Import.Models;
using MyDr_Import.Services;

namespace MyDr_Import.Processors;

/// <summary>
/// Procesor dla modelu PACJENCI
/// Zrodlo: gabinet_patient.xml
/// Cel: pacjenci.csv
/// </summary>
public class PacjenciProcessor : BaseModelProcessor
{
    public override string ModelName => "pacjenci";
    public override string XmlFileName => "gabinet_patient.xml";

    /// <summary>
    /// Mapowanie nazw pol z arkusza Excel na nazwy pol XML dla pacjentow
    /// </summary>
    protected override Dictionary<string, string> FieldNameMappings => new(StringComparer.OrdinalIgnoreCase)
    {
        // Dane osobowe
        { "Imie", "first_name" },
        { "Nazwisko", "last_name" },
        { "DrugieImie", "second_name" },
        { "NazwiskoRodowe", "maiden_name" },
        { "DataUrodzenia", "birth_date" },
        { "DataZgonu", "death_date" },
        { "CzyUmarl", "dead" },
        { "Plec", "sex" },
        
        // Kontakt
        { "Email", "email" },
        { "Telefon", "telephone" },
        { "TelefonDodatkowy", "second_telephone" },
        
        // Dokumenty
        { "NumerDokumentuTozsamosci", "identity_num" },
        { "MiejsceUrodzenia", "place_of_birth" },
        
        // NFZ
        { "KodOddzialuNFZ", "nfz" },
        
        // Inne
        { "Uwagi", "notes" },
        { "VIP", "is_vip" },
        { "NIP", "employer_nip" },
        { "Kraj", "country" },
        { "GrupaKrwi", "blood_type" },
        { "IdImport", "pk" },
        
        // Flagi
        { "Uchodzca", "is_refugee" },
        { "Aktywny", "is_active" },
    };
}
