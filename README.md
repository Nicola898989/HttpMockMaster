# MockAMMT - HTTP Interceptor & Proxy

Una piattaforma completa per il monitoraggio della rete e il testing, progettata per sviluppatori e professionisti QA, che permette l'intercettazione HTTP avanzata, la configurazione di proxy e la gestione di scenari di test.

## Caratteristiche principali

- **Intercettazione HTTP** - Cattura richieste HTTP locali (GET, POST, PUT, ecc.), inclusi header, body e dettagli del percorso
- **Risposte basate su regole** - Configura risposte automatiche a richieste specifiche in base a regole personalizzate
- **Proxy di richieste** - Inoltra richieste a un dominio specifico mantenendo i dettagli della richiesta originale
- **Registrazione delle richieste** - Memorizza sia le richieste proxy in entrata che in uscita
- **Server di simulazione** - Crea risposte mock per il testing e lo sviluppo delle API
- **Scenari di test** - Registra e riproduce sequenze di richieste/risposte per il testing automatizzato
- **Visualizzazione del flusso** - Animazioni per visualizzare il percorso delle richieste attraverso il sistema

## Installazione

### Installer per utenti finali

Per un'esperienza utente ottimale, abbiamo creato installer che automatizzano completamente il processo di configurazione:

- **Windows**: Scarica l'installer `.exe` dalla sezione Releases
- **macOS**: Scarica il pacchetto `.dmg` dalla sezione Releases
- **Linux**: Scarica il pacchetto `.AppImage` o `.deb` dalla sezione Releases

Gli installer includono sia il frontend che il backend, e non richiedono l'installazione separata di .NET o Node.js.

### Prerequisiti per lo sviluppo

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (v14 o versioni successive)
- [npm](https://www.npmjs.com/get-npm) (v6 o versioni successive)

### Compilazione dai sorgenti

#### Backend

1. Naviga nella directory del progetto
2. Compila il backend .NET:

```bash
dotnet build BackendService

#### Frontend

1. Naviga nella directory del frontend
2. Installa le dipendenze:

```bash
cd ClientApp
npm install
```

3. Avvia l'applicazione in modalità sviluppo:

```bash
npm run electron:serve
```

### Creazione degli installer

Il progetto include script automatizzati per generare installer per diverse piattaforme. Vedi la [documentazione degli script](scripts/README.md) per maggiori dettagli.

#### Creazione rapida degli installer

1. Per Windows:

```bash
scripts\build_windows.bat
```

2. Per macOS:

```bash
./scripts/build_macos.sh
```

3. Per Linux:

```bash
./scripts/build_linux.sh
```

Gli installer generati saranno disponibili nella directory `ClientApp/dist_electron/`.

## Architettura del sistema

### Backend

Il backend è sviluppato in .NET 8 ed è composto da diversi servizi principali:

- **InterceptorService**: Gestisce l'intercettazione HTTP su porta 8888
- **RuleService**: Applica le regole configurate alle richieste intercettate
- **ProxyService**: Inoltra le richieste ai domini target quando è attiva la modalità proxy
- **TestScenarioService**: Registra e riproduce scenari di test

### Frontend

Il frontend è sviluppato con Vue.js ed è integrato in un'applicazione desktop con Electron:

- **Vue.js**: Framework per l'interfaccia utente
- **Vuex**: Gestione dello stato dell'applicazione
- **Electron**: Packaging dell'applicazione desktop
- **GSAP**: Animazioni per la visualizzazione del flusso delle richieste

### Database

Il sistema supporta due opzioni di database:

- **SQLite**: Opzione predefinita per lo sviluppo locale
- **PostgreSQL**: Opzione per ambienti di produzione con maggiori prestazioni

## Licenza

Questo progetto è rilasciato sotto licenza MIT.
