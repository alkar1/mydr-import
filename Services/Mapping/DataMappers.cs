using MyDr_Import.Models.Source;
using MyDr_Import.Models.Target;

namespace MyDr_Import.Services.Mapping;

/// <summary>
/// Mapper dla wizyt: MyDrEDM ? Optimed
/// </summary>
public static class VisitMapper
{
    public static OptimedWizyta Map(MyDrVisit source, int instalacjaId = 1, 
        Dictionary<long, MyDrPerson>? personLookup = null,
        Dictionary<long, MyDrPatient>? patientLookup = null)
    {
        var target = new OptimedWizyta
        {
            InstalacjaId = instalacjaId,
            IdImport = source.PrimaryKey,
            
            // Pacjent
            PacjentIdImport = source.PatientPk,
            
            // Lekarz
            PracownikIdImport = source.DoctorPk,
            
            // Jednostka (gabinet)
            JednostkaIdImport = source.OfficePk,
            
            // Daty
            DataUtworzenia = source.LastRevision,
            DataOd = CombineDateAndTime(source.Date, source.TimeTo),
            DataDo = CombineDateAndTime(source.Date, source.TimeTo),
            
            // Status
            Status = 2, // Domyœlnie: odbyta
            NFZ = 1,
            NieRozliczaj = 0,
            Dodatkowy = 0,
            
            // Komentarze (wywiad)
            Komentarz = TruncateComment(source.Interview, source.RecognitionDescription, source.NotePostvisit),
            
            // ICD kody
            RozpoznaniaICD10 = source.Icd10Codes.Any() ? string.Join(",", source.Icd10Codes) : null,
            ProceduryICD9 = source.Icd9Codes.Any() ? string.Join(",", source.Icd9Codes) : null
        };

        // Dodaj czas jeœli dostêpny
        if (source.TimeTo.HasValue)
        {
            target.CzasOd = source.TimeTo.Value.ToString(@"hh\:mm");
            // Dodaj 30 minut jako domyœlny czas zakoñczenia
            var endTime = source.TimeTo.Value.Add(TimeSpan.FromMinutes(30));
            target.CzasDo = endTime.ToString(@"hh\:mm");
        }

        // Uzupe³nij PESEL i NPWZ z lookup tables jeœli dostêpne
        if (patientLookup != null && patientLookup.TryGetValue(source.PatientPk, out var patient))
        {
            target.PacjentPesel = patient.Pesel;
        }

        if (personLookup != null && personLookup.TryGetValue(source.DoctorPk, out var doctor))
        {
            target.PracownikNPWZ = doctor.Npwz;
            target.PracownikPesel = doctor.Pesel;
        }

        return target;
    }

    private static DateTime CombineDateAndTime(DateTime date, TimeSpan? time)
    {
        if (time.HasValue)
        {
            return date.Date + time.Value;
        }
        return date.Date + new TimeSpan(10, 0, 0); // Domyœlnie 10:00
    }

    private static string? TruncateComment(params string?[] parts)
    {
        var combined = string.Join(" | ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        
        if (string.IsNullOrWhiteSpace(combined))
            return null;

        // Optimed limit: 4000 znaków
        return combined.Length > 4000 ? combined.Substring(0, 3997) + "..." : combined;
    }
}

/// <summary>
/// Mapper dla szczepieñ: MyDrEDM ? Optimed
/// </summary>
public static class VaccinationMapper
{
    public static OptimedSzczepienie Map(MyDrVaccination source, int instalacjaId = 1,
        Dictionary<long, MyDrPerson>? personLookup = null,
        Dictionary<long, MyDrPatient>? patientLookup = null)
    {
        var target = new OptimedSzczepienie
        {
            InstalacjaId = instalacjaId,
            IdImport = source.PrimaryKey,
            
            PacjentIdImport = source.PatientPk,
            PracownikIdImport = source.DoctorPk,
            
            Nazwa = source.Name,
            MiejscePodania = source.AdministrationSite,
            NrSerii = source.SeriesNumber,
            DataPodania = source.AdministrationDate,
            DataWaznosci = source.ExpiryDate,
            DrogaPodaniaId = source.AdministrationRoute,
            CzyZKalendarza = source.IsFromCalendar ? 1 : 0,
            SzczepienieId = source.VaccineId,
            Dawka = source.Dose
        };

        // Uzupe³nij PESEL i NPWZ z lookup tables
        if (patientLookup != null && patientLookup.TryGetValue(source.PatientPk, out var patient))
        {
            target.PacjentPesel = patient.Pesel;
        }

        if (source.DoctorPk.HasValue && personLookup != null && 
            personLookup.TryGetValue(source.DoctorPk.Value, out var doctor))
        {
            target.PracownikNPWZ = doctor.Npwz;
            target.PracownikPesel = doctor.Pesel;
        }

        return target;
    }
}

/// <summary>
/// Mapper dla sta³ych chorób: MyDrEDM ? Optimed
/// </summary>
public static class ChronicDiseaseMapper
{
    public static OptimedStalaChorobaPacjenta Map(MyDrRecognition source, int instalacjaId = 1)
    {
        if (source.VisitPk.HasValue)
        {
            throw new ArgumentException("Recognition with VisitPk should not be mapped as chronic disease", nameof(source));
        }

        return new OptimedStalaChorobaPacjenta
        {
            InstalacjaId = instalacjaId,
            PacjentIdImport = source.PatientPk ?? throw new ArgumentException("PatientPk is required"),
            ICD10 = source.Icd10Code ?? string.Empty,
            NumerChoroby = source.PrimaryKey.ToString(),
            Opis = source.Icd10Description
        };
    }
}

/// <summary>
/// Mapper dla sta³ych leków: MyDrEDM ? Optimed
/// </summary>
public static class PermanentDrugMapper
{
    public static OptimedStalyLekPacjenta Map(MyDrPatientPermanentDrug source, int instalacjaId = 1)
    {
        return new OptimedStalyLekPacjenta
        {
            InstalacjaId = instalacjaId,
            PacjentIdImport = source.PatientPk,
            PracownikIdImport = source.DoctorPk,
            KodKreskowy = source.DrugEan,
            DataZalecenia = source.StartDate,
            DataZakonczenia = source.EndDate,
            Dawkowanie = source.Dosage,
            Ilosc = source.Quantity,
            RodzajIlosci = source.QuantityType,
            KodOdplatnosci = source.PaymentCode
        };
    }
}

/// <summary>
/// Mapper dla pracowników: MyDrEDM ? Optimed
/// </summary>
public static class EmployeeMapper
{
    public static OptimedPracownik Map(MyDrPerson source, int instalacjaId = 1)
    {
        return new OptimedPracownik
        {
            InstalacjaId = instalacjaId,
            IdImport = source.PrimaryKey,
            
            Imie = source.FirstName,
            Nazwisko = source.LastName,
            Pesel = source.Pesel,
            Email = source.Email,
            Telefon = source.Phone,
            NPWZ = source.Npwz,
            
            // Domyœlne wartoœci
            PersonelKierujacy = 0,
            Konto = 0,
            NieWymagajZmianyHasla = 0,
            PracownikNiemedyczny = 0,
            SprawdzUnikalnoscPesel = 1,
            SprawdzUnikalnoscNpwz = 1,
            SprawdzUnikalnoscLoginu = 1,
            ZachowajIdentyfikator = 1,
            Usunieto = 0
        };
    }
}
