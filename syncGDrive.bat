@echo off
:: Konfiguracja sciezek
set SRC="C:\PROJ\MyDr_Import"
set DEST="G:\Mój dysk\_PROJ\MyDr_Import"

:: Wykluczenia (Git, binarne, cache)
set EXCLUDE=.git bin obj .vs packages

echo Synchronizacja w toku...
robocopy %SRC% %DEST% /MIR /XD %EXCLUDE% /R:1 /W:1 /MT:8 /FFT

echo Gotowe. Gemini ma teraz dostep do czystych zrodel.
pause