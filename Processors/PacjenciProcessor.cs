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
    /// Pola dostepne w gabinet_patient.xml:
    /// - active, blood_type, cancelled_visits_count, country, dead (relacja), debt
    /// - employer_address (relacja), employer_nip, facility (relacja), identity_num
    /// - is_active, is_ice_auth_signed, is_self_registered, maiden_name, nfz
    /// - nobody_ice_authorized, notification_email (bool), occupation_code, pesel
    /// - place_of_birth, residence_address (relacja), rights_document_info
    /// - rodo_mail_sent, second_name, second_telephone, takes_part_in_loyalty_program
    /// 
    /// UWAGA: Pola takie jak first_name, last_name, email, telephone, birth_date, sex
    /// NIE istnieja w gabinet.patient - musza byc pobrane z innych zrodel lub wyliczone z PESEL
    /// </summary>
    protected override Dictionary<string, string> FieldNameMappings => new(StringComparer.OrdinalIgnoreCase)
    {
        // Identyfikator
        { "IdImport", "pk" },
        { "Pesel", "pesel" },
        
        // Dane osobowe dostepne w gabinet.patient
        { "DrugieImie", "second_name" },
        { "NazwiskoRodowe", "maiden_name" },
        { "MiejsceUrodzenia", "place_of_birth" },
        
        // Kontakt - tylko second_telephone jest dostepny
        { "TelefonDodatkowy", "second_telephone" },
        
        // Dokumenty
        { "NumerDokumentuTozsamosci", "identity_num" },
        
        // NFZ
        { "KodOddzialuNFZ", "nfz" },
        
        // Inne
        { "NIP", "employer_nip" },
        { "Kraj", "country" },
        { "GrupaKrwi", "blood_type" },
        
        // Flagi
        { "Aktywny", "is_active" },
        
        // TODO: Pola wymagajace dodatkowej logiki:
        // - Imie, Nazwisko - brak w gabinet.patient (do uzupelnienia z innego zrodla)
        // - DataUrodzenia, Plec - mozna wyliczyc z PESEL
        // - Email, Telefon - brak w gabinet.patient
        // - CzyUmarl, DataZgonu - relacja do gabinet.patientdead
        // - Uwagi - relacja do gabinet.patientnote
        // - VIP, Uchodzca - brak w gabinet.patient
    };
}
