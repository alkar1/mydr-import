using System.Collections.Generic;

namespace MyDr_Import.Models;

public static class ModelNames
{
    public static readonly Dictionary<string, (string Nazwa, string Opis)> PolishNames = new()
    {
        { "auth.user", ("U¿ytkownik", "Konto u¿ytkownika systemu (loginy, role, has³a)") },
        { "dilo.dilocard", ("Karta DILO", "Karta Diagnostyki i Leczenia Onkologicznego (DiLO) – œcie¿ka onkologiczna") },
        { "ezwolnienia.esickleave", ("e-Zwolnienie", "Elektroniczne zwolnienie lekarskie (e-ZLA)") },
        { "ezwolnienia.esickleavecancellation", ("Anulowanie e-Zwolnienia", "Anulowanie elektronicznego zwolnienia lekarskiego") },
        { "ezwolnienia.esickleavecommon", ("Wspólne dane e-Zwolnienia", "Wspólne pola dla zwolnieñ lekarskich (np. dane pacjenta, lekarza)") },
        { "gabinet.address", ("Adres", "Dane adresowe osoby lub placówki") },
        { "gabinet.attachedprivateservice", ("Do³¹czona us³uga prywatna", "Us³uga prywatna do³¹czona do wizyty lub pakietu") },
        { "gabinet.custodian", ("Opiekun", "Osoba opiekuj¹ca siê pacjentem (np. rodzic, prawny opiekun)") },
        { "gabinet.department", ("Oddzia³ / komórka", "Struktura organizacyjna placówki (np. POZ, pediatria)") },
        { "gabinet.dispanserygroup", ("Grupa dyspanseryjna", "Grupa pacjentów pod sta³¹ opiek¹ (dyspanseryzacja, np. cukrzyca)") },
        { "gabinet.documents", ("Dokumenty", "Ogólne dokumenty medyczne przypisane do pacjenta lub wizyty") },
        { "gabinet.documenttype", ("Typ dokumentu", "Kategoria dokumentów (np. skierowanie, wynik badañ)") },
        { "gabinet.facility", ("Placówka", "Instancja gabinetu/przychodni – ustawienia systemowe (NFZ ID, regon, godziny)") },
        { "gabinet.footer", ("Stopka", "Stopka dokumentów/wydruków (np. dane placówki w footerze recepty)") },
        { "gabinet.genericmedicaldata", ("Ogólne dane medyczne", "Podstawowe dane zdrowotne pacjenta (np. alergie, wywiad)") },
        { "gabinet.icd10", ("Kod ICD-10", "S³ownik ICD-10 z kodami, opisami, flagami raka/ostrych") },
        { "gabinet.icd9", ("Kod ICD-9", "S³ownik procedur ICD-9-CM (starszy system dla raportów)") },
        { "gabinet.imagingtest", ("Badanie obrazowe", "Wyniki badañ obrazowych (RTG, USG, MRI itp.)") },
        { "gabinet.incaseofemergency", ("Kontakt w nag³ych wypadkach", "Dane ICE – kontakt w razie wypadku") },
        { "gabinet.insurance", ("Ubezpieczenie", "S³ownik rodzajów ubezpieczeñ / uprawnieñ NFZ") },
        { "gabinet.insurancedocuments", ("Dokumenty ubezpieczeniowe", "Dokumenty potwierdzaj¹ce ubezpieczenie (np. legitymacja)") },
        { "gabinet.invoice", ("Faktura", "Faktura za us³ugi prywatne lub rozliczenia") },
        { "gabinet.medicalprocedure", ("Procedura medyczna", "S³ownik procedur, kodów NFZ, punktów") },
        { "gabinet.nfzdeclaration", ("Deklaracja NFZ", "Deklaracja wyboru POZ dla NFZ") },
        { "gabinet.nfzdeklreport", ("Raport deklaracji NFZ", "Raport zbiorczy deklaracji POZ do NFZ") },
        { "gabinet.nfzdeklreportfeedback", ("Feedback raportu deklaracji NFZ", "OdpowiedŸ/zwrotka z NFZ na raport deklaracji") },
        { "gabinet.nfzservicereport", ("Raport us³ug NFZ", "Raport œwiadczonych us³ug do NFZ") },
        { "gabinet.nfzservicereportserviceset", ("Zestaw us³ug w raporcie NFZ", "Zbiór us³ug w raporcie do NFZ") },
        { "gabinet.office", ("Gabinet / pokój", "Lokalizacja wewn¹trz placówki (np. gabinet nr 1)") },
        { "gabinet.patient", ("Pacjent", "Dane pacjenta (rozszerzenie osoby o dane medyczne)") },
        { "gabinet.patientdead", ("Zgon pacjenta", "Data i przyczyna zgonu") },
        { "gabinet.patientnote", ("Notatka pacjenta", "Wolny tekst notatki przypisany do pacjenta") },
        { "gabinet.patientpermanentdrug", ("Sta³y lek pacjenta", "Lista leków sta³ych (przewlek³ych) pacjenta") },
        { "gabinet.person", ("Osoba", "Bazowy model dla pacjentów, lekarzy, personelu (PESEL, dane kontaktowe)") },
        { "gabinet.personalsequence", ("Sekwencja osobista", "Sekwencja numeracyjna dla dokumentów osobistych (np. numer recepty)") },
        { "gabinet.personalstencil", ("Szablon osobisty", "Szablon dokumentów osobistych lekarza (np. piecz¹tka)") },
        { "gabinet.personalvisitstencil", ("Szablon wizyty osobistej", "Szablon dla wizyt osobistych lekarza") },
        { "gabinet.printingsettings", ("Ustawienia drukowania", "Konfiguracja wydruków (np. us³ugi prywatne)") },
        { "gabinet.privateservice", ("Us³uga prywatna", "Us³uga odp³atna poza NFZ") },
        { "gabinet.privateservicespackage", ("Pakiet us³ug prywatnych", "Pakiet us³ug prywatnych (np. abonament)") },
        { "gabinet.profile", ("Profil", "Profil u¿ytkownika lub placówki (ustawienia personalne)") },
        { "gabinet.recipe", ("Recepta", "Nag³ówek recepty (numer, data, wystawiaj¹cy)") },
        { "gabinet.recipedrug", ("Pozycja recepty", "Lek na recepcie (dawka, iloœæ)") },
        { "gabinet.recipenumber", ("Numer recepty", "Sekwencja numerów recept") },
        { "gabinet.recipepreferences", ("Preferencje recept", "Ustawienia domyœlne dla recept (np. refundacja)") },
        { "gabinet.recognition", ("Rozpoznanie", "Diagnoza na wizycie (ICD-10, typ)") },
        { "gabinet.shareddocument", ("Dokument wspó³dzielony", "Dokument udostêpniony miêdzy pacjentami/placówkami") },
        { "gabinet.sickleave", ("Zwolnienie lekarskie", "Starszy model zwolnienia (przed e-ZLA)") },
        { "gabinet.specialty", ("Specjalizacja", "Specjalizacja lekarska (np. rodzinna, pediatria)") },
        { "gabinet.teryt", ("TERYT", "Kod terytorialny (miejscowoœæ, gmina)") },
        { "gabinet.tile", ("Kafel", "Element UI / dashboard (np. kafelki w interfejsie)") },
        { "gabinet.vaccination", ("Szczepienie", "Rekord szczepienia (data, szczepionka)") },
        { "gabinet.ver", ("S³ownik leków VER", "Wersja leku z BLOZ (nazwa, dawka)") },
        { "gabinet.visit", ("Wizyta", "Rekord wizyty (data, pacjent, lekarz)") },
        { "gabinet.visitnotes", ("Historia wizyty", "Log zmian statusu wizyty") },
        { "gabinet.visittype", ("Typ wizyty", "Typ wizyty (np. POZ, specjalistyczna, domowa)") },
        { "nfz.attachednfzservice", ("Do³¹czona us³uga NFZ", "Us³uga NFZ do³¹czona do kontraktu lub raportu") },
        { "nfz.nfzcontract", ("Kontrakt NFZ", "Umowa z NFZ na œwiadczenia") },
        { "nfz.nfzcontractfile", ("Plik kontraktu NFZ", "Za³¹cznik do kontraktu NFZ") },
        { "nfz.nfzserviceplan", ("Plan us³ug NFZ", "Planowanie us³ug w kontrakcie NFZ") }
    };

    public static (string Nazwa, string Opis)? Get(string modelName)
    {
        PolishNames.TryGetValue(modelName, out var value);
        return value;
    }
}