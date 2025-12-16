using MyDr_Import.Models.Source;
using MyDr_Import.Models.Target;

namespace MyDr_Import.Services.Mapping;

/// <summary>
/// Mapper dla pacjentów: MyDrEDM ? Optimed
/// </summary>
public static class PatientMapper
{
    public static OptimedPacjent Map(MyDrPatient source, int instalacjaId, MyDrCustodian? custodian = null)
    {
        // Dane osobowe s¹ BEZPOŒREDNIO w patient!
        if (string.IsNullOrEmpty(source.FirstName) || string.IsNullOrEmpty(source.LastName))
        {
            throw new ArgumentException("Patient must have FirstName and LastName", nameof(source));
        }

        return new OptimedPacjent
        {
            InstalacjaId = null,
            IdImport = source.PrimaryKey,
            
            // Dane osobowe z patient
            Imie = source.FirstName,
            Nazwisko = source.LastName,
            DataUrodzenia = source.BirthDate,
            Plec = MapSex(source.Sex),
            Email = source.Email,
            Telefon = source.Phone,
            
            // Dane pacjenta
            Pesel = source.Pesel,
            DrugieImie = source.SecondName,
            NazwiskoRodowe = source.MaidenName,
            NumerDokumentuTozsamosci = source.IdentityNum,
            MiejsceUrodzenia = source.PlaceOfBirth,
            TelefonDodatkowy = source.SecondTelephone,
            
            // Dane medyczne
            KodOddzialuNFZ = source.Nfz,
            
            // Dane adresowe - ZAMIESZKANIE
            KrajZamieszkanie = source.Country ?? "Polska",
            WojewodztwoZamieszkanie = source.ResidenceAddress?.Voivodeship,
            KodTerytGminyZamieszkanie = source.ResidenceAddress?.CommuneTeryt,
            MiejscowoscZamieszkanie = source.ResidenceAddress?.City,
            KodMiejscowosciZamieszkanie = source.ResidenceAddress?.CityTeryt,
            KodPocztowyZamieszkanie = source.ResidenceAddress?.PostalCode,
            UlicaZamieszkanie = source.ResidenceAddress?.Street,
            NrDomuZamieszkanie = source.ResidenceAddress?.HouseNumber,
            NrMieszkaniaZamieszkanie = source.ResidenceAddress?.ApartmentNumber,
            
            // Dane adresowe - ZAMELDOWANIE (u¿yj tego samego co zamieszkanie)
            KrajZameldowanie = source.Country ?? "Polska",
            WojewodztwoZameldowanie = source.ResidenceAddress?.Voivodeship,
            KodTerytGminyZameldowanie = source.ResidenceAddress?.CommuneTeryt,
            MiejscowoscZameldowanie = source.ResidenceAddress?.City,
            KodMiejscowosciZameldowanie = source.ResidenceAddress?.CityTeryt,
            KodPocztowyZameldowanie = source.ResidenceAddress?.PostalCode,
            UlicaZameldowanie = source.ResidenceAddress?.Street,
            NrDomuZameldowanie = source.ResidenceAddress?.HouseNumber,
            NrMieszkaniaZameldowanie = source.ResidenceAddress?.ApartmentNumber,
            
            // Status
            CzyUmarl = source.Dead?.IsDead == true ? 1 : 0,
            DataZgonu = source.Dead?.DeathDate,
            
            // Opiekun
            ImieOpiekuna = custodian?.FirstName,
            NazwiskoOpiekuna = custodian?.LastName,
            StopienPokrewienstwaOpiekuna = custodian?.Relationship,
            
            // Flagi kontrolne
            SprawdzUnikalnoscIdImportu = 1,
            SprawdzUnikalnoscPesel = 0,
            AktualizujPoPesel = 0
        };
    }

    private static string? MapSex(string? sex)
    {
        if (string.IsNullOrEmpty(sex)) return null;
        
        return sex.ToLower() switch
        {
            "kobieta" => "k",
            "mê¿czyzna" => "m",
            "k" => "k",
            "m" => "m",
            _ => null
        };
    }
}
