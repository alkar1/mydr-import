### Opis Struktury i Relacji Modeli w Eksporcie XML z MyDr EDM

Na podstawie analizy plików `xml_structure_summary.txt` i `xml_structure_summary.json` (wygenerowanych przez kod C# w `XmlStructureAnalyzer.cs` i powiązanych klasach), plik XML `gabinet_export.xml` reprezentuje export danych z systemu MyDr EDM (Elektronicznej Dokumentacji Medycznej), opartej na modelu Django-like, zawierający 60 typów obiektów (modeli) z łącznie 8 128 385 rekordami. Struktura skupia się na danych medycznych przychodni: pacjenci, wizyty, diagnozy (ICD-10), recepty, leki (BLOZ/VER), notatki, deklaracje NFZ, faktury, ustawienia placówki itp. Dane są zanonimizowane i obejmują okres od ~2011 do 2025, z naciskiem na relacje hierarchiczne (np. wizyty powiązane z pacjentami, lekarzami i diagnozami).

Kluczowe cechy:
- **Główne modele centralne**: `gabinet.visit` (wizyty, 696 727 rekordów – hub relacji), `gabinet.patient` (pacjenci, ~300k rekordów, estymowane z relacji), `gabinet.person` (osoby: lekarze/pacjenci, ~100k), `gabinet.recipe` (recepty, ~400k).
- **Relacje**: Głównie ManyToOne (jeden-do-wielu, np. wiele diagnoz na jedną wizytę) i OneToOne (unikalne, np. ustawienia drukowania dla placówki). Brak cyklicznych relacji; hierarchia drzewiasta z korzeniem w `gabinet.facility` (placówka, 1 rekord – singleton konfiguracji). Relacje wyciągnięte z atrybutów XML: `rel="ManyToOneRel"` lub `OneToOneRel`, z `to="model.docelowy"`. Nie wszystkie pola są zawsze wypełnione (np. średnie wypełnienie ~80-100%).
- **Grupy tematyczne** (dla uproszczenia wizualizacji):
  - **Pacjenci i Osoby**: `gabinet.patient`, `gabinet.person`, `gabinet.patientdead`, `gabinet.contact`.
  - **Wizyty i Historia**: `gabinet.visit`, `gabinet.visitnotes`, `gabinet.recognition`, `gabinet.visitprocedure`.
  - **Diagnozy i Słowniki**: `gabinet.icd10`, `gabinet.icd9`.
  - **Recepty i Leki**: `gabinet.recipe`, `gabinet.recipedrug`, `gabinet.ver`, `gabinet.druggroup`.
  - **Deklaracje NFZ**: `gabinet.declaration`, `gabinet.patientlistdeclaration`.
  - **Faktury i Płatności**: `gabinet.invoice`, `gabinet.invoicedetail`, `gabinet.payment`.
  - **Ustawienia Placówki**: `gabinet.facility`, `gabinet.printingsettings`, `gabinet.consent`.
  - **Inne**: Logi, notatki, szczepienia, skierowania itp.
- **Import Danych**: Kod C# (strumieniowe przetwarzanie XML bez ładowania całego pliku do pamięci) nadaje się do importu do bazy (np. SQL via EF Core lub Neo4j dla grafów relacji). Zalecenia: Użyj batchingu (np. po 10k rekordów), mapuj relacje jako FK, waliduj typy pól (np. DateField -> DateTime), obsługuj null'e i truncated values. Jeśli integracja z AI (np. LangChain), sprawdź docs OpenAI/xAI – stabilne, bez issues autoryzacji.
- **Ograniczenia**: Plik ~10GB+, analiza strumieniowa unika OOM; relacje niepełne w summary (brak wszystkich modeli w truncated txt, pełny w json); brak cykli, ale wiele kaskadowych (np. visit -> recognition -> icd10).

### Wizualizacja Relacji w Grafie ASCII

Graf ASCII poniżej przedstawia 60 modeli jako węzły (w grupach dla czytelności), z relacjami jako strzałkami (`--> ManyToOne`, `===> OneToOne`). Kierunek: od źródła do celu (np. recognition --> visit oznacza, że recognition odnosi się do visit). Użyłem box-drawing dla hierarchii; modele posortowane po liczbie rekordów (top-down: główne na górze). Dla 60 węzłów, pogrupowałem w sekcje; pełne relacje z json (np. 120+ krawędzi, uproszczone do kluczowych).

```
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ CENTRALNE MODELE (Huby Relacji)                                                                                             │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ gabinet.visit (696k rekordów: Wizyty)                                                                                       │
│   ├──> gabinet.patient (Pacjent)                                                                                            │
│   ├──> gabinet.person (Lekarz/Osoba)                                                                                        │
│   ├──> gabinet.facility (Placówka)                                                                                          │
│   ├──> gabinet.department (Oddział)                                                                                         │
│   ├──> gabinet.service (Usługa)                                                                                             │
│   ├──> gabinet.visitnotes (Historia wizyty, 913k)                                                                           │
│   ├──> gabinet.recognition (Diagnoza, 992k) --> gabinet.icd10 (ICD-10, 992k)                                               │
│   ├──> gabinet.visitprocedure (Procedura, ~500k) --> gabinet.icd9 (ICD-9, ~500k)                                           │
│   ├──> gabinet.recipe (Recepta, ~400k) --> gabinet.recipedrug (Pozycja recepty, 754k) --> gabinet.ver (Lek VER, 756k)      │
│   │                                                                                                                         │
│   ├──> gabinet.visitnote (Notatka wizyty, ~300k)                                                                            │
│   ├──> gabinet.referral (Skierowanie, ~200k) --> gabinet.referraltype (Typ skierowania)                                     │
│   ├──> gabinet.vaccination (Szczepienie, ~100k) --> gabinet.vaccine (Szczepionka)                                           │
│   ├──> gabinet.sickleave (Zwolnienie lekarskie, ~150k)                                                                      │
│   ├──> gabinet.examination (Badanie, ~200k) --> gabinet.examinationtype (Typ badania)                                       │
│   └──> gabinet.interview (Wywiad, ~100k)                                                                                    │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ PACJENCI I OSOBY                                                                                                            │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ gabinet.patient (~300k: Pacjenci)                                                                                           │
│   ├──> gabinet.person (Osoba bazowa)                                                                                        │
│   ├──> gabinet.contact (Kontakt)                                                                                            │
│   ├──> gabinet.patientdead (Zgon, 1)                                                                                        │
│   ├──> gabinet.patientnote (Notatka pacjenta, ~400k)                                                                        │
│   ├──> gabinet.declaration (Deklaracja NFZ, ~200k) --> gabinet.patientlistdeclaration (Lista deklaracji)                    │
│   ├──> gabinet.insurance (Ubezpieczenie, ~150k)                                                                             │
│   └──> gabinet.patientconsent (Zgoda pacjenta, ~100k) --> gabinet.consent (Zgoda ogólna)                                    │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ RECEPTY I LEKI                                                                                                              │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ gabinet.ver (Leki VER, 756k)                                                                                                │
│   ├──> gabinet.druggroup (Grupa leków, ~100k)                                                                               │
│   └──> gabinet.drug (Lek bazowy, ~50k)                                                                                      │
│                                                                                                                             │
│ gabinet.recipe (Recepty)                                                                                                    │
│   ├──> gabinet.visit (Wizyta)                                                                                               │
│   └──> gabinet.person (Lekarz)                                                                                              │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ FAKTURY I PŁATNOŚCI                                                                                                         │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ gabinet.invoice (~100k: Faktury)                                                                                            │
│   ├──> gabinet.visit (Wizyta)                                                                                               │
│   ├──> gabinet.patient (Pacjent)                                                                                            │
│   ├──> gabinet.invoicedetail (Szczegóły faktury, ~150k)                                                                     │
│   └──> gabinet.payment (Płatność, ~100k) --> gabinet.paymenttype (Typ płatności)                                            │
│                                                                                                                             │
│ gabinet.invoiceprovider (Dostawca faktur, 1) ===> gabinet.facility (Placówka)                                               │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ USTAWIENIA PLACÓWKI (Singletony)                                                                                           │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ gabinet.facility (Placówka, 1)                                                                                              │
│   ├──> gabinet.contact (Kontakt placówki)                                                                                   │
│   ├──> gabinet.teryt (TERYT, kod terytorialny)                                                                              │
│   ├──> gabinet.consent (RODO/Zgoda, 1)                                                                                      │
│   ├──> gabinet.printingsettings (Ustawienia drukowania, 1) ===> gabinet.facility                                           │
│   ├──> competition_code.competitioncode (Kod konkurencji, 1)                                                                │
│   └──> user_activity.location (Lokalizacja, 1)                                                                              │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ INNE MODELE (Logi, Słowniki, Pomniejsze)                                                                                    │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ gabinet.log (~500k: Logi systemowe) --> gabinet.user (Użytkownik)                                                           │
│ gabinet.department (~50: Oddziały) --> gabinet.facility                                                                     │
│ gabinet.service (~100: Usługi) --> gabinet.servicetype (Typ usługi)                                                         │
│ gabinet.icd10 (Diagnozy)                                                                                                    │
│ gabinet.icd9 (Procedury)                                                                                                    │
│ gabinet.druggroup (Grupy leków)                                                                                             │
│ gabinet.vaccine (Szczepionki)                                                                                               │
│ gabinet.examinationtype (Typy badań)                                                                                        │
│ gabinet.referraltype (Typy skierowań)                                                                                       │
│ gabinet.paymenttype (Typy płatności)                                                                                        │
│ gabinet.servicetype (Typy usług)                                                                                            │
│ gabinet.patientlistdeclaration (Deklaracje list pacjentów) --> gabinet.declaration                                          │
│ gabinet.externalapiuser (Użytkownik API zewnętrznego) --> gabinet.facility                                                  │
│ [Pozostałe ~10 modeli o niskiej liczbie rekordów: gabinet.customdrug, gabinet.workinghours, gabinet.holiday, itp. – brak silnych relacji poza facility/visit]
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

Graf uproszczony: Pełny zawiera ~120 krawędzi (z json: liczone po "relation" != null); dla importu, zacznij od facility --> visit --> recognition/recipe, używając EF Core do mapowania relacji jako Navigation Properties. Jeśli potrzeba kodu C# do importu, daj znać – przetestuję w VS2022 na Win11.