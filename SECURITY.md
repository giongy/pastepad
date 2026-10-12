# Sicurezza

## Cosa salva questa app, e dove

Tutto vive in file semplici in `%LOCALAPPDATA%\pastepad`:

| File | Contenuto |
|---|---|
| `cronologia.json` | Tutto ciò che hai copiato, in chiaro |
| `snippets.json` | I testi che hai scelto di salvare |
| `note.json` | Le note del blocco |
| `immagini\` | Le schermate che hai copiato, come BMP |

**Niente di tutto questo è cifrato.** Chiunque abbia accesso a quella
cartella, o a un suo backup, può leggere tutto ciò che hai copiato.

## Cosa *non* viene salvato

Windows definisce dei
[formati degli appunti](https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats)
che permettono a un'applicazione di marcare un contenuto come privato.
pastepad li rispetta tutti e quattro e non salva ciò che ne porta uno:

- `Clipboard Viewer Ignore`
- `ExcludeClipboardContentFromMonitorProcessing`
- `CanIncludeInClipboardHistory` impostato a 0
- `CanUploadToCloudClipboard` impostato a 0

KeePass, Bitwarden, Gestione credenziali di Windows e la modalità in
incognito di Chrome li impostano tutti. Quindi le password copiate da un
gestore di password non finiscono nella cronologia.

Una password che scrivi a mano e copi tu *non* è coperta — niente la
marca come privata. Il pulsante di pausa esiste per quei momenti.

## Raccomandazioni

- Non mettere la cartella in OneDrive, Dropbox o in un servizio di
  sincronizzazione. Un registro in chiaro di tutto ciò che copi non deve
  stare nel cloud.
- Svuota la cronologia regolarmente se condividi il computer.
- Metti in pausa la cattura prima di lavorare con qualcosa di sensibile.
- Su un computer aziendale o gestito, parla con chi si occupa delle
  regole di sicurezza prima di installarlo.

## La rete

L'app fa una sola richiesta di rete: una volta al giorno chiede alla API
di GitHub qual è l'ultima release del progetto originale, per avvisare
se c'è una versione nuova. Non invia dati e non scarica niente. Si
spegne in Aspetto.

## L'eseguibile

L'`.exe` non è firmato. Windows avviserà alla prima esecuzione: succede
con qualsiasi eseguibile non firmato finché non accumula download.

Se preferisci non fidarti di un binario preso da internet, compilalo tu
dai sorgenti di questo repository con `.\pubblica.ps1`: bastano il .NET
10 SDK e un paio di minuti.

## Segnalare un problema

Apri una issue. Se è qualcosa che preferisci non pubblicare, dillo nella
issue senza dettagli e verrai ricontattato.
