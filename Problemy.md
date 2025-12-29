Na podstawie [compare_report.md](cci:7://file:///c:/Users/alfred/NC/PROJ/OPTIMED/MyDr_Import/compare_report.md:0:0-0:0) - pliki z polami poniżej 100%:

| Plik | Pola < 100% | Przyczyna |
|------|-------------|-----------|
| **stale_leki_pacjenta.csv** | `DataZalecenia` 0% | Brak pola `date_from` w XML |
| **dokumentacja_zalaczniki.csv** | `Opis` 0%, `TypPliku` 0%, `NazwaPliku` 14% | Pola nie eksportowane do XML |
| **karty_wizyt.csv** | `DataWystawienia` 21%, `Wywiad` 14%, `Zalecenia` 2%, `RozpoznaniaICD10` 0% | Dane medyczne w oddzielnym modelu XML (visitnotes) |
| **szczepienia.csv** | `Dawka` 57% | Pole `dose` częściowo wypełnione w źródle |
| **pacjenci.csv** | `ImieOpiekuna` 0% | Brak w eksporcie XML |
| **deklaracje_poz.csv** | `ProfilaktykaFluorkowa` 0% | Brak pola w źródle |
| **dokumenty_uprawniajace.csv** | `DataWystawienia` 0%, `KodInstytucjiWystawiajacej` 0% | Pola nie eksportowane |
| **wizyty.csv** | `ProceduryICD9` 0%, `RozpoznaniaICD10` 0% | Dane w oddzielnych modelach relacyjnych |

**Główne przyczyny braków:**
1. **Dane nie eksportowane** - niektóre pola nie są uwzględnione w eksporcie XML źródłowym
2. **Dane w relacjach** - wymagają dodatkowych join z innymi plikami XML (np. visitnotes, icd9procedures)
3. **Puste w źródle** - pola są w strukturze ale nie mają wartości w danych