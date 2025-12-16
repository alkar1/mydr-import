# PLAN NAPRAWY EKSPORTU CSV - SZCZEGÓ£OWY

**Status:** ?? W TRAKCIE  
**Utworzono:** 2024-12-16  
**Strategia:** Test-Fix-Verify cycle - ka¿dy plik osobno

---

## ?? STRATEGIA

Ka¿dy z 14 plików CSV bêdzie naprawiany i testowany sekwencyjnie:

1. **Zdiagnozowaæ** - uruchomiæ test i zidentyfikowaæ problemy
2. **Naprawiæ** - poprawiæ kod eksportu/mapowania
3. **Zweryfikowaæ** - uruchomiæ test ponownie
4. **Przejœæ dalej** - tylko gdy test = PASSED ?

---

## ?? KROKI PLANU (16 kroków)

### ?? FAZA 1: NAPRAWA ISTNIEJ¥CYCH EKSPORTÓW (Kroki 1-4)

#### 1?? Krok 1: pacjenci.csv
**Status:** PENDING  
**Priorytet:** NISKI (test prawie zaliczony)

**Problemy do naprawy:**
- [ ] 1 pacjent bez imienia
- [ ] 1 pacjent bez nazwiska  
- [ ] 1 nieprawid³owa data urodzenia

**Akcje:**
1. Dodaæ walidacjê danych w PatientMapper
2. Pomin¹æ/naprawiæ rekordy z brakuj¹cymi danymi
3. Uruchomiæ: `.\bin\Debug\net8.0\MyDr_Import.exe test`
4. Weryfikacja: PacjenciCsvTest = PASSED

**Pliki do modyfikacji:**
- Services/Mapping/PatientMapper.cs
- Services/CsvExportService.cs (sekcja ExportPatientsAsync)

---

#### 2?? Krok 2: pracownicy.csv
**Status:** PENDING  
**Priorytet:** KRYTYCZNY (plik pusty!)

**Problemy do naprawy:**
- [ ] Plik pusty lub nie generowany
- [ ] Brak osób z NPWZ lub b³¹d filtrowania

**Akcje:**
1. Zdiagnozowaæ: czy s¹ osoby z NPWZ w bazie?
2. Sprawdziæ logikê filtrowania w ExportEmployeesAsync
3. Naprawiæ warunek: `_personLookup.Values.Where(p => !string.IsNullOrEmpty(p.Npwz))`
4. Uruchomiæ test
5. Weryfikacja: PracownicyCsvTest = PASSED

**Pliki do modyfikacji:**
- Services/CsvExportService.cs (sekcja ExportEmployeesAsync)
- Services/Mapping/EmployeeMapper.cs

---

#### 3?? Krok 3: wizyty.csv
**Status:** PENDING  
**Priorytet:** KRYTYCZNY (61% b³êdnych dat!)

**Problemy do naprawy:**
- [ ] 427,306 rekordów z nieprawid³owym formatem daty (61%)
- [ ] Niezgodne nazwy kolumn: `Wywiad` vs `Komentarz`
- [ ] Niezgodne nazwy kolumn: `IdInstalacji` vs `InstalacjaId`

**Akcje:**
1. Naprawiæ format dat w VisitMapper.Map()
2. Ujednoliciæ nazwy kolumn w OptimedWizyta
3. Uruchomiæ test
4. Weryfikacja: WizytyCsvTest = PASSED

**Pliki do modyfikacji:**
- Services/Mapping/VisitMapper.cs
- Models/Target/OptimedModels.cs (OptimedWizyta)

---

#### 4?? Krok 4: szczepienia.csv
**Status:** PENDING  
**Priorytet:** KRYTYCZNY (wszystkie nazwy puste!)

**Problemy do naprawy:**
- [ ] Wszystkie 48,048 szczepieñ maj¹ pust¹ nazwê
- [ ] 419 duplikatów kluczy g³ównych
- [ ] Niezgodne nazwy kolumn: `NumerSerii` vs `NrSerii`
- [ ] Niezgodne nazwy kolumn: `IdInstalacji` vs `InstalacjaId`

**Akcje:**
1. Naprawiæ mapowanie nazwy w VaccinationMapper.Map()
2. Naprawiæ generowanie IdImport (duplikaty)
3. Ujednoliciæ nazwy kolumn w OptimedSzczepienie
4. Uruchomiæ test
5. Weryfikacja: SzczepienCsvTest = PASSED

**Pliki do modyfikacji:**
- Services/Mapping/VaccinationMapper.cs
- Models/Target/OptimedModels.cs (OptimedSzczepienie)

---

### ?? FAZA 2: IMPLEMENTACJA NOWYCH EKSPORTÓW (Kroki 5-14)

#### 5?? Krok 5: stale_choroby_pacjenta.csv
**Status:** PENDING  
**Priorytet:** WYSOKI

**Do zrobienia:**
- [ ] Utworzyæ ChronicDiseaseMapper.cs
- [ ] Utworzyæ OptimedChronicDisease model
- [ ] Dodaæ metodê ExportChronicDiseasesAsync
- [ ] Zintegrowaæ z CsvExportService.ExportAllAsync

---

#### 6?? Krok 6: stale_leki_pacjenta.csv
**Status:** PENDING  
**Priorytet:** WYSOKI

**Do zrobienia:**
- [ ] Utworzyæ PermanentDrugMapper.cs
- [ ] Utworzyæ OptimedPermanentDrug model
- [ ] Dodaæ metodê ExportPermanentDrugsAsync
- [ ] Zintegrowaæ z CsvExportService.ExportAllAsync

---

#### 7?? Krok 7: deklaracje_poz.csv
**Status:** PENDING  
**Priorytet:** ŒREDNI

**Do zrobienia:**
- [ ] Utworzyæ DeclarationMapper.cs
- [ ] Utworzyæ OptimedDeclaration model
- [ ] Dodaæ metodê ExportDeclarationsAsync
- [ ] Zintegrowaæ z CsvExportService.ExportAllAsync

---

#### 8?? Krok 8: dokumenty_uprawniajace.csv
**Status:** PENDING  
**Priorytet:** ŒREDNI

**Do zrobienia:**
- [ ] Utworzyæ DocumentMapper.cs
- [ ] Utworzyæ OptimedDocument model
- [ ] Dodaæ metodê ExportDocumentsAsync
- [ ] Zintegrowaæ z CsvExportService.ExportAllAsync

---

#### 9?? Krok 9: dokumentacja_zalaczniki.csv
**Status:** PENDING  
**Priorytet:** ŒREDNI

**Do zrobienia:**
- [ ] Utworzyæ AttachmentMapper.cs
- [ ] Utworzyæ OptimedAttachment model
- [ ] Dodaæ metodê ExportAttachmentsAsync
- [ ] Zintegrowaæ z CsvExportService.ExportAllAsync

---

#### ?? Krok 10: skierowania_wystawione.csv
**Status:** PENDING  
**Priorytet:** ŒREDNI

**Do zrobienia:**
- [ ] Utworzyæ ReferralMapper.cs
- [ ] Utworzyæ OptimedReferral model
- [ ] Dodaæ metodê ExportReferralsAsync
- [ ] Zintegrowaæ z CsvExportService.ExportAllAsync

---

#### 1??1?? Krok 11: karty_wizyt.csv
**Status:** PENDING  
**Priorytet:** ŒREDNI

**Do zrobienia:**
- [ ] Utworzyæ VisitCardMapper.cs
- [ ] Utworzyæ OptimedVisitCard model
- [ ] Dodaæ metodê ExportVisitCardsAsync
- [ ] Zintegrowaæ z CsvExportService.ExportAllAsync

---

#### 1??2?? Krok 12: wyniki_badan_laboratoryjnych_szczegoly.csv
**Status:** PENDING  
**Priorytet:** ŒREDNI

**Do zrobienia:**
- [ ] Utworzyæ LabResultMapper.cs
- [ ] Utworzyæ OptimedLabResult model
- [ ] Dodaæ metodê ExportLabResultsAsync
- [ ] Zintegrowaæ z CsvExportService.ExportAllAsync

---

#### 1??3?? Krok 13: departments.csv
**Status:** PENDING  
**Priorytet:** NISKI

**Do zrobienia:**
- [ ] Utworzyæ DepartmentMapper.cs
- [ ] Utworzyæ OptimedDepartment model
- [ ] Dodaæ metodê ExportDepartmentsAsync
- [ ] Zintegrowaæ z CsvExportService.ExportAllAsync

---

#### 1??4?? Krok 14: office.csv
**Status:** PENDING  
**Priorytet:** NISKI

**Do zrobienia:**
- [ ] Utworzyæ OfficeMapper.cs
- [ ] Utworzyæ OptimedOffice model
- [ ] Dodaæ metodê ExportOfficesAsync
- [ ] Zintegrowaæ z CsvExportService.ExportAllAsync

---

### ? FAZA 3: WERYFIKACJA FINALNA (Kroki 15-16)

#### 1??5?? Krok 15: Pe³ny test wszystkich 14 plików
**Status:** PENDING

**Akcje:**
1. Uruchomiæ: `.\bin\Debug\net8.0\MyDr_Import.exe test`
2. Sprawdziæ wynik: **14/14 testów PASSED**
3. Zweryfikowaæ raporty w `output_csv/test_reports/`
4. Aktualizowaæ dokumentacjê

**Oczekiwany wynik:**
```
???????????????????????????????????????????????????????????????
PODSUMOWANIE TESTÓW
???????????????????????????????????????????????????????????????
?? WSZYSTKIE TESTY ZALICZONE!
   ? Zaliczone: 14
   ? Niezaliczone: 0
   ?? B³êdy: 0
   ??  Ostrze¿enia: 0
```

---

#### 1??6?? Krok 16: Commit i Push
**Status:** PENDING

**Akcje:**
1. `git add .`
2. `git commit -m "Naprawa eksportu CSV - wszystkie 14 testów PASSED"`
3. `git push origin master`
4. Zaktualizowaæ dokumentacjê projektu

---

## ?? POSTÊP

| Faza | Kroki | Ukoñczono | Status |
|------|--------|-----------|--------|
| **Faza 1: Naprawa** | 1-4 | 0/4 | ?? 0% |
| **Faza 2: Implementacja** | 5-14 | 0/10 | ?? 0% |
| **Faza 3: Weryfikacja** | 15-16 | 0/2 | ?? 0% |
| **RAZEM** | **1-16** | **0/16** | **?? 0%** |

---

## ?? SUKCES = 14/14 TESTÓW PASSED

**Warunek zakoñczenia:** Wszystkie 14 testów musz¹ zwróciæ status PASSED

```bash
# Komenda testowa
.\bin\Debug\net8.0\MyDr_Import.exe test

# Oczekiwany wynik
? 1. pacjenci.csv: PASSED
? 2. pracownicy.csv: PASSED
? 3. wizyty.csv: PASSED
? 4. szczepienia.csv: PASSED
? 5. stale_choroby_pacjenta.csv: PASSED
? 6. stale_leki_pacjenta.csv: PASSED
? 7. deklaracje_poz.csv: PASSED
? 8. dokumenty_uprawniajace.csv: PASSED
? 9. dokumentacja_zalaczniki.csv: PASSED
? 10. skierowania_wystawione.csv: PASSED
? 11. karty_wizyt.csv: PASSED
? 12. wyniki_badan_laboratoryjnych_szczegoly.csv: PASSED
? 13. departments.csv: PASSED
? 14. office.csv: PASSED
```

---

**Plan utworzony:** 2024-12-16  
**Rozpoczêcie:** Oczekuje na start  
**Szacowany czas:** 8-12 godzin pracy
