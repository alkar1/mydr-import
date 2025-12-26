$source = "C:\PROJ\MyDr_Import"
$destination = "G:\Mój dysk\_PROJ\MyDr_Import"

# Wykluczamy foldery Git, binarne i cache VS
$excludeDirs = ".git", "bin", "obj", ".vs", "packages"

robocopy $source $destination /MIR /XD $excludeDirs /R:1 /W:1 /MT:8
