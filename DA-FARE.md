# Da fare

Ciò che è deciso ma non ancora fatto. Si svuota dall'alto: ciò che si
fa, si toglie da qui e si racconta in `CHANGELOG.md`.

Non è una lista di idee. Qui entra solo ciò che l'utente ha chiesto
esplicitamente, con un'eccezione dichiarata: la sezione su ciò che è
rimasto aperto dalla traduzione in italiano.

### Le righe e il loro pulsante non si annunciano bene

Trovato il 10 set 2026 automatizzando le prove della scheda Note, con UI
Automation. Non l'ha chiesto l'utente; si annota perché il README
originale diceva che i pulsanti con icona hanno un nome accessibile, e
questi due no.

- **Ogni riga della lista si annuncia come `Pastepad.App.Riga`** — il
  `ToString()` dell'oggetto. Un lettore di schermo legge quello al posto
  del testo della nota. Manca `AutomationProperties.Name` nel
  `ListViewItem`, o un `ToString()` che restituisca il titolo.
- **Il pulsante dei tre punti si annuncia con il glifo dell'icona**, che
  non è una parola. Dovrebbe avere una dicitura, come l'hanno già quelli
  dell'intestazione e quello di Nuovo.

Fu scoperto perché la sonda cercava il pulsante per «nome vuoto» e non
lo trovava mai: il nome non era vuoto, era un carattere invisibile.

### SCARTATO: che il pannello non rubi la scena

**Decisione dell'utente, 14 ago 2026: non è fattibile, si lascia.** Non
tornare a indagare senza che lo chieda lui.

Fu studiato a fondo e resta scritto perché nessuno rifaccia la strada:

- **Win+V ci riesce perché non è un'applicazione.** Misurato: non cambia
  il primo piano, né la finestra attiva, né il focus, e **non crea
  nessuna finestra** — lo dipinge `TextInputHost.exe`, l'host di input
  di testo del sistema, su una superficie che esisteva già. Riceve i
  tasti perché sta dentro la catena di input, non perché ha il focus.
- **Senza attivarsi, la tastiera non arriva.** «The system posts
  keyboard messages to the message queue of the foreground thread that
  created the window with the keyboard focus». Il mouse funziona; la
  tastiera no.
- **Nessun altro ci riesce.** PowerToys Run ruba il focus e ha quattro
  segnalazioni aperte per questo; CopyQ e Ditto fanno come pastepad.
  L'unico che ci riesce —una reimplementazione di Ditto in AutoHotkey—
  **ha rinunciato alla ricerca**: solo Ctrl+1..5 e mouse.
- **C'era una strada e funzionava**: registrare ogni tasto con
  `RegisterHotKey` mentre il pannello è aperto. Misurato: 53 tasti
  singoli su 53 accettati, 0,3 ms per apertura, ingoia il tasto, e
  convive con la scorciatoia del programma. Anche gli accenti, verificato
  con la tastiera spagnola: `´` + `a` dà `á`.
- **Fu scartata per il rischio, non perché impossibile.** Ciò che non
  registri finisce nel documento dell'utente, e se pastepad si blocca
  con il pannello aperto non ci sono lettere in tutto Windows finché non
  si riprende. Su una macchina con agenti di sicurezza di mezzo, è
  troppo.

Di tutto questo è rimasto ciò che valeva: togliere la chiamata che si
bloccava. Vedi la 4.9.0.

## Rimasto aperto dalla traduzione in italiano

Queste voci non le ha chieste l'utente una per una: sono conseguenze
della traduzione dell'ottobre 2026, annotate perché non si perdano.

### I riferimenti al progetto originale

Tre punti portano ancora a `Josemgu/pastepad`, il progetto originale in
spagnolo, e non al fork `giongy/pastepad`:

- **Il controllo aggiornamenti.** `Sistema/Aggiornamento.cs` chiede a
  GitHub l'ultima release dell'originale, quindi l'avviso «C'è una
  versione nuova» porterebbe a una versione in spagnolo. In una build di
  sviluppo, che si dichiara 1.0.0, l'avviso scatta di sicuro. Va puntato
  al fork, oppure tolto.
- **I link dell'installer.** `installer/pastepad.iss` indica il
  repository originale come sito, supporto e aggiornamenti.
- **L'autore nei metadati.** `Pastepad.App.csproj` e l'installer
  dichiarano `Josemgu` come autore. Il copyright va lasciato; se il nome
  dell'autore pubblicato debba cambiare è una decisione da prendere.

### Ricontrollare che i testi in italiano ci stiano

Le misure dell'interfaccia furono prese con i testi originali. Diverse
frasi in italiano sono più lunghe —«Metti in pausa la cattura», «Elenco
a discesa», «Rimuovi formattazione», «Ctrl + Shift + Spazio»— e nessuno
ha ancora guardato il pannello e i dialoghi alla larghezza minima di
300. Da verificare catturando i pixel, come dice `CLAUDE.md`.

### I pallini degli accenti mostrano l'identificatore

Il suggerimento e il nome accessibile dei 18 pallini in Aspetto sono
l'identificatore crudo: passando il mouse si legge `menta_fredda`, non
«Menta fredda». C'era già nella versione originale; qui si nota di più
perché l'identificatore è in italiano e sembra un testo sbagliato.

### Non ancora eseguiti dopo la traduzione

- **L'installer.** `installer/pastepad.iss` si compila con Inno Setup
  e produce `dist\pastepad-4.15.0-installer.exe` (66,6 MB), ma
  l'installer non è stato eseguito: restano da provare l'installazione,
  l'aggiornamento sopra una copia aperta —che dipende dal nome della
  finestra di chiusura, cambiato— e la disinstallazione.
- **Il workflow di release.** Non è mai girato sul fork.
- **`docs/genera-icone.py`.** Tradotto, nomi delle funzioni compresi,
  ma non eseguito.

`pubblica.ps1`, la compilazione e le 118 prove sono invece verificati.

### Rifare le immagini della documentazione

Le schermate dell'interfaccia sono state tolte perché mostravano la
versione originale. Vanno rifatte con l'interfaccia in italiano, e il
README va aggiornato per mostrarle.

## Per la prossima versione

### Segnalati il 14 ago 2026, non ancora indagati

Tutti e tre sono dell'utente originale, descritti da lui. **Cercare
l'opzione prima di toccare codice.**

**1. «Copiato, ma non incollato» la prima volta.** Non sempre, ma quando
succede è *la prima volta* che sceglie un posto dove incollare; dopo va
bene. Significa che `FocusFinestra.Restituisci` ha restituito false.

La 4.9.0 gli ha messo davanti `ConLeBuone`, che può averlo sistemato di
passaggio — **non è verificato**.

Se succede ancora: l'avviso esce dall'area di notifica dalla 4.6.0,
quindi adesso si legge. E nel log resta `SetForegroundWindow rifiutata
per 0x…`.

**2. Il dialogo del testo nuovo si taglia con la finestra piccola.** La
casella ha `MinHeight = 140` e sta nella riga a stella di
`CorpoConBuco`, che la taglia quando non ci sta. Con il pannello al suo
minimo non entra: titolo, cartella, nome, tipo, barra di formato,
casella, nota e piede.

È già misurato nel `CHANGELOG` della 4.3.0 che al minimo di 340 i
pulsanti restano sotto la piega. Adesso in più la tendina del tipo
aggiunge altezza. Opzioni non ancora valutate: che la casella si
restringa sotto i 140 quando non c'è posto, che la barra di formato si
richiuda, o che il dialogo abbia un suo scorrimento invece di tagliare.

**3. Non si può spostare la finestra con un dialogo aperto.** Il
`ContentDialog` copre l'intero pannello, compresa `ZonaTrascinamento`,
che è la fascia da cui si trascina. Non resta niente da afferrare.

### La lentezza dopo il riposo, a metà

Escluso che sia la memoria: svuotare l'intero working set costa ~14 ms e
compare in `comparsa`, mai in `coda`. La `coda` di 15–47 ms coincide con
la risoluzione predefinita del timer (15,625 ms), e la correzione
—`SetProcessInformation` con `ProcessPowerThrottling`— **è individuata
ma non è provato che funzioni**: un campione a favore e uno contro non
sono una prova.

Manca anche sapere se la macchina in questione è un portatile a
batteria. Cambia il quadro: la limitazione automatica di Windows abbassa
la frequenza solo a batteria, e su un fisso senza core efficienti non si
applica.

### L'avvio di 2.810 ms su una macchina di lavoro

Non toccato. Su una macchina di casa sono 450–500 ms. Si misura
confrontando l'avvio con e senza un'esclusione dell'antivirus su
`%LOCALAPPDATA%\Programs\pastepad`, e quello lo decide l'utente con il
suo reparto IT.

### Firmare l'eseguibile

Lasciare pronta la firma con certificato di Windows, per quando ce ne
sarà uno. Oggi non c'è un certificato, quindi è solo preparare la
strada:

- Inno Setup ha `SignTool` e `SignedUninstaller`
- L'`.exe` pubblicato si firma prima di impacchettarlo, non dopo
- Il certificato **non può finire nel repo**. Va come segreto del
  repository di GitHub e si usa dal workflow
- Senza firma, SmartScreen avvisa all'installazione; con la firma smette
  di avvisare quando il certificato accumula reputazione —o subito con
  uno EV

Chiedere all'utente che tipo di certificato ha ottenuto prima di
implementare qualcosa: OV ed EV non si usano allo stesso modo.

## Regola permanente

A ogni aggiornamento, prima di toccare qualcosa, il subagente
**pianificatore** ha due incarichi fissi:

1. **Cercare cosa ottimizzare e cosa ripulire** perché il programma non
   arrugginisca: dipendenze, codice che non si usa più, misure rimaste
   vecchie.
2. **Controllare cosa ha pubblicato Microsoft per il Windows App SDK e
   per WinUI 3** dalla versione precedente, e dire se conviene
   aggiornare e perché. Oggi il progetto usa la 2.3.1 e l'ultima stabile
   era la 2.4.0.

Sta anche in `.claude/agents/pianificatore.md`, che è dove il subagente
lo legge da solo.
