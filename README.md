# MyDr_Import

Narzędzie do importu i przetwarzania danych z eksportu Django XML (MyDr/Gabinet).

## Wymagania

- .NET 8.0

## Użycie

```bash
# Podstawowe uruchomienie (etap 2 - mapowanie)
dotnet run

# Pełne przetwarzanie od etapu 1 (analiza XML)
dotnet run -- --etap1 sciezka/do/pliku.xml

# Przetwarzanie tylko wybranego modelu
dotnet run -- --model=pacjenci
```

## Etapy przetwarzania

1. **Etap 1** - Analiza struktury XML, ekstrakcja modeli do osobnych plików
2. **Etap 2** - Mapowanie pól, generowanie CSV z danymi

---

## XML Explorer - Narzędzie do eksploracji dużych plików XML

Strumieniowe narzędzie do przeglądania dużych plików XML (10GB+) bez ładowania całego pliku do pamięci. Idealne dla LLM do analizy struktury danych.

### Uruchomienie

```bash
dotnet run -- explore [plik.xml] [komenda] [opcje]
```

### Komendy

| Komenda | Opis | Przykład |
|---------|------|----------|
| `head [n] [model]` | Pierwsze N rekordów | `explore data.xml head 5` |
| `tail [n] [model]` | Ostatnie N rekordów | `explore data.xml tail 10` |
| `sample [n] [skip] [model]` | Co [skip]-ty rekord, max [n] | `explore data.xml sample 10 100` |
| `search [pole] [wartość] [n] [model]` | Wyszukiwanie po polu | `explore data.xml search name "Jan" 20` |
| `get [pk] [model]` | Pobierz rekord po kluczu głównym | `explore data.xml get 12345` |
| `models [próbka]` | Lista modeli w pliku | `explore data.xml models` |
| `schema [model] [próbka]` | Schemat pól dla modelu | `explore data.xml schema patients.patient 100` |
| `stats` | Pełne statystyki pliku | `explore data.xml stats` |
| `report [n] [model]` | Raport tekstowy dla LLM | `explore data.xml report 3` |

### Przykłady użycia

```bash
# Wyświetl pierwsze 5 rekordów
dotnet run -- explore gabinet_export.xml head 5

# Pierwsze 10 rekordów konkretnego modelu
dotnet run -- explore gabinet_export.xml head 10 patients.patient

# Wyszukaj rekordy zawierające "Kowalski" w polu "nazwisko"
dotnet run -- explore gabinet_export.xml search nazwisko "Kowalski" 20

# Pokaż schemat pól dla modelu (na próbce 100 rekordów)
dotnet run -- explore gabinet_export.xml schema patients.patient 100

# Lista wszystkich modeli w pliku
dotnet run -- explore gabinet_export.xml models

# Wygeneruj raport dla LLM
dotnet run -- explore gabinet_export.xml report 3 patients.patient
```

### Funkcje API (LargeXmlExplorer)

```csharp
var explorer = new LargeXmlExplorer("plik.xml");

// Pierwsze N rekordów
var head = explorer.Head(10, "model.name");

// Ostatnie N rekordów
var tail = explorer.Tail(10);

// Próbkowanie
var sample = explorer.Sample(maxRecords: 10, skipEvery: 100);

// Wyszukiwanie
var results = explorer.Search("pole", "wartość", maxResults: 10);

// Pobierz po PK
var record = explorer.GetByPk("12345");

// Lista modeli
var models = explorer.GetModels();

// Schemat pól
var schema = explorer.GetSchema("model.name", sampleSize: 100);

// Statystyki
var stats = explorer.GetStats();

// Raport dla LLM
var report = explorer.GenerateReport(headCount: 3);

// Eksport do JSON
var json = explorer.ToJson(records);
```

## Struktura projektu

```
MyDr_Import/
├── Program.cs              # Punkt wejścia
├── Etap1.cs                # Analiza struktury XML
├── Etap2.cs                # Mapowanie i generowanie CSV
├── XmlExplorerCli.cs       # CLI dla XML Explorer
├── Services/
│   ├── LargeXmlExplorer.cs # Strumieniowy eksplorer XML
│   ├── XmlStructureAnalyzer.cs
│   └── CsvGenerator.cs
├── Processors/
│   └── BaseModelProcessor.cs
├── Models/
│   └── XmlObjectInfo.cs
└── data_etap1/             # Wyniki etapu 1
```

## Licencja

Projekt wewnętrzny OPTIMED.
