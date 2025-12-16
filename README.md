# MyDr Import - Analiza i Import Danych Medycznych

[![.NET Version](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

## ?? Opis Projektu

**MyDr Import** to zaawansowane narzêdzie do analizy i importu danych medycznych z plików XML (Django fixtures) do formatu CSV. Program zosta³ zaprojektowany do pracy z bardzo du¿ymi plikami (8+ GB) przy u¿yciu strumieniowego przetwarzania XML, co pozwala na efektywn¹ analizê bez przeci¹¿ania pamiêci RAM.

### ? G³ówne Funkcje

- ?? **Analiza struktury XML** - automatyczne wykrywanie typów obiektów i ich pól
- ?? **Statystyki szczegó³owe** - liczba rekordów, zakresy kluczy, typy pól
- ?? **Export do wielu formatów** - CSV, TXT, JSON
- ? **Wydajne przetwarzanie** - streaming XML, minimalne u¿ycie pamiêci
- ?? **Monitoring w czasie rzeczywistym** - postêp, ETA, prêdkoœæ przetwarzania
- ?? **Obs³uga polskich znaków** - UTF-8 encoding

---

## ?? Etapy Projektu

### ? Etap 1: Analiza Struktury (ZREALIZOWANY)

Program analizuje plik XML i generuje szczegó³owe raporty o strukturze danych:

- Liczba typów obiektów
- Liczba rekordów dla ka¿dego typu
- Pe³na lista pól z typami danych
- Zakresy kluczy g³ównych (Primary Keys)
- Statystyki wartoœci NULL
- Maksymalne d³ugoœci pól tekstowych
- Przyk³adowe wartoœci

### ?? Etap 2: Import do CSV (PLANOWANY)

- Export danych do plików CSV dla ka¿dego typu obiektu
- Konfigurowalny batch processing
- Walidacja danych
- Obs³uga b³êdów i logowanie

### ?? Etap 3: Zaawansowane Przetwarzanie (PLANOWANY)

- Filtrowanie danych
- Transformacje i mapowania
- Deduplikacja
- Integracja z bazami danych

---

## ??? Wymagania Techniczne

- **.NET 8.0 SDK** lub nowszy
- **System operacyjny**: Windows, Linux, macOS
- **Pamiêæ RAM**: minimum 2 GB (rekomendowane 4+ GB)
- **Przestrzeñ dyskowa**: ~20 GB wolnego miejsca (dla plików Ÿród³owych i raportów)

---

## ?? Instalacja

### 1. Klonowanie repozytorium

```bash
git clone https://github.com/yourusername/mydr-import.git
cd mydr-import
```

### 2. Instalacja zale¿noœci

```bash
dotnet restore
```

### 3. Kompilacja projektu

```bash
dotnet build
```

---

## ?? U¿ycie

### Analiza pliku XML

#### Opcja 1: Automatyczne wykrywanie pliku

Umieœæ plik `gabinet_export_2025_12_09.xml` w folderze `data/` i uruchom:

```bash
dotnet run
```

#### Opcja 2: Podanie œcie¿ki do pliku

```bash
dotnet run -- "C:\path\to\your\file.xml"
```

#### Opcja 3: Uruchomienie skompilowanej aplikacji

```bash
cd bin\Debug\net8.0
MyDr_Import.exe "C:\path\to\your\file.xml"
```

### Przyk³ad wyjœcia

```
??????????????????????????????????????????????????????????????????????????????
?              MyDr Import - Analiza Struktury XML (Etap 1)                  ?
?                  Analiza pliku gabinet_export_2025_12_09.xml               ?
??????????????????????????????????????????????????????????????????????????????

?? Plik: C:\PROJ\MyDr_Import\data\gabinet_export_2025_12_09.xml
?? Rozmiar: 8.54 GB
??  Rozpoczêcie analizy: 2025-12-16 02:35:12

? Postêp: 45.3% | Obiektów: 3 500 000 | Prêdkoœæ: 12500 obj/s | ETA: 00:05:23

================================================================================
? ANALIZA ZAKOÑCZONA
================================================================================
??  Czas wykonania: 00:07:45
?? Ca³kowita liczba obiektów: 8 128 385
???  Liczba typów obiektów: 60
? Prêdkoœæ: 17 456 obiektów/s
```

---

## ?? Struktura Raportów

Po zakoñczeniu analizy, w folderze `output/` znajdziesz nastêpuj¹ce pliki:

### 1. **structure_summary.txt**
Kompleksowy raport tekstowy zawieraj¹cy:
- Podsumowanie ogólne
- Szczegó³owe informacje o ka¿dym typie obiektu
- Pe³na lista pól z opisami

### 2. **structure_summary.json**
Raport w formacie JSON dla automatycznego przetwarzania:

```json
{
  "generatedAt": "2025-12-16 02:41:08",
  "totalRecords": 8128385,
  "totalTypes": 60,
  "objects": [
    {
      "modelName": "gabinet.visit",
      "recordCount": 696727,
      "minPrimaryKey": 174764990,
      "maxPrimaryKey": 382315057,
      "fieldCount": 25,
      "fields": [...]
    }
  ]
}
```

### 3. **structure_[model_name].csv**
Indywidualny raport CSV dla ka¿dego typu obiektu:

```csv
Model,gabinet.visit
RecordCount,696727
MinPK,174764990
MaxPK,382315057
FieldCount,25

FieldName,Type,Relation,RelationTo,OccurrenceCount,NullCount,MaxLength
date,DateField,,,696727,0,10
doctor,,ManyToOneRel,gabinet.person,696727,0,6
interview,TextField,,,488281,208446,10001
...
```

---

## ??? Struktura Projektu

```
MyDr_Import/
?
??? ?? Models/                      # Modele danych
?   ??? FieldInfo.cs               # Model informacji o polu
?   ??? XmlObjectInfo.cs           # Model metadanych obiektu
?
??? ?? Services/                    # Serwisy biznesowe
?   ??? XmlStructureAnalyzer.cs    # Analizator XML (streaming)
?   ??? ProgressReporter.cs        # Reporter postêpu
?
??? ?? data/                        # Folder danych wejœciowych
?   ??? gabinet_export_2025_12_09.xml
?
??? ?? output/                      # Folder raportów (generowany)
?   ??? structure_summary.txt
?   ??? structure_summary.json
?   ??? structure_*.csv
?
??? Program.cs                      # G³ówny program
??? MyDr_Import.csproj             # Plik projektu
??? README.md                       # Dokumentacja
??? .gitignore                      # Ignorowane pliki Git
```

---

## ?? Zidentyfikowane Typy Obiektów

Plik `gabinet_export_2025_12_09.xml` zawiera **60 typów obiektów** (modeli Django):

### ?? Dane Pacjentów i Wizyt

| Model | Liczba Rekordów | Opis |
|-------|----------------|------|
| `gabinet.patient` | 22 397 | Dane pacjentów |
| `gabinet.visit` | 696 727 | Wizyty medyczne |
| `gabinet.visitnotes` | 913 366 | Notatki z wizyt |
| `gabinet.patientnote` | 1 593 968 | Notatki o pacjentach |
| `gabinet.recognition` | 992 503 | Rozpoznania |
| `gabinet.incaseofemergency` | 6 036 | Kontakt awaryjny |

### ?? Dokumentacja Medyczna

| Model | Liczba Rekordów | Opis |
|-------|----------------|------|
| `gabinet.icd10` | 992 503 | Kody ICD-10 |
| `gabinet.icd9` | 48 803 | Kody ICD-9 |
| `gabinet.recipe` | 350 551 | Recepty |
| `gabinet.recipedrug` | 413 206 | Leki na receptach |
| `gabinet.sickleave` | 45 299 | Zwolnienia lekarskie |
| `gabinet.documents` | 187 254 | Dokumenty |

### ?? Procedury i Us³ugi

| Model | Liczba Rekordów | Opis |
|-------|----------------|------|
| `gabinet.medicalprocedure` | 161 166 | Procedury medyczne |
| `gabinet.vaccination` | 51 588 | Szczepienia |
| `gabinet.imagingtest` | 7 889 | Badania obrazowe |
| `gabinet.privateservice` | 188 | Us³ugi prywatne |

### ?? Struktura Placówki

| Model | Liczba Rekordów | Opis |
|-------|----------------|------|
| `gabinet.facility` | 1 | Placówka medyczna |
| `gabinet.office` | 10 | Gabinety |
| `gabinet.department` | 15 | Oddzia³y |
| `gabinet.person` | 162 | Pracownicy (lekarze) |
| `auth.user` | 127 | U¿ytkownicy systemu |

### ?? NFZ i Ubezpieczenia

| Model | Liczba Rekordów | Opis |
|-------|----------------|------|
| `gabinet.insurance` | 38 374 | Ubezpieczenia |
| `nfz_nfzcontract` | 38 | Umowy NFZ |
| `gabinet.nfzdeclaration` | 99 254 | Deklaracje NFZ |
| `gabinet.nfzservicereport` | 1 084 | Raporty us³ug NFZ |

*Pe³na lista 60 typów dostêpna w raportach.*

---

## ?? Kluczowe Pola w Najwa¿niejszych Obiektach

### `gabinet.patient` (Pacjent)

```
- active: BooleanField - czy pacjent aktywny
- pesel: CharField(11) - numer PESEL (19 845 wype³nionych)
- blood_type: CharField(1) - grupa krwi
- nfz: CharField(2) - oddzia³ NFZ
- residence_address: OneToOneRel -> gabinet.address
- employer_address: OneToOneRel -> gabinet.address
- maiden_name: CharField(29) - nazwisko panieñskie (1 437)
- identity_num: CharField(28) - nr dowodu (1 371)
- second_telephone: CharField(14) - drugi telefon (339)
```

### `gabinet.visit` (Wizyta)

```
- date: DateField - data wizyty
- timeTo: TimeField - godzina wizyty
- doctor: ManyToOneRel -> gabinet.person - lekarz
- office: ManyToOneRel -> gabinet.office - gabinet
- interview: TextField(10001) - wywiad (488 281 wype³nionych)
- recognition_description: TextField(1195) - opis rozpoznania (322 056)
- specialty: ManyToOneRel -> gabinet.specialty - specjalnoœæ
- evisit: BooleanField - czy e-wizyta
```

### `gabinet.patientnote` (Notatka o pacjencie)

```
- patient: ManyToOneRel -> gabinet.patient - pacjent
- addition_date: DateField - data dodania
- title: CharField - tytu³ notatki
- text: TextField - treœæ notatki
- author: ManyToOneRel -> gabinet.person - autor
- is_important: BooleanField - czy wa¿na
- is_medical_data: NullBooleanField - czy dane medyczne
```

---

## ?? Konfiguracja

### Zmiana rozmiaru batcha (dla przysz³ego eksportu CSV)

W pliku `Services/CsvExporter.cs` (gdy zostanie zaimplementowany):

```csharp
private const int BATCH_SIZE = 5000; // Zwiêksz dla wiêkszej wydajnoœci
```

### Poziom logowania

```csharp
// W Program.cs
var logLevel = LogLevel.Information; // Debug, Information, Warning, Error
```

---

## ?? Znane Problemy i Rozwi¹zania

### Problem: B³¹d pamiêci przy bardzo du¿ych plikach

**Rozwi¹zanie**: Program u¿ywa streaming parsingu, ale jeœli nadal wystêpuj¹ problemy:
- Zwiêksz pamiêæ wirtualn¹ Windows
- U¿yj wersji 64-bit .NET

### Problem: Niew³aœciwe kodowanie polskich znaków

**Rozwi¹zanie**: Upewnij siê, ¿e konsola u¿ywa UTF-8:
```bash
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
```

---

## ?? Testowanie

### Szybki test na ma³ym pliku

```bash
# Utwórz testowy plik XML
dotnet run -- test-small.xml
```

### Test wydajnoœci

```bash
# Pomiar czasu dla pe³nego pliku
Measure-Command { dotnet run }
```

---

## ?? Struktura Danych XML

Plik Ÿród³owy ma strukturê Django fixtures:

```xml
<?xml version="1.0" encoding="utf-8"?>
<django-objects>
  <object model="gabinet.patient" pk="23653120">
    <field name="pesel" type="CharField">12345678901</field>
    <field name="active" type="BooleanField">True</field>
    <field name="blood_type" type="CharField">A</field>
    <field name="facility" rel="ManyToOneRel" to="gabinet.facility">21540</field>
    ...
  </object>
  
  <object model="gabinet.visit" pk="174764990">
    <field name="date" type="DateField">2022-05-07</field>
    <field name="doctor" rel="ManyToOneRel" to="gabinet.person">84480</field>
    <field name="interview" type="TextField">Wywiad medyczny...</field>
    ...
  </object>
</django-objects>
```

### Typy pól w XML:

- **CharField** - tekst krótki (max okreœlona d³ugoœæ)
- **TextField** - tekst d³ugi (nieograniczony)
- **DateField** - data (YYYY-MM-DD)
- **TimeField** - czas (HH:MM:SS)
- **DateTimeField** - data i czas
- **BooleanField** - True/False
- **NullBooleanField** - True/False/None
- **DecimalField** - liczba dziesiêtna
- **IntegerField/BigIntegerField** - liczba ca³kowita
- **ManyToOneRel** - relacja wiele-do-jednego (klucz obcy)
- **OneToOneRel** - relacja jeden-do-jednego
- **ManyToManyRel** - relacja wiele-do-wielu

---

## ?? Wk³ad w Projekt

Contributions are welcome! Jeœli chcesz pomóc:

1. Fork repozytorium
2. Utwórz branch dla nowej funkcji (`git checkout -b feature/AmazingFeature`)
3. Commit zmian (`git commit -m 'Add some AmazingFeature'`)
4. Push do brancha (`git push origin feature/AmazingFeature`)
5. Otwórz Pull Request

---

## ?? Licencja

Ten projekt jest dostêpny na licencji MIT. Zobacz plik `LICENSE` dla szczegó³ów.

---

## ????? Autor

MyDr Import Project Team

---

## ?? Kontakt

W razie pytañ lub problemów, otwórz Issue na GitHubie.

---

## ?? Technologie

- **C# 12** - najnowsza wersja jêzyka
- **.NET 8.0** - najnowszy framework
- **System.Xml (XmlReader)** - streaming XML parsing
- **CsvHelper** - generowanie CSV
- **System.Text.Json** - generowanie JSON

---

## ?? Dokumentacja API

### XmlStructureAnalyzer

```csharp
public class XmlStructureAnalyzer
{
    public XmlStructureAnalyzer(string filePath, IProgress<AnalysisProgress>? progress = null)
    
    public async Task<Dictionary<string, XmlObjectInfo>> AnalyzeAsync(
        CancellationToken cancellationToken = default)
}
```

### XmlObjectInfo

```csharp
public class XmlObjectInfo
{
    public string ModelName { get; set; }
    public long RecordCount { get; set; }
    public Dictionary<string, FieldInfo> Fields { get; set; }
    public long MinPrimaryKey { get; set; }
    public long MaxPrimaryKey { get; set; }
    
    public void PrintSummary()
    public string ToCSVSummary()
}
```

### FieldInfo

```csharp
public class FieldInfo
{
    public string Name { get; set; }
    public string? Type { get; set; }
    public string? Relation { get; set; }
    public string? RelationTo { get; set; }
    public int OccurrenceCount { get; set; }
    public int NullCount { get; set; }
    public int MaxLength { get; set; }
    public HashSet<string> SampleValues { get; set; }
}
```

---

## ?? Roadmap

- [x] Etap 1: Analiza struktury XML
- [ ] Etap 2: Export do CSV
- [ ] Etap 3: Import do bazy danych SQL
- [ ] Etap 4: Web API dla dostêpu do danych
- [ ] Etap 5: Dashboard analityczny
- [ ] Etap 6: Machine Learning dla predykcji

---

**? Jeœli ten projekt by³ pomocny, zostaw gwiazdkê na GitHubie!**
