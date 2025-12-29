# Instrukcja Mapowania - MyDr_Import

Dokument opisuje mapowanie pól z eksportu Django XML (MyDr/Gabinet) do formatu CSV dla systemu OPTIMED.

---

## 1. Przegląd architektury

### 1.1 Przepływ danych

```
XML Django dump → [Etap1.cs] → data_etap1/data_full/*.xml → [Etap2.cs + Procesory] → data_etap2/*.csv
```

### 1.2 Etapy przetwarzania

| Etap | Komponent | Opis |
|------|-----------|------|
| **Etap 1** | `Etap1.cs` | Analiza struktury XML, ekstrakcja modeli do osobnych plików XML |
| **Etap 2** | `Etap2.cs` + `Processors/` | Wczytanie mapowań z Excel, walidacja, generowanie CSV |
| **Walidacja** | `MappingValidator.cs` | Waliduje mapowania względem struktury XML |

---

## 2. Pliki źródłowe (data_etap1/data_full/)

Pliki XML wyekstrahowane z głównego eksportu Django:

| Plik XML | Model Django | Opis |
|----------|--------------|------|
| `gabinet_visit.xml` | `gabinet.visit` | Wizyty |
| `gabinet_person.xml` | `gabinet.person` | Personel medyczny |
| `gabinet_patient.xml` | `gabinet.patient` | Pacjenci |
| `gabinet_address.xml` | `gabinet.address` | Adresy |
| `gabinet_incaseofemergency.xml` | `gabinet.incaseofemergency` | Opiekunowie pacjentów (ICE) |
| `gabinet_insurance.xml` | `gabinet.insurance` | Ubezpieczenia |
| `gabinet_patientnote.xml` | `gabinet.patientnote` | Notatki pacjentów |
| `gabinet_recognition.xml` | `gabinet.recognition` | Rozpoznania (ICD10) |
| `gabinet_icd10.xml` | `gabinet.icd10` | Słownik kodów ICD10 |
| `gabinet_medicalprocedure.xml` | `gabinet.medicalprocedure` | Procedury medyczne |
| `gabinet_icd9.xml` | `gabinet.icd9` | Słownik kodów ICD9 |
| `gabinet_visitnotes.xml` | `gabinet.visitnotes` | Historia zmian wizyt |
| `gabinet_patientpermanentdrug.xml` | `gabinet.patientpermanentdrug` | Stałe leki pacjentów |
| `auth_user.xml` | `auth.user` | Użytkownicy systemu |
| `gabinet_profile.xml` | `gabinet.profile` | Profile użytkowników |
| `gabinet_specialty.xml` | `gabinet.specialty` | Specjalizacje lekarzy |
| `ezwolnienia_esickleave.xml` | `ezwolnienia.esickleave` | Zwolnienia lekarskie |
| `gabinet_office.xml` | `gabinet.office` | Jednostki/gabinety |
| `gabinet_ver.xml` | `gabinet.ver` | Słownik leków (BLOZ) |
| `gabinet_nfzdeclaration.xml` | `gabinet.nfzdeclaration` | Deklaracje POZ |
| `gabinet_insurancedocuments.xml` | `gabinet.insurancedocuments` | Dokumenty ubezpieczeniowe |
| `gabinet_documents.xml` | `gabinet.documents` | Załączniki dokumentacji |
| `gabinet_documenttype.xml` | `gabinet.documenttype` | Typy dokumentów |
| `gabinet_genericmedicaldata.xml` | `gabinet.genericmedicaldata` | Dane medyczne (choroby przewlekłe) |
| `gabinet_vaccination.xml` | `gabinet.vaccination` | Szczepienia |
| `gabinet_recipe.xml` | `gabinet.recipe` | Recepty |
| `gabinet_recipedrug.xml` | `gabinet.recipedrug` | Leki na receptach |
| `gabinet_patientdead.xml` | `gabinet.patientdead` | Dane o zgonach pacjentów |

### 2.1 Struktura pliku XML

```xml
<django-objects version="1.0">
  <object model="gabinet.patient" pk="12345">
    <field name="pesel" type="CharField">12345678901</field>
    <field name="residence_address" rel="ManyToOneRel" to="gabinet.address">5678</field>
    <!-- ... -->
  </object>
</django-objects>
```

### 2.2 Pola niedostępne w eksporcie

**UWAGA:** Niektóre dane osobowe NIE są eksportowane z systemu MyDr:
- `patient.name`, `patient.surname` - imiona i nazwiska pacjentów
- `patient.email`, `patient.telephone` - dane kontaktowe

Dane te należy pozyskać z innych źródeł lub wyliczyć (np. płeć i data urodzenia z PESEL).

---

## 3. Pliki docelowe (data_etap2/)

Pliki wyjściowe CSV generowane przez procesory:

| Plik docelowy | Opis |
|---------------|------|
| `wizyty.csv` | Wizyty |
| `karty_wizyt.csv` | Karty wizyt |
| `pacjenci.csv` | Pacjenci |
| `pracownicy.csv` | Pracownicy |
| `stale_leki_pacjenta.csv` | Stałe leki |
| `deklaracje_poz.csv` | Deklaracje POZ |
| `jednostki.csv` | Jednostki organizacyjne |
| `dokumenty_uprawniajace.csv` | Dokumenty uprawniające |
| `dokumentacja_zalaczniki.csv` | Załączniki |
| `stale_choroby_pacjenta.csv` | Choroby przewlekłe |
| `szczepienia.csv` | Szczepienia |

---

## 4. Szczegółowe mapowanie pól

> **Uwaga:** Nazwy pól źródłowych odnoszą się do atrybutów `name` w elementach `<field>` plików XML.
> Pola oznaczone jako "Lookup" wymagają załadowania danych z powiązanych plików XML do cache.

### 4.1 WIZYTY (`wizyty.csv`)

| Pole docelowe | Źródło | Transformacja |
|---------------|--------|---------------|
| `InstalacjaId` | - | `null` |
| `IdImport` | `visit.id` | Bezpośrednio |
| `JednostkaId` | - | Puste |
| `JednostkaIdImport` | `visit.office` | Bezpośrednio |
| `PacjentId` | - | Puste |
| `PacjentIdImport` | `visit.patient` | Bezpośrednio |
| `PacjentPesel` | `patient.pesel` | Lookup przez patient_id |
| `PracownikId` | - | Puste |
| `PracownikIdImport` | `visit.doctor` | Bezpośrednio |
| `ZasobIdImport` | - | Puste |
| `PracownikNPWZ` | `person.pwz` | Lookup przez doctor_id |
| `PracownikPesel` | `person.pesel` | Lookup przez doctor_id |
| `PlatnikIdImportu` | - | Puste |
| `JednostkaRozliczeniowaId` | - | Puste |
| `JednostkaRozliczeniowaIdImportu` | - | Puste |
| `DataUtworzenia` | `visit.created` | ISO → `YYYY-MM-DD HH:MM:SS` |
| `DataOd` | `visit.date` + `visit.timeFrom` | Połączenie daty i czasu |
| `DataDo` | `visit.date` + `visit.timeTo` | Połączenie daty i czasu |
| `CzasOd` | `visit.timeFrom` | Bezpośrednio |
| `CzasDo` | `visit.timeTo` | Bezpośrednio |
| `Status` | `visit.state` | Mapowanie (patrz 5.1) |
| `NFZ` | `visit.visit_kind` | `"1"` jeśli zawiera "NFZ", inaczej `"0"` |
| `NieRozliczaj` | - | `"0"` (domyślnie) |
| `Dodatkowy` | - | `"0"` (domyślnie) |
| `Komentarz` | - | Puste |
| `TrybPrzyjecia` | `visit.reception_mode_choice` | Bezpośrednio lub `"5"` jeśli puste |
| `TrybDalszegoLeczenia` | - | Puste |
| `TypWizyty` | - | `"1"` (domyślnie - Porada lekarska) |
| `KodSwiadczeniaNFZ` | - | Puste |
| `KodUprawnieniaPacjenta` | `patient.rights` | Usuń "X" |
| `ProceduryICD9` | `medicalprocedure → icd9` | Lista kodów oddzielona przecinkiem |
| `RozpoznaniaICD10` | `recognition → icd10` | Lista kodów oddzielona przecinkiem |
| `DokumentSkierowujacyIdImportu` | - | Puste |

### 4.2 KARTY WIZYT (`karty_wizyt.csv`)

| Pole docelowe | Źródło | Transformacja |
|---------------|--------|---------------|
| `InstalacjaId` | - | `null` |
| `IdImport` | `visit.id` | Bezpośrednio |
| `IdImportPrefix` | - | `"VISITCARD"` |
| `DataWystawienia` | `visit.date` | `YYYY-MM-DD 00:00:00` |
| `DataAutoryzacji` | `visit.last_revision_date/time` | Połączenie lub data wizyty |
| `PracownikWystawiajacyIdImport` | `visit.doctor` | Bezpośrednio |
| `PracownikWystawiajacyNpwz` | `person.pwz` | Lookup |
| `PracownikWystawiajacyPesel` | `person.pesel` | Lookup |
| `WizytaIdImport` | `visit.id` | Bezpośrednio |
| `Wywiad` | `visit.interview` | Zamień `\n` na `\\n` |
| `BadaniePrzedmiotowe` | `visit.examination` | Zamień `\n` na `\\n` |
| `PrzyjmowaneLeki` | - | Puste |
| `PrzebiegLeczenia` | `recipe + recipedrug` | Agregacja recept z lekami |
| `Zalecenia` | `visit.recommendation` + `visit.note` | Połącz przez `\\n` |
| `Zabiegi` | `medicalprocedure.icd9` | Lista kodów ICD9 |
| `Inne` | `visitnotes.note` | Agregacja notatek |
| `NiezdolnoscOd` | `sickleave.date_from` | Bezpośrednio |
| `NiezdolnoscDo` | `sickleave.date_to` | Bezpośrednio |
| `RozpoznaniaICD10` | `recognition.icd10` | Lista kodów |
| `RozpoznanieGlowneICD10` | `recognition.icd10[0]` | Pierwszy kod |
| `RozpoznanieWspolistniejaceICD10` | `recognition.icd10[1:]` | Pozostałe kody |
| `RozpoznanieOpisowe` | `visit.recognition_description` | Bezpośrednio |
| `ProceduryICD9` | `medicalprocedure.icd9` | Lista kodów |

### 4.3 PACJENCI (`pacjenci.csv`)

> **⚠️ UWAGA:** Pola `name`, `surname`, `email`, `telephone` NIE są dostępne w eksporcie XML!
> Dane `DataUrodzenia` i `Plec` należy wyliczyć z numeru PESEL.
> Dane o zgonie pobierać z `gabinet_patientdead.xml`.
> Dane opiekuna pobierać z `gabinet_incaseofemergency.xml`.

| Pole docelowe | Źródło XML | Transformacja |
|---------------|------------|---------------|
| `InstalacjaId` | - | `null` |
| `IdImport` | `pk` (atrybut) | Bezpośrednio |
| `UprawnieniePacjentaId` | `patient.rights` | Mapowanie (patrz 5.2) |
| `RodzajPacjenta` | - | `"1"` (osoba fizyczna) |
| `Imie` | **BRAK W XML** | Puste (do uzupełnienia z innego źródła) |
| `Nazwisko` | **BRAK W XML** | Puste (do uzupełnienia z innego źródła) |
| `Pesel` | `pesel` | Bezpośrednio |
| `DataUrodzenia` | `pesel` | **Wyliczyć z PESEL** (patrz 6.3) |
| `CzyUmarl` | `patientdead.pk` | `"1"` jeśli istnieje rekord, inaczej `"0"` |
| `DataZgonu` | `patientdead.date` | Lookup z gabinet_patientdead.xml |
| `DrugieImie` | `second_name` | Bezpośrednio |
| `NazwiskoRodowe` | `maiden_name` | Bezpośrednio |
| `ImieOjca` | **BRAK W XML** | Puste |
| `NIP` | `employer_nip` | Bezpośrednio |
| `Plec` | `pesel` | **Wyliczyć z PESEL** (patrz 6.4) |
| `Email` | **BRAK W XML** | Puste |
| `Telefon` | **BRAK W XML** | Puste |
| `TelefonDodatkowy` | `second_telephone` | Bezpośrednio |
| `NumerDokumentuTozsamosci` | `identity_num` | Bezpośrednio |
| `TypDokumentuTozsamosci` | `identity_type` | Mapowanie (patrz 5.4) |
| `KrajDokumentuTozsamosciKod` | - | `"PL"` (domyślnie) |
| `MiejsceUrodzenia` | `place_of_birth` | Bezpośrednio |
| `KodOddzialuNFZ` | `nfz` | Bezpośrednio (99.6% wypełnione) |
| `UprawnieniePacjenta` | `patient.rights` | Usuń "X" |
| `NumerPacjenta` | `user` | Bezpośrednio |

#### Adres zameldowania (z `address` przez `patient.registration_address`):

| Pole docelowe | Źródło |
|---------------|--------|
| `KrajZameldowanie` | `address.country` → mapowanie kraju |
| `WojewodztwoZameldowanie` | `address.province` |
| `KodTerytGminyZameldowanie` | `address.teryt` |
| `MiejscowoscZameldowanie` | `address.city` |
| `KodPocztowyZameldowanie` | `address.postal_code` |
| `UlicaZameldowanie` | `address.street` |
| `NrDomuZameldowanie` | `address.street_number` |
| `NrMieszkaniaZameldowanie` | `address.flat_number` |

#### Adres zamieszkania (z `address` przez `patient.residence_address`):
*Fallback na adres zameldowania jeśli brak*

#### Opiekun (z `gabinet_incaseofemergency.xml` przez `patient.pk`):

| Pole docelowe | Źródło XML |
|---------------|------------|
| `ImieOpiekuna` | **BRAK** (tylko `last_name` dostępne) |
| `NazwiskoOpiekuna` | `incaseofemergency.last_name` |
| `PlecOpiekuna` | **BRAK** |
| `DataUrodzeniaOpiekuna` | **BRAK** |
| `PeselOpiekuna` | `incaseofemergency.identity_num` |
| `TelefonOpiekuna` | **BRAK** |
| `MiejscowoscOpiekuna` | `incaseofemergency.city` |
| `UlicaOpiekuna` | `incaseofemergency.street` |
| `NrDomuOpiekuna` | `incaseofemergency.flat_number` (parsować) |

#### Flagi kontrolne:

| Pole | Wartość domyślna |
|------|------------------|
| `Uchodzca` | `patient.is_refugee` → `"1"/"0"` |
| `VIP` | `patient.vip` → `"1"/"0"` |
| `SprawdzUnikalnoscIdImportu` | `"1"` |
| `SprawdzUnikalnoscPesel` | `"0"` |
| `AktualizujPoPesel` | `"0"` |
| `Uwagi` | Puste (ustalenia z 25.07) |

### 4.4 PRACOWNICY (`pracownicy.csv`)

| Pole docelowe | Źródło | Transformacja |
|---------------|--------|---------------|
| `InstalacjaId` | - | `null` |
| `IdImport` | `person.id` | Bezpośrednio |
| `Imie` | `user.first_name` | Lookup przez person.user |
| `Nazwisko` | `user.last_name` | Lookup |
| `DrugieImie` | `person.second_name` | Bezpośrednio |
| `Pesel` | `person.pesel` | Bezpośrednio |
| `NIP` | `person.nip` | Bezpośrednio |
| `Plec` | `person.sex` | `"K"/"M"` |
| `Email` | `user.email` | Lookup |
| `Telefon` | `person.telephone` | Bezpośrednio |
| `NumerPWZ` | `person.pwz` | Bezpośrednio |
| `TytulNaukowy` | `person.academic_degree` | Bezpośrednio |
| `Specjalizacja` | `profile.specialties → specialty.name` | Lista oddzielona `;` |
| `TypPersoneluNFZ` | `person.profession_code` | Mapowanie (patrz 5.6) |
| `Login` | `user.username` | Lookup |
| `CzyAktywny` | `user.is_active` | `"1"/"0"` |

### 4.5 STAŁE LEKI (`stale_leki_pacjenta.csv`)

| Pole docelowe | Źródło | Transformacja |
|---------------|--------|---------------|
| `InstalacjaId` | - | `null` |
| `PacjentId` | - | `null` |
| `PacjentIdImport` | `patientpermanentdrug.patient` | Bezpośrednio |
| `PacjentPesel` | `patient.pesel` | Lookup |
| `PracownikId` | - | `null` |
| `PracownikIdImport` | - | `null` |
| `KodKreskowy` | `ver.ean13` lub `composition` | Lookup przez drug_id, fallback na composition |
| `DataZalecenia` | - | Aktualny timestamp |
| `DataZakonczenia` | - | `null` |
| `Dawkowanie` | `patientpermanentdrug.recommendation` | Bezpośrednio |
| `Ilosc` | `patientpermanentdrug.dosation` | Parsuj liczbę |
| `RodzajIlosci` | `patientpermanentdrug.dosation` | Mapowanie jednostki (patrz 5.7) |
| `KodOdplatnosci` | `patientpermanentdrug.payment` | Bezpośrednio |

### 4.6 DEKLARACJE POZ (`deklaracje_poz.csv`)

| Pole docelowe | Źródło | Transformacja |
|---------------|--------|---------------|
| `InstalacjaId` | - | `null` |
| `IdImport` | `nfzdeclaration.id` | Bezpośrednio |
| `TypDeklaracjiPOZ` | `nfzdeclaration.type` | Uppercase (L, P, O) |
| `DataZlozenia` | `nfzdeclaration.creation_date` | `YYYY-MM-DD 00:00:00` |
| `DataWygasniecia` | `nfzdeclaration.deletion_date` | `YYYY-MM-DD 00:00:00` lub `null` |
| `JednostkaId` | - | `null` |
| `JednostkaIdImport` | `nfzdeclaration.department` | Bezpośrednio |
| `PacjentId` | - | `null` |
| `PacjentIdImport` | `nfzdeclaration.patient` | Bezpośrednio |
| `PacjentPesel` | `patient.pesel` | Lookup |
| `TypPacjentaId` | - | `"1"` |
| `PracownikId` | - | `null` |
| `PracownikIdImport` | `nfzdeclaration.personnel` | Bezpośrednio |
| `PracownikNPWZ` | `person.pwz` | Lookup |
| `ProfilaktykaFluorkowa` | - | `"0"` |
| `Komentarz` | `nfzdeclaration.note` | Truncate do 255 znaków |

### 4.7 JEDNOSTKI (`jednostki.csv`)

| Pole docelowe | Źródło | Transformacja |
|---------------|--------|---------------|
| `IdImport` | `office.department` | Bezpośrednio |
| `Nazwa` | `office.name` | Bezpośrednio |
| `Aktywna` | `office.active` | `"1"/"0"` |
| `IdWewnetrzny` | `office.id` | Bezpośrednio |

### 4.8 DOKUMENTY UPRAWNIAJĄCE (`dokumenty_uprawniajace.csv`)

| Pole docelowe | Źródło | Transformacja |
|---------------|--------|---------------|
| `InstalacjaId` | - | `null` |
| `IdImport` | `insurancedocuments.id` | Bezpośrednio |
| `KodDokumentu` | `insurancedocuments.permission_basis` | Mapowanie (patrz 5.8) |
| `KodUprawnienia` | `insurance.permissions_title` | Lookup |
| `NazwaDokumentu` | `insurancedocuments.document_name` | Bezpośrednio |
| `PacjentIdImport` | `insurance.patient` | Lookup |
| `PacjentPesel` | `patient.pesel` | Lookup |
| `KodOddzialuNFZ` | `insurance.id_nfz` | Lookup |
| `DataOd` | `insurancedocuments.valid_from` | Format daty |
| `DataDo` | `insurancedocuments.valid_to` | Format daty |
| `DataWystawienia` | `insurancedocuments.set_date` | Format daty |
| `Numer` | `insurancedocuments.document_number` | Bezpośrednio |

### 4.9 ZAŁĄCZNIKI (`dokumentacja_zalaczniki.csv`)

| Pole docelowe | Źródło | Transformacja |
|---------------|--------|---------------|
| `IdImport` | `documents.id` | Bezpośrednio |
| `InstalacjaId` | - | Puste |
| `PacjentIdImport` | `documents.patient` | Bezpośrednio |
| `PacjentPesel` | `patient.pesel` | Lookup |
| `WizytaIdImport` | `documents.visit` | Bezpośrednio |
| `Data` | `documents.uploaded_date` | ISO → `YYYY-MM-DD HH:MM:SS` |
| `NazwaPliku` | `documents.original_filename` | Bezpośrednio |
| `Opis` | `documenttype.name` + `documents.note` | Połącz przez " - " |
| `Sciezka` | `documents.uploaded_file` | Bezpośrednio |
| `TypPliku` | - | Ekstrahuj rozszerzenie z NazwaPliku |

### 4.10 CHOROBY PRZEWLEKŁE (`stale_choroby_pacjenta.csv`)

| Pole docelowe | Źródło | Transformacja |
|---------------|--------|---------------|
| `InstalacjaId` | - | Puste |
| `PacjentIdImport` | `genericmedicaldata.patient` | Bezpośrednio |
| `PacjentPesel` | `patient.pesel` | Lookup |
| `ICD10` | `genericmedicaldata.icd10` | Bezpośrednio |
| `Opis` | `genericmedicaldata.description` | Bezpośrednio |

**Filtr:** `type_of_data == "chronic"`

### 4.11 SZCZEPIENIA (`szczepienia.csv`)

| Pole docelowe | Źródło | Transformacja |
|---------------|--------|---------------|
| `InstalacjaId` | - | `null` |
| `IdImport` | `vaccination.id` | Bezpośrednio |
| `PacjentIdImport` | `vaccination.patient` | Bezpośrednio |
| `PacjentPesel` | `patient.pesel` | Lookup |
| `PracownikIdImport` | `vaccination.vaccinator` lub `person` | Bezpośrednio |
| `PracownikNPWZ` | `person.pwz` | Lookup |
| `PracownikPesel` | `person.pesel` | Lookup |
| `Nazwa` | `vaccination.drug` | Bezpośrednio |
| `MiejscePodania` | `vaccination.vaccination_site` | Bezpośrednio |
| `NrSerii` | `vaccination.vaccine_series` | Bezpośrednio |
| `DataPodania` | `vaccination.datetime` | ISO → `YYYY-MM-DD HH:MM:SS` |
| `DataWaznosci` | `vaccination.expiration_date` | Format daty |
| `CzyZKalendarza` | `vaccination.vaccination_kind` | `"1"` jeśli zawiera "z kalendarza" |
| `Dawka` | `vaccination.dose` lub `number_of_dose` | Bezpośrednio |

---

## 5. Słowniki mapowań

### 5.1 Status wizyty (`VISIT_STATUS_MAP`)

| Wartość źródłowa | Kod docelowy | Opis |
|------------------|--------------|------|
| `anulowana` | `9` | Odwołane |
| `archiwalna` | `3` | Zrealizowane |
| `zaplanowana` | `1` | Zaplanowane |
| `w trakcie` | `2` | W trakcie |
| `rozliczona` | `4` | Rozliczane |
| `do rozliczenia` | `4` | Rozliczane |
| `pacjent czeka` | `2` | W trakcie |
| `zakończona` | `3` | Zrealizowane |
| `zaplanowana i opłacona` | `1` | Zaplanowane |
| *(inne)* | `3` | Domyślnie: Zrealizowane |

### 5.2 Uprawnienia pacjenta (`PATIENT_RIGHTS_ID_MAP`)

| Wartość źródłowa | ID docelowe |
|------------------|-------------|
| `x` | `null` |
| *(do uzupełnienia)* | |

### 5.3 Płeć (`convert_sex_to_plec`)

| Wartość źródłowa | Kod docelowy |
|------------------|--------------|
| `kobieta`, `k`, `female`, `f` | `K` |
| `mężczyzna`, `m`, `male` | `M` |

### 5.4 Typ dokumentu tożsamości (`DOC_TYPE_MAP`)

| Wartość źródłowa | Kod docelowy |
|------------------|--------------|
| `id_card` | `D` |
| *(do uzupełnienia)* | |

### 5.5 Stopień pokrewieństwa (`RELATIONSHIP_ID_MAP`)

| Wartość źródłowa | ID docelowe |
|------------------|-------------|
| `matka` | `4` |
| `ojciec` | `5` |
| `rodzic` | `1` |

### 5.6 Typ personelu NFZ (`PERSONNEL_TYPE_NFZ_MAP`)

| Kod źródłowy | Kod NFZ |
|--------------|---------|
| `11` | `1` (Lekarz) |
| `18` | `4` (Pielęgniarka) |
| `36` | `36` (Rejestratorka) |

### 5.7 Rodzaj ilości leku (`DRUG_QUANTITY_TYPE_MAP`)

| Jednostka źródłowa | Kod docelowy |
|--------------------|--------------|
| `op` | `1` (Opakowanie) |
| `szt` | `2` (Inna ilość) |
| `ml` | `2` (Inna ilość) |
| *(domyślnie)* | `2` |

### 5.8 Kod dokumentu ubezpieczeniowego (`INSURANCE_DOC_CODE_MAP`)

| ID źródłowe | Kod docelowy | Opis |
|-------------|--------------|------|
| `1` | `OS` | Oświadczenie pacjenta |
| `2` | `OS` | Oświadczenie opiekuna |
| `3` | `E` | EKUZ |
| `4` | `C` | Certyfikat Tymczasowo Zastępujący EKUZ |
| `5`-`11` | `O` | Poświadczenie |
| `12`-`16` | `F` | Formularz serii E |
| `17` | `A` | Decyzja wójta/burmistrza/prezydenta |
| `18` | `KP` | Karta Polaka |
| `19`, `20` | `ESA_ART_INNE` | Dokument potwierdzający uprawnienia |
| `21` | `ESA_RMUA` | Imienny raport miesięczny |
| `22` | `ESA_LU` | Legitymacja ubezpieczeniowa |
| `23` | `ESA_ZG` | Zgłoszenie do ubezpieczenia |
| `24` | `ESA_ZA` | Zaświadczenie potwierdzające prawo |
| `25` | `ESA_LE` | Legitymacja emeryta/rencisty |
| `26` | `KB` | Karta pobytu |
| `27` | `K` | Karta ubezpieczenia zdrowotnego |
| `28` | `ESA_I` | Inny |
| `29`, `30` | `OS` | Oświadczenie (opieka stacjonarna) |
| *(domyślnie)* | `ESA_ART_INNE` | |

### 5.9 Kody krajów (`COUNTRY_CODE_MAP`)

| Kod | Nazwa |
|-----|-------|
| `pl` | Polska |
| `de` | Niemcy |
| `ua` | Ukraina |
| `gb` | Wielka Brytania |
| `fr` | Francja |
| ... | (pełna lista w kodzie) |

---

## 6. Funkcje pomocnicze (C#)

Implementacja w `Processors/BaseModelProcessor.cs` lub dedykowanych procesorach.

### 6.1 Łączenie daty i czasu
```csharp
private string CombineDateTime(string date, string time)
{
    if (string.IsNullOrEmpty(date)) return "";
    if (string.IsNullOrEmpty(time)) return $"{date} 00:00:00";
    return $"{date} {time}";
}
```

### 6.2 Konwersja boolean
```csharp
private string ConvertBooleanTo01(string value)
{
    if (string.IsNullOrEmpty(value)) return "0";
    return value.Equals("True", StringComparison.OrdinalIgnoreCase) 
        || value == "1" || value.Equals("tak", StringComparison.OrdinalIgnoreCase)
        ? "1" : "0";
}
```

### 6.3 Wyliczenie daty urodzenia z PESEL
```csharp
private string ExtractBirthDateFromPesel(string pesel)
{
    if (string.IsNullOrEmpty(pesel) || pesel.Length < 6) return "";
    
    int year = int.Parse(pesel.Substring(0, 2));
    int month = int.Parse(pesel.Substring(2, 2));
    int day = int.Parse(pesel.Substring(4, 2));
    
    // Określenie stulecia na podstawie miesiąca
    int century = month switch
    {
        >= 1 and <= 12 => 1900,
        >= 21 and <= 32 => 2000,
        >= 41 and <= 52 => 2100,
        >= 61 and <= 72 => 2200,
        >= 81 and <= 92 => 1800,
        _ => 1900
    };
    
    if (month >= 21) month -= 20;
    else if (month >= 41) month -= 40;
    else if (month >= 61) month -= 60;
    else if (month >= 81) month -= 80;
    
    return $"{year + century:D4}-{month:D2}-{day:D2}";
}
```

### 6.4 Wyliczenie płci z PESEL
```csharp
private string ExtractSexFromPesel(string pesel)
{
    if (string.IsNullOrEmpty(pesel) || pesel.Length < 10) return "";
    int genderDigit = int.Parse(pesel.Substring(9, 1));
    return genderDigit % 2 == 0 ? "K" : "M";
}
```

### 6.5 Escape nowych linii
```csharp
private string EscapeNewlines(string value)
{
    if (string.IsNullOrEmpty(value)) return "";
    return value.Replace("\r\n", "\\n").Replace("\n", "\\n");
}
```

### 6.6 Parsowanie ilości leku
```csharp
private (string quantity, string unit) ParseDrugQuantity(string dosation)
{
    if (string.IsNullOrEmpty(dosation)) return ("", "");
    var match = Regex.Match(dosation, @"^(\d+)\s*(\w+)");
    return match.Success 
        ? (match.Groups[1].Value, match.Groups[2].Value) 
        : (dosation, "");
}
```

---

## 7. Walidacja

Walidator (`Services/MappingValidator.cs`) sprawdza:

1. **Istnienie pliku**
2. **Poprawność nagłówka** - zgodność z definicją
3. **Liczba kolumn** w każdym wierszu
4. **Wymagane pola** - niepuste wartości
5. **Format dat** - obecność `-` lub `/`
6. **Format PESEL** - 11 cyfr
7. **Format płci** - `M` lub `K`
8. **Nieescapowane znaki nowej linii** w polach

### Wymagane pola per plik:

| Plik | Wymagane pola |
|------|---------------|
| `wizyty.csv` | IdImport, PacjentIdImport, PracownikIdImport, DataOd, DataDo, CzasOd, CzasDo, Status, TypWizyty |
| `karty_wizyt.csv` | IdImport, WizytaIdImport, DataWystawienia |
| `pacjenci.csv` | IdImport, Imie, Nazwisko, Pesel, DataUrodzenia, Plec |
| `pracownicy.csv` | IdImport, Imie, Nazwisko, Pesel, Plec, NumerPWZ |

---

## 8. Format plików wyjściowych

- **Kodowanie:** UTF-8
- **Separator:** `;` (średnik)
- **Escape:** Podwójny cudzysłów dla wartości zawierających `;`, `"`, `\n`
- **Newline escape:** `\n` → `\\n` (literalnie)

---

## 9. Implementacja w MyDr_Import

### 9.1 Struktura projektów

```
MyDr_Import/
├── Program.cs              # Punkt wejścia, parsowanie argumentów
├── Etap1.cs                # Analiza i ekstrakcja XML
├── Etap2.cs                # Wczytanie mapowań, walidacja, generowanie CSV
├── Services/
│   ├── LargeXmlExplorer.cs   # Strumieniowy eksplorer XML
│   ├── ExcelMappingReader.cs # Wczytywanie mapowań z Excel
│   ├── MappingValidator.cs   # Walidacja mapowań vs struktura XML
│   └── CsvGenerator.cs       # Generowanie plików CSV
├── Processors/
│   ├── BaseModelProcessor.cs # Bazowa klasa procesora
│   ├── IModelProcessor.cs    # Interfejs procesora
│   ├── ProcessorRegistry.cs  # Rejestr procesorów + słowniki
│   └── PacjenciProcessor.cs  # Dedykowany procesor dla pacjentów
└── Models/
    ├── FieldMapping.cs       # Model mapowania pola
    ├── ModelMapping.cs       # Model mapowania modelu
    └── XmlObjectInfo.cs      # Info o obiekcie XML
```

### 9.2 Dodawanie nowego procesora

1. Utwórz klasę dziedziczącą po `BaseModelProcessor`:

```csharp
public class WizytyProcessor : BaseModelProcessor
{
    public override string ModelName => "wizyty";
    public override string XmlFileName => "gabinet_visit.xml";
    
    // Cache dla powiązanych danych
    private Dictionary<string, Dictionary<string, string>>? _recognitionCache;
    
    public override CsvGenerationResult Process(...)
    {
        // 1. Załaduj cache z powiązanych XML
        LoadRecognitionCache(dataEtap1Path);
        
        // 2. Wczytaj rekordy z głównego XML
        var records = LoadXmlRecords(xmlPath);
        
        // 3. Generuj CSV z transformacjami
        // ...
    }
}
```

2. Zarejestruj procesor w `ProcessorRegistry.cs`:

```csharp
private static readonly Dictionary<string, Func<IModelProcessor>> _processors = new()
{
    { "pacjenci", () => new PacjenciProcessor() },
    { "wizyty", () => new WizytyProcessor() },
    // ...
};
```

### 9.3 Implementacja słowników mapowań

Słowniki z sekcji 5 należy zaimplementować w `ProcessorRegistry.cs` lub dedykowanych procesorach:

```csharp
public static class MappingDictionaries
{
    public static readonly Dictionary<string, string> VisitStatusMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "anulowana", "9" },
        { "archiwalna", "3" },
        { "zaplanowana", "1" },
        { "w trakcie", "2" },
        { "rozliczona", "4" },
        { "do rozliczenia", "4" },
        { "pacjent czeka", "2" },
        { "zakończona", "3" },
        { "zaplanowana i opłacona", "1" }
    };
    
    public static readonly Dictionary<string, string> PersonnelTypeNfzMap = new()
    {
        { "11", "1" },  // Lekarz
        { "18", "4" },  // Pielęgniarka
        { "36", "36" }  // Rejestratorka
    };
    
    // ... pozostałe słowniki
}
```

### 9.4 Obsługa powiązań (Lookup)

Dla pól wymagających lookup z innych plików XML:

```csharp
private void LoadPatientCache(string dataEtap1Path)
{
    var path = Path.Combine(dataEtap1Path, "data_full", "gabinet_patient.xml");
    _patientCache = new Dictionary<string, Dictionary<string, string>>();
    
    var doc = XDocument.Load(path);
    foreach (var obj in doc.Root.Elements("object"))
    {
        var pk = obj.Attribute("pk")?.Value;
        if (string.IsNullOrEmpty(pk)) continue;
        
        var data = new Dictionary<string, string>();
        foreach (var field in obj.Elements("field"))
        {
            var name = field.Attribute("name")?.Value;
            if (!string.IsNullOrEmpty(name))
                data[name] = field.Value?.Trim() ?? "";
        }
        _patientCache[pk] = data;
    }
}
```

### 9.5 Uwagi wydajnościowe

1. **Pamięć** - duże pliki XML (>100MB) wczytuj strumieniowo z `XmlReader`
2. **Cache** - ładuj powiązane dane do słowników przed przetwarzaniem głównym
3. **Batch** - dla bardzo dużych plików rozważ przetwarzanie w partiach
4. **Logowanie** - nieznane wartości mapowań zbieraj i raportuj na końcu

---

## 10. Użycie

```bash
# Etap 1 - analiza XML i ekstrakcja modeli
dotnet run -- --etap1 sciezka/do/eksport.xml

# Etap 2 - generowanie CSV (wszystkie modele)
dotnet run

# Etap 2 - pojedynczy model (tryb testowy)
dotnet run -- --model=pacjenci

# Eksploracja XML
dotnet run -- explore eksport.xml head 10 gabinet.patient
```
