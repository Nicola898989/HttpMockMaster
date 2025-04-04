# Istruzioni per l'uso di MockAMMT

Grazie per aver installato MockAMMT, la piattaforma avanzata per l'intercettazione, il proxy e il mock di richieste HTTP.

## Avvio dell'applicazione

Una volta completata l'installazione, puoi avviare MockAMMT in diversi modi:

- **Windows**: Dal menu Start o dall'icona sul desktop
- **macOS**: Dalla cartella Applicazioni o dal Dock se l'hai aggiunto
- **Linux**: Dal menu delle applicazioni o dalla barra di avvio rapido

## Primo avvio

Al primo avvio dell'applicazione:

1. **Backend automatico**: Il servizio backend .NET verrà avviato automaticamente in background
2. **Porte utilizzate**:
   - Il servizio di intercettazione utilizza la porta 8888
   - L'API REST è disponibile sulla porta 5000

Non è necessario avviare manualmente alcun servizio; tutto è gestito automaticamente dall'applicazione.

## Configurazione dell'intercettazione HTTP

Per iniziare a intercettare le richieste HTTP:

1. Configura il tuo browser o applicazione per utilizzare `localhost:8888` come proxy HTTP
2. Tutte le richieste verranno visualizzate nella vista "Richieste" dell'applicazione

## Configurazione del proxy

Per utilizzare la modalità proxy:

1. Vai alla sezione "Proxy" nell'applicazione
2. Attiva la modalità proxy e inserisci il dominio di destinazione
3. Tutte le richieste verranno inoltrate al dominio specificato, ma saranno anche visibili e modificabili

## Regole automatiche

Per configurare risposte automatiche:

1. Vai alla sezione "Regole" nell'applicazione
2. Crea una nuova regola specificando un pattern URL e la risposta desiderata
3. Le richieste che corrispondono al pattern verranno intercettate e riceveranno la risposta configurata

## Scenari di test

Per registrare e riprodurre scenari di test:

1. Vai alla sezione "Scenari" nell'applicazione
2. Avvia la registrazione durante la navigazione o l'utilizzo della tua applicazione
3. Salva lo scenario e riproducilo quando necessario per i test di regressione

## Risoluzione dei problemi

Se riscontri problemi:

- **L'applicazione non si avvia**: Verifica che le porte 8888 e 5000 non siano in uso da altre applicazioni
- **Nessuna richiesta viene visualizzata**: Controlla che il tuo browser o applicazione sia configurato correttamente per utilizzare il proxy
- **Problemi di connessione**: Verifica che le impostazioni del firewall permettano l'accesso alle porte utilizzate

## Supporto

Per ricevere supporto o segnalare problemi, visita il nostro repository GitHub o contatta l'assistenza tecnica.