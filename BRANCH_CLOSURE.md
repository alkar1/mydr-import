# Zamknięcie Gałęzi: MyDr_Import v1

**Data:** 2026-01-04  
**Status:** Zamknięta - koncepcja zastąpiona nową architekturą

---

## Podsumowanie

Niniejsza gałąź zawiera implementację narzędzia do migracji danych z eksportu Django XML (MyDr/Gabinet) do formatu CSV zgodnego z systemem docelowym.

### Architektura (stara koncepcja)

```
┌─────────────────┐     ┌─────────────────────┐     ┌──────────────┐
│   XML źródłowy  │────>│ Arkusze mapowań XLS │────>│  CSV wynikowe│
│ (gabinet.xml)   │     │  (bezpośrednie)     │     │              │
└─────────────────┘     └─────────────────────┘     └──────────────┘
```

**Przepływ danych:**
1. XML Django (10GB+) → strumieniowe parsowanie
2. Bezpośrednie mapowanie pól XML → pola docelowe (przez arkusze Excel)
3. Generowanie CSV wynikowych

---

## Co zostało zrealizowane

### Etap 1 - Analiza struktury XML
- ✅ Strumieniowy parser XML dla dużych plików (10GB+)
- ✅ Ekstrakcja wszystkich modeli Django do osobnych plików XML
- ✅ Generowanie raportów struktury (TXT, JSON)
- ✅ XML Explorer CLI do eksploracji danych

### Etap 2 - Mapowanie i generowanie CSV
- ✅ Czytnik arkuszy Excel z definicjami mapowań
- ✅ Walidacja mapowań względem struktury XML
- ✅ Dedykowane procesory dla modeli:
  - Pacjenci
  - Jednostki
  - Wizyty
  - Karty wizyt
  - Deklaracje POZ
  - Szczepienia
  - Stałe choroby/leki pacjenta
  - Dokumenty uprawniające
  - Dokumentacja/załączniki
  - Pracownicy

### Pliki wynikowe
11 plików CSV z danymi migracyjnymi (patrz `compare_report.md`)

---

## Problemy i ograniczenia

### 1. Złożoność mapowań
Bezpośrednie mapowanie XML→CSV wymagało:
- Hardkodowanych mapowań nazw pól w kodzie C#
- Dedykowanych procesorów dla każdego modelu
- Trudności w walidacji poprawności mapowań

### 2. Brakujące dane
Niektóre pola docelowe nie mają odpowiedników w źródle:
- Dane w relacjach wymagające JOIN
- Pola nieeksportowane do XML
- Dane rozproszone w wielu modelach

### 3. Utrzymanie
Każda zmiana w mapowaniu wymagała:
- Modyfikacji kodu C#
- Rekompilacji
- Ponownego testowania

---

## Pliki projektu

```
MyDr_Import/
├── Program.cs              # Punkt wejścia
├── Etap1.cs                # Analiza struktury XML
├── Etap2.cs                # Mapowanie i generowanie CSV
├── XmlExplorerCli.cs       # CLI dla XML Explorer
├── Services/
│   ├── LargeXmlExplorer.cs
│   ├── XmlStructureAnalyzer.cs
│   ├── CsvGenerator.cs
│   ├── ExcelMappingReader.cs
│   └── MappingValidator.cs
├── Processors/             # Dedykowane procesory modeli
├── Models/
├── data_etap1/             # Wyniki analizy XML
├── compare_report.md       # Porównanie wyników
└── Problemy.md             # Lista problemów
```

---

## Decyzja o zamknięciu

Gałąź zamknięta na rzecz nowej architektury opisanej w `NEW_ARCHITECTURE.md`.

**Powody:**
1. Zbyt sztywne powiązanie mapowań z kodem
2. Trudność w iteracyjnym dostosowywaniu mapowań
3. Brak możliwości łatwej walidacji danych źródłowych przed mapowaniem
4. Nowa koncepcja umożliwia rozdzielenie odpowiedzialności

---

## Wartość do przeniesienia

Następujące komponenty mogą być wykorzystane w nowym projekcie:
- `LargeXmlExplorer` - strumieniowy parser XML
- `XmlStructureAnalyzer` - analiza struktury
- Logika eksportu do CSV
- Raport struktury XML (`xml_structure_summary.json`)

---

*Dokument wygenerowany automatycznie przy zamknięciu gałęzi.*
