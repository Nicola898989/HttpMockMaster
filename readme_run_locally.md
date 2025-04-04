# Guida per l'esecuzione locale dell'applicazione

## Panoramica
Questa guida ti aiuta a eseguire l'applicazione localmente, in quanto l'ambiente Replit ha alcuni limiti che possono causare timeout durante l'avvio del servizio.

## Problema di avvio su Replit
Il problema principale sembra essere legato all'uso di `HttpListener` nel `InterceptorService`, che richiede privilegi elevati su Linux e può causare timeout sull'ambiente Replit durante l'avvio del workflow.

## Esecuzione locale

### Prerequisiti
1. .NET SDK 7.0 o successivo installato localmente
2. Un IDE come Visual Studio, VS Code, o Rider (opzionale, ma consigliato)

### Passaggi per l'esecuzione

1. **Clona o scarica il repository** sul tuo computer locale

2. **Esecuzione dal terminale**:
   ```bash
   cd /percorso/alla/cartella/BackendService
   dotnet run
   ```

3. **In alternativa, da Visual Studio**:
   - Apri il file della soluzione `MockAMMT.sln`
   - Imposta `BackendService` come progetto di avvio
   - Premi F5 o il pulsante "Avvia"

### Note per Windows
Su Windows, per utilizzare HttpListener sulla porta 8888, potrebbe essere necessario eseguire Visual Studio come amministratore o eseguire questo comando in un prompt come amministratore:
```
netsh http add urlacl url=http://+:8888/ user=Everyone
```

## Struttura del progetto

L'applicazione è composta da:

1. **BackendService**: Il backend .NET che gestisce:
   - Intercettazione HTTP (porta 8888)
   - API REST (porta 5000)
   - Funzionalità di proxy
   - Regole di risposta
   - Simulazione di rete
   - Scenari di test

2. **ClientApp**: L'applicazione front-end Vue.js/Electron

## Verifica delle correzioni nel PerformanceController

Le correzioni al `PerformanceController.cs` sono state implementate introducendo classi tipizzate anziché utilizzare oggetti anonimi. Tutti i tipi di risposta sono ora definiti come classi pubbliche.

Per verificare le correzioni localmente:
1. Sostituisci il file `BackendService/Controllers/PerformanceController.cs` con `BackendService/Controllers/PerformanceController.new.cs`
2. Esegui l'applicazione
3. Accedi agli endpoint:
   - http://localhost:5000/api/performance/metrics
   - http://localhost:5000/api/performance/timeseries

## Cosa fare se persiste il problema su Replit

Se il problema di timeout persiste su Replit anche dopo le correzioni, considera:

1. Modificare temporaneamente `InterceptorService` per disabilitare l'avvio di `HttpListener` durante i test su Replit
2. Eliminare operazioni di pulizia inutili all'avvio
3. Ottimizzare la creazione del database rimuovendo le migrazioni manuali

Buon debugging!