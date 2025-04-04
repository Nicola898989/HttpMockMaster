# Scripts per generare l'installer di MockAMMT

Questa directory contiene script che automatizzano la creazione di installer per l'applicazione MockAMMT per diversi sistemi operativi.

## Prerequisiti

- **.NET SDK 7.0 o superiore** - Necessario per compilare il backend
- **Node.js e npm** - Necessari per compilare il frontend e l'applicazione Electron
- **Vue CLI** - Installabile con `npm install -g @vue/cli`
- **Vue CLI Plugin Electron Builder** - Installato automaticamente come dipendenza del progetto

## Script disponibili

### Per Windows

```
scripts\build_windows.bat
```

Questo script:
1. Compila il backend .NET in modalità Release
2. Pubblica il backend nella directory `ClientApp/backend-dist`
3. Crea un installer Windows utilizzando Electron Builder con NSIS

L'installer generato sarà disponibile nella directory `ClientApp\dist_electron\`.

### Per macOS

```
./scripts/build_macos.sh
```

Questo script:
1. Compila il backend .NET in modalità Release
2. Pubblica il backend nella directory `ClientApp/backend-dist`
3. Crea un installer macOS (.dmg) e un pacchetto .zip utilizzando Electron Builder

Gli installer generati saranno disponibili nella directory `ClientApp/dist_electron/`.

### Per Linux

```
./scripts/build_linux.sh
```

Questo script:
1. Compila il backend .NET in modalità Release
2. Pubblica il backend nella directory `ClientApp/backend-dist`
3. Crea un AppImage e un pacchetto .deb utilizzando Electron Builder

Gli installer generati saranno disponibili nella directory `ClientApp/dist_electron/`.

### Multi-piattaforma

```
./scripts/build_installer.sh
```

Questo script rileva automaticamente il sistema operativo e genera l'installer appropriato.

## Note

- Gli installer includono sia il frontend Vue.js che il backend .NET, integrati insieme.
- Gli utenti finali non avranno bisogno di installare .NET o Node.js per eseguire l'applicazione.
- Gli installer creano icone desktop e voci nel menu Start/Applicazioni.
- Sono incluse associazioni di file per aprire direttamente i file .mammt con l'applicazione.