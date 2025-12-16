# ANALIZA B£ÊDÓW CSV Z U¯YCIEM MAPOWANIA STAREJ WERSJI

**Data analizy:** 2024-12-16  
**ród³a:** 
- REPAIR_PLAN.md (b³êdy w nowej wersji)
- old_mapping_plan.md (mapowanie ze starej wersji)

---

## ?? PODSUMOWANIE WYKONAWCZE

| Plik CSV | Status B³êdów | Mo¿e pomóc stare mapowanie? | Priorytet |
|----------|---------------|------------------------------|-----------|
| **pacjenci.csv** | ?? 3 b³êdy (1 bez imienia, 1 bez nazwiska, 1 z³a data) | ? TAK - mapowanie pokazuje pola Ÿród³owe | NISKI |
| **pracownicy.csv** | ? KRYTYCZNY - plik pusty | ? TAK - mapowanie pokazuje ¿e trzeba ³¹czyæ z auth.user.txt | KRYTYCZNY |
| **wizyty.csv** | ? KRYTYCZNY - 61% z³ych dat, z³e nazwy kolumn | ? TAK - mapowanie pokazuje format dat i poprawne nazwy | KRYTYCZNY |
| **szczepienia.csv** | ? KRYTYCZNY - wszystkie nazwy puste, duplikaty | ? TAK - mapowanie pokazuje Ÿród³o nazwy | KRYTYCZNY |
| **stale_leki_pacjenta.csv** | ?? DO IMPLEMENTACJI | ? TAK - pe³ne mapowanie dostêpne | WYSOKI |
| **deklaracje_poz.csv** | ?? DO IMPLEMENTACJI | ? TAK - pe³ne mapowanie dostêpne | ŒREDNI |
| **dokumenty_uprawniajace.csv** | ?? DO IMPLEMENTACJI | ? TAK - pe³ne mapowanie dostêpne | ŒREDNI |
| **karty_wizyt.csv** | ?? DO IMPLEMENTACJI | ? TAK - pe³ne mapowanie dostêpne | ŒREDNI |

---

## ?? SZCZEGÓ£OWA ANALIZA B£ÊDÓW

### 1?? PACJENCI.CSV - Analiza

#### B³êdy z REPAIR_PLAN.md:
- ? 1 pacjent bez imienia
- ? 1 pacjent bez nazwiska  
- ? 1 nieprawid³owa data urodzenia

#### Co mówi stare mapowanie:

**Imiê:**
```json
{
  "source": "gabinet.patient.txt -> name",
  "logic": "Mapowanie bezpoœrednie",
  "required": true,
  "notes": "lub Nazwisko/Pesel"
}
```

**Nazwisko:**
```json
{
  "source": "gabinet.patient.txt -> surname",
  "logic": "Mapowanie bezpoœrednie",
  "required": true,
  "notes": "lub Imie/Pesel"
}
```

**DataUrodzenia:**
```json
{
  "source": "gabinet.patient.txt -> birthdate lub date_of_birth",
  "logic": "U¿yj birthdate, jeœli dostêpne; w przeciwnym razie date_of_birth. Format RRRR-MM-DD GG:MM:SS",
  "format": "RRRR-MM-DD GG:MM:SS",
  "required": false
}
```

#### ?? ROZWI¥ZANIE:

1. **Walidacja Ÿród³a:** SprawdŸ czy w `gabinet.patient.txt` s¹ puste pola `name` lub `surname`
2. **Fallback:** Zgodnie z mapowaniem, jeœli brakuje imienia/nazwiska, wymagany jest przynajmniej PESEL
3. **Data urodzenia:** 
   - SprawdŸ oba pola: `birthdate` i `date_of_birth`
   - Upewnij siê ¿e format to `RRRR-MM-DD GG:MM:SS`
   - Zwaliduj wzglêdem PESEL (data z PESEL powinna zgadzaæ siê z dat¹ urodzenia)

#### ?? Kod do poprawy:
```csharp
// W PatientMapper.cs
if (string.IsNullOrWhiteSpace(patient.Name) || string.IsNullOrWhiteSpace(patient.Surname))
{
    if (string.IsNullOrWhiteSpace(patient.Pesel))
    {
        _logger.LogWarning($"Pacjent {patient.Id} nie ma imienia/nazwiska ani PESEL - pomijam");
        return null; // Pomiñ nieprawid³owy rekord
    }
}

// Data urodzenia - sprawdŸ oba pola
var birthDate = patient.Birthdate ?? patient.DateOfBirth;
if (birthDate != null)
{
    // Walidacja wzglêdem PESEL
    if (!string.IsNullOrWhiteSpace(patient.Pesel) && patient.Pesel.Length == 11)
    {
        var peselDate = ExtractDateFromPesel(patient.Pesel);
        if (peselDate != null && Math.Abs((peselDate.Value - birthDate.Value).TotalDays) > 1)
        {
            _logger.LogWarning($"Pacjent {patient.Id}: data urodzenia {birthDate} nie zgadza siê z PESEL {patient.Pesel}");
        }
    }
}
```

---

### 2?? PRACOWNICY.CSV - Analiza

#### B³êdy z REPAIR_PLAN.md:
- ? **KRYTYCZNY:** Plik pusty lub nie generowany
- ? Brak osób z NPWZ lub b³¹d filtrowania

#### Co mówi stare mapowanie:

**Kluczowa informacja:** Pracownicy wymagaj¹ **³¹czenia dwóch tabel**!

```json
{
  "IdImport": {
    "source": "gabinet.person.txt -> id",
    "required": true
  },
  "Imie": {
    "source": "auth.user.txt -> first_name?",
    "logic": "Po³¹cz person.user z user.id",
    "join": "person.user = user.id",
    "notes": "Wymaga ³¹czenia"
  },
  "Nazwisko": {
    "source": "auth.user.txt -> last_name?",
    "logic": "Po³¹cz person.user z user.id",
    "join": "person.user = user.id",
    "notes": "Wymaga ³¹czenia"
  },
  "NPWZ": {
    "source": "gabinet.person.txt -> pwz",
    "logic": "Mapowanie bezpoœrednie"
  }
}
```

#### ?? ROZWI¥ZANIE:

**G³ówny problem:** Prawdopodobnie kod filtruje po NPWZ przed za³adowaniem danych z `auth.user.txt`!

1. **Najpierw za³aduj osoby z gabinet.person.txt** (wszyscy z NPWZ)
2. **Potem do³¹cz dane z auth.user.txt** (imiê, nazwisko, email, login)
3. Nie odrzucaj rekordu jeœli brakuje danych z user.txt - to opcjonalne

#### ?? Kod do poprawy:
```csharp
// W CsvExportService.ExportEmployeesAsync
// PRZED:
var employees = _personLookup.Values.Where(p => !string.IsNullOrEmpty(p.Npwz));

// PO:
// 1. Za³aduj najpierw lookup dla auth.user.txt
var userLookup = await LoadUserLookupAsync();

// 2. Pobierz osoby z NPWZ
var personsWithNpwz = _personLookup.Values.Where(p => !string.IsNullOrEmpty(p.Npwz));

// 3. Dla ka¿dej osoby spróbuj do³¹czyæ dane u¿ytkownika
foreach (var person in personsWithNpwz)
{
    var employee = new OptimedPracownik
    {
        IdImport = person.Id,
        NPWZ = person.Npwz,
        Pesel = person.Pesel,
        Telefon = person.Telephone,
        TytulNaukowyNazwa = person.AcademicDegree,
        Usunieto = person.Erased ? "1" : "0"
    };
    
    // Do³¹cz dane z auth.user.txt jeœli istniej¹
    if (person.UserId != null && userLookup.TryGetValue(person.UserId.Value, out var user))
    {
        employee.Imie = user.FirstName;
        employee.Nazwisko = user.LastName;
        employee.Email = user.Email;
        employee.KontoLogin = user.Username;
        employee.Konto = "1";
    }
    else
    {
        employee.Konto = "0";
        _logger.LogWarning($"Osoba {person.Id} nie ma powi¹zanego konta u¿ytkownika");
    }
    
    employees.Add(employee);
}
```

---

### 3?? WIZYTY.CSV - Analiza

#### B³êdy z REPAIR_PLAN.md:
- ? **KRYTYCZNY:** 427,306 rekordów z nieprawid³owym formatem daty (61%)
- ? Niezgodne nazwy kolumn: `Wywiad` vs `Komentarz`
- ? Niezgodne nazwy kolumn: `IdInstalacji` vs `InstalacjaId`

#### Co mówi stare mapowanie:

**Format dat:**
```json
{
  "DataOd": {
    "source": "gabinet.visit.txt -> date + timeFrom",
    "logic": "Po³¹cz datê i czas. Format RRRR-MM-DD GG:MM:SS",
    "format": "RRRR-MM-DD GG:MM:SS",
    "required": true
  },
  "DataDo": {
    "source": "gabinet.visit.txt -> date + timeTo",
    "logic": "Po³¹cz datê i czas. Format RRRR-MM-DD GG:MM:SS",
    "format": "RRRR-MM-DD GG:MM:SS",
    "required": false
  },
  "DataUtworzenia": {
    "source": "gabinet.visit.txt -> created",
    "logic": "U¿yj znacznika czasu visit.created, format RRRR-MM-DD GG:MM:SS",
    "format": "RRRR-MM-DD GG:MM:SS"
  }
}
```

**Nazwy kolumn:**
```json
{
  "Komentarz": {
    "source": "Niedostêpne bezpoœrednio",
    "logic": "Ustaw na pusty ci¹g znaków ''",
    "notes": "Pole visit.note zosta³o przeniesione do karty_wizyt.Zalecenia"
  },
  "InstalacjaId": {
    "source": "Niedostêpne bezpoœrednio",
    "logic": "Ustaw na null"
  }
}
```

#### ?? ROZWI¥ZANIE:

1. **Format daty:** Wszystkie daty MUSZ¥ byæ w formacie `RRRR-MM-DD GG:MM:SS`
2. **Nazwy kolumn:** SprawdŸ model `OptimedWizyta` - prawdopodobnie u¿ywa z³ych nazw
3. **Komentarz:** Pole powinno byæ puste (dane przeniesione do karty_wizyt)

#### ?? Kod do poprawy:
```csharp
// W VisitMapper.cs
public OptimedWizyta Map(Visit visit)
{
    return new OptimedWizyta
    {
        // POPRAWNE NAZWY:
        InstalacjaId = null,  // NIE IdInstalacji!
        Komentarz = "",       // NIE Wywiad!
        
        // POPRAWNY FORMAT DAT:
        DataOd = FormatDateTime(visit.Date, visit.TimeFrom),
        DataDo = FormatDateTime(visit.Date, visit.TimeTo),
        DataUtworzenia = FormatDateTime(visit.Created)
    };
}

private string FormatDateTime(DateTime? date, TimeSpan? time = null)
{
    if (!date.HasValue) return null;
    
    var dt = date.Value;
    if (time.HasValue)
    {
        dt = dt.Date.Add(time.Value);
    }
    
    // MUSI byæ format: RRRR-MM-DD GG:MM:SS
    return dt.ToString("yyyy-MM-dd HH:mm:ss");
}
```

**W OptimedModels.cs:**
```csharp
public class OptimedWizyta
{
    // POPRAWNE NAZWY:
    public string InstalacjaId { get; set; }  // NIE IdInstalacji
    public string Komentarz { get; set; }     // NIE Wywiad
    
    // ...pozosta³e pola
}
```

---

### 4?? SZCZEPIENIA.CSV - Analiza

#### B³êdy z REPAIR_PLAN.md:
- ? **KRYTYCZNY:** Wszystkie 48,048 szczepieñ maj¹ pust¹ nazwê
- ? 419 duplikatów kluczy g³ównych
- ? Niezgodne nazwy kolumn: `NumerSerii` vs `NrSerii`
- ? Niezgodne nazwy kolumn: `IdInstalacji` vs `InstalacjaId`

#### Co mówi stare mapowanie:

**Nazwa szczepionki:**
```json
{
  "Nazwa": {
    "source": "gabinet.vaccination.txt -> drug",
    "logic": "Mapowanie bezpoœrednie",
    "notes": "Nazwa handlowa lub opis szczepionki"
  }
}
```

**IdImport:**
```json
{
  "IdImport": {
    "source": "gabinet.vaccination.txt -> id",
    "logic": "Mapowanie bezpoœrednie",
    "required": true,
    "notes": "Klucz g³ówny szczepienia"
  }
}
```

**Nazwy kolumn:**
```json
{
  "InstalacjaId": {
    "source": "Niedostêpne bezpoœrednio",
    "logic": "Ustaw na null"
  },
  "NrSerii": {
    "source": "gabinet.vaccination.txt -> vaccine_series",
    "logic": "Mapowanie bezpoœrednie"
  }
}
```

#### ?? ROZWI¥ZANIE:

1. **Pusta nazwa:** Pole `drug` w `gabinet.vaccination.txt` powinno byæ mapowane bezpoœrednio do `Nazwa`
2. **Duplikaty IdImport:** U¿ywaj `vaccination.id` jako unikalnego klucza - sprawdŸ czy nie ma duplikatów w Ÿródle
3. **Nazwy kolumn:** Popraw w modelu

#### ?? Kod do poprawy:
```csharp
// W VaccinationMapper.cs
public OptimedSzczepienie Map(Vaccination vaccination)
{
    return new OptimedSzczepienie
    {
        // POPRAWNE NAZWY:
        InstalacjaId = null,           // NIE IdInstalacji!
        NrSerii = vaccination.VaccineSeries,  // NIE NumerSerii!
        
        // NAZWA - bezpoœrednio z pola drug:
        Nazwa = vaccination.Drug,      // BY£O: vaccination.Name lub coœ innego
        
        // IdImport - unikalny klucz:
        IdImport = vaccination.Id.ToString(),
        
        // Format daty:
        DataPodania = vaccination.DateTime?.ToString("yyyy-MM-dd HH:mm:ss"),
        DataWaznosci = vaccination.ExpirationDate?.ToString("yyyy-MM-dd HH:mm:ss")
    };
}
```

**W OptimedModels.cs:**
```csharp
public class OptimedSzczepienie
{
    // POPRAWNE NAZWY:
    public string InstalacjaId { get; set; }  // NIE IdInstalacji
    public string NrSerii { get; set; }       // NIE NumerSerii
    
    // ...pozosta³e pola
}
```

**Problem duplikatów:**
```csharp
// W CsvExportService.ExportVaccinationsAsync
var uniqueVaccinations = vaccinations
    .GroupBy(v => v.Id)
    .Select(g => g.First())
    .ToList();

if (uniqueVaccinations.Count < vaccinations.Count)
{
    _logger.LogWarning($"Znaleziono {vaccinations.Count - uniqueVaccinations.Count} duplikatów szczepieñ");
}
```

---

## ?? PLAN DZIA£ANIA - PRIORITY FIX ORDER

### ?? FAZA 1: KRYTYCZNE NAPRAWY (Kroki 1-4 z REPAIR_PLAN)

#### ? 1. PRACOWNICY.CSV - FIX #1
**Problem:** Plik pusty  
**Rozwi¹zanie:** Za³aduj auth.user.txt i po³¹cz z person.txt  
**Pliki:** `CsvExportService.cs`, `EmployeeMapper.cs`

#### ? 2. WIZYTY.CSV - FIX #2  
**Problem:** 61% z³ych dat, z³e nazwy kolumn  
**Rozwi¹zanie:** Format `yyyy-MM-dd HH:mm:ss`, popraw nazwy w modelu  
**Pliki:** `VisitMapper.cs`, `OptimedModels.cs`

#### ? 3. SZCZEPIENIA.CSV - FIX #3
**Problem:** Puste nazwy, duplikaty, z³e nazwy kolumn  
**Rozwi¹zanie:** Mapuj `drug` do `Nazwa`, deduplikuj, popraw nazwy  
**Pliki:** `VaccinationMapper.cs`, `OptimedModels.cs`

#### ? 4. PACJENCI.CSV - FIX #4
**Problem:** 3 b³êdy walidacji  
**Rozwi¹zanie:** Waliduj/pomiñ nieprawid³owe rekordy  
**Pliki:** `PatientMapper.cs`

---

### ?? FAZA 2: IMPLEMENTACJA NOWYCH (Kroki 5-14)

Wszystkie nowe pliki maj¹ **pe³ne mapowanie dostêpne w `mapping_extracted.json`**:

- ? **stale_leki_pacjenta.csv** - mapowanie dostêpne (sekcja `stale_leki_pacjenta.csv`)
- ? **deklaracje_poz.csv** - mapowanie dostêpne (sekcja `deklaracje_poz.csv`)
- ? **dokumenty_uprawniajace.csv** - mapowanie dostêpne (sekcja `dokumenty_uprawniajace.csv`)
- ? **karty_wizyt.csv** - mapowanie dostêpne (sekcja `karty_wizyt.csv`)
- ? **jednostki.csv** - mapowanie dostêpne (sekcja `jednostki.csv`)

Dla ka¿dego pliku:
1. Otwórz `mapping_extracted.json`
2. ZnajdŸ sekcjê dla danego pliku
3. Zaimplementuj mapper wed³ug specyfikacji `fields`
4. Zwróæ uwagê na `join` - wymagane ³¹czenia tabel
5. U¿yj formatu dat z `format` (zawsze `yyyy-MM-dd HH:mm:ss`)

---

## ?? KLUCZOWE WNIOSKI

### ? Co dzia³a w starym mapowaniu:

1. **Jasne Ÿród³a danych:** Ka¿de pole ma okreœlone Ÿród³o
2. **Logika ³¹czenia:** Dokumentuje wymagane JOIN'y miêdzy tabelami
3. **Formaty:** Jednoznacznie okreœla format dat (`RRRR-MM-DD GG:MM:SS`)
4. **Nazwy kolumn:** Potwierdza poprawne nazwy (np. `InstalacjaId`, nie `IdInstalacji`)
5. **Mapowania kodów:** Dokumentuje wymagane transformacje wartoœci

### ?? Najczêstsze b³êdy w nowej wersji:

1. **Brak ³¹czenia tabel:** Np. pracownicy bez auth.user.txt
2. **Z³y format dat:** U¿ywanie innego formatu ni¿ `yyyy-MM-dd HH:mm:ss`
3. **Z³e nazwy kolumn:** Nazwy niezgodne z definicj¹
4. **Brak walidacji:** Nie sprawdza siê poprawnoœci danych Ÿród³owych
5. **Ignorowanie alternatywnych pól:** Np. `birthdate` vs `date_of_birth`

### ?? Najwa¿niejsze zasady:

1. **Format dat:** ZAWSZE `yyyy-MM-dd HH:mm:ss`
2. **Nazwy kolumn:** DOK£ADNIE jak w definicji (InstalacjaId, nie IdInstalacji)
3. **£¹czenia:** MUSZ¥ byæ wykonane tam gdzie mapowanie wskazuje JOIN
4. **Walidacja:** SprawdŸ dane Ÿród³owe, loguj b³êdy, pomiñ z³e rekordy
5. **Domyœlne wartoœci:** U¿yj zgodnie z mapowaniem (np. `null`, `""`, `"0"`)

---

## ?? REFERENCJE

- **Mapowanie JSON:** `mapping_extracted.json`
- **Plan napraw:** `REPAIR_PLAN.md`
- **Oryginalne mapowanie:** `old_mapping_plan.md`

---

**Nastêpne kroki:**
1. Napraw krytyczne b³êdy (pracownicy, wizyty, szczepienia, pacjenci)
2. Uruchom testy po ka¿dej naprawie
3. Zaimplementuj nowe pliki u¿ywaj¹c `mapping_extracted.json`
4. Weryfikuj ka¿dy plik testem przed przejœciem do kolejnego
