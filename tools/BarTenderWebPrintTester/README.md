# BarTender Web Print Tester

Web application ASP.NET Core 8 (Blazor Server) per testare l'invio di XML a **BarTender** per la stampa di **etichette di pericolo GHS/CLP**.

## Funzionalita'

- **Dashboard**: riepilogo job ultime 24h (completati/falliti/in coda), link rapido "Nuovo Job"
- **Nuovo Job**: scelta modalita' (File Drop / HTTP), template .btw, stampante, copie, editor XML con generazione sample, validazione, Dry Run, invio con stato live
- **Storico Job**: tabella con filtri (data/esito/modalita'), dettaglio XML (dati sensibili mascherati), log, response/path
- **Impostazioni**: hotfolder path (locale/UNC), endpoint URL, catalogo template, stampanti, test accesso hotfolder e test endpoint HTTP
- **Log Viewer**: ultimi log live con filtro per livello (INFO/WARN/ERROR), auto-refresh

## Stack tecnologico

| Componente | Tecnologia |
|---|---|
| Framework | .NET 8, ASP.NET Core |
| UI | Blazor Server + Bootstrap 5 |
| Database | SQLite (EF Core) |
| Logging | Serilog (console + file + in-memory per UI) |
| Job asincroni | BackgroundService + Channel<T> |
| Real-time | SignalR |
| Responsive | Bootstrap 5 (desktop/tablet/mobile) |

## Requisiti

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- BarTender con Commander/Integration Service attivo (per l'invio reale)

## Setup e avvio

```bash
cd tools/BarTenderWebPrintTester

# Ripristina pacchetti e avvia
dotnet restore
dotnet run --project BarTenderWebPrintTester

# Oppure in modalita' development con hot-reload
dotnet watch --project BarTenderWebPrintTester
```

L'applicazione sara' disponibile su:
- `https://localhost:7150`
- `http://localhost:5150`

## Configurazione

Tutti i parametri sono in `appsettings.json` e possono essere sovrascritti con variabili d'ambiente.

### appsettings.json - sezioni principali

```json
{
  "BarTender": {
    "DefaultIntegrationMode": "FileDrop",
    "Hotfolder": {
      "LocalOutboxPath": "./outbox",
      "TargetPath": "\\\\SERVER\\BarTender\\Hotfolder",
      "UseAtomicMove": true
    },
    "Http": {
      "EndpointUrl": "http://localhost:5160/api/print",
      "AuthMode": "None",
      "TimeoutSeconds": 30
    },
    "RetryPolicy": {
      "MaxRetries": 3,
      "DelaySeconds": [1, 3, 5]
    },
    "Templates": [
      { "Name": "Etichetta Pericolo GHS", "Path": "C:\\BarTender\\Templates\\GHS_HazardLabel.btw" }
    ],
    "Printers": ["Zebra ZT410", "Zebra ZD620"]
  }
}
```

### Override via variabili d'ambiente

```bash
# Esempio: sovrascrivere hotfolder path
export BarTender__Hotfolder__TargetPath="\\\\NEWSERVER\\Share\\Hotfolder"
export BarTender__Http__EndpointUrl="http://bt-server:5160/api/print"
```

## Struttura XML etichetta di pericolo

```xml
<?xml version="1.0" encoding="utf-8"?>
<HazardLabel Version="1.0" Language="it">
  <Product>
    <ProductIdentifier>Acetone</ProductIdentifier>
    <InternalCode>CHM-ACE-001</InternalCode>
    <BatchLot>LOT-2026-0042</BatchLot>
    <NetQuantity>1 L</NetQuantity>
  </Product>
  <Hazard>
    <SignalWord>Pericolo</SignalWord>
    <HazardStatements>
      <Statement>H225 - Liquido e vapori facilmente infiammabili</Statement>
    </HazardStatements>
    <PrecautionaryStatements>
      <Statement>P210 - Tenere lontano da fonti di calore</Statement>
    </PrecautionaryStatements>
    <SupplementalInfo>
      <Statement>EUH066 - L'esposizione ripetuta puo' provocare secchezza della pelle</Statement>
    </SupplementalInfo>
  </Hazard>
  <Pictograms>
    <Pictogram>GHS02</Pictogram>
    <Pictogram>GHS07</Pictogram>
  </Pictograms>
  <Supplier>
    <Name>Azienda S.r.l.</Name>
    <Address>Via Roma 1, 20100 Milano</Address>
    <Phone>+39 02 1234567</Phone>
    <EmergencyPhone>+39 02 7654321</EmergencyPhone>
  </Supplier>
  <UFI>N1QV-10GN-J00G-XXXX</UFI>
  <UNNumber>UN1090</UNNumber>
</HazardLabel>
```

## Pubblicazione su IIS

### 1. Pubblica il progetto

```bash
dotnet publish -c Release -o ./publish
```

### 2. Configura IIS

1. Installa il modulo **ASP.NET Core Hosting Bundle** per .NET 8
2. Crea un nuovo **sito IIS** puntando alla cartella `publish/`
3. Configura l'**Application Pool**:
   - .NET CLR Version: **No Managed Code**
   - Pipeline Mode: **Integrated**
   - Identity: vedi sezione "Best Practice UNC + IIS" sotto
4. Assicurati che il pool abbia permessi di scrittura su:
   - La directory dell'applicazione (per SQLite DB e logs)
   - La cartella outbox
   - La hotfolder UNC

### 3. Troubleshooting IIS

| Errore | Causa | Soluzione |
|---|---|---|
| HTTP 500.30 | ANCM In-Process Start Failure | Verifica che l'Hosting Bundle .NET 8 sia installato |
| HTTP 502.5 | Process Failure | Controlla i log in `logs/` e Event Viewer |
| DB locked | SQLite file locked | Verifica che un solo processo acceda al DB |
| Blank page | Static files | Verifica `UseStaticFiles()` e permessi sulla cartella `wwwroot/` |

---

## Best Practice per UNC + IIS (Hotfolder)

Questa sezione e' critica per il funzionamento della modalita' File Drop quando l'app e' pubblicata su IIS.

### A) Principio base: l'identita' che scrive su UNC non sei "tu", ma il processo IIS

Quando pubblichi su IIS, la web app gira sotto un'identita':

- **Application Pool Identity** (default: `IIS AppPool\<NomePool>`)
- oppure un **Custom Identity** (utente di dominio o locale)
- oppure un **gMSA** (Group Managed Service Account, consigliato in ambienti enterprise)

**Quell'identita' deve avere permessi sul percorso UNC**, altrimenti otterrai errori tipo `Access denied`, `UnauthorizedAccessException`, o silent fail.

### B) Permessi: servono sia Share Permissions sia NTFS Permissions

Su un percorso `\\SERVER\Share\Folder` devi verificare **due livelli**:

1. **Share permissions** (sul network share)
2. **NTFS permissions** (sul file system della cartella)

Per scrivere file XML, assegna all'identita' del pool (o all'utente custom) almeno:

- **Read + Write + Modify** (consigliato) sulla cartella target
- Assicurati che l'**ereditarieta'** su file/sottocartelle sia coerente

### C) Scelta dell'identita' IIS: opzioni raccomandate

#### Opzione 1 (semplice, spesso sufficiente): Custom Identity di dominio

1. Crea un utente di dominio dedicato (es. `DOM\svc_bartender_test`)
2. Imposta l'Application Pool con quell'identita'
3. Dai permessi NTFS + share su UNC a quell'utente

| Pro | Contro |
|---|---|
| Semplice e affidabile | Gestione password/rotazione |

#### Opzione 2 (enterprise consigliata): gMSA

1. Usa un gMSA per l'Application Pool
2. Assegna permessi su UNC al gMSA

| Pro | Contro |
|---|---|
| Niente password da gestire, piu' sicuro | Richiede AD configurato |

#### Opzione 3 (DA EVITARE per UNC): default ApplicationPoolIdentity

Se lasci `ApplicationPoolIdentity`, l'accesso a UNC dipende dal contesto macchina e **spesso fallisce in dominio**.

### D) Il problema "Double-hop" (Kerberos) spiegato semplice

Il "double-hop" capita quando:

1. L'utente si autentica sul server web (**hop 1**)
2. La web app tenta di accedere ad un altro server (file server UNC) usando le credenziali dell'utente (**hop 2**)

In molti casi, IIS **non puo' delegare automaticamente** le credenziali dell'utente al file server -> access denied.

**Soluzione pratica piu' robusta**: non impersonare l'utente, usa sempre una **service identity** (custom identity o gMSA) per accedere all'UNC.

Se invece vuoi davvero accedere "come l'utente loggato", serve:
- Kerberos + SPN corretti + delega configurata in AD (complesso e fragile: non consigliato per un tool di test)

### E) Credenziali: evitare hardcoding, usare configurazioni sicure

Se devi usare credenziali per accedere al file server:

- **NON** salvarle in chiaro in `appsettings.json`
- Usare:
  - Windows Integrated (pool identity) - **consigliato**
  - Secret store (Windows Credential Manager / DPAPI)
  - Variabili ambiente protette in pipeline
  - Key Vault (se gia' presente nell'infrastruttura)

Per un tool interno di test, la via migliore e': **Application Pool con identity di dominio/gMSA e permessi UNC**.

### F) Validazione in app: "Test accesso hotfolder"

Nella pagina **Impostazioni**, il pulsante "Test accesso hotfolder" esegue:

1. Risolve il percorso UNC
2. Verifica:
   - Esistenza cartella
   - Possibilita' di creare un file temporaneo
   - Rename/move (operazione comune per "atomic drop")
   - Delete del file temporaneo
3. Mostra in UI:
   - **Identita' processo** (utente effettivo che sta scrivendo)
   - **Esito di ogni step**
   - **Eccezione completa + suggerimento**

#### Errori comuni e diagnosi

| Errore | Causa | Soluzione |
|---|---|---|
| `UnauthorizedAccessException` | Permessi share/NTFS mancanti per l'identita' IIS | Aggiungere Read+Write+Modify al service account |
| `Path not found` | Share non raggiungibile/DNS/permessi | Verificare `\\server\share` da cmd del server IIS |
| `The network path was not found` | Rete/firewall/SMB disabilitato | Verificare porta 445 (SMB) e connettivita' |
| `Logon failure` | Credenziali errate o account bloccato | Verificare l'account in AD |

### G) Best practice di scrittura file su hotfolder (evita file parziali)

Per evitare che BarTender legga un file mentre lo stai ancora scrivendo:

1. **Scrivi in outbox locale**: `job.partial`
2. **Sposta/rinomina in hotfolder** come operazione finale atomica: `job.xml`
   (BarTender lo vede solo quando e' completo)

L'applicazione implementa questo pattern automaticamente con `UseAtomicMove: true`.

### H) Considerazioni SMB e performance

- Preferire SMB su **LAN stabile / VPN affidabile**
- Evitare scritture troppo frequenti se non necessario
- Il sistema di **retry con backoff** (1s/3s/5s) gestisce errori transient automaticamente
- I **tempi di copia** e dimensioni file sono loggati per troubleshooting

---

## Struttura progetto

```
tools/BarTenderWebPrintTester/
├── BarTenderWebPrintTester.sln
├── README.md
├── samples/
│   ├── sample_hazard_label.xml
│   └── sample_hazard_label_naoh.xml
└── BarTenderWebPrintTester/
    ├── BarTenderWebPrintTester.csproj
    ├── Program.cs
    ├── appsettings.json
    ├── appsettings.Development.json
    ├── Properties/launchSettings.json
    ├── Data/AppDbContext.cs
    ├── Models/
    │   ├── BarTenderSettings.cs
    │   ├── PrintJob.cs
    │   └── XmlLabelData.cs
    ├── Services/
    │   ├── XmlGeneratorService.cs
    │   ├── PrintJobService.cs
    │   ├── PrintJobQueue.cs
    │   ├── PrintJobBackgroundService.cs
    │   ├── HotfolderService.cs
    │   ├── HttpIntegrationService.cs
    │   └── HotfolderTestService.cs
    ├── Hubs/PrintJobHub.cs
    ├── Logging/InMemoryLogSink.cs
    ├── Components/
    │   ├── App.razor
    │   ├── Routes.razor
    │   ├── _Imports.razor
    │   ├── Layout/
    │   │   ├── MainLayout.razor
    │   │   └── NavMenu.razor
    │   └── Pages/
    │       ├── Dashboard.razor
    │       ├── NewJob.razor
    │       ├── JobHistory.razor
    │       ├── Settings.razor
    │       └── LogViewer.razor
    └── wwwroot/css/site.css
```
