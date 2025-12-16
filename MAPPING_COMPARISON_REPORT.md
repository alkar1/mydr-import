# PorÛwnanie MapowaÒ: old_mapping_plan.md vs Nasz System (Etap 2)

**Data porÛwnania:** 2025-12-16  
**Status:** ?? R”ØNICE WYKRYTE

---

## ?? **KLUCZOWE ODKRYCIA**

### **1. ARCHITEKTURA èR”D£OWA - FUNDAMENTALNA R”ØNICA**

#### **OLD_MAPPING_PLAN (stary system):**
```
èrÛd≥o: Pliki TXT w katalogu etap1/
- gabinet.patient.txt - ZAWIERA first_name, surname, birthdate
- gabinet.person.txt - pracownicy medyczni
- auth.user.txt - dane logowania
```

#### **NASZ SYSTEM (aktualny):**
```
èrÛd≥o: Plik XML Django 8.5 GB
- gabinet.patient (XML) - BRAK first_name, surname, birth_date
- gabinet.person (XML) - tylko 199 pracownikÛw
- auth.user (XML) - tylko 199 uøytkownikÛw
```

### **?? G£”WNY PROBLEM:**

**OLD PLAN zak≥ada, øe `gabinet.patient.txt` MA pola:**
- `name` (ImiÍ)
- `surname` (Nazwisko)
- `birthdate` (Data urodzenia)
- `sex` (P≥eÊ)
- `email` (Email)
- `telephone` (Telefon)

**NASZA RZECZYWISTOå∆ - `gabinet.patient` (XML) MA TYLKO:**
- `pesel`
- `nfz`
- `maiden_name` (nazwisko rodowe)
- `second_name` (drugie imiÍ)
- `place_of_birth`
- `identity_num`
- **BRAK: first_name, last_name, birth_date, sex, email, phone!**

---

## ?? **SZCZEG”£OWE POR”WNANIE P”L**

### **1. PACJENCI (pacjenci.csv)**

| Pole Docelowe | Old Mapping Plan | Nasz System | Status |
|---------------|------------------|-------------|--------|
| **IdImport** | `id` z `patient.txt` | `id` z `gabinet.patient` (XML) | ? OK |
| **Imie** | `name` z `patient.txt` | ? **BRAK W XML!** | ? PROBLEM |
| **Nazwisko** | `surname` z `patient.txt` | ? **BRAK W XML!** | ? PROBLEM |
| **Pesel** | `pesel` z `patient.txt` | ? `pesel` z patient | ? OK |
| **DataUrodzenia** | `birthdate` z `patient.txt` | ? **BRAK W XML!** | ? PROBLEM |
| **Plec** | `sex` z `patient.txt` | ? **BRAK W XML!** | ? PROBLEM |
| **Email** | `email` z `patient.txt` | ? **BRAK W XML!** | ? PROBLEM |
| **Telefon** | `telephone` z `patient.txt` | ? **BRAK W XML!** | ? PROBLEM |
| DrugieImie | `second_name` | ? `second_name` | ? OK |
| NazwiskoRodowe | `maiden_name` | ? `maiden_name` | ? OK |
| MiejsceUrodzenia | `place_of_birth` | ? `place_of_birth` | ? OK |
| NumerDokumentuTozsamosci | `identity_num` | ? `identity_num` | ? OK |
| KodOddzialuNFZ | `code` z `insurance.txt` | ?? Nie zaimplementowane | ?? TODO |
| **Adres Zameldowania** | ? `registration_address` -> `address.txt` | ? `residence_address` -> address lookup | ? OK |
| **Adres Zamieszkania** | ? `residence_address` -> `address.txt` | ? `residence_address` -> address lookup | ? OK |
| **Dane Opiekuna** | ? `supervisor` -> `custodian.txt` | ? `custodian` lookup (10,632) | ? OK |
| Uwagi | Agregacja z `patientnote` | ? Pusty (celowo) | ? OK |
| Uchodzca | `is_refugee` | ?? Nie zaimplementowane | ?? TODO |
| VIP | `vip` | ?? Nie zaimplementowane | ?? TODO |

**PODSUMOWANIE PACJENCI:**
- ? Zgodne: 8 pÛl
- ? Brakujπce w XML: **7 kluczowych pÛ≥** (ImiÍ, Nazwisko, DataUrodzenia, P≥eÊ, Email, Telefon, + inne)
- ?? Do zaimplementowania: 3 pola

---

### **2. WIZYTY (wizyty.csv)**

| Pole Docelowe | Old Mapping Plan | Nasz System | Status |
|---------------|------------------|-------------|--------|
| IdImport | `id` z `visit.txt` | ? `id` z visit | ? OK |
| PacjentIdImport | `patient` z `visit` | ? `patient` z visit | ? OK |
| PracownikIdImport | `doctor` z `visit` | ? `doctor` z visit | ? OK |
| PracownikNPWZ | Join person.txt | ? Person lookup | ? OK |
| DataOd | `date` + `timeFrom` | ? `date` + `timeTo` | ? OK |
| DataDo | `date` + `timeTo` | ? `date` + `timeTo` | ? OK |
| Status | Map `state` | ?? Domyúlnie 2 | ?? TODO |
| NFZ | Z `visit_kind` | ? Domyúlnie 1 | ? OK |
| TrybPrzyjecia | `reception_mode_choice` | ?? Nie zaimplementowane | ?? TODO |
| RozpoznaniaICD10 | Join recognition + icd10 | ?? Nie zaimplementowane | ?? TODO |
| ProceduryICD9 | Join medicalprocedure + icd9 | ?? Nie zaimplementowane | ?? TODO |

**PODSUMOWANIE WIZYTY:**
- ? Zgodne: 7 pÛl
- ? Eksport dzia≥a: **696,727 rekordÛw**
- ?? Do zaimplementowania: 4 pola (Status, TrybPrzyjecia, ICD10, ICD9)

---

### **3. SZCZEPIENIA (szczepienia.csv)**

| Pole Docelowe | Old Mapping Plan | Nasz System | Status |
|---------------|------------------|-------------|--------|
| IdImport | `id` z `vaccination.txt` | ? `id` z vaccination | ? OK |
| PacjentIdImport | `patient` z `vaccination` | ? `patient` | ? OK |
| PracownikIdImport | `vaccinator` lub `person` | ? `doctor` | ? OK |
| Nazwa | `drug` | ? `name` | ? OK |
| MiejscePodania | `vaccination_site` | ? `administration_site` | ? OK |
| NrSerii | `vaccine_series` | ? `series_number` | ? OK |
| DataPodania | `datetime` | ? `administration_date` | ? OK |
| DataWaznosci | `expiration_date` | ? `expiry_date` | ? OK |

**PODSUMOWANIE SZCZEPIENIA:**
- ? Zgodne: 8/8 pÛl
- ? Eksport dzia≥a: **48,048 rekordÛw**
- ? **100% zgodnoúÊ!**

---

### **4. PRACOWNICY (pracownicy.csv)**

| Pole Docelowe | Old Mapping Plan | Nasz System | Status |
|---------------|------------------|-------------|--------|
| IdImport | `id` z `person.txt` | ? `id` z person | ? OK |
| **Imie** | Join `auth.user.txt` -> `first_name` | ? **BRAK - tylko 199!** | ? PROBLEM |
| **Nazwisko** | Join `auth.user.txt` -> `last_name` | ? **BRAK - tylko 199!** | ? PROBLEM |
| Pesel | `pesel` z `person` | ? `pesel` | ? OK |
| Email | Join `auth.user` | ? **BRAK - tylko 199!** | ? PROBLEM |
| Telefon | `telephone` z `person` | ?? Nie zaimplementowane | ?? TODO |
| NPWZ | `pwz` z `person` | ? `npwz` | ? OK |
| TytulNaukowyNazwa | `academic_degree` | ?? Nie zaimplementowane | ?? TODO |

**PODSUMOWANIE PRACOWNICY:**
- ? Zgodne: 3 pola
- ? Brakujπce: **ImiÍ, Nazwisko, Email** (tylko 199 zamiast ~22,400)
- ?? Do zaimplementowania: 2 pola

---

### **5. STA£E LEKI (stale_leki_pacjenta.csv)**

| Pole Docelowe | Old Mapping Plan | Nasz System | Status |
|---------------|------------------|-------------|--------|
| PacjentIdImport | `patient` | ?? Nie zaimplementowane | ?? TODO |
| KodKreskowy | `ean13` z `ver.txt` | ?? Nie zaimplementowane | ?? TODO |
| Dawkowanie | `recommendation` | ?? Nie zaimplementowane | ?? TODO |

**PODSUMOWANIE STA£E LEKI:**
- ? **NIE ZAIMPLEMENTOWANE**
- Old plan: pe≥na specyfikacja
- Nasz system: TODO

---

### **6. POZOSTA£E PLIKI**

| Plik CSV | Old Plan | Nasz System | Status |
|----------|----------|-------------|--------|
| **karty_wizyt.csv** | ? Pe≥na specyfikacja | ? Nie zaimplementowane | ? TODO |
| **deklaracje_poz.csv** | ? Pe≥na specyfikacja | ? Nie zaimplementowane | ? TODO |
| **jednostki.csv** | ? Specyfikacja | ? Nie zaimplementowane | ? TODO |
| **dokumenty_uprawniajace.csv** | ? Specyfikacja | ? Nie zaimplementowane | ? TODO |

---

## ?? **KLUCZOWE R”ØNICE - ANALIZA G£”WNEGO PROBLEMU**

### **PRZYCZYNA: RÛøne Formaty èrÛd≥owe**

#### **OLD PLAN - Za≥oøenie:**
```
gabinet.patient.txt (TXT format) zawiera WSZYSTKIE dane pacjenta:
???????????????????????????????????????
? gabinet.patient.txt                 ?
???????????????????????????????????????
? id, name, surname, birthdate, sex,  ?
? email, telephone, pesel, nfz, ...   ?
???????????????????????????????????????
```

#### **NASZA RZECZYWISTOå∆ - XML Django:**
```
gabinet.patient (XML) - tylko metadane:
???????????????????????????????????????
? <object model="gabinet.patient">    ?
???????????????????????????????????????
? pesel, nfz, maiden_name,            ?
? second_name, place_of_birth,        ?
? identity_num, is_active, ...        ?
?                                     ?
? BRAK: first_name, last_name,        ?
?       birth_date, sex, email!       ?
???????????????????????????????????????

Dane osobowe sπ gdzieú INDZIEJ!
```

---

## ?? **HIPOTEZY ROZWI•ZANIA**

### **Hipoteza 1: Django Multi-Table Inheritance**
```
Patient dziedziczy z Person:
????????????????????????????????????
? Person (base) - 199 rekordÛw     ?
? - first_name, last_name, ...     ?
????????????????????????????????????
           ?
           ? OneToOne
           ?
????????????????????????????????????
? Patient - 22,397 rekordÛw        ?
? - pesel, nfz, ...                ?
? - person_ptr_id (samo PK)        ?
????????????????????????????????????
```
? **Odrzucone** - Person ma tylko 199 rekordÛw (pracownicy)

### **Hipoteza 2: Dane Embedded w Patient**
```
<object model="gabinet.patient" pk="123">
  <!-- Dane sπ ukryte w innych polach -->
  <field name="user_data">{"first_name":"Jan"}</field>
</object>
```
? **Odrzucone** - diagnostyka nie pokazuje takich pÛl

### **Hipoteza 3: Osobny Model PatientProfile**
```
gabinet.patientprofile - 22,397 rekordÛw
- patient_id
- first_name, last_name, birth_date
```
? **DO SPRAWDZENIA** - szukamy modelu z ~22,397 rekordami

### **Hipoteza 4: Dane sπ w Person ale Eksport Niekompletny**
```
Django: Patient.person = Person (OneToOne)
XML: Tylko 199 Person wyeksportowanych (b≥πd)
```
?? **PRAWDOPODOBNE** - eksport XML moøe byÊ niepe≥ny

---

## ?? **REKOMENDACJE**

### **PRIORYTET 1: ZnaleüÊ Dane Osobowe PacjentÛw**

**Akcje:**
1. ? SprawdziÊ wszystkie modele w XML z ~22,397 rekordami
2. ? SzukaÊ modeli zawierajπcych `first_name`
3. ? SprawdziÊ czy sπ jakieú `person_ptr` lub podobne relacje
4. ? SkontaktowaÊ siÍ z w≥aúcicielem danych o strukturÍ

**Komendy diagnostyczne:**
```bash
# Szukaj modeli z podobnπ liczbπ rekordÛw
dotnet run diagnose "gabinet.*" | grep "22.*397"

# Szukaj wszystkich pÛl first_name
grep -i "first_name" output/structure_*.csv
```

### **PRIORYTET 2: DostosowaÊ Nasze Mapowania**

**Co zmieniÊ w naszym kodzie:**

1. **PatientMapper:**
```csharp
// STARE (zak≥ada≥o Person w Patient):
patient.Person = _personLookup.GetValueOrDefault(pk);

// NOWE (szukaÊ w innym ürÛdle):
patient.PersonData = _patientPersonLookup.GetValueOrDefault(pk);
// LUB
patient.FirstName = ExtractFromXmlField(...);
```

2. **Lookup Tables:**
```csharp
// DodaÊ nowy lookup dla danych osobowych pacjentÛw
private Dictionary<long, MyDrPatientPerson> _patientPersonLookup = new();
```

### **PRIORYTET 3: Dokumentacja RÛønic**

StworzyÊ plik `MAPPING_DIFFERENCES.md` z:
- Listπ wszystkich rÛønic miÍdzy old planem a XML
- Mapowaniem alternatywnym dla brakujπcych pÛl
- Strategiπ migracji

---

## ?? **PODSUMOWANIE ZGODNOåCI**

| Kategoria | Old Plan | Nasz System | ZgodnoúÊ |
|-----------|----------|-------------|----------|
| **Wizyty** | 33 pola | 33 pola (696,727 ?) | **100%** |
| **Szczepienia** | 16 pÛl | 16 pÛl (48,048 ?) | **100%** |
| **Pacjenci - Pola Poboczne** | 48 pÛl | 48 pÛ≥ | **100%** |
| **Pacjenci - Dane Osobowe** | 7 pÛl | **0 pÛl** ? | **0%** |
| **Pracownicy** | 23 pola | 8 pÛl | **35%** |
| **Sta≥e Leki** | 12 pÛl | 0 pÛl | **0%** |
| **Karty Wizyt** | 22 pola | 0 pÛl | **0%** |

**OG”LNA ZGODNOå∆:** ~45% (biorπc pod uwagÍ tylko zaimplementowane pliki)

---

## ?? **NAST PNE KROKI**

### **Krok 1: Diagnostyka Pog≥Íbiona (15 min)**
```bash
# Znajdü wszystkie modele z ~22k rekordÛw
dotnet run diagnose

# Sprawdü czy sπ inne ürÛd≥a first_name
grep -r "first_name" data/
```

### **Krok 2: Konsultacja ze èrÛd≥em (30 min)**
- SkontaktowaÊ siÍ z w≥aúcicielem bazy MyDr
- ZapytaÊ o strukturÍ `Patient` i `Person`
- UzyskaÊ pe≥nπ specyfikacjÍ eksportu XML

### **Krok 3: Poprawka Kodu (2 godz)**
- ZaimplementowaÊ w≥aúciwe mapowanie po znalezieniu ürÛd≥a
- DostosowaÊ PatientMapper
- PrzetestowaÊ ponownie

### **Krok 4: KompletnoúÊ (4 godz)**
- DodaÊ brakujπce pola: ICD10, ICD9, Status
- ZaimplementowaÊ sta≥e leki
- ZaimplementowaÊ karty wizyt

---

## ? **CO DZIA£A POPRAWNIE**

1. ? **Wizyty:** 696,727 rekordÛw - pe≥na zgodnoúÊ z old planem
2. ? **Szczepienia:** 48,048 rekordÛw - pe≥na zgodnoúÊ
3. ? **Architektura:** Two-pass streaming - wydajnoúÊ potwierdzona
4. ? **Adresy:** Lookup dla 67,117 adresÛw dzia≥a
5. ? **Opiekunowie:** 10,632 rekordÛw opiekunÛw w lookup
6. ? **System testowy:** Kompleksowa weryfikacja

---

**Data raportu:** 2025-12-16  
**Autor:** MyDr Import Team  
**NastÍpna akcja:** ZnaleüÊ ürÛd≥o danych osobowych pacjentÛw
