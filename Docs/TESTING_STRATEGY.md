# Strategia Testowania - Etap 2

## Problem: Du¿a Iloœæ Danych

- Plik Ÿród³owy: **8.5 GB XML** (8,128,385 rekordów)
- Czas pe³nej analizy: **~8 minut**
- Nie mo¿emy testowaæ na pe³nym pliku przy ka¿dej zmianie

---

## Rozwi¹zanie: Test Data Subset

### 1. Utworzenie Testowego Podzbioru XML

Wytnij pierwsze N obiektów ka¿dego typu do ma³ego pliku testowego:

```xml
<?xml version="1.0" encoding="utf-8"?>
<django-objects>
  <!-- 10 pacjentów -->
  <object model="gabinet.patient" pk="23653120">...</object>
  <object model="gabinet.patient" pk="23653121">...</object>
  ...
  
  <!-- 50 wizyt -->
  <object model="gabinet.visit" pk="174764990">...</object>
  ...
  
  <!-- 20 szczepieñ -->
  <object model="gabinet.vaccination" pk="1001">...</object>
  ...
</django-objects>
```

**Wielkoœæ testowa:** ~10 MB (zamiast 8.5 GB)  
**Czas przetwarzania:** <5 sekund

---

## 2. Generator Pliku Testowego

### Skrypt PowerShell: `Generate-TestXml.ps1`

```powershell
param(
    [string]$SourceFile = "C:\PROJ\MyDr_Import\data\gabinet_export_2025_12_09.xml",
    [string]$OutputFile = "C:\PROJ\MyDr_Import\data\test_sample.xml",
    [hashtable]$Limits = @{
        "gabinet.patient" = 10
        "gabinet.visit" = 50
        "gabinet.person" = 5
        "gabinet.vaccination" = 20
        "gabinet.patientnote" = 30
    }
)

$reader = [System.IO.XmlReader]::Create($SourceFile)
$writer = [System.IO.StreamWriter]::new($OutputFile, $false, [System.Text.Encoding]::UTF8)

$writer.WriteLine('<?xml version="1.0" encoding="utf-8"?>')
$writer.WriteLine('<django-objects>')

$counts = @{}
$capturing = $false
$objectXml = ""

while ($reader.Read()) {
    if ($reader.NodeType -eq "Element" -and $reader.Name -eq "object") {
        $model = $reader.GetAttribute("model")
        
        if (-not $counts.ContainsKey($model)) {
            $counts[$model] = 0
        }
        
        $limit = if ($Limits.ContainsKey($model)) { $Limits[$model] } else { 0 }
        
        if ($counts[$model] -lt $limit) {
            $capturing = $true
            $objectXml = $reader.ReadOuterXml()
            $writer.WriteLine($objectXml)
            $counts[$model]++
        }
    }
}

$writer.WriteLine('</django-objects>')
$writer.Close()

Write-Host "Test file created: $OutputFile"
$counts.GetEnumerator() | Sort-Object Name | ForEach-Object {
    Write-Host "  $($_.Key): $($_.Value) records"
}
```

---

## 3. Metody Testowania

### A. Unit Tests (Szybkie - ms)

Test pojedynczych transformacji bez I/O:

```csharp
[Test]
public void PatientMapper_TransformsPesel_Correctly()
{
    var xmlPatient = new XmlPatient { Pesel = "92010112345" };
    var csvPatient = PatientMapper.Map(xmlPatient);
    Assert.AreEqual("92010112345", csvPatient.Pesel);
}
```

### B. Integration Tests (Œrednie - sekundy)

Test na ma³ym pliku testowym:

```csharp
[Test]
public async Task ExportService_ExportsTestFile_Successfully()
{
    var exporter = new CsvExportService("data/test_sample.xml");
    await exporter.ExportAllAsync("output/test");
    
    Assert.IsTrue(File.Exists("output/test/pacjenci.csv"));
    var lines = File.ReadAllLines("output/test/pacjenci.csv");
    Assert.AreEqual(11, lines.Length); // Header + 10 records
}
```

### C. Smoke Tests (Wolne - minuty)

Test na wiêkszym podzbiorze (100-1000 rekordów ka¿dego typu):

```csharp
[Test]
[Category("Smoke")]
public async Task ExportService_ExportsMediumSet_WithoutErrors()
{
    var exporter = new CsvExportService("data/smoke_test_1000.xml");
    var result = await exporter.ExportAllAsync("output/smoke");
    
    Assert.AreEqual(0, result.ErrorCount);
}
```

### D. Full Integration (Bardzo wolne - 8+ minut)

Tylko przed releasem lub na CI/CD:

```csharp
[Test]
[Category("FullIntegration")]
[Ignore("Run only before release")]
public async Task ExportService_ExportsFullFile_Successfully()
{
    var exporter = new CsvExportService("data/gabinet_export_2025_12_09.xml");
    await exporter.ExportAllAsync("output/full");
}
```

---

## 4. Struktura Testów

```
Tests/
??? Unit/
?   ??? Mappers/
?   ?   ??? PatientMapperTests.cs
?   ?   ??? VisitMapperTests.cs
?   ?   ??? VaccinationMapperTests.cs
?   ??? Validators/
?       ??? PeselValidatorTests.cs
?       ??? DateValidatorTests.cs
?
??? Integration/
?   ??? CsvExportServiceTests.cs
?   ??? XmlParserTests.cs
?
??? Data/
    ??? test_sample.xml          (10 MB)
    ??? smoke_test_1000.xml      (100 MB)
    ??? expected_outputs/
        ??? pacjenci.csv
        ??? wizyty.csv
```

---

## 5. Mock Data Generator

Dla testów jednostkowych bez plików:

```csharp
public static class TestDataFactory
{
    public static XmlPatient CreatePatient(string pesel = "92010112345")
    {
        return new XmlPatient
        {
            PrimaryKey = 23653120,
            Pesel = pesel,
            FirstName = "Jan",
            LastName = "Kowalski",
            BirthDate = new DateTime(1992, 1, 1),
            Sex = "M"
        };
    }
    
    public static XmlVisit CreateVisit(long patientPk, long doctorPk)
    {
        return new XmlVisit
        {
            PrimaryKey = 174764990,
            PatientPk = patientPk,
            DoctorPk = doctorPk,
            Date = DateTime.Now.Date,
            TimeTo = new TimeSpan(10, 30, 0)
        };
    }
}
```

---

## 6. Performance Benchmarking

### BenchmarkDotNet dla optymalizacji:

```csharp
[MemoryDiagnoser]
public class MapperBenchmarks
{
    private XmlPatient _testPatient;
    
    [GlobalSetup]
    public void Setup()
    {
        _testPatient = TestDataFactory.CreatePatient();
    }
    
    [Benchmark]
    public OptimedPatient MapPatient()
    {
        return PatientMapper.Map(_testPatient);
    }
}
```

**Uruchom:** `dotnet run -c Release --project Benchmarks`

---

## 7. Workflow Testowania

### Podczas Developmentu:

1. **Napisz test** (TDD approach)
2. **Uruchom Unit Tests** (~1-2 sekundy)
3. **Implementuj funkcjê**
4. **Uruchom Integration Tests** (~5-10 sekund)
5. **Commit** gdy testy przechodz¹

### Przed Commit:

```bash
dotnet test --filter "TestCategory!=FullIntegration"
```

### Przed Push:

```bash
dotnet test --filter "TestCategory=Smoke"
```

### Przed Release:

```bash
dotnet test  # Wszystkie testy w³¹cznie z Full Integration
```

---

## 8. Continuous Integration (GitHub Actions)

```yaml
# .github/workflows/test.yml
name: Tests

on: [push, pull_request]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      - run: dotnet test --filter "TestCategory=Unit"
  
  integration-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
      - run: dotnet test --filter "TestCategory=Integration"
```

---

## 9. Metryki Sukcesu

| Metryka | Target |
|---------|--------|
| Unit test coverage | >80% |
| Integration test pass rate | 100% |
| Performance degradation | <5% per commit |
| Memory usage | <500 MB |
| Export time (full) | <10 minut |

---

## 10. Przyk³adowy Testowy XML

Utworzymy plik `data/test_sample.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<django-objects>
  <!-- Minimalna reprezentacja -->
  <object model="gabinet.patient" pk="23653120">
    <field name="pesel" type="CharField">92010112345</field>
    <field name="active" type="BooleanField">True</field>
  </object>
  
  <object model="gabinet.person" pk="84480">
    <field name="first_name" type="CharField">Jan</field>
    <field name="last_name" type="CharField">Lekarz</field>
  </object>
  
  <object model="gabinet.visit" pk="174764990">
    <field name="patient" rel="ManyToOneRel" to="gabinet.patient">23653120</field>
    <field name="doctor" rel="ManyToOneRel" to="gabinet.person">84480</field>
    <field name="date" type="DateField">2025-01-15</field>
    <field name="timeTo" type="TimeField">10:30:00</field>
  </object>
</django-objects>
```

---

## Podsumowanie

? **Unit Tests** - szybkie, bez plików  
? **Test Sample** - 10 MB plik testowy  
? **Integration Tests** - pe³ny pipeline na ma³ych danych  
? **Smoke Tests** - œredni zestaw przed commit  
? **Full Integration** - tylko przed release  

**Czas oszczêdzony:** ~95% podczas developmentu!
