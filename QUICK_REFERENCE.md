# QUICK REFERENCE: Mapowanie Starej Wersji vs B≥Ídy Nowej

## ?? TOP PRIORITY FIXES

### 1. PRACOWNICY.CSV - PLIK PUSTY ?
```
PROBLEM: Brak danych, plik pusty
PRZYCZYNA: Nie ≥aduje siÍ auth.user.txt
ROZWI•ZANIE: Za≥aduj auth.user.txt i po≥πcz przez person.user = user.id

STARE MAPOWANIE:
  Imie <- auth.user.txt.first_name (JOIN: person.user = user.id)
  Nazwisko <- auth.user.txt.last_name (JOIN: person.user = user.id)
  Email <- auth.user.txt.email (JOIN: person.user = user.id)
  KontoLogin <- auth.user.txt.username (JOIN: person.user = user.id)
  NPWZ <- gabinet.person.txt.pwz (direct)

AKCJA: Dodaj LoadUserLookupAsync() i po≥πcz dane
```

### 2. WIZYTY.CSV - 61% Z£YCH DAT ?
```
PROBLEM: 427,306 rekordÛw z nieprawid≥owym formatem daty
PRZYCZYNA: Z≥y format daty
ROZWI•ZANIE: Uøyj formatu "yyyy-MM-dd HH:mm:ss"

STARE MAPOWANIE:
  DataOd <- visit.date + visit.timeFrom (format: RRRR-MM-DD GG:MM:SS)
  DataDo <- visit.date + visit.timeTo (format: RRRR-MM-DD GG:MM:SS)
  DataUtworzenia <- visit.created (format: RRRR-MM-DD GG:MM:SS)

AKCJA: 
  return dt.ToString("yyyy-MM-dd HH:mm:ss");

BONUS FIXES:
  - Komentarz = "" (NIE visit.note - to idzie do karty_wizyt.Zalecenia)
  - InstalacjaId (NIE IdInstalacji) - nazwa kolumny
```

### 3. SZCZEPIENIA.CSV - WSZYSTKIE NAZWY PUSTE ?
```
PROBLEM: Wszystkie 48,048 szczepieÒ majπ pustπ nazwÍ
PRZYCZYNA: èle mapowane pole
ROZWI•ZANIE: Mapuj vaccination.drug bezpoúrednio do Nazwa

STARE MAPOWANIE:
  Nazwa <- gabinet.vaccination.txt.drug (direct)
  NrSerii <- gabinet.vaccination.txt.vaccine_series (direct)

AKCJA: 
  Nazwa = vaccination.Drug  // bezpoúrednio z pola drug

BONUS FIXES:
  - InstalacjaId (NIE IdInstalacji) - nazwa kolumny
  - NrSerii (NIE NumerSerii) - nazwa kolumny
  - Deduplikuj IdImport (419 duplikatÛw)
```

### 4. PACJENCI.CSV - 3 B£ DY WALIDACJI ??
```
PROBLEM: 1 bez imienia, 1 bez nazwiska, 1 z≥a data urodzenia
PRZYCZYNA: Brak walidacji danych ürÛd≥owych
ROZWI•ZANIE: Dodaj walidacjÍ i pomiÒ z≥e rekordy

STARE MAPOWANIE:
  Imie <- gabinet.patient.txt.name (required: lub Nazwisko/Pesel)
  Nazwisko <- gabinet.patient.txt.surname (required: lub Imie/Pesel)
  DataUrodzenia <- birthdate LUB date_of_birth (fallback)

AKCJA: 
  if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(surname) 
      && string.IsNullOrWhiteSpace(pesel))
  {
      _logger.LogWarning($"Pacjent {id} - brak danych identyfikacyjnych - pomijam");
      return null;
  }
  
  var birthDate = patient.Birthdate ?? patient.DateOfBirth;
```

---

## ?? SZCZEG”£OWE MAPOWANIE - CHEAT SHEET

### WIZYTY.CSV
| Pole Docelowe | èrÛd≥o | Logika | Format |
|---------------|--------|--------|--------|
| InstalacjaId | null | - | - |
| IdImport | visit.id | direct | int |
| JednostkaIdImport | visit.office | direct | - |
| PacjentIdImport | visit.patient | direct | - |
| PacjentPesel | patient.pesel | JOIN: visit.patient = patient.id | - |
| PracownikIdImport | visit.doctor | direct | - |
| PracownikNPWZ | person.pwz | JOIN: visit.doctor = person.id | - |
| DataOd | visit.date + visit.timeFrom | combine | yyyy-MM-dd HH:mm:ss |
| DataDo | visit.date + visit.timeTo | combine | yyyy-MM-dd HH:mm:ss |
| DataUtworzenia | visit.created | direct | yyyy-MM-dd HH:mm:ss |
| Status | visit.state | map: "Archiwalna"?"3", "Anulowana"?"9" | - |
| NFZ | visit.visit_kind | contains "NFZ" ? "1", else "0" | - |
| Komentarz | "" | EMPTY! (note ? karty_wizyt) | - |
| TrybPrzyjecia | visit.reception_mode_choice | direct, default "5" | - |
| RozpoznaniaICD10 | recognition.icd10 ? icd10.code | JOIN: visit.id = recognition.visit, aggregate | comma-separated |
| ProceduryICD9 | medicalprocedure.icd9 ? icd9.code | JOIN: visit.id = medicalprocedure.visit, aggregate | comma-separated |

### PACJENCI.CSV
| Pole Docelowe | èrÛd≥o | Logika | Format |
|---------------|--------|--------|--------|
| InstalacjaId | null | - | - |
| IdImport | patient.id | direct | int |
| Imie | patient.name | direct, required (or Nazwisko/Pesel) | - |
| Nazwisko | patient.surname | direct, required (or Imie/Pesel) | - |
| Pesel | patient.pesel | direct, required (or Imie/Nazwisko) | - |
| DataUrodzenia | patient.birthdate OR patient.date_of_birth | fallback, validate vs PESEL | yyyy-MM-dd HH:mm:ss |
| Plec | patient.sex | map: "Kobieta"?"k", "MÍøczyzna"?"m" | - |
| Email | patient.email | direct | - |
| Telefon | patient.telephone | direct | - |
| KodOddzialuNFZ | insurance.code | JOIN: patient.insurance = insurance.id | - |
| AdresZameldowaniaXXX | address.xxx | JOIN: patient.registration_address = address.id | - |
| AdresZamieszkaniaXXX | address.xxx | JOIN: patient.residence_address = address.id, fallback to registration | - |
| Uwagi | "" | EMPTY! (intentionally) | - |
| OpiekunXXX | custodian.xxx | JOIN: patient.supervisor = custodian.id | - |

### PRACOWNICY.CSV
| Pole Docelowe | èrÛd≥o | Logika | Format |
|---------------|--------|--------|--------|
| InstalacjaId | null | - | - |
| IdImport | person.id | direct | int |
| Imie | user.first_name | JOIN: person.user = user.id | - |
| Nazwisko | user.last_name | JOIN: person.user = user.id | - |
| Email | user.email | JOIN: person.user = user.id | - |
| Pesel | person.pesel | direct | - |
| Telefon | person.telephone | direct | - |
| NPWZ | person.pwz | direct | - |
| TytulNaukowyNazwa | person.academic_degree | direct | - |
| KontoLogin | user.username | JOIN: person.user = user.id | - |
| Konto | "1" if user exists, else "0" | derived | - |
| Usunieto | person.erased | map: true?"1", false?"0" | - |

### SZCZEPIENIA.CSV
| Pole Docelowe | èrÛd≥o | Logika | Format |
|---------------|--------|--------|--------|
| InstalacjaId | null | - | - |
| IdImport | vaccination.id | direct, UNIQUE! | int |
| PacjentIdImport | vaccination.patient | direct | - |
| PacjentPesel | patient.pesel | JOIN: vaccination.patient = patient.id | - |
| PracownikIdImport | vaccination.vaccinator OR vaccination.person | fallback | - |
| PracownikNPWZ | person.pwz | JOIN: vaccination.vaccinator = person.id | - |
| Nazwa | vaccination.drug | direct | - |
| MiejscePodania | vaccination.vaccination_site | direct | - |
| NrSerii | vaccination.vaccine_series | direct | - |
| DataPodania | vaccination.datetime | direct | yyyy-MM-dd HH:mm:ss |
| DataWaznosci | vaccination.expiration_date | direct | yyyy-MM-dd HH:mm:ss |
| CzyZKalendarza | vaccination.vaccination_kind | "Z kalendarza szczepieÒ"?"1", else "0" | - |
| Dawka | vaccination.dose OR vaccination.number_of_dose | fallback | - |

### STALE_LEKI_PACJENTA.CSV
| Pole Docelowe | èrÛd≥o | Logika | Format |
|---------------|--------|--------|--------|
| InstalacjaId | null | - | - |
| PacjentIdImport | patientpermanentdrug.patient | direct | - |
| PacjentPesel | patient.pesel | JOIN: patientpermanentdrug.patient = patient.id | - |
| KodKreskowy | ver.ean13 OR patientpermanentdrug.composition | JOIN: patientpermanentdrug.drug = ver.id, fallback to composition | - |
| Dawkowanie | patientpermanentdrug.recommendation | direct | - |
| Ilosc | patientpermanentdrug.dosation | parse number (e.g., "2 op." ? "2") | - |
| RodzajIlosci | patientpermanentdrug.dosation | parse unit (e.g., "op.") ? "1" (opakowanie) or "2" (inne) | - |
| KodOdplatnosci | patientpermanentdrug.payment | direct (e.g., "100%") | - |

### DEKLARACJE_POZ.CSV
| Pole Docelowe | èrÛd≥o | Logika | Format |
|---------------|--------|--------|--------|
| InstalacjaId | null | - | - |
| IdImport | nfzdeclaration.id | direct | int |
| TypDeklaracjiPOZ | nfzdeclaration.type | direct (L, P, O, S, C, H) | - |
| DataZlozenia | nfzdeclaration.creation_date | direct | yyyy-MM-dd HH:mm:ss |
| DataWygasniecia | nfzdeclaration.deletion_date | direct | yyyy-MM-dd HH:mm:ss |
| JednostkaIdImport | nfzdeclaration.department | direct, verify in office.txt | - |
| PacjentIdImport | nfzdeclaration.patient | direct | - |
| PacjentPesel | patient.pesel | JOIN: nfzdeclaration.patient = patient.id | - |
| TypPacjentaId | "1" | default (Pacjent z PESEL) | - |
| PracownikIdImport | nfzdeclaration.personnel | direct | - |
| PracownikNPWZ | person.pwz | JOIN: nfzdeclaration.personnel = person.id | - |
| PeselOpiekuna | custodian.pesel | JOIN: nfzdeclaration.patient ? patient.supervisor ? custodian.id | - |
| Komentarz | nfzdeclaration.note | direct, max 255 chars | - |

### DOKUMENTY_UPRAWNIAJACE.CSV
| Pole Docelowe | èrÛd≥o | Logika | Format |
|---------------|--------|--------|--------|
| InstalacjaId | null | - | - |
| IdImport | insurancedocuments.id | direct | int |
| KodDokumentu | insurancedocuments.document_name | direct | - |
| KodUprawnienia | insurance.permissions_title | JOIN: insurancedocuments.insurance = insurance.id | - |
| NazwaDokumentu | insurancedocuments.document_name | direct | - |
| PacjentIdImport | insurance.patient | JOIN: insurancedocuments.insurance = insurance.id | - |
| PacjentPesel | patient.pesel | JOIN: insurancedocuments.insurance ? insurance.patient ? patient.id | - |
| DataOd | insurancedocuments.valid_from | direct | yyyy-MM-dd HH:mm:ss |
| DataDo | insurancedocuments.valid_to | direct | yyyy-MM-dd HH:mm:ss |
| DataWystawienia | insurancedocuments.set_date OR receive_date | fallback | yyyy-MM-dd HH:mm:ss |
| Numer | insurancedocuments.document_number | direct | - |

### KARTY_WIZYT.CSV
| Pole Docelowe | èrÛd≥o | Logika | Format |
|---------------|--------|--------|--------|
| InstalacjaId | null | - | - |
| IdImport | visit.id | convert to string | string |
| IdImportPrefix | "VISITCARD" | constant | - |
| WizytaIdImport | visit.id | direct | int |
| DataWystawienia | visit.date | direct | yyyy-MM-dd HH:mm:ss |
| PracownikWystawiajacyIdImport | visit.doctor | direct | - |
| PracownikWystawiajacyNpwz | person.pwz | JOIN: visit.doctor = person.id | - |
| Wywiad | visit.interview | direct | - |
| BadaniePrzedmiotowe | visit.examination | direct | - |
| PrzebiegLeczenia | recipe + recipedrug + ver | JOIN: visit.id = recipe.visit, aggregate drug names | - |
| Zalecenia | visit.recommendation + visit.note | combine (note contains meds!) | - |
| Inne | visitnotes.note | JOIN: visitnotes.visit = visit.id, aggregate | - |
| NiezdolnoscOd | sickleave.date_from | JOIN: visit.id = sickleave.visit | yyyy-MM-dd HH:mm:ss |
| NiezdolnoscDo | sickleave.date_to | JOIN: visit.id = sickleave.visit | yyyy-MM-dd HH:mm:ss |
| RozpoznaniaICD10 | same as wizyty.csv | - | comma-separated |
| ProceduryICD9 | same as wizyty.csv | - | comma-separated |

### JEDNOSTKI.CSV
| Pole Docelowe | èrÛd≥o | Logika | Format |
|---------------|--------|--------|--------|
| IdImport | office.department | direct (use department as import key) | - |
| Nazwa | office.name | direct | - |
| Aktywna | office.active | map: true?"1", false?"0" | - |
| IdWewnetrzny | office.id | internal source PK | - |

---

## ?? KLUCZOWE ZASADY

### Format Daty
```csharp
// ZAWSZE:
dt.ToString("yyyy-MM-dd HH:mm:ss")

// NIGDY:
dt.ToString("yyyy-MM-dd")  // ? brak czasu
dt.ToString()              // ? nieznany format
```

### Nazwy Kolumn
```csharp
// POPRAWNE:
InstalacjaId    // ?
NrSerii         // ?
Komentarz       // ?

// B£ DNE:
IdInstalacji    // ?
NumerSerii      // ?
Wywiad          // ? (to jest inne pole!)
```

### £πczenie Tabel (JOIN)
```csharp
// PRZYK£AD: pracownicy + uøytkownicy
var userLookup = await LoadUserLookupAsync();

foreach (var person in persons)
{
    if (person.UserId != null && userLookup.TryGetValue(person.UserId.Value, out var user))
    {
        employee.Imie = user.FirstName;  // z user
        employee.Nazwisko = user.LastName; // z user
    }
    employee.NPWZ = person.Pwz;  // z person
}
```

### Walidacja i Fallback
```csharp
// PRZYK£AD: data urodzenia
var birthDate = patient.Birthdate ?? patient.DateOfBirth;  // fallback

// PRZYK£AD: wymagane pola
if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(surname) && string.IsNullOrWhiteSpace(pesel))
{
    _logger.LogWarning($"Brak danych identyfikacyjnych - pomijam rekord {id}");
    return null;
}
```

### Mapowanie KodÛw
```csharp
// PRZYK£AD: p≥eÊ
var plec = patient.Sex switch
{
    "Kobieta" => "k",
    "MÍøczyzna" => "m",
    _ => null
};

// PRZYK£AD: status wizyty
var status = visit.State switch
{
    "Archiwalna" => "3",
    "Anulowana" => "9",
    _ => "1"  // default: Zaplanowane
};
```

### Agregacja (wiele wartoúci ? 1 pole)
```csharp
// PRZYK£AD: ICD10 (wiele rozpoznaÒ na wizytÍ)
var icd10Codes = _recognitionLookup
    .Where(r => r.VisitId == visit.Id)
    .Select(r => _icd10Lookup[r.Icd10Id].Code)
    .ToList();

var icd10String = string.Join(",", icd10Codes);
```

---

## ? QUICK ACTION CHECKLIST

### Przed rozpoczÍciem pracy nad plikiem:
- [ ] OtwÛrz `mapping_extracted.json`
- [ ] Znajdü sekcjÍ dla tego pliku
- [ ] Sprawdü wszystkie `join` - czy masz te tabele za≥adowane?
- [ ] Sprawdü wszystkie `format` - uøyj `yyyy-MM-dd HH:mm:ss`
- [ ] Sprawdü wszystkie `mapping` - czy masz s≥ownik kodÛw?

### Podczas implementacji:
- [ ] Uøyj DOK£ADNIE takich nazw pÛl jak w definicji
- [ ] Dla dat: ZAWSZE `yyyy-MM-dd HH:mm:ss`
- [ ] Dla boolean: ZAWSZE `"1"` lub `"0"` (string!)
- [ ] Dla null: uøyj `null`, nie `"null"` (string)
- [ ] Loguj wszystkie ostrzeøenia (brakujπce dane, b≥Ídy mapowania)

### Po implementacji:
- [ ] Uruchom test: `.\bin\Debug\net8.0\MyDr_Import.exe test`
- [ ] Sprawdü raport w `output_csv/test_reports/`
- [ ] Jeúli b≥Ídy - sprawdü mapowanie ponownie
- [ ] Jeúli OK - commit i przejdü do kolejnego

---

**èR”D£A:**
- mapping_extracted.json
- ANALIZA_BLEDOW_CSV.md
- old_mapping_plan.md
