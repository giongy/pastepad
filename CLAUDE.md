# CLAUDE.md

Contesto per lavorare a questo progetto. Leggilo prima di toccare il
codice.

## Che cos'è

pastepad: gestore di appunti per Windows. Salva tutto ciò che si copia,
più i testi che l'utente organizza in cartelle, e li incolla dove stava
il cursore con una scorciatoia globale.

Sostituisce la cronologia di Windows (Win+V), che conserva solo 25 voci
e le perde al riavvio.

## Stato

Versione 4.15.0, scritta in C# con WinUI 3 sul Windows App SDK 2.3.1 e
.NET 10. Non impacchettata e self-contained.

**Questa copia è interamente in italiano**: interfaccia in una sola
lingua, nomi nel codice, commenti e documentazione. Deriva dal progetto
originale di Josemgu, che era in spagnolo; nomi dei file, chiavi dei
JSON e opzione `--dati` sono stati tradotti, quindi **i dati non sono
compatibili con quelli della versione originale**.

La versione precedente (3.x, Python con Flet) **non è più nel repo**.
Vive nel tag `ultima-version-python` e in `v3.0.1`, in spagnolo. Per
vedere come faceva qualcosa:

```
git show ultima-version-python:pastepad/busqueda.py
```

Perché fu riscritta, cosa fu conservato e cosa si imparò strada
facendo: `PASSAGGIO.md`. Come fu fatta, passo per passo e con le fonti:
`PIANO.md`. Tutti e due sono ancora validi.

**Ciò che è stato chiesto e non è ancora fatto sta in `DA-FARE.md`.**
Guardalo prima di cominciare una versione. E a ogni aggiornamento il
pianificatore consegna anche cosa ottimizzare e cosa ha pubblicato
Microsoft per il Windows App SDK, che sia nell'incarico o no.

## Struttura

```
csharp/
  Pastepad.Nucleo/           net10.0 puro, senza Windows App SDK
    Regole.cs                le regole dei dati
    Archivio.cs              l'unico che tocca il disco
    Ricerca.cs               punteggio e indice con cache
    Testi.cs                 riempire i buchi %s e %d di una frase
    Versioni.cs              confrontare versioni e decidere se avvisare
    Argomenti.cs             la riga con cui Windows ci riapre
    Tipi.cs                  di che tipo è ogni salvato, e quando si scrive
    DateLeggibili.cs         una data salvata, in parole
    Config.cs, Dati.cs, AvvioAutomatico.cs
  Pastepad.Nucleo.Prove/     118 prove, senza aprire finestre
  Pastepad.App/
    Sistema/                 tutto ciò che parla con Win32
      Cassetta.cs            finestra solo-messaggi: scorciatoia e appunti
      Chiusura.cs            sente che stanno per chiuderci e riversa prima
      Appunti.cs             lettura, scrittura, formati privati, RTF
      FocusFinestra.cs       restituire il focus e incollare
      Aggiornamento.cs       interroga la API di GitHub. Solo interroga
      Avvio.cs               avvio automatico: chiave Run E attività di accesso
      Pacchetto.cs           se i nostri file finiscono in un altro posto
      AreaNotifica.cs, Schermo.cs, Combinazione.cs, Nativo.cs
    Pannello.xaml(.cs)       il pannello
    Formato.cs               la barra di formato sul RichEditBox
    Dialoghi.cs, Stile.cs, Riga.cs, Registro.cs
    Program.cs               Main proprio: istanza unica
installer/pastepad.iss       Inno Setup, l'unico file
pubblica.ps1                 compila in Release e copia in una destinazione
docs/                        35 maquette, specifiche, loghi
```

`Pastepad.Nucleo` non importa niente di grafico **apposta**. È ciò che
permette a 118 prove di girare senza aprire finestre e senza il Windows
App SDK. Non metterci dentro WinUI.

Questa divisione è anche il motivo per cui `Versioni.cs` sta nel nucleo
e `Aggiornamento.cs` nell'applicazione: la decisione —confrontare
versioni, sapere quando tocca guardare e quando avvisare— è ciò che si
può rompere in silenzio, quindi vive dove si può provare. La richiesta
di rete no.

## Come lavorare qui

```powershell
dotnet build csharp/Pastepad.App -p:Platform=x86 -r win-x86
dotnet test csharp/Pastepad.Nucleo.Prove
```

**L'architettura va indicata.** La soluzione chiede x86 per
l'applicazione, ma il progetto deduce l'architettura da quella dell'SDK:
con un SDK a 64 bit, `dotnet build csharp/Pastepad.slnx` fallisce con
`NETSDK1032`. I due comandi qui sopra funzionano.

**Occhio a quale binario stai misurando.** x86 e x64 scrivono in
cartelle diverse (`bin/x86/...` e `bin/x64/...`). Misurare sul binario
vecchio ha già dato due conclusioni false —che `ResizeClient`
funzionava, e che una correzione di disposizione non era stata
applicata—. Controlla data e ora dell'`.exe` prima di credere a una
misura.

Da VS Code: **F5** compila a 64 bit e avvia con dati di prova
(`.vscode/launch.json`). Il debugger C# di VS Code lavora con processi a
64 bit; la versione che si distribuisce resta a 32.

**Per eseguire, sempre `--dati`.** Con quell'opzione non si tocca
nemmeno la voce di avvio del registro. Comunque, se qualcosa è strano,
guarda `HKCU\...\Run\pastepad`.

```powershell
& ".\csharp\Pastepad.App\bin\x86\Debug\net10.0-windows10.0.26100.0\win-x86\pastepad.exe" --dati "C:\temp\prova"
```

Senza quell'opzione usa `%LOCALAPPDATA%\pastepad`, che è **la cronologia
reale dell'utente**, e registra l'avvio automatico puntando a quella
build. Una sessione di prove si è già portata via delle voci.

Per pubblicare:

```powershell
.\pubblica.ps1
```

oppure, a mano:

```powershell
dotnet publish csharp/Pastepad.App -c Release -p:Platform=x86 -r win-x86
```

## Decisioni misurate. Non buttarle giù senza misurare tu

Queste non sono opinioni: sono state verificate eseguendo, e c'è un
numero.

**La scorciatoia va su una finestra solo-messaggi**, non facendo il
subclass della finestra XAML. `Sistema/Cassetta.cs`. Misurato 100/100
nel sondaggio iniziale e 30/30 due volte dal QA sull'eseguibile. Il
subclass si porta dietro un `ExecutionEngineException` aperto nel
repository di WinUI, e legare la scorciatoia alla finestra visibile
—che si nasconde— è l'accoppiamento che uccise la versione precedente.

**L'ascolto della chiusura è una finestra SEPARATA dalla cassetta**, e
di primo livello. `Sistema/Chiusura.cs`. Una finestra solo-messaggi
«cannot be enumerated», e ciò che non si enumera non riceve
`WM_QUERYENDSESSION`: la cassetta è sorda a questo per costruzione.
Misurato con la stessa API del Restart Manager su pastepad in
esecuzione: `bRestartable=False`, e il tipo cambiava tra `RmOtherWindow`
e `RmMainWindow` **a seconda che il pannello fosse nascosto o aperto**.
Dopo il cambio: chiusura pulita in 157–201 ms, riapertura in 56–68 ms, e
una copia fatta 400 ms prima sopravvive.

**`RegisterApplicationRestart(null, ...)` non conserva gli argomenti,
li cancella.** «If this parameter is NULL or an empty string, the
previously registered command line is removed». Verificato: un'istanza
lanciata con l'opzione della cartella dati tornò senza e aprì
l'archivio reale.

**Gli appunti vanno per Win32**, non per la classe `Clipboard` di
WinRT. La sua documentazione dice che vi si accede solo con
l'applicazione in primo piano, e un gestore di appunti vive in secondo
piano per definizione.

**Il filtro per sequenza degli appunti non è ridondante.**
`WM_CLIPBOARDUPDATE` arriva **più di una volta per copia** —PowerShell
ne fa partire tre—. Senza `GetClipboardSequenceNumber` ogni copia entra
tre volte nella cronologia.

**Istanza unica con `AppInstance.FindOrRegisterForKey`**, non con un
mutex. E la chiave si riassume con SHA-256, non con `GetHashCode`, che
in .NET Core è randomizzato per processo.

**`CorniceFinestra.cs` resta.** `AppWindow.ResizeClient` dice nella sua
documentazione che calcola l'area non client per te; misurato, dà 31 px
in più e il pannello cresce a ogni apertura.

**Le impostazioni dei dialoghi vanno sempre impilate.** La disposizione
su due colonne non ci sta fino a un pannello di ~455 px, e quello di
fabbrica è 380. La tendina non mette i puntini di sospensione: taglia da
sinistra, e usciva `l + Shift + V` invece di `Ctrl + Shift + V`.

**L'avvio automatico va per due strade, ed è apposta.** La chiave
`HKCU\...\Run` e un'attività all'accesso con 30 s di ritardo. Non è
cintura e bretelle per gusto: ci fu un avvio in cui la chiave era
corretta, l'eseguibile esisteva, non era disattivata in Gestione
attività, e Windows elaborò quella chiave —OneDrive, che sta nella
stessa, partì 15 s dopo explorer— e pastepad non lasciò **nemmeno una
riga** in nessuno dei due log né un rapporto di errore. O il processo
non arrivò a crearsi, o morì prima della sua prima istruzione, che è
dove l'host di .NET fallisce in silenzio. La causa è ancora ignota.

L'attività si registra senza essere amministratore perché va con
`InteractiveToken` e `LeastPrivilege`: «you do not need to specify a
password when registering the task if you register the task to run under
the security context of your account and you use the S4U or interactive
logon type». Con `Password` o `S4U` servirebbe il privilegio di accesso
come processo batch, che un utente normale non ha. Verificato
registrandola ed eliminandola senza elevazione.

L'avvio automatico non ha un interruttore nell'interfaccia: si spegne
mettendo `"avvio_automatico": "no"` in `config.json`.

**Non si può registrare ciò che non succede.** Per questo ogni avvio
annota da quanto era acceso Windows (`Environment.TickCount64`): pochi
secondi significano che ci ha aperto Windows, alcune ore che l'avvio
automatico è fallito e l'ha aperto l'utente. È l'unica cosa che
trasforma «non è partito» in qualcosa che si legge.

**Misurare la scorciatoia da un capo all'altro, non solo il pezzo
comodo.** La misura vecchia iniziava in `AllaScorciatoia` e finiva in
`Activate()`, e diceva 25 ms mentre l'utente notava attese. Restavano
fuori i due pezzi dove può stare il ritardo: quanto il messaggio aspetta
in coda —che esce da `GetTickCount - GetMessageTime()`, ed è dove
compare un processo che Windows ha tolto dalla memoria o rallentato
perché in secondo piano— e quanto ci mette a disegnarsi il primo
fotogramma, perché `AppWindow.Show()` torna prima che ci siano pixel.

## Cose che romperai se non stai attento

- **`dotnet publish` non copia lo XAML compilato né il `.pri` né gli
  Assets.** C'è un `Target` nel `.csproj` che li aggiunge, e un
  `<Error>` che rompe la compilazione se manca il `.pri`. Senza, ciò che
  è pubblicato **muore all'avvio** con `XamlParseException` — e il build
  funziona lo stesso, quindi si vede solo eseguendo ciò che si
  distribuisce.
- **`dotnet publish` non svuota nemmeno la cartella di destinazione.**
  Ciò che resta lì viaggia dentro l'installer. Si è già infilato un file
  di log.
- **`File.Exists` restituisce `false` se non c'è il permesso di
  lettura**, senza lanciare. Per questo `Archivio.Leggi` apre il file
  invece di chiedere: un file illeggibile che passa per «non esiste»
  finisce sovrascritto.
- **I dati dell'utente vivono in `%LOCALAPPDATA%\pastepad`** e il
  programma in `%LOCALAPPDATA%\Programs\pastepad`. Separati apposta:
  così disinstallare non può toccare la cronologia.
- **Le chiavi delle risorse XAML si usano anche da codice, come
  stringhe.** `Stile.cs` e `Pannello.xaml.cs` cercano `"PpSfondo"`,
  `"PpAccento"` e simili per nome. Rinominarne una nello XAML senza
  cambiarla nel codice non dà errori di compilazione: fallisce
  all'avvio.

## Provare con contenuto lungo. Non è facoltativo

La 4.0.0 uscì con **sei errori che 24 controlli non videro**, perché
tutti usavano testi di due linee. L'utente li trovò il primo giorno. Due
dei sei distruggevano testo in silenzio.

Quando provi qualsiasi cosa che tocca il testo, fallo con **trenta,
sessanta e cento linee**. E controlla il risultato **in
`snippets.json`**, non a schermo: i due che distruggevano dati si
vedevano bene finché non si apriva il file.

Tre trappole che sono già costate care:

- **Un `TextBox` di WinUI con `AcceptsReturn` a `false` si tiene la
  prima linea** di ciò che gli si assegna. In un inizializzatore di
  oggetto, `Text` deve venire **dopo** `AcceptsReturn`, o il testo si
  tronca entrando.
- **`TextBox.Text` restituisce `\r` e basta**, non `\r\n` né `\n`.
  Dividere per `\n` lascia una sola linea con tutto dentro.
- Confrontando le lunghezze, la casella carica **tanti caratteri in meno
  quanti sono gli a capo** del file. Con 100 linee, 5290 su disco sono
  5191 nella casella. Se il conto torna, non c'è perdita.

**Il pannello si compone una volta all'avvio, fuori dallo schermo.**
`Pannello.Riscalda()`. Non è prudenza: misurato, nell'istante in cui la
finestra compare il contenuto è dipinto a metà —circa 5.500 pixel dei
36.500 che ha quando è completa— e finisce in circa 150 ms. Con la
macchina carica si allunga fino a sembrare che manchi mezza interfaccia.
Dopo il cambio, sei giri sotto carico: 6.562 nell'intestazione e 36.518
nel corpo **già all'istante 0**, identico ai 300 ms.

Va con `Show(false)` —«Shows the window with an option to activate it or
not»— per non rubare il primo piano a nessuno all'accesso, e a
`-32000,-32000`, che resta fuori da qualsiasi monitor: il desktop
virtuale della macchina di prova andava da `X -2560..2560, Y 0..1440`. E
**non tocca `EVisibile`**, quindi il gestore che nasconde il pannello
quando perde il focus esce dalla sua prima riga e non se ne accorge.

**Chiedere il primo piano può costare secondi, quindi prima si chiede
con le buone.** `FocusFinestra.ConLeBuone`. `AttachThreadInput` fa sì
che «keyboard and mouse events received by both threads are processed in
the order they were received»: ci mettiamo in coda al programma davanti
e, se quello è occupato, aspettiamo. Misurato contro una finestra con il
thread bloccato apposta: `SetForegroundWindow` ci mise **5.399 ms** e in
più restituì false.

La chiamata non si toglie —senza, il pannello esce visibile ma senza
tastiera—: le si mette davanti un tentativo **senza aggancio**, che
Windows accetta perché pastepad è appena stato svegliato da una
scorciatoia globale. Stessa prova dopo: **da 5.604 ms a 35,7 ms**.

**E l'attesa prima di incollare non è più un numero scritto a mano.**
Erano 60 ms fissi; misurato, la finestra di destinazione era già davanti
quando iniziava l'attesa, quindi si buttavano tutti e 60 — e su una
macchina carica lo stesso numero poteva non bastare e mandare il Ctrl+V
troppo presto. Adesso si aspetta che sia davvero davanti. Un incolla
completo è passato da ~130 ms a 73,9 ms.

**Una scorciatoia che non si può registrare si segnala dall'area di
notifica, non dal pannello.** Il pannello si apre con la scorciatoia:
chi ha il problema è esattamente chi non può arrivare al messaggio che
glielo spiega.

E il peggio non è restare senza scorciatoia, è **cambiarla senza
dirlo**. Se quella dell'utente è presa, pastepad ricade su quella di
fabbrica, e fino alla 4.8.0 restava tranquillo perché quella di fabbrica
entrava e non c'era un `Problema` da mostrare. L'utente continua a
premere la sua per sempre e il tasto finisce al programma davanti — che
è proprio il segnale che pastepad non l'ha ricevuto. Riprodotto con due
istanze in lotta: `Windows ha rifiutato ctrl+alt+p (errore 1409)`, che è
`ERROR_HOTKEY_ALREADY_REGISTERED`.

**Gli avvisi di quando il pannello non è davanti passano dall'area di
notifica.** Incollando, il pannello si nasconde PRIMA di restituire il
focus, quindi l'avviso «Copiato, ma non incollato» si dipingeva in una
finestra già invisibile: l'utente restava con il testo copiato, non
incollato e senza sapere perché. E in più l'avviso sopravviveva nascosto
e ricompariva all'apertura successiva, ormai senza contesto. Se scrivi
un `Avvisa()` nuovo, guarda prima se in quel punto il pannello è
visibile.

### L'albero di accessibilità non dice niente di ciò che si dipinge

L'utente segnalò che la scorciatoia dell'intestazione non si vedeva
alla prima apertura. **I dump di UI Automation la vedevano tutte e due
le volte**, quindi nessun controllo per automazione l'avrebbe mai
rilevato. E ciò che l'automazione riporta come «Esc» non è nemmeno
un'etichetta: quella stringa non esiste nel codice, è il
`KeyboardAccelerator` con `Key="Escape"`, che UIA espone come proprietà.

**Per qualsiasi cosa che sia «non si vede», bisogna catturare i
pixel.** E catturare la finestra INTERA, non un ritaglio: con un
ritaglio di 44 px dell'intestazione si concluse che «falliva solo
quella riga», e misurando la finestra completa risultò che il primo
fotogramma è dipinto a metà dappertutto —da ~5.500 pixel di contenuto a
0 ms a ~36.500 ai 300 ms—. Il ritaglio provava solo che il bordo della
barra di ricerca era disegnato.

Attenzione anche a dove si prende il colore di sfondo di riferimento:
la finestra ha gli angoli arrotondati, quindi in `(2,2)` si infila il
desktop e il conteggio schizza.

E per catturare il pannello c'è un ostacolo: **si nasconde da solo
quando perde il focus**. Farlo comparire lanciando una seconda istanza,
o invocare qualcosa per UIA, restituisce il focus al terminale e il
pannello sparisce prima della cattura. Bisogna farlo comparire con la
scorciatoia e sondare ogni 100-150 ms.

### E per automatizzare l'interfaccia

I `MenuFlyoutItem` di questa applicazione **non rispondono al clic
sintetico** con `mouse_event`. Bisogna usare `InvokePattern.Invoke()`.
Perché compaia il pulsante con i tre punti di una riga, in più bisogna
selezionare la riga per pattern. Questo lasciò due controlli non fatti
in due giri diversi prima di essere scoperto.

**Se i file non finiscono dove sono stati chiesti, non si scrive
niente.** `Sistema/Pacchetto.cs` + `Archivio.Congela`. Quando un'altra
applicazione impacchettata apre pastepad, questo ne eredita il
contenitore e Windows reindirizza `%LOCALAPPDATA%` verso
`…\Packages\<pacchetto>\LocalCache\Local`. pastepad calcola bene il suo
percorso, crede di leggerlo, e legge e scrive su una copia; l'utente lo
apre dopo dal suo posto e **la sua cronologia e i suoi testi sono
spariti**, anche se su disco sono intatti. È successo il 14 ago 2026.

Rilevarlo costò tre misure, e due di esse sembravano buone e non lo
erano:

- **L'identità di pacchetto non vale.** `GetCurrentPackageFullName`
  restituisce `APPMODEL_ERROR_NO_PACKAGE`: il processo figlio eredita il
  reindirizzamento dei file ma **non** l'identità.
- **Nemmeno un handle di cartella.** La cartella si risolve in se
  stessa; solo un file rivela il reindirizzamento.
- **E il file deve essere appena scritto.** È copia-alla-scrittura, file
  per file: uno che c'era già si risolve nel percorso reale finché
  qualcuno non lo scrive.

Per questo il controllo è una sonda che si scrive all'avvio, con
`DELETE_ON_CLOSE` perché non lasci traccia.

**E attenzione provando con `--dati`: metti il percorso tra
virgolette.** `Start-Process -ArgumentList` non lo fa, e con un profilo
che contiene spazi il programma riceve il percorso spezzato e usa come
cartella dati solo il primo pezzo. Questo fece sì che il guardiano "non
scattasse" per vari giri: a fallire era la prova.

## L'errore che rimase aperto, e come fu chiuso

Per cinque versioni rimase annotato questo: *«un'istanza partì senza
poter leggere né scrivere nella sua cartella, con la scorciatoia che
funzionava perfettamente, e chiudendosi salvò una cronologia vuota sopra
quella reale. Non è stato riprodotto.»*

**Fu riprodotto il 14 ago 2026 ed era il reindirizzamento di
contenitore** descritto più sopra. L'utente aprì pastepad e trovò le sue
note, i suoi stili e la sua scorciatoia cancellati; su disco erano
intatti, e ciò che vedeva era una copia dentro
`…\Packages\<pacchetto>\LocalCache\Local`. Fu recuperato copiando da
quella copia, a programma chiuso e da fuori dal contenitore.

Ciò che lo trasformò in «si è cancellato tutto» non fu il
reindirizzamento ma il silenzio: pastepad partì vuoto e proseguì come se
fosse normale. Per questo la correzione non è solo rilevarlo, è
**congelare la sessione**: `Archivio.Congela` blocca tutte le scritture
in `Scrivi<T>`, che è l'unico posto da cui passano, e il pannello si
apre con il motivo.

Due avvertenze per chi indaga su qualcosa di simile:

- **Da una sessione che vive dentro un contenitore, ciò che leggi da
  `%LOCALAPPDATA%` non è ciò che c'è sul disco.** È successo qui: per
  ore si fece diagnosi su una vista reindirizzata. Per vedere il disco
  vero bisogna uscire dal contenitore — un'attività pianificata va bene,
  la lancia l'Utilità di pianificazione e non eredita niente.
- **Controlla la tua prova prima di credere al risultato.** Il guardiano
  «non scattava» per tre giri di fila e funzionava: a fallire era il
  percorso senza virgolette della cartella dati.

## Stile del codice

- Nomi e commenti in italiano. Gli **identificatori** vanno senza
  accenti (`Attivita`, `opacita`); i commenti e **il testo che vede
  l'utente** vanno in italiano corretto, con i loro accenti.
- I commenti spiegano **perché**, non cosa fa la riga.
- Niente codice morto né opzioni «per sicurezza».
- La prova si scrive insieme a ciò che implementa, non dopo.
- **Nessun `catch` muto.** Tutto ciò che si cattura si registra con dove
  e perché. E compila senza avvisi: tre `CS8601` che sembravano
  cosmetici erano tabelle a `null`, cioè l'applicazione che esplodeva.
- I testi dell'interfaccia stanno scritti dove si usano. Per quelli con
  un valore dentro c'è `Testi.Componi("... %s ...", valore)`.

## Storia

`CHANGELOG.md` ha il dettaglio. Riassunto:

- 1.x — un solo file, tkinter
- 2.0 — riscritto in moduli, con prove
- 3.0 — interfaccia migrata a Flet
- 4.0 — riscritto in C# con WinUI 3, per la scorciatoia globale
