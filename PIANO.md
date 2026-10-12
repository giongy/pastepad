# Piano — riscrittura in C# con WinUI 3

Piano tecnico verificato sulla documentazione ufficiale, 12 ago 2026.
Completa `PASSAGGIO.md`: quello dice cosa conservare e perché fu
abbandonato Flet; questo dice come si costruisce il nuovo e in che
ordine.

Tutto ciò che si afferma qui ha una fonte. Ciò che non si è potuto
verificare è segnato come tale e ha un passo o un piano B che lo copre.

È un documento storico, scritto dall'autore originale durante la
riscrittura. I nomi di file e classi sono riportati come sono oggi in
questa copia in italiano; quelli della versione in Python (`modelo.py`,
`windows.py`...) sono rimasti com'erano.

---

## Stato

| Passo | Stato |
|---|---|
| 1 — scorciatoia e appunti | Meccanismo provato in un progetto di sondaggio a parte, poi tolto dal repo |
| 2 — logica e 19 prove | **Fatto.** In verde in `csharp/Pastepad.Nucleo.Prove` (le 19 della versione in Python più quelle che sorvegliano il formato del file) |
| 3 — il guscio | Montato e verificato dal qa: **l'incolla passa** (12/12), scorciatoia 30/30 due volte, istanza unica, area di notifica, bordi |
| 4 — l'interfaccia | **Fatto.** Il qa la dà per pronta: 24 controlli, 24 passano. Due giri del ciclo designer → programmatore → pianificatore → qa |
| 5 — pubblicare e misurare | **Fatto.** Installer di 47,1 MB, verificato dal qa: 22 controlli, 21 puliti |
| 6 — ritirare la versione in Python | **Fatto.** Vive nel tag `ultima-version-python` |

Soluzione in `csharp/Pastepad.slnx`. Le prove girano con
`dotnet test csharp/Pastepad.Nucleo.Prove`, senza finestra e senza il
Windows App SDK. Oggi sono **118**.

**Occhio a quale binario si misura.** Le diverse architetture scrivono
in cartelle diverse, e misurare sul binario vecchio ha già dato due
conclusioni false: al programmatore, che `ResizeClient` funzionava; e al
qa, che l'impilamento non avveniva e che il margine del dialogo era di
17 px. Controllare data e ora dell'`.exe` prima di credere a una misura.

### Aperto: l'installer silenzioso non restituisce il controllo

Trovato il 13 ago 2026 aggiornando dalla 4.2.0 alla 4.3.0 sulla macchina
dell'utente, con l'installer pubblicato.

**L'aggiornamento riuscì perfettamente.** Chiuse in modo pulito, riversò
—una copia fatta 300 ms prima e non ancora scritta sopravvisse— e
pastepad tornò da solo sei secondi dopo.

La cosa strana è un'altra: `Start-Process -Wait` sull'installer **non
tornò mai**, e fu interrotto dopo 10 minuti. Il lavoro era fatto dopo 15
secondi.

Il sospetto, non verificato: `RmRestart` lancia pastepad e il processo
eredita gli handle dell'installer, quindi chi aspetta l'installer
finisce per aspettare pastepad, che non si chiude mai. Se è così,
**qualsiasi script di aggiornamento silenzioso resta appeso** — e la
4.2.0 fu pubblicata proprio perché aggiornare senza sorveglianza
funzionasse.

Non riguarda chi esegue l'installer a mano. Verificarlo è guardare il
processo padre del pastepad riaperto, o lanciare l'installer
reindirizzando l'output su un file e vedere se torna.

### Ciò che resta aperto alla chiusura del passo 6

- **L'avvio a freddo dopo il riavvio non è ancora misurato.** È l'unico
  numero che manca al requisito due. Lo dà la prima riga di
  `errori.log` il giorno in cui si riavvia il computer: cerca `pronto
  in`. A caldo sono 420–463 ms.
- ~~**L'opzione della cartella dati isola i dati ma NON l'avvio
  automatico.**~~ **Chiuso nella 4.2.0.** Era successo altre due volte
  durante il lavoro su quella versione, quindi quell'opzione ora implica
  non toccare il registro e lo dice nel log.
- **Senza firma.** SmartScreen avvisa. Il certificato di SignPath è da
  richiedere, ed esige verifica in due passaggi e approvazione manuale
  di ogni release. Bisogna firmare **due volte**: l'`.exe` prima di
  costruire l'installer e l'installer dopo.
- **Il changelog non ha voci per la 3.0.0 né per la 3.0.1**, anche se i
  tag esistono. Salta dalla 2.0.0 alla 4.0.0.

### Ciò che rimase aperto alla chiusura del passo 4

Niente di questo blocca; resta scritto perché non si perda.

- **I 18 pallini di accento espongono il loro identificatore crudo**
  come nome accessibile: `menta_fredda`, `ambra`, `pesca`.
- **`Registro.PercorsoUsato` non lo legge nessuno.** O si usa
  nell'avviso di avvio degradato —che oggi dice cosa è fallito ma non
  dove sta il dettaglio— o si toglie.
- **`App.xaml` e `Stile.cs` dichiarano gli stessi colori due volte.**
  Oggi coincidono. Niente impedisce che smettano di farlo, e non ci
  sarebbe un errore di compilazione a segnalarlo.

### L'errore dell'istanza che non vede la sua cartella

Trovato dal qa il 12 ago 2026, ed era lo stesso sintomo già visto due
volte senza spiegazione.

Un'istanza partì **senza poter usare `%LOCALAPPDATA%\pastepad` per
niente**: non lesse la cronologia né `config.json`, e non scrisse una
sola riga nel log. Eppure la scorciatoia andava perfettamente (60/60) e
l'incolla funzionava. Da fuori sembrava sana. Chiudendosi salvò la
cronologia vuota sopra quella reale e si perse tutto ciò che era di
quella sessione.

Ciò che fu sistemato subito è il danno, che era la cosa grave:

- Un file che esiste e non si può leggere **resta segnato e non si
  sovrascrive** per tutta la sessione. Quattro prove lo fissano.
- L'avvio in quello stato **apre il pannello con l'avviso**, invece di
  fingere normalità.
- Il log accanto all'eseguibile dice dove sta il dettaglio, e avvisa se
  quello principale non si è potuto scrivere.

La causa fu trovata il 14 ago 2026: era il reindirizzamento di
contenitore descritto in `CLAUDE.md`.

---

**Attenzione provando:** senza l'opzione `--dati` il programma usa
`%LOCALAPPDATA%\pastepad`, che è la cartella dei **dati veri**.
Qualsiasi prova sporca la cronologia reale.

---

## Il problema in una frase

Riscrivere pastepad conservando la logica già provata (1.208 righe e 19
prove) e il formato dei dati, in modo che **la scorciatoia globale non
muoia più** — che è l'unica cosa che la versione in Python non ottenne
mai.

---

## Ciò che fu scoperto

### Versioni e modello di partenza

| Cosa | Valore |
|---|---|
| Windows App SDK stabile | 2.3.1 (16 lug 2026) |
| .NET | SDK 10; TFM `net10.0-windows10.0.26100.0` |
| IDE | Visual Studio 2026, workload *WinUI application development* |
| Senza VS | `dotnet new install Microsoft.WindowsAppSDK.WinUI.CSharp.Templates` e poi `dotnet new winui` |
| Minimo di Windows | 10 v1809 (17763) |

Fonti: [download dell'SDK][d1], [prima app WinUI 3][d2],
[WinUI 3][d3].

**Non impacchettato.** `<WindowsPackageType>None</WindowsPackageType>`
attiva l'auto-inizializzatore del bootstrapper. Richiede il Visual C++
Redistributable sulla macchina di destinazione. Il runtime dell'SDK si
risolve in due modi: installer a parte, o
`<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>`, che lo
mette nella cartella di output ed elimina la dipendenza esterna.
Vedi [distribuzione non impacchettata][d4] e
[distribuire non impacchettato][d5].

Senza identità di pacchetto si perdono: aggiornamenti tramite App
Installer, attività in background da manifesto, associazioni di file da
manifesto e personalizzazione del riquadro. **pastepad non usa niente di
tutto questo.** La stessa pagina conferma che i meccanismi Win32
tradizionali —voci di registro scritte dall'installer e collegamenti—
continuano a funzionare, quindi **l'avvio automatico da `HKCU\...\Run`
sopravvive così com'è**.

### La scorciatoia globale — il punto critico

Tre pezzi, ognuno verificato:

1. **HWND della finestra** — `WinRT.Interop.WindowNative.GetWindowHandle(this)`.
   Vedi [ottenere l'HWND][d6].
2. **Registrazione** — `RegisterHotKey(hWnd, id, MOD_CONTROL|MOD_SHIFT|MOD_NOREPEAT, vk)`.
   La documentazione dice che con `hWnd` nullo i `WM_HOTKEY` si
   pubblicano nella coda del **thread** chiamante, e che con un HWND
   reale il sistema li pubblica nella coda di **quella finestra**.
   Fallisce se si associa la scorciatoia a una finestra creata da un
   altro thread, quindi va chiamato dal thread dell'interfaccia.
   `MOD_NOREPEAT` è `0x4000`, F12 è riservato e l'`id` va da `0x0000` a
   `0xBFFF`. Vedi [RegisterHotKey][d7] e [WM_HOTKEY][d8].
3. **Dove si gestisce** — WinUI 3 non espone `WndProc`. La strada
   documentata per intercettare i messaggi di un HWND è
   `SetWindowSubclass` con un `SUBCLASSPROC`, tolto con
   `RemoveWindowSubclass`. Vedi [SetWindowSubclass][d9], la
   [discussione 8934][d10] del repo di WinUI e la [domanda 5815][d11],
   che pone esattamente questo dubbio e fu chiusa senza risposta
   ufficiale.

**L'errore di fondo della versione precedente.** Il codice in Python
chiama `RegisterHotKey(None, ...)` — scorciatoia di thread, non di
finestra — e pompa la coda in un thread proprio. In WinUI non vale: un
messaggio di thread (`msg.hwnd == NULL`) `DispatchMessage` non lo
smista a nessuna procedura di finestra, quindi il ciclo di XAML lo
scarterebbe. **Bisogna passare un HWND reale.** Questo è il cambio di
fondo, non un dettaglio del porting.

**I due meccanismi possibili:**

| | Subclass della finestra XAML | Finestra solo-messaggi propria (`HWND_MESSAGE`) |
|---|---|---|
| Affidabilità | Dipende dal fatto che la finestra XAML non venga mai distrutta | Indipendente dal ciclo di vita dell'interfaccia |
| Parti mobili | 1 (il subclass) | 1 (la nostra classe di finestra), ma è codice interamente nostro |
| Rischio noto | [issue 8669][d12]: `ExecutionEngineException` casuale facendo il subclass | Nessuna issue nota |
| Verificato | Sì, tutti i pezzi | `RegisterHotKey` sì; che il ciclo di WinUI smisti lì, **no** (poi verificato) |
| Serve per gli appunti | Sì | Sì |

**Si sceglie la finestra solo-messaggi propria, con il subclass come
piano B.** Il requisito numero uno è che la scorciatoia funzioni sempre,
e pastepad si nasconde nell'area di notifica: legare la scorciatoia alla
finestra visibile reintroduce proprio l'accoppiamento che uccise la
versione precedente. Inoltre lo schema era già scritto e provato in
`windows.py` (`abrir_buzon`, con gli `argtypes` di `HWND_MESSAGE` che
era costato trovare).

### Gli appunti

| | `AddClipboardFormatListener` (Win32) | `Clipboard.ContentChanged` (WinRT) |
|---|---|---|
| Avviso | `WM_CLIPBOARDUPDATE` alla nostra finestra | Evento statico |
| Leggere in secondo piano | Sì | **No.** La documentazione dice che agli appunti si accede solo con l'app in primo piano |
| Formati privati | `RegisterClipboardFormat`, `IsClipboardFormatAvailable`, `GetClipboardData` | Nessuna strada documentata |

**Vince Win32, senza discussione.** Un gestore di appunti vive in
secondo piano per definizione: la API WinRT è scartata per progetto,
non per gusto. Vedi [AddClipboardFormatListener][d13] e
[Clipboard di WinRT][d14].

I quattro formati privati si leggono come prima — due per presenza
(`Clipboard Viewer Ignore`, `ExcludeClipboardContentFromMonitorProcessing`)
e due per valore zero (`CanIncludeInClipboardHistory`,
`CanUploadToCloudClipboard`) — portando `contenido_privado()` riga per
riga.

### Finestra e area di notifica

- **Senza cornice** — `OverlappedPresenter.SetBorderAndTitleBar(...)`,
  con `IsAlwaysOnTop`, `IsResizable` e
  `PreferredMinimum/MaximumWidth/Height`, che copre i limiti da 300×340
  a 720×1100 senza scrivere codice. Vedi [OverlappedPresenter][d15].
- **Nascondere senza distruggere** — `AppWindow.Hide()` nasconde la
  finestra da tutte le rappresentazioni del sistema ma tiene vivo
  l'oggetto. Inoltre `IsShownInSwitchers` perché non compaia in Alt+Tab
  né nella barra delle applicazioni, e `MoveAndResize` per collocarla
  vicino al cursore. Vedi [AppWindow][d16].
- **Mica** — `<Window.SystemBackdrop><MicaBackdrop/></Window.SystemBackdrop>`,
  proprietà dall'SDK 1.3. Acrilico con `DesktopAcrylicBackdrop`.
  **Mica dipinge solo su Windows 11 (22000+)**; sotto, con la
  trasparenza disattivata o in risparmio batteria, ricade su un colore
  pieno. La tavolozza `#0B0B0D` di `SPECIFICHE-UI.md` è il piano B
  naturale. Vedi [system backdrops][d17] e [Mica][d18].
- **Area di notifica** — WinUI 3 non la porta e la [proposta 2020][d19]
  non è ancora implementata. Si sceglie `Shell_NotifyIcon` diretto sulla
  stessa finestra solo-messaggi della scorciatoia, non la libreria
  `H.NotifyIcon.WinUI`: quel `WndProc` esisterà comunque, quindi l'icona
  costa una quarantina di righe in più e zero dipendenze.

### Istanza unica

`AppInstance.FindOrRegisterForKey`, `IsCurrent` e
`RedirectActivationToAsync`, con `DISABLE_XAML_GENERATED_MAIN` e un
`Program.cs` proprio — obbligatorio comunque, perché il
reindirizzamento va deciso prima di creare qualsiasi finestra. C'è una
[guida ufficiale con codice][d20] e un
[esempio per Win32 non impacchettato][d21].

Sfumatura: la guida stessa avverte di problemi noti con le app
sottoposte a trimming e raccomanda `PublishTrimmed=false`.

### Impacchettamento e Native AOT

- **Native AOT è fattibile**: `PublishAot` è supportato dal Windows App
  SDK **1.6** ed esige `Microsoft.Windows.CsWinRT` 2.1.1 o successivo.
  Avvertenza ufficiale in quelle note: le app compilate con AOT possono
  bloccarsi dopo aver navigato tra le pagine per una condizione di corsa
  nel garbage collector. Vedi [note della 1.6][d22].
- **EXE unico**: `PublishSingleFile` funziona solo se non impacchettato
  *e* self-contained, con l'insieme esatto `WindowsPackageType=None`,
  `WindowsAppSDKSelfContained`, `SelfContained`, `EnableMsixTooling`,
  `IncludeAllContentForSelfExtract` e `PublishSingleFile`. Estrae le
  dipendenze in una cartella temporanea al primo avvio: non è un binario
  chiuso.
- **Installazione in `%LOCALAPPDATA%`**: si copia la cartella di
  output. La documentazione suggerisce WiX o Inno Setup per avvolgerla.
  Alla fine fu scelto Inno Setup: `installer/pastepad.iss`.
- **P/Invoke**: [`Microsoft.Windows.CsWin32`][d23], generatore di
  codice di Microsoft. Alla fine le firme furono scritte a mano in
  `Sistema/Nativo.cs`, perché sono poche.

### Prove

Le 19 prove coprivano `modelo.py` e `busqueda.py`, che non importavano
niente di grafico. Vanno in una **libreria di classi `net10.0` pura,
senza TFM di Windows e senza riferimento al Windows App SDK**, e si
provano con **MSTest** (`dotnet test`). Senza finestra, senza runtime
dell'SDK, senza thread STA. È la stessa separazione che permise di
migrare da tkinter a Flet riusando 906 righe: si conserva perché ha
funzionato.

---

## Il piano per passi

### Passo 1 — Prova del fuoco della scorciatoia e degli appunti

Prima di tutto il resto. Un progetto WinUI 3 non impacchettato
**vuoto**: una finestra, un contatore a schermo, nient'altro.

- `Program.cs` proprio con `DISABLE_XAML_GENERATED_MAIN` e istanza
  unica.
- Creare nel thread dell'interfaccia una finestra solo-messaggi
  (`CreateWindowExW` con padre `HWND_MESSAGE`) con `WndProc` proprio e
  il delegato tenuto in un campo **statico**.
- `RegisterHotKey(hwnd, 1, MOD_CONTROL|MOD_SHIFT|MOD_NOREPEAT, 0x56)` e
  `AddClipboardFormatListener(hwnd)`.
- Contare `WM_HOTKEY` e `WM_CLIPBOARDUPDATE` a schermo e in un log.

**Come si sa che è venuto bene:** 100 pressioni danno 100 incrementi,
senza nessuna perdita; e continua a contare dopo 8 ore residente, dopo
aver bloccato e sbloccato la sessione, dopo aver sospeso il computer, e
con la finestra nascosta con `AppWindow.Hide()`. Se fallisce lo
smistamento alla finestra solo-messaggi, si ripete con
`SetWindowSubclass` sull'HWND della finestra XAML. **Non si passa al
passo 2 finché uno dei due non dà 100 su 100.**

### Passo 2 — La logica e le 19 prove

Libreria `net10.0` pura con le regole dei dati, la ricerca e la
configurazione. Tradurre `modelo.py` e `busqueda.py` rispettando il
formato JSON alla lettera: chiavi `t/f/s/b/i/u/c`, fissati per primi,
scrittura atomica su `.tmp` e `File.Move` con sovrascrittura,
riversamento rimandato di 3 s con salvataggio immediato per ciò che
l'utente fa apposta. Poi, le 19 prove di `prueba.py` in MSTest, una per
una.

**Come si sa che è venuto bene:** `dotnet test` dà tutto in verde senza
aprire finestre, e un'installazione esistente della v3.0.1 si apre con
la versione nuova senza perdere né uno snippet né un fissato.

### Passo 3 — Il guscio

Sullo scheletro validato nel passo 1: finestra senza cornice,
`IsShownInSwitchers = false`, Mica con ricaduta su `#0B0B0D`, icona
nell'area di notifica con `Shell_NotifyIcon` sulla stessa finestra
solo-messaggi, mostrare e nascondere vicino al cursore con
`MoveAndResize` rispettando l'area utile del monitor, e il ciclo di
incolla portato da `windows.py`: salvare l'`hwnd` attivo **nell'istante
della scorciatoia**, `AttachThreadInput` e `SetForegroundWindow`,
rilasciare Shift e Alt, e `Ctrl+V`. Registro degli errori dal primo
giorno, con `AppDomain.UnhandledException` e
`TaskScheduler.UnobservedTaskException` agganciati tutti e due. Nessun
`catch` muto.

**Come si sa che è venuto bene:** copiare in Word, aprire con la
scorciatoia, incollare nel Blocco note e il testo cade dove stava il
cursore. Copiare da KeePass o dalla modalità in incognito di Chrome e
**non** compare nella cronologia.

### Passo 4 — L'interfaccia

Su `docs/SPECIFICHE-UI.md` e le maquette 01–19, nell'ordine che il
documento stesso raccomanda: prima i colori, poi 13, 14 e 15, poi gli
stati vuoti 10, 11 e 12. Le maquette 20 e 33 non sono specifica, e la 26
e la 27 sono dialoghi di Windows.

**Come si sa che è venuto bene:** gli esadecimali del codice coincidono
uno a uno con la tabella della sezione 2 del documento.

### Passo 5 — Pubblicare e misurare

`WindowsPackageType=None` e `WindowsAppSDKSelfContained=true`, installer
per utente, avvio automatico da `HKCU\...\Run`. Misurare la memoria
residente, il tempo di apertura del pannello e l'avvio a freddo. Native
AOT **alla fine e come esperimento misurato**, non come premessa: se
aggiunge poco o ricompare il blocco documentato dopo la navigazione, si
lascia fuori. La versione esce dal tag, non scritta a mano.

**Come si sa che è venuto bene:** l'`.exe` installato —non il progetto
in debug— passa le prove del passo 1 e del passo 3 su una macchina
pulita, spostato di cartella almeno una volta.

### Cambi chiesti dall'utente durante il passo 4

Non sono deviazioni dal piano: sono decisioni sue, prese vedendo
l'applicazione girare. Stanno qui perché contraddicono le specifiche,
scritte prima.

**1. Il design si adatta a WinUI 3, non il contrario.** Usare i
materiali e il sistema di temi del framework e poi coprirli è pagare il
costo senza incassare il beneficio. Coincide con il motivo per cui
`PASSAGGIO.md` scelse WinUI 3.

**2. Il tema segue Windows e cambia dal vivo.** A pannello aperto, se
Windows passa da scuro a chiaro, il pannello passa con lui. `"tema":
"auto"` di `config.json` significa questo. Chiaro e scuro espliciti si
conservano come preferenza.

**3. Via le dimensioni fisse.** `mini`, `piccolo`, `medio` e `grande`
spariscono da Aspetto: il pannello è adattabile e tutto il suo contenuto
si regola ridimensionando.

**4. Mancava il selettore della lingua.** La versione originale aveva 4
lingue —es, en, pt, fr— e la maquette 08 non dichiarava nessuna lingua.
Era un buco delle specifiche, non del codice. (In questa copia la
lingua è una sola e il selettore non c'è.)

**5. Aspetto va riorganizzato.** Così com'era non sembrava un programma
finito, e l'obiettivo è che vada in produzione.

**6. `tenue` sale fino a rispettare il contrasto AA.** Misurato dal
designer: dava tra 2.89:1 e 3.38:1 sui 12 sfondi, quando WCAG AA chiede
4.5:1 per il testo normale. È il colore dei sottotitoli di riga e degli
stati vuoti, cioè testo che bisogna poter leggere.

L'utente decise il 12 ago 2026 di alzarlo **accettando che cambiasse
l'aspetto delle dodici tavolozze**. Leggibilità prima dell'aspetto
ereditato.

Si ricalcola per tavolozza, non un valore unico: ogni sfondo ha bisogno
del suo per arrivare a 4.5:1. E si verifica **calcolando il contrasto**,
non guardandolo.

**E si portò dietro una correzione non richiesta: `medio` sale in sei
tavolozze.** Alzando `tenue` fino ad AA, in quattro sfondi chiari
restava attaccato a `medio` —in `salvia`, 1.07:1 tra l'uno e l'altro— e
il sottotitolo smetteva di leggersi come secondario. Siccome `tenue` non
può più scendere senza rompere AA, il gradino si recupera solo alzando
`medio`. Fu fissato a 1.30:1, quello che avevano già le tavolozze sane.

Il designer lo segnò come fuori dall'autorizzato e l'utente lo approvò
dopo, il 12 ago 2026: **resta**. Non si annulla senza un motivo nuovo.

**7. Il raggio degli angoli lo mette Windows, ed è il massimo
possibile.** L'utente chiese più arrotondamento. Non si può:
`DWM_WINDOW_CORNER_PREFERENCE` ha quattro valori —predefinito, non
arrotondare, arrotondato e arrotondato piccolo— e **nessuno accetta un
numero di pixel**. Si chiede già quello grande, che misura 8 px al 100%
di scala e cresce con il DPI.

Per averne di più bisognerebbe disegnare gli angoli noi con una
finestra senza cornice di sistema, e questo **riapre i due difetti già
sistemati**: torna la fascia della barra del titolo e tornano i cunei
negli angoli. Deciso il 12 ago 2026: **si lascia com'è.**

**8. Resta da decidere: la barra bianca di focus sull'accento `oro`**
dà 1.44:1, sotto il 3:1 che chiede WCAG 1.4.11 per gli elementi non
testuali. Resta bianca perché il suo compito è essere un segnale di
forma, non di colore, e comanda la maquette. Annotato con il numero nel
caso un giorno si riveda.

### Dopo la pubblicazione: che l'aggiornamento arrivi

Deciso il 13 ago 2026, con il piano del pianificatore davanti.

**Il requisito**, con le parole dell'utente: «è importante che chiunque
abbia il mio programma si aggiorni anche lui». Chi aveva installato la
4.0.0 restava lì per sempre — e si portava dietro due errori che
distruggevano testo in silenzio.

**Si scarta Velopack**, anche se è lo strumento fatto per questo, per
tre motivi misurati:

1. Installa in `%LOCALAPPDATA%\{nome}`, che con il nostro nome **è la
   cartella dei dati**. Metterebbe il suo aggiornatore accanto alla
   cronologia.
2. L'avvio automatico scriverebbe il percorso dell'eseguibile interno e
   non quello del lanciatore che Velopack usa per sopravvivere agli
   aggiornamenti, e la sua documentazione **non copre l'avvio con
   Windows**. Il modo di fallire sarebbe «la scorciatoia non parte», che
   è il requisito numero uno.
3. Le 4.0.x sono già installate con Inno: migrare lascerebbe **due
   installazioni**, e quella che perde la scorciatoia resta come
   finestra muta.

**Si sceglie il noioso**: avviso dentro il pannello usando la API di
GitHub. Nessun canale nuovo, nessun modello di installazione nuovo.
L'installer che c'è sostituisce già senza duplicare, ammette la modalità
silenziosa e non tocca i dati.

Dato verificato dal vivo: la API restituisce il **`digest`** dell'asset
nella stessa risposta della URL di download. Ancora migliore del
`SHA256.txt`, che è un file sciolto dentro la release stessa.

**Avvisa, non aggiorna da solo.** Decisione dell'utente: «che chieda
alle persone». E c'è un motivo tecnico a sostegno: il riversamento della
cronologia è rimandato e **si forza solo uscendo dall'area di
notifica**, quindi una chiusura imposta può perdere fino a 3 secondi di
copie. Chi deve chiudere pastepad è pastepad, riversando prima.

**Diviso in due, su richiesta dell'utente:**

- **4.1.0** — controllare, avvisare nel pannello, e il pulsante **apre
  la pagina della release nel browser**. Un solo pezzo nuovo, zero
  rischi per la cronologia. Effetto collaterale buono: scaricando con il
  browser torna il contrassegno del web, quindi SmartScreen continua a
  proteggere finché il binario non è firmato.
- **4.2.0 — fatto, e risultò essere un'altra cosa.** La domanda era se
  il Restart Manager raggiunge la finestra nascosta. Misurato con la sua
  stessa API su pastepad in esecuzione: **no**, e per due motivi
  insieme.

  Una finestra solo-messaggi «cannot be enumerated», quindi la cassetta
  non riceve `WM_QUERYENDSESSION` — non è un errore, è la definizione. E
  `bRestartable` era `False` perché nessuno aveva chiamato
  `RegisterApplicationRestart`, che è la condizione posta dalla stessa
  documentazione di Inno perché `RestartApplications=yes` serva a
  qualcosa.

  Il peggio non era nessuno dei due: **il tipo che vedeva il Restart
  Manager cambiava a seconda che il pannello fosse aperto o nascosto**
  (`RmMainWindow` contro `RmOtherWindow`). Lo stesso programma si
  comportava in due modi aggiornandosi a seconda di dove lo si era
  lasciato.

  Sistemato con una finestra di primo livello a parte che ascolta
  soltanto (`Sistema/Chiusura.cs`), la registrazione per il riavvio, e
  `RestartApplications=yes`. Lo `skipifsilent` resta com'è: con il
  riavvio funzionante è già di troppo, e toglierlo aprirebbe pastepad a
  chi installa in silenzio senza averlo aperto.

  **Verificato con l'installer vero**, sopra l'installazione dell'utente
  e con pastepad in esecuzione. Ripetuto il ciclo sulla 4.2.0 già
  installata: chiusura pulita in 205 ms, riapertura in 52 ms, torna con
  la sua scorciatoia in 414 ms, e una copia che era **non ancora
  riversata** sopravvive.

  Un'avvertenza per chi ripete la prova: il riversamento rimandato può
  scattare dentro la finestra di mezzo secondo tra il copiare e il
  lanciare l'installer, e allora la copia è già su disco e la prova non
  dimostra niente.

- **Il pulsante che scarica e lancia l'installer** resta da fare. Prima
  di scriverlo va deciso se pastepad si lancia da solo l'installer o se
  si limita ad aprire la release, che è ciò che fa oggi.

Con una preferenza per spegnere l'avviso, visibile dal primo giorno.

### Passo 6 — Ritirare la versione in Python

**Non prima del tempo.** Finché l'interfaccia era in migrazione, quei
file erano la fonte: il designer leggeva `estilo.py` per la tavolozza,
`filas.py` per il gruppo dei segnalibri e `ventanas.py` per i dialoghi.

**Quando:** a passo 4 chiuso e con la conferma del qa sulla parità di
funzioni. Prima di pubblicare la release del passo 5, non dopo.

**Cosa resta, e perché:**

| | |
|---|---|
| `docs/mockups/` e le specifiche dell'interfaccia | Il design non cambia con il linguaggio |
| `PASSAGGIO.md` e `PIANO.md` | La memoria del perché è fatto così |
| `CHANGELOG.md`, `LICENSE`, `SECURITY.md`, `.github/` | Non dipendono dall'implementazione |

**Come si sa che è venuto bene:** `dotnet test` resta in verde,
l'applicazione parte da una copia pulita del repo, e non restano
riferimenti vivi a file `.py` in documentazione né negli script.

**Prima di cancellare, un tag.** La versione in Python fu tre anni di
lavoro ed è l'unico riferimento di comportamento se qualcosa è
sfuggito. Che resti recuperabile con un nome, non solo nella storia.

---

## I rischi

| Rischio | Segnale che lo rivela | Cosa fare |
|---|---|---|
| Il ciclo di WinUI non smista alla finestra solo-messaggi | Il contatore del passo 1 non sale mai | Piano B: `SetWindowSubclass` sull'HWND della finestra XAML |
| `ExecutionEngineException` da subclass (issue 8669) | Chiusura del processo senza eccezione gestita, casuale | Delegato in campo statico e `RemoveWindowSubclass` alla distruzione. Se persiste, tornare alla finestra propria |
| Un'altra app si tiene `Ctrl+Shift+V` | `RegisterHotKey` restituisce 0 | Avvisare **nell'interfaccia** con l'errore reale, non in silenzio, e offrire le altre scorciatoie di `Config.Scorciatoie` |
| Mica non compare | Sfondo piatto | È il comportamento documentato. La tavolozza `#0B0B0D` copre già il caso; verificare che si veda bene, non forzare Mica |
| Native AOT rompe qualcosa | Blocco dopo la navigazione, o errore solo in Release | È isolato nel passo 5. Si scarta senza costo |
| Manca il runtime dell'SDK sulla macchina dell'utente | L'app non parte, senza finestra né messaggio | `WindowsAppSDKSelfContained=true` lo elimina alla radice. Il VC++ Redistributable resta un requisito: verificarlo nell'installer |
| Le cifre di memoria di `PASSAGGIO.md` non si rispettano | Misura del passo 5 molto sopra i 60 MB | Non è un motivo per abbandonare: contro i 207 MB di Flet c'è margine. Si annota la cifra reale e si corregge il documento |

---

## Ciò che resta fuori e perché

- **Ridiscutere il framework.** Nella documentazione ufficiale non c'è
  nessun blocco duro di WinUI 3 per i requisiti non negoziabili. L'unico
  punto in cui dà fastidio —non espone `WndProc`— ha una soluzione
  documentata, e in WPF quel codice Win32 sarebbe identico salvo per
  l'`HwndSource`.
- **L'interfaccia nel passo 1.** Il passo 1 è deliberatamente brutto: se
  mescolasse l'interfaccia, un errore della scorciatoia tornerebbe ad
  avere venti sospettati, che è esattamente come si persero giorni la
  volta precedente.
- **MSIX e lo Store.** Richiede identità di pacchetto e contraddice il
  requisito di installare in `%LOCALAPPDATA%` con un installer proprio.
- **`PublishSingleFile`.** Estrae in una cartella temporanea al primo
  avvio, quindi non dà ciò che promette il nome e aggiunge una variabile
  all'avvio a freddo, che è già la cosa più lenta. Un installer sulla
  cartella fa lo stesso lavoro con meno magia.
- **Prove di interfaccia automatizzate.** Con le prove delle regole e
  una lista di controlli manuali basta per uno strumento di un solo
  utente. WinAppDriver aggiungerebbe più manutenzione che segnale.

---

## Ciò che NON si poté verificare

Detto con queste parole, e ognuno coperto da un passo o da un piano B:

1. **Le cifre di memoria di `PASSAGGIO.md`** (40–60 MB, 15–20% in meno
   di WPF). Non ci sono cifre ufficiali di Microsoft. Si misurano nel
   passo 5.
2. **La causa dell'`ExecutionEngineException`** della issue 8669: il
   server interruppe la connessione leggendo il thread. Il sospetto
   abituale in .NET è che il garbage collector si porti via il delegato,
   e la mitigazione sarebbe tenerlo in un campo statico — ma è un
   sospetto, non una verifica.
3. ~~**Che il ciclo dei messaggi di WinUI 3 smisti a finestre estranee
   allo XAML** create nello stesso thread.~~ **Verificato il 12 ago 2026
   nel progetto di sondaggio: smista.** 100 pressioni sintetiche, 100
   `WM_HOTKEY` ricevuti, tutti nel thread dell'interfaccia.

---

[d1]: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads
[d2]: https://learn.microsoft.com/en-us/windows/apps/get-started/start-here
[d3]: https://learn.microsoft.com/en-us/windows/apps/winui/winui3/
[d4]: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/deploy-unpackaged-apps
[d5]: https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/unpackage-winui-app
[d6]: https://learn.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd
[d7]: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey
[d8]: https://learn.microsoft.com/en-us/windows/win32/inputdev/wm-hotkey
[d9]: https://learn.microsoft.com/en-us/windows/win32/api/commctrl/nf-commctrl-setwindowsubclass
[d10]: https://github.com/microsoft/microsoft-ui-xaml/discussions/8934
[d11]: https://github.com/microsoft/microsoft-ui-xaml/issues/5815
[d12]: https://github.com/microsoft/microsoft-ui-xaml/issues/8669
[d13]: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-addclipboardformatlistener
[d14]: https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.datatransfer.clipboard
[d15]: https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.overlappedpresenter
[d16]: https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.appwindow
[d17]: https://learn.microsoft.com/en-us/windows/apps/develop/ui/system-backdrops
[d18]: https://learn.microsoft.com/en-us/windows/apps/design/style/mica
[d19]: https://github.com/microsoft/microsoft-ui-xaml/issues/2020
[d20]: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/applifecycle/applifecycle-single-instance
[d21]: https://github.com/microsoft/WindowsAppSDK-Samples/blob/main/Samples/AppLifecycle/Instancing/cpp-win32-unpackaged/CppWinMainInstancing/CppWinMainInstancing.cpp
[d22]: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-1-6
[d23]: https://www.nuget.org/packages/Microsoft.Windows.CsWin32
