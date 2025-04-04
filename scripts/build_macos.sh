#!/bin/bash

# Uscita in caso di errore
set -e

# Colori per output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== Script di creazione installer MockAMMT per macOS ===${NC}"

# Assicurati che la directory per il backend sia presente
echo -e "${YELLOW}Creazione directory per il backend...${NC}"
mkdir -p ClientApp/backend-dist

# Pubblica il backend .NET
echo -e "${YELLOW}Compilazione e pubblicazione del servizio backend .NET...${NC}"
dotnet publish BackendService/BackendService.csproj -c Release -o ./ClientApp/backend-dist

# Assicurati che le directory per le icone esistano
echo -e "${YELLOW}Verifica delle directory per le icone...${NC}"
mkdir -p ClientApp/build/icons

# Vai nella directory ClientApp ed esegui il build
echo -e "${YELLOW}Compilazione dell'applicazione Electron...${NC}"
cd ClientApp
npx vue-cli-service electron:build --mac

echo -e "${GREEN}=== Installer completato! ===${NC}"
echo -e "Puoi trovare l'installer nella directory: ClientApp/dist_electron/"