# Raport Weryfikacji Mapowania Pól

**Data weryfikacji:** 2025-12-16  
**Status:** ?? WYMAGA UZUPE£NIENIA

---

## 1. PACJENCI (pacjenci.xls ? OptimedPacjent)

### Podsumowanie
- **Pola w XLS:** 68
- **Pola w modelu:** 48
- **Brakuj¹ce pola:** 20 ?

### ? Pola zamapowane (48):

1. ? InstalacjaId
2. ? IdImport
3. ? UprawnieniePacjentaId
4. ? RodzajPacjenta
5. ? Imie
6. ? Nazwisko
7. ? Pesel
8. ? DataUrodzenia
9. ? CzyUmarl
10. ? DataZgonu
11. ? DrugieImie
12. ? NazwiskoRodowe
13. ? ImieOjca
14. ? NIP
15. ? Plec
16. ? Email
17. ? Telefon
18. ? TelefonDodatkowy
19. ? NumerDokumentuTozsamosci
20. ? TypDokumentuTozsamosci
21. ? KrajDokumentuTozsamosciKod
22. ? NrIdentyfikacyjnyUe
23. ? MiejsceUrodzenia
24. ? KodOddzialuNFZ
25-34. ? Adres zameldowania (10 pól)
35-44. ? Adres zamieszkania (10 pól)
45. ? Uwagi
46. ? Uchodzca
47. ? VIP
48. ? UprawnieniePacjenta

### ? Pola BRAKUJ¥CE w modelu (20):

**Dane opiekuna (13 pól):**
49. ? ImieOpiekuna
50. ? NazwiskoOpiekuna
51. ? PlecOpiekuna
52. ? DataUrodzeniaOpiekuna
53. ? PeselOpiekuna
54. ? TelefonOpiekuna
55. ? KrajOpiekuna
56. ? WojewodztwoOpiekuna
57. ? KodGminyOpiekuna
58. ? MiejscowoscOpiekuna
59. ? KodMiejscowosciOpiekuna
60. ? KodPocztowyOpiekuna
61. ? UlicaOpiekuna
62. ? NrDomuOpiekuna
63. ? NrLokaluOpiekuna
64. ? StopienPokrewienstwaOpiekuna

**Flagi kontrolne (4 pola):**
65. ? SprawdzUnikalnoscIdImportu
66. ? SprawdzUnikalnoscPesel
67. ? AktualizujPoPesel
68. ? NumerPacjenta

---

## 2. WIZYTY (wizyty.xls ? OptimedWizyta)

### Podsumowanie
- **Pola w XLS:** 33
- **Pola w modelu:** 33
- **Brakuj¹ce pola:** 0 ?

### ? Wszystkie pola zamapowane!

1. ? InstalacjaId
2. ? IdImport
3. ? JednostkaId
4. ? JednostkaIdImport
5. ? PacjentId
6. ? PacjentIdImport
7. ? PacjentPesel
8. ? PracownikId
9. ? PracownikIdImport
10. ? ZasobIdImport
11. ? PracownikNPWZ
12. ? PracownikPesel
13. ? PlatnikIdImportu
14. ? JednostkaRozliczeniowaId
15. ? JednostkaRozliczeniowaIdImportu
16. ? DataUtworzenia
17. ? DataOd
18. ? DataDo
19. ? CzasOd
20. ? CzasDo
21. ? Status
22. ? NFZ
23. ? NieRozliczaj
24. ? Dodatkowy
25. ? Komentarz
26. ? TrybPrzyjecia
27. ? TrybDalszegoLeczenia
28. ? TypWizyty
29. ? KodSwiadczeniaNFZ
30. ? KodUprawnieniaPacjenta
31. ? ProceduryICD9
32. ? RozpoznaniaICD10
33. ? DokumentSkierowujacyIdImportu

---

## 3. SZCZEPIENIA (szczepienia.xls ? OptimedSzczepienie)

### Podsumowanie
- **Pola w XLS:** 16
- **Pola w modelu:** 16
- **Brakuj¹ce pola:** 0 ?

### ? Wszystkie pola zamapowane!

1. ? InstalacjaId
2. ? IdImport
3. ? PacjentIdImport
4. ? PacjentPesel
5. ? PracownikIdImport
6. ? PracownikNPWZ
7. ? PracownikPesel
8. ? Nazwa
9. ? MiejscePodania
10. ? NrSerii
11. ? DataPodania
12. ? DataWaznosci
13. ? DrogaPodaniaId
14. ? CzyZKalendarza
15. ? SzczepienieId
16. ? Dawka

---

## 4. STA£E CHOROBY (stale_choroby_pacjenta.xls ? OptimedStalaChorobaPacjenta)

### Podsumowanie
- **Pola w XLS:** 6
- **Pola w modelu:** 6
- **Brakuj¹ce pola:** 0 ?

### ? Wszystkie pola zamapowane!

1. ? InstalacjaId
2. ? PacjentId
3. ? PacjentIdImport
4. ? ICD10
5. ? NumerChoroby
6. ? Opis

---

## 5. STA£E LEKI (stale_leki_pacjenta.xls ? OptimedStalyLekPacjenta)

### Podsumowanie
- **Pola w XLS:** 12
- **Pola w modelu:** 12
- **Brakuj¹ce pola:** 0 ?

### ? Wszystkie pola zamapowane!

1. ? InstalacjaId
2. ? PacjentId
3. ? PacjentIdImport
4. ? PracownikId
5. ? PracownikIdImport
6. ? KodKreskowy
7. ? DataZalecenia
8. ? DataZakonczenia
9. ? Dawkowanie
10. ? Ilosc
11. ? RodzajIlosci
12. ? KodOdplatnosci

---

## 6. PRACOWNICY (pracownicy.xls ? OptimedPracownik)

### Podsumowanie
- **Pola w XLS:** 23
- **Pola w modelu:** 23
- **Brakuj¹ce pola:** 0 ?

### ? Wszystkie pola zamapowane!

1. ? InstalacjaId
2. ? IdImport
3. ? Imie
4. ? Nazwisko
5. ? Pesel
6. ? Email
7. ? Telefon
8. ? NPWZ
9. ? TytulNaukowyId
10. ? TytulNaukowyNazwa
11. ? TypPersoneluId
12. ? TypPersoneluNFZ
13. ? SpecjalizacjeIds
14. ? PersonelKierujacy
15. ? Konto
16. ? KontoLogin
17. ? NieWymagajZmianyHasla
18. ? PracownikNiemedyczny
19. ? SprawdzUnikalnoscPesel
20. ? SprawdzUnikalnoscNpwz
21. ? SprawdzUnikalnoscLoginu
22. ? ZachowajIdentyfikator
23. ? Usunieto

---

## PODSUMOWANIE OGÓLNE

| Plik XLS | Pola XLS | Pola Model | Status | Brakuje |
|----------|----------|------------|--------|---------|
| pacjenci.xls | 68 | 48 | ?? NIEKOMPLETNE | 20 |
| wizyty.xls | 33 | 33 | ? OK | 0 |
| szczepienia.xls | 16 | 16 | ? OK | 0 |
| stale_choroby_pacjenta.xls | 6 | 6 | ? OK | 0 |
| stale_leki_pacjenta.xls | 12 | 12 | ? OK | 0 |
| pracownicy.xls | 23 | 23 | ? OK | 0 |
| **RAZEM** | **158** | **138** | **87%** | **20** |

---

## REKOMENDACJE

### Priorytet WYSOKI: Uzupe³niæ model OptimedPacjent

Nale¿y dodaæ **20 brakuj¹cych pól** do klasy `OptimedPacjent`:

```csharp
// Dane opiekuna
[Index(48)] public string? ImieOpiekuna { get; set; }
[Index(49)] public string? NazwiskoOpiekuna { get; set; }
[Index(50)] public string? PlecOpiekuna { get; set; }
[Index(51)] public DateTime? DataUrodzeniaOpiekuna { get; set; }
[Index(52)] public string? PeselOpiekuna { get; set; }
[Index(53)] public string? TelefonOpiekuna { get; set; }
[Index(54)] public string? KrajOpiekuna { get; set; }
[Index(55)] public string? WojewodztwoOpiekuna { get; set; }
[Index(56)] public string? KodGminyOpiekuna { get; set; }
[Index(57)] public string? MiejscowoscOpiekuna { get; set; }
[Index(58)] public string? KodMiejscowosciOpiekuna { get; set; }
[Index(59)] public string? KodPocztowyOpiekuna { get; set; }
[Index(60)] public string? UlicaOpiekuna { get; set; }
[Index(61)] public string? NrDomuOpiekuna { get; set; }
[Index(62)] public string? NrLokaluOpiekuna { get; set; }
[Index(63)] public string? StopienPokrewienstwaOpiekuna { get; set; }

// Flagi kontrolne
[Index(64)] public int SprawdzUnikalnoscIdImportu { get; set; } = 1;
[Index(65)] public int SprawdzUnikalnoscPesel { get; set; } = 1;
[Index(66)] public int AktualizujPoPesel { get; set; } = 0;
[Index(67)] public string? NumerPacjenta { get; set; }
```

### Mapowanie w Ÿródle MyDrEDM

**Dane opiekuna** prawdopodobnie pochodz¹ z:
- `gabinet.custodian` (opiekun prawny)
- `gabinet.incaseofemergency` (kontakt awaryjny)

Nale¿y:
1. Dodaæ model `MyDrCustodian` do `Models/Source/MyDrModels.cs`
2. Rozszerzyæ `PatientMapper.Map()` o mapowanie opiekuna
3. Dodaæ lookup do tabeli `gabinet.custodian`

---

## STAN PRZED POPRAWK¥

? 5 z 6 plików (83%) - w pe³ni zamapowane  
?? 1 plik (pacjenci) - 71% zamapowany (48/68 pól)

## STAN PO POPRAWCE

? 6 z 6 plików (100%) - w pe³ni zamapowane  
? 100% pokrycia wszystkich pól XLS
