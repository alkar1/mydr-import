namespace MyDr_Import.Models.Source;

/// <summary>
/// Model pacjenta z MyDrEDM (gabinet.patient)
/// </summary>
public class MyDrPatient
{
    public long PrimaryKey { get; set; }
    
    // Dane osobowe (s¹ w patient!)
    public string? FirstName { get; set; }      // name
    public string? LastName { get; set; }       // surname
    public DateTime? BirthDate { get; set; }    // date_of_birth
    public string? Sex { get; set; }            // sex (Kobieta/Mê¿czyzna)
    public string? Email { get; set; }          // email
    public string? Phone { get; set; }          // telephone
    
    // Dane pacjenta
    public string? Pesel { get; set; }
    public string? SecondName { get; set; }
    public string? MaidenName { get; set; }
    public string? IdentityNum { get; set; }
    public string? Country { get; set; }
    public string? PlaceOfBirth { get; set; }
    public string? Nfz { get; set; }
    public string? SecondTelephone { get; set; }
    
    // Relacje
    public long? ResidenceAddressPk { get; set; }
    public MyDrAddress? ResidenceAddress { get; set; }
    
    // Dane za³adowane z lookup
    public MyDrPerson? Person { get; set; }  // Ju¿ nie potrzebne - dane s¹ bezpoœrednio!
    public MyDrPatientDead? Dead { get; set; }
}

/// <summary>
/// Model osoby (gabinet.person) - wspólny dla patient i pracowników
/// </summary>
public class MyDrPerson
{
    public long PrimaryKey { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string? Sex { get; set; } // M, K, null
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Pesel { get; set; }
    public string? Npwz { get; set; }
}

/// <summary>
/// Adres (gabinet.address)
/// </summary>
public class MyDrAddress
{
    public long PrimaryKey { get; set; }
    public string? Country { get; set; }
    public string? Voivodeship { get; set; }
    public string? CommuneTeryt { get; set; }
    public string? City { get; set; }
    public string? CityTeryt { get; set; }
    public string? PostalCode { get; set; }
    public string? Street { get; set; }
    public string? HouseNumber { get; set; }
    public string? ApartmentNumber { get; set; }
    public string? District { get; set; }
}

/// <summary>
/// Informacja o zgonie pacjenta (gabinet.patientdead)
/// </summary>
public class MyDrPatientDead
{
    public long PrimaryKey { get; set; }
    public long PatientPk { get; set; }
    public bool IsDead { get; set; }
    public DateTime? DeathDate { get; set; }
}

/// <summary>
/// Wizyta (gabinet.visit)
/// </summary>
public class MyDrVisit
{
    public long PrimaryKey { get; set; }
    public long PatientPk { get; set; }
    public long DoctorPk { get; set; }
    public long? OfficePk { get; set; }
    public long? SpecialtyPk { get; set; }
    
    public DateTime Date { get; set; }
    public TimeSpan? TimeTo { get; set; }
    public DateTime? LastRevision { get; set; }
    
    public string? Interview { get; set; }
    public string? RecognitionDescription { get; set; }
    public string? NotePostvisit { get; set; }
    
    public bool Evisit { get; set; }
    public bool CreatedByPlugin { get; set; }
    
    // Will be populated from related tables
    public List<string> Icd10Codes { get; set; } = new();
    public List<string> Icd9Codes { get; set; } = new();
}

/// <summary>
/// Szczepienie (gabinet.vaccination)
/// </summary>
public class MyDrVaccination
{
    public long PrimaryKey { get; set; }
    public long PatientPk { get; set; }
    public long? DoctorPk { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string? AdministrationSite { get; set; }
    public string? SeriesNumber { get; set; }
    public DateTime AdministrationDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? AdministrationRoute { get; set; }
    public bool IsFromCalendar { get; set; }
    public long? VaccineId { get; set; }
    public int? Dose { get; set; }
}

/// <summary>
/// Rozpoznanie (gabinet.recognition) - mo¿e byæ sta³e lub zwi¹zane z wizyt¹
/// </summary>
public class MyDrRecognition
{
    public long PrimaryKey { get; set; }
    public long? VisitPk { get; set; }
    public long? PatientPk { get; set; }
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    
    // Will be populated from gabinet.icd10
    public string? Icd10Code { get; set; }
    public string? Icd10Description { get; set; }
}

/// <summary>
/// Sta³y lek pacjenta (gabinet.patientpermanentdrug)
/// </summary>
public class MyDrPatientPermanentDrug
{
    public long PrimaryKey { get; set; }
    public long PatientPk { get; set; }
    public long? DoctorPk { get; set; }
    
    public string? DrugEan { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Dosage { get; set; }
    public string? Quantity { get; set; }
    public string? QuantityType { get; set; }
    public string? PaymentCode { get; set; }
}

/// <summary>
/// ICD-10 kod (gabinet.icd10)
/// </summary>
public class MyDrIcd10
{
    public long PrimaryKey { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCancer { get; set; }
    public bool IsImmediate { get; set; }
}

/// <summary>
/// ICD-9 kod procedury (gabinet.icd9)
/// </summary>
public class MyDrIcd9
{
    public long PrimaryKey { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Procedura medyczna (gabinet.medicalprocedure)
/// </summary>
public class MyDrMedicalProcedure
{
    public long PrimaryKey { get; set; }
    public long VisitPk { get; set; }
    public long? Icd9Pk { get; set; }
    public DateTime Date { get; set; }
}

/// <summary>
/// Opiekun prawny / kontakt awaryjny (gabinet.custodian / gabinet.incaseofemergency)
/// </summary>
public class MyDrCustodian
{
    public long PrimaryKey { get; set; }
    public long PatientPk { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Sex { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Pesel { get; set; }
    public string? Phone { get; set; }
    public string? Relationship { get; set; } // Stopieñ pokrewieñstwa
    
    // Adres opiekuna
    public string? Country { get; set; }
    public string? Voivodeship { get; set; }
    public string? CommuneCode { get; set; }
    public string? City { get; set; }
    public string? CityCode { get; set; }
    public string? PostalCode { get; set; }
    public string? Street { get; set; }
    public string? HouseNumber { get; set; }
    public string? ApartmentNumber { get; set; }
}
