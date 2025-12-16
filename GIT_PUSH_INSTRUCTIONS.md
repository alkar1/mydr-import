# Instrukcje Push do Remote Repository

## ? Status: Initial Commit Wykonany

Pierwsze commit zosta³o pomyœlnie wykonane:
- **Commit hash:** `4b21760`
- **Branch:** `master`
- **Message:** "Initial commit: Etap 1 - Analiza struktury XML"
- **Pliki:** 31 plików, 2050 linii kodu

---

## ?? Krok 1: Utworzenie Remote Repository

### Opcja A: GitHub

1. PrzejdŸ na https://github.com/new
2. Nazwa repozytorium: `mydr-import`
3. Opis: `Medical data XML to CSV import tool with structure analysis`
4. Typ: Public lub Private (wybierz wed³ug preferencji)
5. **NIE** zaznaczaj opcji:
   - ? Add a README file
   - ? Add .gitignore
   - ? Choose a license
   
   *(Mamy ju¿ te pliki lokalnie)*

6. Kliknij **Create repository**

### Opcja B: GitLab

1. PrzejdŸ na https://gitlab.com/projects/new
2. Nazwa: `mydr-import`
3. Opis: `Medical data XML to CSV import tool`
4. Visibility: Private/Public
5. NIE inicjalizuj z README
6. Utwórz projekt

### Opcja C: Azure DevOps

1. PrzejdŸ do swojej organizacji Azure DevOps
2. Utwórz nowy projekt: `MyDr_Import`
3. Wybierz Git jako version control
4. Utwórz projekt

---

## ?? Krok 2: Dodanie Remote do Lokalnego Repozytorium

Po utworzeniu remote repository, skopiuj URL i wykonaj:

### Dla GitHub:

```powershell
cd C:\PROJ\MyDr_Import

# HTTPS (³atwiejsze dla Windows)
git remote add origin https://github.com/TWOJA-NAZWA-UZYTKOWNIKA/mydr-import.git

# LUB SSH (wymaga konfiguracji kluczy)
git remote add origin git@github.com:TWOJA-NAZWA-UZYTKOWNIKA/mydr-import.git
```

### Dla GitLab:

```powershell
cd C:\PROJ\MyDr_Import
git remote add origin https://gitlab.com/TWOJA-NAZWA-UZYTKOWNIKA/mydr-import.git
```

### Dla Azure DevOps:

```powershell
cd C:\PROJ\MyDr_Import
git remote add origin https://dev.azure.com/ORGANIZACJA/PROJEKT/_git/MyDr_Import
```

---

## ?? Krok 3: Push do Remote Repository

### Pierwsza synchronizacja:

```powershell
# SprawdŸ czy remote zosta³ dodany
git remote -v

# Push master branch
git push -u origin master
```

Flaga `-u` (upstream) ³¹czy lokalny branch `master` z remote branch `origin/master`.

### Jeœli preferujesz nazwê "main" zamiast "master":

```powershell
# Zmieñ nazwê brancha
git branch -M main

# Push
git push -u origin main
```

---

## ?? Krok 4: Uwierzytelnienie

### GitHub - Personal Access Token (PAT)

Jeœli u¿ywasz HTTPS i GitHub poprosi o credentials:

1. PrzejdŸ: https://github.com/settings/tokens
2. Generate new token (classic)
3. Zaznacz scopes: `repo` (full control)
4. Skopiuj token
5. Przy pierwszym push:
   - Username: twoja_nazwa_uzytkownika
   - Password: WKLEJ_TOKEN (nie has³o!)

**Windows Credential Manager** zapamiêta token automatycznie.

### Alternatywnie: GitHub CLI

```powershell
# Zainstaluj GitHub CLI
winget install GitHub.cli

# Zaloguj siê
gh auth login

# Push
git push -u origin master
```

### SSH Keys (Zaawansowane)

```powershell
# Generuj klucz SSH (jeœli nie masz)
ssh-keygen -t ed25519 -C "twoj@email.com"

# Kopiuj klucz publiczny
cat ~/.ssh/id_ed25519.pub | clip

# Dodaj na GitHub: Settings > SSH and GPG keys > New SSH key
# Wklej skopiowany klucz

# U¿yj SSH URL
git remote set-url origin git@github.com:USERNAME/mydr-import.git
git push -u origin master
```

---

## ? Krok 5: Weryfikacja

Po pomyœlnym push:

```powershell
# SprawdŸ status
git status

# Powinno pokazaæ:
# On branch master
# Your branch is up to date with 'origin/master'.
# nothing to commit, working tree clean
```

OdwiedŸ swoje repozytorium online i sprawdŸ:
- ? Wszystkie pliki zosta³y przes³ane
- ? README.md jest wyœwietlany na stronie g³ównej
- ? Historia commitów jest widoczna

---

## ?? Przydatne Komendy Git

### Codzienne u¿ycie:

```powershell
# Status (co siê zmieni³o)
git status

# Dodaj zmienione pliki
git add .
# LUB konkretny plik
git add Program.cs

# Commit
git commit -m "Opis zmian"

# Push
git push

# Pull (pobierz zmiany z remote)
git pull

# Historia
git log --oneline --graph --all
```

### Cofniêcie zmian:

```powershell
# Cofnij zmiany w pliku (przed add)
git checkout -- plik.cs

# Cofnij staged changes (po add)
git reset HEAD plik.cs

# Cofnij ostatni commit (zachowaj zmiany)
git reset --soft HEAD~1

# Cofnij ostatni commit (usuñ zmiany)
git reset --hard HEAD~1
```

### Branches:

```powershell
# Lista branchy
git branch

# Utwórz nowy branch
git checkout -b feature/nowa-funkcja

# Prze³¹cz siê miêdzy branchami
git checkout master

# Merge branch do master
git checkout master
git merge feature/nowa-funkcja

# Usuñ branch
git branch -d feature/nowa-funkcja
```

---

## ??? Tagowanie Wersji

Dla wersji 1.0.0 (Etap 1):

```powershell
# Utwórz annotated tag
git tag -a v1.0.0 -m "Release v1.0.0: Etap 1 - Analiza struktury XML"

# Push tag do remote
git push origin v1.0.0

# Push wszystkich tagów
git push --tags
```

---

## ?? Statystyki Repozytorium

```powershell
# Liczba commitów
git rev-list --count HEAD

# Liczba linii kodu
git ls-files | where { $_ -match '\.(cs|csproj|md)$' } | foreach { (Get-Content $_).Count } | Measure-Object -Sum

# Ostatnie zmiany
git log --since="1 week ago" --oneline

# Contributors
git shortlog -s -n
```

---

## ?? Praca Zespo³owa

### Pull Request / Merge Request workflow:

```powershell
# 1. Utwórz feature branch
git checkout -b feature/csv-export

# 2. Pracuj nad funkcj¹
# ... edytuj pliki ...

# 3. Commit lokalnie
git add .
git commit -m "Dodano CSV export"

# 4. Push feature branch
git push -u origin feature/csv-export

# 5. Na GitHub/GitLab/Azure:
#    - Otwórz Pull Request / Merge Request
#    - Code review
#    - Merge do master/main
```

---

## ?? .gitignore - Co Jest Ignorowane

Zgodnie z `.gitignore`:

```
? Œledzone (w repo):
- Kod Ÿród³owy (*.cs)
- Pliki projektu (*.csproj)
- Dokumentacja (*.md)
- Konfiguracja (.gitignore, LICENSE)

? Ignorowane (NIE w repo):
- Pliki binarne (bin/, obj/)
- Dane wejœciowe (data/*.xml) - za du¿e!
- Raporty wyjœciowe (output/*.csv)
- Logi (*.log)
- User settings (.vs/, *.user)
- NuGet packages (automatycznie przywracane)
```

**UWAGA:** Plik `gabinet_export_2025_12_09.xml` (8.5 GB) NIE zostanie przes³any do repo!
Jest w `.gitignore`. To prawid³owe - dane trzymaj lokalnie lub w cloud storage.

---

## ?? Udostêpnianie Projektu

Po push do GitHub, inni mog¹ sklonowaæ:

```powershell
# Klonowanie repo
git clone https://github.com/USERNAME/mydr-import.git
cd mydr-import

# Instalacja zale¿noœci
dotnet restore

# Kompilacja
dotnet build

# Uruchomienie
dotnet run -- "sciezka/do/pliku.xml"
```

---

## ?? Troubleshooting

### Problem: "failed to push some refs"

```powershell
# Remote ma zmiany których nie masz lokalnie
git pull --rebase origin master
git push
```

### Problem: "Updates were rejected because the remote contains work"

```powershell
# Wymuszone przes³anie (OSTRO¯NIE!)
git push --force origin master
```

### Problem: Zbyt du¿y plik (GitHub limit: 100 MB)

```powershell
# Usuñ plik z historii
git filter-branch --tree-filter 'rm -f duzy_plik.xml' HEAD

# LUB u¿yj Git LFS dla du¿ych plików
git lfs install
git lfs track "*.xml"
git add .gitattributes
```

### Problem: Przypadkowo dodano wra¿liwe dane

```powershell
# Usuñ z historii
git filter-branch --force --index-filter \
  "git rm --cached --ignore-unmatch sciezka/do/pliku" \
  --prune-empty --tag-name-filter cat -- --all

# Force push
git push origin --force --all
```

---

## ?? Przyk³adowy Workflow

```powershell
# Dzieñ 1: Praca nad now¹ funkcj¹
git checkout -b feature/etap2-csv-export
# ... kod ...
git add .
git commit -m "WIP: CSV exporter podstawowa implementacja"
git push -u origin feature/etap2-csv-export

# Dzieñ 2: Kontynuacja
# ... wiêcej kodu ...
git add .
git commit -m "Dodano batch processing do CSV exportera"
git push

# Dzieñ 3: Ukoñczenie
# ... finalizacja ...
git add .
git commit -m "Ukoñczono Etap 2: CSV Export z testami"
git push

# Na GitHub: Create Pull Request -> Code Review -> Merge

# Lokalnie: aktualizacja master
git checkout master
git pull
git branch -d feature/etap2-csv-export
```

---

## ?? Dodatkowe Zasoby

- **Git Documentation:** https://git-scm.com/doc
- **GitHub Guides:** https://guides.github.com/
- **Git Cheat Sheet:** https://training.github.com/downloads/github-git-cheat-sheet.pdf
- **Pro Git Book (PL):** https://git-scm.com/book/pl/v2

---

**Powodzenia z pierwszym push! ??**

Po wykonaniu push, zaktualizuj README.md z w³aœciwym URL repozytorium w badge'ach.
