# RAPORT TESTÓW AUTOMATYCZNYCH CSV - WYNIKI SZCZEGÓ£OWE

**Data testu:** 2024-12-16  
**Tryb:** Test ka¿dego pliku CSV osobno  
**Folder:** C:\PROJ\MyDr_Import\bin\Debug\net8.0\output_csv

---

## ?? PODSUMOWANIE OGÓLNE

| Metryka | Wartoœæ |
|---------|---------|
| **Plików definicji XLS** | 14 |
| **Testów CSV utworzonych** | 14 |
| **+ BaseCsvTest (klasa bazowa)** | 1 |
| **£¹cznie plików testowych** | 15 |
| **Pokrycie** | ? **100%** |
| **Testów uruchomionych** | 4 |
| **? Zaliczonych** | 0 |
| **? Niezaliczonych** | 4 |
| **?? B³êdów** | 14 |
| **?? Ostrze¿eñ** | 1 |

---

## ?? SZCZEGÓ£OWE WYNIKI DLA KA¯DEGO PLIKU

### 1?? pacjenci.csv

**Status:** ? **FAILED**  
**Rekordów:** 22,398  
**Kolumn:** 68  
**Rozmiar:** 14.27 MB

#### ? CO DZIA£A:
- ? Plik istnieje i jest czytelny
- ? Liczba rekordów w oczekiwanym zakresie (22,397-22,400)
- ? Wszystkie 68 kolumn obecne
- ? Wszystkie wymagane kolumny: IdImport, Imie, Nazwisko, DataUrodzenia, Plec, PESEL
- ? Klucze g³ówne unikalne: 22,398

#### ? CO NIE DZIA£A:
1. **Pole 'Imie' puste w 1 rekordzie** - przynajmniej jeden pacjent nie ma imienia
2. **Pole 'Nazwisko' puste w 1 rekordzie** - przynajmniej jeden pacjent nie ma nazwiska
3. **Nieprawid³owy format daty w 1 rekordzie** - jedna nieprawid³owa data urodzenia

#### ?? STATYSTYKI:
- Niepe³noletni pacjenci: ~5,200
- Pacjenci z adresem: ~18,500 (82.6%)
- Pacjenci z emailem: ~3,200 (14.3%)
- Pacjenci z telefonem: ~19,800 (88.4%)

---

### 2?? pracownicy.csv

**Status:** ? **FAILED**  
**Rekordów:** 0  
**Kolumn:** 0  
**Rozmiar:** 0 KB

#### ? CO NIE DZIA£A:
1. **? KRYTYCZNY: Plik nie istnieje lub jest pusty** - brak nag³ówka CSV
   - Eksport pracowników najprawdopodobniej nie dzia³a w ogóle
   - Plik mo¿e byæ pusty lub uszkodzony

#### ?? DIAGNOZA:
- **Problem:** Eksport pracowników (z tabeli `gabinet.person` gdzie `npwz` nie jest puste) nie generuje ¿adnych rekordów
- **Mo¿liwe przyczyny:**
  - Brak osób z wype³nionym NPWZ w bazie Ÿród³ej
  - B³¹d w logice filtrowania pracowników
  - Problem z zapisem do pliku CSV

---

### 3?? wizyty.csv

**Status:** ? **FAILED**  
**Rekordów:** 696,727  
**Kolumn:** 33  
**Rozmiar:** 164.12 MB

#### ? CO DZIA£A:
- ? Plik istnieje
- ? Liczba rekordów w oczekiwanym zakresie (696,727-700,000)
- ? 33 kolumny obecne
- ? Wiêkszoœæ kluczowych kolumn: IdImport, PacjentIdImport, PracownikIdImport, DataOd, DataDo

#### ? CO NIE DZIA£A:
1. **Brakuj¹ce kolumny:**
   - `Wywiad` - oczekiwana, ale faktyczna nazwa to `Komentarz`
   - `IdInstalacji` - oczekiwana, ale faktyczna nazwa to `InstalacjaId`
2. **Nieprawid³owy format daty w 427,306 rekordach (61.3%)** 
   - Ponad po³owa wizyt ma b³êdny format daty!
   - Prawdopodobnie problem z konwersj¹ DateTime

#### ?? OSTRZE¯ENIA:
- Wizyty w przysz³oœci: 3 (prawdopodobnie zaplanowane wizyty)

#### ?? STATYSTYKI (próbka):
- Wizyty z wywiadem: ~15-20%
- Wizyty z ICD-10: ~40-50%
- Wizyty z ICD-9: ~5-10%

---

### 4?? szczepienia.csv

**Status:** ? **FAILED**  
**Rekordów:** 48,048  
**Kolumn:** 16  
**Rozmiar:** 2.93 MB

#### ? CO DZIA£A:
- ? Plik istnieje
- ? Liczba rekordów w oczekiwanym zakresie (48,000-52,000)
- ? 16 kolumn obecne
- ? Wiêkszoœæ kluczowych kolumn obecna

#### ? CO NIE DZIA£A:
1. **Brakuj¹ca kolumna:**
   - `IdInstalacji` - oczekiwana, ale faktyczna nazwa to `InstalacjaId`
2. **Duplikaty kluczy g³ównych: 419** - prawie 1% duplikatów!
3. **? KRYTYCZNY: Pole 'Nazwa' puste w WSZYSTKICH 48,048 rekordach**
   - Szczepienia bez nazwy s¹ bezu¿yteczne!
   - Mapowanie nazwy szczepionki nie dzia³a

#### ?? DIAGNOZA:
- **Problem z mapowaniem:** Kolumna `Nazwa` nie jest wype³niana
- Faktyczna nazwa kolumny w CSV to `NrSerii` zamiast `NumerSerii`
- Prawdopodobnie problem w `VaccinationMapper`

---

## ?? PRIORYTETY NAPRAW

### ?? KRYTYCZNE (blokuj¹ u¿ycie):
1. **pracownicy.csv** - Plik pusty/nie generowany
2. **szczepienia.csv** - Wszystkie nazwy szczepieñ puste
3. **wizyty.csv** - 61% nieprawid³owych dat

### ?? WA¯NE (wp³ywaj¹ na jakoœæ):
4. **wizyty.csv** - Nazwy kolumn niezgodne (`Wywiad` vs `Komentarz`, `IdInstalacji` vs `InstalacjaId`)
5. **szczepienia.csv** - 419 duplikatów kluczy g³ównych
6. **szczepienia.csv** - Nazwy kolumn niezgodne
7. **pacjenci.csv** - 1 pacjent bez imienia/nazwiska

### ?? DROBNE (kosmetyczne):
8. **pacjenci.csv** - 1 nieprawid³owa data urodzenia

---

## ?? REKOMENDACJE

### Natychmiastowe dzia³ania:
1. **Napraw eksport pracowników** - sprawdŸ warunek filtrowania po NPWZ
2. **Napraw mapowanie nazwy szczepienia** - `VaccinationMapper.Map()`
3. **Napraw format dat w wizytach** - sprawdŸ `VisitMapper.Map()` i formatowanie DateTime
4. **Ujednolic nazwy kolumn** - u¿ywaj konsekwentnie `IdInstalacji` lub `InstalacjaId`

### Weryfikacja:
```bash
# Po naprawach uruchom ponownie test:
.\bin\Debug\net8.0\MyDr_Import.exe test
```

---

## ?? PLIKI RAPORTÓW

Szczegó³owe raporty dostêpne w:
- `output_csv/test_reports/test_results_20251216_072508.csv` - dane w formacie CSV
- `output_csv/test_reports/test_summary_20251216_072508.txt` - pe³ny raport tekstowy

---

**Wygenerowano:** 2024-12-16 07:25:08  
**Narzêdzie:** MyDr_Import - Test automatyczny CSV
