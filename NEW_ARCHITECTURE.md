# Nowa Architektura Migracji Danych

**Data:** 2026-01-04  
**Status:** Propozycja do implementacji

---

## Koncepcja

Nowa architektura rozdziela proces migracji na trzy niezależne etapy:

```
┌─────────────────┐     ┌─────────────────────┐     ┌────────────────┐     ┌──────────────┐
│   XML źródłowy  │────>│  CSV źródłowe       │────>│   MAPOWANIE    │────>│ CSV wynikowe │
│ (gabinet.xml)   │     │ (oryginalna struktura)│    │  (zewnętrzne)  │     │  (docelowe)  │
└─────────────────┘     └─────────────────────┘     └────────────────┘     └──────────────┘
      ETAP 1                   ETAP 2                    ETAP 3                ETAP 4
    (ekstrakcja)             (eksport)               (transformacja)         (wynik)
```

---

## Etapy procesu

### Etap 1: Ekstrakcja XML → Analiza struktury

**Wejście:** Duży plik XML (eksport Django)  
**Wyjście:** Raport struktury + osobne pliki XML per model

Istniejąca funkcjonalność z `XmlStructureAnalyzer`:
- Strumieniowe parsowanie XML
- Identyfikacja wszystkich modeli
- Zapis każdego modelu do osobnego pliku
- Raport JSON ze strukturą pól

```
data_etap1/
├── xml_structure_summary.json    # Struktura wszystkich modeli
├── data_full/
│   ├── gabinet_patient.xml       # Dane pacjentów
│   ├── gabinet_visit.xml         # Dane wizyt
│   ├── gabinet_person.xml        # Dane pracowników
│   └── ...
└── data_heads/                   # Próbki (pierwsze rekordy)
```

---

### Etap 2: XML → CSV źródłowe (1:1)

**Wejście:** Osobne pliki XML per model  
**Wyjście:** Pliki CSV z oryginalną strukturą pól

**Kluczowa zmiana:** Eksport "jak leci" - bez mapowania, zachowując:
- Oryginalne nazwy pól z XML
- Oryginalne wartości (bez transformacji)
- Wszystkie pola (nawet puste)
- Relacje jako FK (id referencji)

```
data_source_csv/
├── patient.csv          # Kolumny: pk, first_name, last_name, birth_date, ...
├── visit.csv            # Kolumny: pk, patient, doctor, date, status, ...
├── person.csv           # Kolumny: pk, first_name, last_name, pwz, ...
├── address.csv          # Kolumny: pk, patient, street, city, postal_code, ...
└── ...
```

**Format CSV źródłowego:**
- Separator: `;` (średnik)
- Kodowanie: UTF-8 BOM
- Nagłówki: oryginalne nazwy pól XML
- Puste wartości: puste string

---

### Etap 3: Mapowanie (zewnętrzne)

**Wejście:** CSV źródłowe + Definicja mapowań  
**Wyjście:** Instrukcje transformacji

Mapowanie realizowane **poza kodem** za pomocą:

#### Opcja A: Arkusz Excel/CSV z definicją mapowań
```
| Model źr. | Pole źr.    | Model doc. | Pole doc.       | Transformacja       |
|-----------|-------------|------------|-----------------|---------------------|
| patient   | first_name  | pacjenci   | Imie            | TRIM                |
| patient   | last_name   | pacjenci   | Nazwisko        | TRIM, UPPER         |
| patient   | birth_date  | pacjenci   | DataUrodzenia   | FORMAT:yyyy-MM-dd   |
| patient   | nfz         | pacjenci   | KodOddzialuNFZ  | LOOKUP:nfz_codes    |
| address   | street      | pacjenci   | UlicaZamieszkanie| JOIN:patient.pk    |
```

#### Opcja B: Narzędzie graficzne
- Wizualne łączenie pól źródłowych z docelowymi
- Podgląd danych w czasie rzeczywistym
- Walidacja typów i wartości

#### Opcja C: Skrypt transformacji (Python/SQL)
```python
# mapping_pacjenci.py
def transform(source_df):
    result = pd.DataFrame()
    result['Imie'] = source_df['first_name'].str.strip()
    result['Nazwisko'] = source_df['last_name'].str.strip().str.upper()
    result['DataUrodzenia'] = pd.to_datetime(source_df['birth_date'])
    # JOIN z adresami
    addresses = pd.read_csv('address.csv')
    ...
    return result
```

---

### Etap 4: Generowanie CSV wynikowych

**Wejście:** CSV źródłowe + Mapowania  
**Wyjście:** CSV w formacie docelowym

```
data_result_csv/
├── pacjenci.csv         # Format zgodny z systemem docelowym
├── wizyty.csv
├── pracownicy.csv
└── ...
```

---

## Zalety nowej architektury

### 1. Rozdzielenie odpowiedzialności
- **Ekstrakcja:** Techniczne parsowanie XML (stabilne)
- **Mapowanie:** Logika biznesowa (zmienne)
- **Transformacja:** Wykonanie mapowań (automatyczne)

### 2. Łatwiejsze debugowanie
- CSV źródłowe można przeglądać w Excel
- Łatwa walidacja danych przed mapowaniem
- Możliwość porównania źródło vs wynik

### 3. Iteracyjne dostosowywanie
- Zmiana mapowań bez rekompilacji kodu
- Szybkie testowanie różnych wariantów
- Łatwe dodawanie nowych pól

### 4. Czytelna dokumentacja
- Plik mapowań jako dokumentacja transformacji
- Historia zmian w Git
- Łatwe review przez analityków biznesowych

### 5. Wielokrotne użycie
- Ten sam eksport źródłowy dla różnych mapowań
- Możliwość równoległego testowania
- Cache danych źródłowych

---

## Komponenty do implementacji

### Nowe komponenty

| Komponent | Opis | Priorytet |
|-----------|------|-----------|
| `XmlToCsvExporter` | Eksport XML → CSV źródłowe (1:1) | Wysoki |
| `MappingDefinitionReader` | Czytnik definicji mapowań (Excel/CSV) | Wysoki |
| `CsvTransformer` | Wykonanie transformacji wg mapowań | Wysoki |
| `DataValidator` | Walidacja danych źródłowych | Średni |
| `MappingUI` | Narzędzie graficzne do mapowań | Niski |

### Do przeniesienia z obecnego projektu

| Komponent | Status |
|-----------|--------|
| `LargeXmlExplorer` | Gotowy |
| `XmlStructureAnalyzer` | Gotowy |
| `XmlExplorerCli` | Gotowy |
| Raport struktury JSON | Gotowy |

---

## Struktura nowego projektu

```
MyDr_Import_v2/
├── src/
│   ├── Extraction/           # Etap 1 - ekstrakcja XML
│   │   ├── XmlStructureAnalyzer.cs
│   │   └── LargeXmlExplorer.cs
│   ├── Export/               # Etap 2 - eksport do CSV źródłowych
│   │   └── XmlToCsvExporter.cs
│   ├── Mapping/              # Etap 3 - mapowania
│   │   ├── MappingDefinitionReader.cs
│   │   └── MappingValidator.cs
│   └── Transform/            # Etap 4 - transformacja
│       └── CsvTransformer.cs
├── mappings/                 # Definicje mapowań (Excel/CSV)
│   ├── pacjenci.xlsx
│   ├── wizyty.xlsx
│   └── ...
├── data/
│   ├── source_csv/           # CSV źródłowe (etap 2)
│   └── result_csv/           # CSV wynikowe (etap 4)
└── docs/
    └── mapping_guide.md
```

---

## Przykład użycia

```bash
# Etap 1: Analiza struktury XML
dotnet run -- analyze gabinet_export.xml --output data/

# Etap 2: Eksport do CSV źródłowych
dotnet run -- export --input data/data_full/ --output data/source_csv/

# Etap 3+4: Transformacja wg mapowań
dotnet run -- transform --source data/source_csv/ --mappings mappings/ --output data/result_csv/

# Pełny pipeline
dotnet run -- migrate gabinet_export.xml --mappings mappings/ --output data/result_csv/
```

---

## Następne kroki

1. [ ] Utworzenie nowego repozytorium `MyDr_Import_v2`
2. [ ] Przeniesienie komponentów ekstrakcji
3. [ ] Implementacja `XmlToCsvExporter`
4. [ ] Definicja formatu mapowań
5. [ ] Implementacja `CsvTransformer`
6. [ ] Migracja istniejących mapowań do nowego formatu
7. [ ] Testy na danych produkcyjnych

---

## Diagram przepływu danych

```
                    ┌─────────────────────────────────────────────────────────┐
                    │                    NOWA ARCHITEKTURA                    │
                    └─────────────────────────────────────────────────────────┘
                                              │
     ┌────────────────────────────────────────┼────────────────────────────────────────┐
     │                                        │                                        │
     ▼                                        ▼                                        ▼
┌─────────┐                            ┌─────────────┐                          ┌─────────────┐
│   XML   │                            │ CSV źródłowe│                          │CSV wynikowe │
│ źródłowy│                            │ (surowe)    │                          │ (docelowe)  │
└────┬────┘                            └──────┬──────┘                          └─────────────┘
     │                                        │                                        ▲
     │ ETAP 1                                 │ ETAP 3                                 │
     │ Strumieniowe                           │ Mapowanie                              │
     │ parsowanie                             │ zewnętrzne                             │
     ▼                                        ▼                                        │
┌─────────────┐      ETAP 2            ┌─────────────┐      ETAP 4              ┌──────┴──────┐
│ Osobne XML  │─────────────────────>  │ Definicja   │────────────────────────> │Transformacja│
│ per model   │   Eksport 1:1          │ mapowań     │    Aplikacja mapowań     │             │
└─────────────┘                        │ (Excel/CSV) │                          └─────────────┘
                                       └─────────────┘
                                             │
                                             │ Edycja przez
                                             │ analityka
                                             ▼
                                       ┌─────────────┐
                                       │  Narzędzie  │
                                       │  mapowania  │
                                       │  (opcja)    │
                                       └─────────────┘
```

---

*Dokument opisujący nową koncepcję migracji danych MyDr → system docelowy.*
