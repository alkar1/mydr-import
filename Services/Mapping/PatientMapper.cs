using MyDr_Import.Models.Source;
using MyDr_Import.Models.Target;

namespace MyDr_Import.Services.Mapping;

/// <summary>
/// Mapper dla pacjentów: MyDrEDM ? Optimed
/// </summary>
public static class PatientMapper
{
    public static OptimedPacjent Map(MyDrPatient source, int instalacjaId = 1, MyDrCustodian? custodian = null)
    {
        if (source.Person == null)
        {
            throw new ArgumentException("Patient must have Person data loaded", nameof(source));
        }

        var target = new OptimedPacjent
        {
            InstalacjaId = instalacjaId,
            IdImport = source.PrimaryKey,
            RodzajPacjenta = 1, // Domyœlnie osoba fizyczna
            
            // Dane podstawowe z Person
            Imie = source.Person.FirstName,
            Nazwisko = source.Person.LastName,
            Pesel = source.Pesel,
            DataUrodzenia = source.Person.BirthDate,
            Plec = source.Person.Sex,
            Email = source.Person.Email,
            Telefon = source.Person.Phone,
            
            // Dodatkowe dane pacjenta
            DrugieImie = source.SecondName,
            NazwiskoRodowe = source.MaidenName,
            TelefonDodatkowy = source.SecondTelephone,
            NumerDokumentuTozsamosci = source.IdentityNum,
            KrajDokumentuTozsamosciKod = source.Country,
            MiejsceUrodzenia = source.PlaceOfBirth,
            KodOddzialuNFZ = source.Nfz,
            
            // Dane o zgonie
            CzyUmarl = source.Dead?.IsDead == true ? 1 : 0,
            DataZgonu = source.Dead?.DeathDate,
            
            // Domyœlne wartoœci
            Uchodzca = 0,
            VIP = 0,
            
            // Flagi kontrolne
            SprawdzUnikalnoscIdImportu = 1,
            SprawdzUnikalnoscPesel = 1,
            AktualizujPoPesel = 0
        };

        // Mapowanie adresu zamieszkania
        if (source.ResidenceAddress != null)
        {
            MapAddress(source.ResidenceAddress, target, isResidence: true);
        }

        // Mapowanie adresu zameldowania (u¿ywamy tego samego co zamieszkania jeœli brak employer)
        if (source.EmployerAddress != null)
        {
            MapAddress(source.EmployerAddress, target, isResidence: false);
        }
        else if (source.ResidenceAddress != null)
        {
            // Jeœli brak adresu zameldowania, u¿yj zamieszkania
            MapAddress(source.ResidenceAddress, target, isResidence: false);
        }

        // Mapowanie opiekuna
        if (custodian != null)
        {
            MapCustodian(custodian, target);
        }

        return target;
    }

    private static void MapAddress(MyDrAddress address, OptimedPacjent target, bool isResidence)
    {
        var prefix = isResidence ? "Zamieszkanie" : "Zameldowanie";
        
        var countryProp = typeof(OptimedPacjent).GetProperty($"Kraj{prefix}");
        var wojewodzstwoProp = typeof(OptimedPacjent).GetProperty($"Wojewodztwo{prefix}");
        var kodTerytGminyProp = typeof(OptimedPacjent).GetProperty($"KodTerytGminy{prefix}");
        var miejscowoscProp = typeof(OptimedPacjent).GetProperty($"Miejscowosc{prefix}");
        var kodMiejscowosciProp = typeof(OptimedPacjent).GetProperty($"KodMiejscowosci{prefix}");
        var kodPocztowyProp = typeof(OptimedPacjent).GetProperty($"KodPocztowy{prefix}");
        var ulicaProp = typeof(OptimedPacjent).GetProperty($"Ulica{prefix}");
        var nrDomuProp = typeof(OptimedPacjent).GetProperty($"NrDomu{prefix}");
        var nrMieszkaniaProp = typeof(OptimedPacjent).GetProperty($"NrMieszkania{prefix}");
        var dzielnicaProp = typeof(OptimedPacjent).GetProperty($"Dzielnica{prefix}");

        countryProp?.SetValue(target, address.Country ?? "PL");
        wojewodzstwoProp?.SetValue(target, address.Voivodeship);
        kodTerytGminyProp?.SetValue(target, address.CommuneTeryt);
        miejscowoscProp?.SetValue(target, address.City);
        kodMiejscowosciProp?.SetValue(target, address.CityTeryt);
        kodPocztowyProp?.SetValue(target, address.PostalCode);
        ulicaProp?.SetValue(target, address.Street);
        nrDomuProp?.SetValue(target, address.HouseNumber);
        nrMieszkaniaProp?.SetValue(target, address.ApartmentNumber);
        dzielnicaProp?.SetValue(target, address.District);
    }
    
    private static void MapCustodian(MyDrCustodian custodian, OptimedPacjent target)
    {
        target.ImieOpiekuna = custodian.FirstName;
        target.NazwiskoOpiekuna = custodian.LastName;
        target.PlecOpiekuna = custodian.Sex;
        target.DataUrodzeniaOpiekuna = custodian.BirthDate;
        target.PeselOpiekuna = custodian.Pesel;
        target.TelefonOpiekuna = custodian.Phone;
        target.StopienPokrewienstwaOpiekuna = custodian.Relationship;
        
        // Adres opiekuna
        target.KrajOpiekuna = custodian.Country ?? "PL";
        target.WojewodztwoOpiekuna = custodian.Voivodeship;
        target.KodGminyOpiekuna = custodian.CommuneCode;
        target.MiejscowoscOpiekuna = custodian.City;
        target.KodMiejscowosciOpiekuna = custodian.CityCode;
        target.KodPocztowyOpiekuna = custodian.PostalCode;
        target.UlicaOpiekuna = custodian.Street;
        target.NrDomuOpiekuna = custodian.HouseNumber;
        target.NrLokaluOpiekuna = custodian.ApartmentNumber;
    }
}
