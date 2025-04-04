@echo off
echo === Script di creazione installer MockAMMT per Windows ===

REM Assicurati che la directory per il backend sia presente
echo Creazione directory per il backend...
if not exist ClientApp\backend-dist mkdir ClientApp\backend-dist

REM Pubblica il backend .NET
echo Compilazione e pubblicazione del servizio backend .NET...
dotnet publish BackendService\BackendService.csproj -c Release -o .\ClientApp\backend-dist

REM Assicurati che le directory per le icone esistano
echo Verifica delle directory per le icone...
if not exist ClientApp\build\icons mkdir ClientApp\build\icons

REM Vai nella directory ClientApp ed esegui il build
echo Compilazione dell'applicazione Electron...
cd ClientApp
npx vue-cli-service electron:build --windows

echo === Installer completato! ===
echo Puoi trovare l'installer nella directory: ClientApp\dist_electron\
cd ..