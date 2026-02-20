# BarTender XML Label Test

Tool per testare l'invio di script BTXML a **BarTender** per la stampa di **etichette di pericolo GHS/CLP**.

## Funzionalità

- **Generazione BTXML**: crea script XML conformi a BTXML Version 2.0 con dati etichetta GHS
- **Invio TCP**: invia gli script a BarTender Commander via socket TCP/IP
- **Etichette campione**: include etichette GHS pre-configurate (Acetone, NaOH, Etanolo, H₂O₂)
- **Import JSON**: carica dati etichetta da file JSON personalizzati
- **Test connessione**: verifica la raggiungibilità di BarTender Commander

## Struttura BTXML

Il programma genera XML nel formato BarTender XML Script (BTXML) v2.0:

```xml
<?xml version="1.0" encoding="utf-8"?>
<XMLScript Version="2.0" Name="HazardLabel" ID="HazardLabel_001">
  <Command Name="HazardLabel">
    <Print>
      <Format>C:\Labels\HazardLabel.btw</Format>
      <NamedSubString Name="ProductName">
        <Value>Acetone</Value>
      </NamedSubString>
      <NamedSubString Name="SignalWord">
        <Value>Pericolo</Value>
      </NamedSubString>
      <!-- ... altri campi ... -->
      <PrintSetup>
        <Printer>NomStampante</Printer>
        <IdenticalCopiesOfLabel>1</IdenticalCopiesOfLabel>
        <NumberSerializedLabels>1</NumberSerializedLabels>
      </PrintSetup>
    </Print>
  </Command>
</XMLScript>
```

## Requisiti

- Python 3.9+
- BarTender con Commander attivo (per l'invio TCP)

## Installazione

```bash
pip install -e .
```

## Uso

### Elencare le etichette campione

```bash
btxml-test list-samples
```

### Generare XML (senza inviare)

```bash
# Da etichetta campione
btxml-test generate --sample acetone --btw-path "C:\Labels\GHS.btw"

# Da file JSON
btxml-test generate --json-file samples/acetone_label.json --btw-path "C:\Labels\GHS.btw"

# Salvare su file
btxml-test generate --sample acetone -o output.xml

# Tutte le etichette campione in un batch
btxml-test generate --sample all --btw-path "C:\Labels\GHS.btw"
```

### Testare la connessione a Commander

```bash
btxml-test test-connection --host 192.168.1.100 --port 5170
```

### Inviare a BarTender Commander

```bash
# Inviare etichetta campione
btxml-test send --host 192.168.1.100 --sample acetone --btw-path "C:\Labels\GHS.btw" --printer "ZebraZT410"

# Inviare file XML pre-generato
btxml-test send --host 192.168.1.100 --xml-file output.xml

# Inviare da JSON
btxml-test send --host 192.168.1.100 --json-file samples/acetone_label.json --btw-path "C:\Labels\GHS.btw"
```

## Campi NamedSubString

I nomi dei campi nel BTXML devono corrispondere ai **Named Data Sources** configurati nel template `.btw` di BarTender:

| Campo BTXML              | Descrizione                        |
|--------------------------|------------------------------------|
| `ProductName`            | Nome del prodotto                  |
| `SignalWord`             | Avvertenza (Pericolo/Attenzione)   |
| `Pictograms`            | Pittogrammi GHS (es. GHS02, GHS07)|
| `HazardStatements`      | Indicazioni di pericolo (H-codes)  |
| `PrecautionaryStatements`| Consigli di prudenza (P-codes)    |
| `SupplierName`           | Nome fornitore/produttore          |
| `SupplierAddress`        | Indirizzo fornitore                |
| `SupplierPhone`          | Telefono emergenza                 |
| `CASNumber`              | Numero CAS                         |
| `UNNumber`               | Numero UN                          |
| `Quantity`               | Quantità                           |
| `BatchNumber`            | Numero lotto                       |
| `Date`                   | Data                               |

## Configurazione template BarTender

Nel template `.btw` di BarTender Designer, i campi dati devono essere configurati come **Screen Data** (non database) con la proprietà **Share/Name** corrispondente ai nomi nella tabella sopra.

## Test

```bash
pip install -e ".[dev]"
pytest
```

## Formato JSON etichetta

```json
{
  "product_name": "Acetone",
  "signal_word": "Pericolo",
  "pictograms": ["GHS02", "GHS07"],
  "hazard_statements": ["H225 - Liquido e vapori facilmente infiammabili"],
  "precautionary_statements": ["P210 - Tenere lontano da fonti di calore"],
  "supplier_name": "Azienda S.r.l.",
  "supplier_address": "Via Roma 1, Milano",
  "supplier_phone": "+39 02 1234567",
  "cas_number": "67-64-1",
  "un_number": "UN1090",
  "quantity": "1 L",
  "batch_number": "LOT-001",
  "date": "2026-02-20",
  "copies": 1
}
```
