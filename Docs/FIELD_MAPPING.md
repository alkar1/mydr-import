# Mapowanie PÛl: MyDrEDM (XML) ? Optimed (CSV)

## Informacje o Systemach

- **System ürÛd≥owy:** MyDrEDM (Django XML export)
- **System docelowy:** Optimed (CSV import)
- **Rozmiar danych:** 8.5 GB XML, ~8.1M rekordÛw

---

## 1. PACJENCI (gabinet.patient ? pacjenci.csv)

### èrÛd≥o: gabinet.patient (22,397 rekordÛw)

| Pole XML (MyDrEDM) | Pole CSV (Optimed) | Typ Optimed | Transformacja | Wymagane |
|-------------------|-------------------|-------------|---------------|----------|
| pk | IdImport | Liczba ca≥kowita | Direct | TAK |
| - | InstalacjaId | Liczba ca≥kowita | Sta≥a: 1 | TAK |
| - | UprawnieniePacjentaId | Liczba ca≥kowita | NULL | NIE |
| - | RodzajPacjenta | Liczba ca≥kowita | Sta≥a: 1 | TAK |
| person.first_name | Imie | Tekst | Direct | TAK |
| person.last_name | Nazwisko | Tekst | Direct | TAK |
| pesel | Pesel | Tekst | Direct (11 znakÛw) | NIE* |
| person.birth_date | DataUrodzenia | Data i czas | Parse Date | TAK |
| dead.is_dead | CzyUmarl | Tak/Nie | Boolean ? 1/0 | TAK |
| dead.death_date | DataZgonu | Data i czas | Parse Date | Warunek |
| second_name | DrugieImie | Tekst | Direct | NIE |
| maiden_name | NazwiskoRodowe | Tekst | Direct | NIE |
| - | ImieOjca | Tekst | NULL | NIE |
| - | NIP | Tekst | Direct (10 cyfr) | NIE |
| person.sex | Plec | Znak | M/K/NULL | TAK |
| person.email | Email | Tekst | Direct | NIE |
| person.phone | Telefon | Tekst | Direct | NIE |
| second_telephone | TelefonDodatkowy | Tekst | Direct | NIE |
| identity_num | NumerDokumentuTozsamosci | Tekst | Direct | NIE |
| - | TypDokumentuTozsamosci | Tekst | NULL | NIE |
| country | KrajDokumentuTozsamosciKod | Tekst | 2-letter code | NIE |
| - | NrIdentyfikacyjnyUe | Tekst | NULL | NIE |
| place_of_birth | MiejsceUrodzenia | Tekst | Direct | NIE |
| nfz | KodOddzialuNFZ | Tekst | 2-letter code | NIE |
| residence_address.country | KrajZamieszkanie | Tekst | Default: "PL" | NIE |
| residence_address.voivodeship | WojewodztwoZamieszkanie | Tekst | Map voivodeship | NIE |
| residence_address.commune_teryt | KodTerytGminyZamieszkanie | Tekst | TERYT code | NIE |
| residence_address.city | MiejscowoscZamieszkanie | Tekst | Direct | NIE |
| residence_address.city_teryt | KodMiejscowosciZamieszkanie | Tekst | TERYT code | NIE |
| residence_address.postal_code | KodPocztowyZamieszkanie | Tekst | XX-XXX format | NIE |
| residence_address.street | UlicaZamieszkanie | Tekst | Direct | NIE |
| residence_address.house_number | NrDomuZamieszkanie | Tekst | Direct | NIE |
| residence_address.apartment_number | NrMieszkaniaZamieszkanie | Tekst | Direct | NIE |
| residence_address.district | DzielnicaZamieszkanie | Tekst | Direct | NIE |
| - | Uwagi | Tekst | Aggregate notes | NIE |
| - | Uchodzca | Tak/Nie | Default: 0 | NIE |
| - | VIP | Tak/Nie | Default: 0 | NIE |
| - | UprawnieniePacjenta | Tekst | NULL | NIE |

**Uwagi:**
- *PESEL niewymagany dla noworodkÛw, zagranicznych, NN
- Adres zameldowania = adres zamieszkania (jeúli brak employer_address)
- Relacje: OneToOneRel do gabinet.address, gabinet.patientdead

---

## 2. WIZYTY (gabinet.visit ? wizyty.csv)

### èrÛd≥o: gabinet.visit (696,727 rekordÛw)

| Pole XML (MyDrEDM) | Pole CSV (Optimed) | Typ Optimed | Transformacja | Wymagane |
|-------------------|-------------------|-------------|---------------|----------|
| pk | IdImport | Liczba ca≥kowita | Direct | TAK |
| - | InstalacjaId | Liczba ca≥kowita | Sta≥a: 1 | NIE |
| - | JednostkaId | Liczba ca≥kowita | Map from office | NIE |
| office | JednostkaIdImport | Liczba ca≥kowita | Direct | NIE |
| - | PacjentId | Liczba ca≥kowita | NULL (uøyj Import) | NIE |
| patient | PacjentIdImport | Liczba ca≥kowita | Direct FK | TAK |
| patient.pesel | PacjentPesel | Tekst | Lookup | NIE |
| - | PracownikId | Liczba ca≥kowita | NULL | NIE |
| doctor | PracownikIdImport | Liczba ca≥kowita | Direct FK | TAK |
| - | ZasobIdImport | Liczba ca≥kowita | NULL | NIE |
| doctor.npwz | PracownikNPWZ | Tekst | Lookup | NIE |
| doctor.pesel | PracownikPesel | Tekst | Lookup | NIE |
| - | PlatnikIdImportu | Liczba ca≥kowita | NULL | NIE |
| - | JednostkaRozliczeniowaId | Liczba ca≥kowita | NULL | NIE |
| - | JednostkaRozliczeniowaIdImportu | Liczba ca≥kowita | NULL | NIE |
| last_revision | DataUtworzenia | Data i czas | Parse DateTime | NIE |
| date | DataOd | Data i czas | Combine date+timeTo | TAK |
| date | DataDo | Data i czas | Same as DataOd | NIE |
| timeTo | CzasOd | Tekst | HH:MM format | NIE |
| timeTo | CzasDo | Tekst | +30 min default | NIE |
| - | Status | Liczba ca≥kowita | Map status | TAK |
| - | NFZ | Tak/Nie | Default: 1 | TAK |
| - | NieRozliczaj | Tak/Nie | Default: 0 | NIE |
| - | Dodatkowy | Tak/Nie | Default: 0 | NIE |
| interview | Komentarz | Tekst | Max 4000 chars | NIE |
| - | TrybPrzyjecia | Liczba ca≥kowita | NULL | NIE |
| - | TrybDalszegoLeczenia | Liczba ca≥kowita | NULL | NIE |
| - | TypWizyty | Liczba ca≥kowita | NULL | NIE |
| - | KodSwiadczeniaNFZ | Tekst | NULL | NIE |
| - | KodUprawnieniaPacjenta | Tekst | NULL | NIE |
| medicalprocedure (rel) | ProceduryICD9 | Tekst | Join ICD9 codes | NIE |
| recognition (rel) | RozpoznaniaICD10 | Tekst | Join ICD10 codes | NIE |
| - | DokumentSkierowujacyIdImportu | Liczba ca≥kowita | NULL | NIE |

**Uwagi:**
- Status wizyty: 1=Zaplanowana, 2=Odbyta, 3=Anulowana
- Relacje: ManyToOneRel do gabinet.patient, gabinet.person (doctor)
- ICD9/ICD10: Pobierz z tabel powiπzanych przez visit FK

---

## 3. SZCZEPIENIA (gabinet.vaccination ? szczepienia.csv)

### èrÛd≥o: gabinet.vaccination (51,588 rekordÛw)

| Pole XML (MyDrEDM) | Pole CSV (Optimed) | Typ Optimed | Transformacja | Wymagane |
|-------------------|-------------------|-------------|---------------|----------|
| pk | IdImport | Liczba ca≥kowita | Direct | TAK |
| - | InstalacjaId | Liczba ca≥kowita | Sta≥a: 1 | NIE |
| patient | PacjentIdImport | Liczba ca≥kowita | Direct FK | TAK |
| patient.pesel | PacjentPesel | Tekst | Lookup | NIE |
| doctor | PracownikIdImport | Liczba ca≥kowita | Direct FK | NIE |
| doctor.npwz | PracownikNPWZ | Tekst | Lookup | NIE |
| doctor.pesel | PracownikPesel | Tekst | Lookup | NIE |
| name | Nazwa | Tekst | Direct | TAK |
| administration_site | MiejscePodania | Tekst | Direct | NIE |
| series_number | NrSerii | Tekst | Direct | NIE |
| administration_date | DataPodania | Data i czas | Parse Date | TAK |
| expiry_date | DataWaznosci | Data i czas | Parse Date | NIE |
| administration_route | DrogaPodaniaId | Liczba ca≥kowita | Map route | NIE |
| is_from_calendar | CzyZKalendarza | Tak/Nie | Boolean ? 1/0 | NIE |
| vaccine_id | SzczepienieId | Liczba ca≥kowita | Direct | NIE |
| dose | Dawka | Liczba ca≥kowita | Direct | NIE |

---

## 4. STA£E CHOROBY (gabinet.recognition ? stale_choroby_pacjenta.csv)

### èrÛd≥o: gabinet.recognition (992,503 rekordÛw - FILTRUJ tylko sta≥e!)

| Pole XML (MyDrEDM) | Pole CSV (Optimed) | Typ Optimed | Transformacja | Wymagane |
|-------------------|-------------------|-------------|---------------|----------|
| - | InstalacjaId | Liczba ca≥kowita | Sta≥a: 1 | NIE |
| patient | PacjentId | Liczba ca≥kowita | NULL | NIE |
| patient | PacjentIdImport | Liczba ca≥kowita | Direct FK | TAK |
| icd10.code | ICD10 | Tekst | Direct (5 chars) | TAK |
| pk | NumerChoroby | Tekst | Convert to string | NIE |
| icd10.descr | Opis | Tekst | Description | NIE |

**UWAGA:** Filtruj tylko rozpoznania oznaczone jako "sta≥e" (chronic) lub bez powiπzania z wizytπ!

---

## 5. STA£E LEKI (gabinet.patientpermanentdrug ? stale_leki_pacjenta.csv)

### èrÛd≥o: gabinet.patientpermanentdrug (liczba rekordÛw: TBD)

| Pole XML (MyDrEDM) | Pole CSV (Optomed) | Typ Optimed | Transformacja | Wymagane |
|-------------------|-------------------|-------------|---------------|----------|
| - | InstalacjaId | Liczba ca≥kowita | Sta≥a: 1 | NIE |
| patient | PacjentId | Liczba ca≥kowita | NULL | NIE |
| patient | PacjentIdImport | Liczba ca≥kowita | Direct FK | TAK |
| doctor | PracownikId | Liczba ca≥kowita | NULL | NIE |
| doctor | PracownikIdImport | Liczba ca≥kowita | Direct FK | NIE |
| drug.ean | KodKreskowy | Tekst | EAN-13 | NIE |
| start_date | DataZalecenia | Data i czas | Parse Date | NIE |
| end_date | DataZakonczenia | Data i czas | Parse Date | NIE |
| dosage | Dawkowanie | Tekst | Direct | NIE |
| quantity | Ilosc | Tekst | Direct | NIE |
| quantity_type | RodzajIlosci | Enum | Map type | NIE |
| payment_code | KodOdplatnosci | Tekst | Direct | NIE |

---

## 6. PRACOWNICY (gabinet.person + auth.user ? pracownicy.xls)

### èrÛd≥o: gabinet.person (162 rekordÛw) + auth.user (127 rekordÛw)

| Pole XML (MyDrEDM) | Pole CSV (Optimed) | Typ Optimed | Transformacja | Wymagane |
|-------------------|-------------------|-------------|---------------|----------|
| pk | IdImport | Liczba ca≥kowita | Direct | TAK |
| - | InstalacjaId | Liczba ca≥kowita | Sta≥a: 1 | NIE |
| first_name | Imie | Tekst | Direct | TAK |
| last_name | Nazwisko | Tekst | Direct | TAK |
| pesel | Pesel | Tekst | Direct (11) | NIE |
| email | Email | Tekst | Direct | NIE |
| phone | Telefon | Tekst | Direct | NIE |
| npwz | NPWZ | Tekst | Direct | NIE |
| academic_title | TytulNaukowyId | Liczba ca≥kowita | Map title | NIE |
| academic_title | TytulNaukowyNazwa | Tekst | Title name | NIE |
| personnel_type | TypPersoneluId | Liczba ca≥kowita | Map type | NIE |
| personnel_type_nfz | TypPersoneluNFZ | Liczba ca≥kowita | Map NFZ | NIE |
| specialties | SpecjalizacjeIds | Tekst | Join IDs | NIE |
| is_chief | PersonelKierujacy | Tak/Nie | Boolean ? 1/0 | NIE |
| user.is_active | Konto | Tak/Nie | Boolean ? 1/0 | NIE |
| user.username | KontoLogin | Tekst | Direct | NIE |
| - | NieWymagajZmianyHasla | Tak/Nie | Default: 0 | NIE |
| is_nonmedical | PracownikNiemedyczny | Tak/Nie | Boolean ? 1/0 | NIE |
| - | SprawdzUnikalnoscPesel | Tak/Nie | Default: 1 | NIE |
| - | SprawdzUnikalnoscNpwz | Tak/Nie | Default: 1 | NIE |
| - | SprawdzUnikalnoscLoginu | Tak/Nie | Default: 1 | NIE |
| - | ZachowajIdentyfikator | Tak/Nie | Default: 1 | NIE |
| - | Usunieto | Tak/Nie | Default: 0 | NIE |

---

## Dodatkowe Pliki (TODO - jeúli potrzebne)

### 7. DEKLARACJE POZ
- èrÛd≥o: `gabinet.nfzdeclaration` (99,254 rekordÛw)

### 8. DOKUMENTY UPRAWNIE—
- èrÛd≥o: `gabinet.insurance` (38,374 rekordÛw) + `gabinet.insurancedocuments`

### 9. SKIEROWANIA
- èrÛd≥o: `gabinet.documents` (187,254 rekordÛw) - filtr typ skierowania

### 10. KARTY WIZYT (szczegÛ≥y)
- èrÛd≥o: `gabinet.visitnotes` (913,366 rekordÛw)

### 11. WYNIKI BADA— LABORATORYJNYCH
- èrÛd≥o: `gabinet.genericmedicaldata` (potencjalnie)

### 12. JEDNOSTKI / GABINETY
- èrÛd≥o: `gabinet.office` (10 rekordÛw)
- Cel: `office.xlsx`

### 13. ODDZIA£Y
- èrÛd≥o: `gabinet.department` (15 rekordÛw)
- Cel: `departments.xlsx`

---

## Regu≥y Transformacji

### Typy Danych

| Typ XML | Typ Optimed | Transformacja |
|---------|-------------|---------------|
| DateField | Data i czas | YYYY-MM-DD HH:MM:SS |
| DateTimeField | Data i czas | YYYY-MM-DD HH:MM:SS |
| TimeField | Tekst | HH:MM:SS |
| BooleanField | Tak/Nie | True?1, False?0 |
| NullBooleanField | Tak/Nie | True?1, False?0, None?NULL |
| CharField | Tekst | Direct (trim) |
| TextField | Tekst | Truncate if max length |
| IntegerField | Liczba ca≥kowita | Direct |
| BigIntegerField | Liczba ca≥kowita | Direct |
| DecimalField | Liczba dziesiÍtna | Direct |

### Relacje (Foreign Keys)

```
ManyToOneRel ? Pobierz PK powiπzanego rekordu
OneToOneRel ? Pobierz dane z powiπzanej tabeli
ManyToManyRel ? Join PKs separatorem (,)
```

### Wartoúci NULL/None

```xml
<field name="author"><None></None></field>
```
? CSV: puste pole lub NULL

### Znaki Specjalne w CSV

- Przecinek ? escape cudzys≥owami
- Nowa linia ? \n
- Cudzys≥Ûw ? ""

---

## Priorytet Implementacji

1. ? **PACJENCI** - podstawa systemu
2. ? **PRACOWNICY** - wymagane do wizyt
3. ? **WIZYTY** - g≥Ûwna funkcjonalnoúÊ
4. ? **SZCZEPIENIA** - dane medyczne
5. ? **STA£E CHOROBY** - historia pacjenta
6. ? **STA£E LEKI** - historia pacjenta
7. ? **Pozosta≥e** - wed≥ug potrzeb

---

## Walidacja Danych

### Kryteria Walidacji

- **PESEL:** 11 cyfr, checksum
- **Data urodzenia:** <= dzisiaj
- **Email:** format RFC 5322
- **Telefon:** 9-15 cyfr
- **Kod pocztowy:** XX-XXX
- **NPWZ:** 7 cyfr
- **ICD-10:** format A00.0
- **ICD-9:** format 00.00

### Obs≥uga B≥ÍdÛw

- B≥Ídy walidacji ? `errors.log`
- Rekord z b≥Ídem ? pomiÒ, ale loguj
- Krytyczne b≥Ídy ? zatrzymaj import
- Ostrzeøenia ? kontynuuj, ale oznacz

---

**Wersja:** 1.0  
**Data:** 2025-12-16  
**Status:** Draft - Etap 2
