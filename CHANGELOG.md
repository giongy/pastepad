# Changelog

Tutti i cambi rilevanti di questo progetto sono documentati qui.
Il progetto segue il [Semantic Versioning](https://semver.org/).

## [Non rilasciato] — copia in italiano

Questa copia deriva dalla 4.15.0 del progetto originale di Josemgu.

### Cambiato
- **Tutto in italiano**: interfaccia, nomi nel codice, commenti,
  messaggi di log, installer, workflow e documentazione.
- **Una sola lingua.** Tolte le tabelle di traduzione (spagnolo,
  inglese, portoghese, francese) e il selettore della lingua in Aspetto.
- **Formato dei dati tradotto**: `historial.json` è diventato
  `cronologia.json`, `notas.json` è `note.json`, `imagenes` è
  `immagini`, e le chiavi dei JSON e di `config.json` sono in italiano.
  I dati creati dalla versione originale non si leggono più.
- L'opzione per la cartella dati di prova si chiama `--dati`.
- La ricerca ignora anche gli accenti italiani (à, è, ì, ò, ù), oltre a
  quelli che ignorava già.
- Le lettere dei pulsanti di formato sono G, C e S, come in Word in
  italiano.
- 122 prove diventano 118: quattro riguardavano solo le traduzioni.

### Aggiunto
- `pubblica.ps1`, che compila in Release e copia il programma in una
  destinazione.
- Configurazione per avviare e fare il debug da VS Code.

### Tolto
- Il progetto di sondaggio della scorciatoia, che non si pubblicava.
- Le schermate della documentazione, che mostravano l'interfaccia
  originale.

## [4.15.0] - 2026-09-10

### Aggiunto
- **Le note si possono formattare**: grassetto, corsivo, sottolineato,
  punti elenco, numerazione e rimuovi formattazione, in una barra corta
  sopra la nota. Senza selettori di carattere, dimensione o colore — una
  nota non esce mai da pastepad, quindi sceglierne il carattere non
  serve a niente; evidenziare ciò che conta e fare elenchi sì. La barra
  lunga resta dove si guadagna il posto, nei testi salvati, che
  viaggiano verso Word e Outlook.

  Il sottolineato qui c'è anche se fu tolto dalla barra dei testi
  salvati. Lì fu tolto perché non aggiungeva niente a un testo che sta
  per essere incollato in un altro programma, e una nota non si incolla
  in nessuno.

### Cambiato
- Le note si salvano come i testi salvati, a frammenti con formato. Un
  file delle note scritto dalla 4.14.0 o precedente si apre ancora: il
  suo testo semplice diventa un frammento, e il file si riscrive nella
  forma nuova senza la chiave vecchia, così lo stesso contenuto non può
  esistere in due posti che discordano. C'è una prova che fallisce
  rumorosamente se questo si rompe — l'alternativa è un utente che
  aggiorna e trova il blocco vuoto.
- Modificare una nota dal menu dei tre punti apre la stessa lavagna che
  apre il clic. Due ingressi, un solo editor.
- Da 120 a 122 prove.

## [4.14.0] - 2026-09-10

**Le note si vedevano male, e il motivo era uno stile che non avrebbero
mai dovuto ereditare.**

### Corretto
- **Il riquadro di una nota mostrava una linea e mezzo riquadro vuoto.**
  L'anteprima era costruita con più linee e al riquadro si diceva di
  disegnarle, ma prendeva in prestito lo stile della riga, che imposta
  `MaxLines` a 1 e spegne l'a capo automatico — giusto per una riga, il
  contrario di giusto per un riquadro. Le note di quattro linee si
  mostravano come una.
- **I riquadri erano di due colori diversi.** Quello selezionato era
  dipinto a piena forza e gli altri attenuati, e si leggeva come due
  tipi di nota invece che come una nota selezionata. Adesso sono tutti
  dello stesso colore, e quello selezionato è marcato con un bordo, che
  dice "questo" senza dire "questo è diverso".
- **La nota aperta era una casella scura dentro un riquadro giallo**,
  con testo bianco che non si poteva leggere una volta sparita la
  casella. Un `TextBox` porta il proprio sfondo e il proprio colore del
  testo per stato, e vincono su qualsiasi cosa impostata da fuori; gli
  stessi pennelli che la barra di ricerca già spegne andavano spenti
  anche qui.

### Cambiato
- La data sta in alto a destra nel riquadro, dove l'occhio non sta
  leggendo, invece che in alto a sinistra dove inizia il testo.
- I riquadri crescono con ciò che contengono invece di avere un'altezza
  fissa, quindi una nota di due linee è un riquadro di due linee.
- **Un ✓ chiude la nota** e torna alla lista. Non dice "Salva", perché
  il salvataggio è già avvenuto a ogni tasto — un pulsante che promette
  di salvare ciò che è già salvato insegna a diffidare del resto.

## [4.13.0] - 2026-09-10

**Le note sono diventate un blocco che si richiama.**

### Aggiunto
- **Una nota ora si apre dentro il pannello e resta aperta.** Cliccane
  una, scrivici, nascondi pastepad con la tua scorciatoia — e la
  prossima volta che lo richiami quella nota è ancora aperta, con il
  cursore dentro e tutto ciò che hai scritto. Niente pulsante Salva:
  scrive a ogni tasto, perché qualcosa che chiami con una scorciatoia e
  congedi con un'altra non può dipendere dal ricordarsi di salvare.
- **Le note possono avere un nome**, e nella scheda Note la barra di
  ricerca le trova per quel nome. Il nome pesa più del corpo, quindi
  chiamare una nota per nome la mette per prima anche quando quelle
  lettere compaiono dentro altre dieci. Le note restano fuori dalla
  ricerca generale: tutto ciò che compare lì si può incollare, e una
  nota no.
- **La barra di ricerca resta visibile mentre una nota è aperta.**
  Cercare copre la nota senza chiuderla, e svuotare la ricerca la
  riporta subito com'era.
- Una nota lasciata completamente vuota si elimina da sola, quindi
  iniziarne una e cambiare idea non lascia niente.

### Cambiato
- Da 113 a 120 prove.

## [4.12.0] - 2026-09-10

### Cambiato
- **Le note ora sono riquadri, non righe.** Una riga mostra una linea, e
  una nota rapida in una non ci sta: due note che iniziano allo stesso
  modo —la stessa intestazione incollata dalla stessa console— nella
  lista sembravano identiche e bisognava aprirle tutte e due per
  distinguerle. Un riquadro mostra la data e le prime linee, quindi si
  distinguono a colpo d'occhio.

  Il riquadro usa il tuo colore di accento come sfondo, con il colore di
  testo che la tavolozza ha già verificato per il contrasto.

## [4.11.0] - 2026-09-10

### Aggiunto
- **Una scheda Note**, per le cose che scrivi invece di quelle che
  incolli. Come le note rapide di Windows: ne scrivi una, la leggi, la
  elimini. **Niente lì dentro si può incollare** — cliccare una nota la
  apre per modificarla, e il suo menu offre Copia, Modifica ed Elimina
  ma non Incolla. Le note vivono in un file loro, quindi niente di ciò
  che annoti può finire nelle liste che esistono per essere incollate
  altrove.
- La nota toccata più di recente sta in alto.

### Corretto
- **I modelli non si potevano incollare in Outlook.** Prima di ogni
  incolla, pastepad rilasciava Shift e Alt nel caso la scorciatoia li
  usasse — ma li rilasciava che fossero premuti o no, quindi una
  scorciatoia come `Ctrl+Q` mandava comunque un rilascio di Alt che
  nessuno aveva premuto. Windows trasforma un rilascio di Alt isolato in
  un comando di menu: la documentazione dice che `DefWindowProc` manda
  `WM_SYSCOMMAND` con `SC_KEYMENU` quando il tasto ALT viene rilasciato.
  Outlook rispondeva mettendo una lettera su ogni pulsante della barra
  multifunzione, e il `Ctrl+V` che seguiva andava al menu invece che al
  messaggio. Adesso si rilasciano solo i tasti davvero premuti, e `Ctrl`
  scende per primo così che il rilascio di Alt non sia mai isolato.

### Cambiato
- Dentro Salvati, il gruppo che si chiamava **Note ora si chiama
  Testi**. Altrimenti una scheda e un gruppo avrebbero avuto lo stesso
  nome con significati diversi — uno incolla, l'altro no.
- Da 99 a 107 prove.

## [4.10.0] - 2026-08-15

### Cambiato
- **Un segnalibro salvato si apre di nuovo quando lo clicchi.** Salvare
  un indirizzo come segnalibro significa "voglio visitarlo", non "voglio
  scriverlo", e la 4.4.0 l'aveva tolto insieme all'errore che stava
  correggendo.

  Adesso i due casi convivono, perché a decidere è il **tipo**, non che
  il testo sembri un link:

  | | Clic | Menu dei tre punti |
  |---|---|---|
  | Segnalibro salvato | Apre il browser | Copia, Incolla, Apri |
  | Indirizzo nella cronologia | Incolla | Copia, Incolla, Apri |
  | Nota, email, modello, prompt | Incolla | Copia, Incolla |

  Un indirizzo copiato continua a incollarsi — quello era l'errore della
  4.4.0 e non torna. E a un indirizzo salvato che preferisci incollare
  basta cambiare il tipo in Nota.

  **Incolla nel menu continua a incollare, segnalibro o no.** È
  esattamente la trappola in cui cadde la 4.3.0: le quattro strade che
  portano a incollare passavano tutte da un solo posto, quindi la voce
  di menu che diceva "Incolla" apriva Chrome.

### Interno
- **Ogni riga percorreva tutto il suo testo due volte.** Capire se è un
  link e se contiene `[[campi]]` si faceva una volta per la riga, e poi
  di nuovo dentro la deduzione del tipo — fino a ottanta volte per
  apertura, e ancora a ogni tasto nella barra di ricerca. Le righe
  salvate inoltre riunivano una seconda volta tutti i loro frammenti in
  una stringa. Adesso il lavoro si fa una volta e si riusa.
- **I pennelli dello stesso colore si condividono** invece di creare un
  oggetto nuovo a ogni chiamata, e ogni aggiornamento ne chiede decine.

  Misurato onestamente: **nessuna delle due cose ha spostato il tempo di
  apertura del pannello.** Con ottanta voci lunghe la mediana è passata
  da 20,3 ms a 19,3 ms, che è rumore — il costo lì è il sistema di
  finestre, non la nostra aritmetica. Restano perché fare due volte lo
  stesso lavoro merita di essere tolto di per sé, non perché abbiano
  reso qualcosa più veloce. Se aiutano mentre si scrive nella barra di
  ricerca **non è stato misurato**, quindi non si afferma niente.

## [4.9.0] - 2026-08-14

**Aprire il pannello poteva richiedere cinque secondi e mezzo, e nessuno
lo sapeva.**

### Corretto
- **La chiamata che ruba il primo piano poteva bloccarsi per secondi, e
  stava in mezzo al percorso che deve essere istantaneo.** Misurato
  contro una finestra il cui thread di interfaccia era occupato apposta:
  `SetForegroundWindow` ci mise **5.399 ms** — e poi restituì false. La
  causa è documentata: agganciarsi al thread dell'altra finestra
  significa che "keyboard and mouse events received by both threads are
  processed in the order they were received", quindi pastepad si accoda
  dietro a qualsiasi cosa stia facendo quel programma.

  Non è un caso da laboratorio. Su una macchina di lavoro carica di
  agenti di sicurezza aziendali, il programma davanti è occupato molto
  più spesso, ed è esattamente il motivo per cui lo stesso pannello si
  apre in 18 ms una volta e in 163 ms la successiva senza uno schema.

  La chiamata non si toglie — senza, il pannello compare visibile ma
  senza focus di tastiera. Adesso ha davanti una strada veloce: chiedere
  il primo piano **senza agganciarsi a nessuno**, che è permesso perché
  pastepad è appena stato svegliato da una scorciatoia globale, e
  ricadere sulla strada vecchia solo se Windows rifiuta. Misurato nella
  stessa prova: **da 5.604 ms a 35,7 ms**.
- **Incollare aspettava 60 millisecondi ogni singola volta, per
  niente.** Quel numero era scritto a mano per lasciare assestare il
  focus. Misurato, la finestra di destinazione era già davanti quando
  iniziava l'attesa, quindi tutti i 60 ms erano buttati — e su una
  macchina carica lo stesso numero fisso poteva altrettanto facilmente
  essere troppo corto, mandando il Ctrl+V prima che la destinazione
  potesse riceverlo. Adesso aspetta che la finestra sia davvero davanti,
  e incolla allora. Un incolla completo è passato da circa 130 ms a
  **73,9 ms**.
- **Aprire il pannello ricostruiva la lista due volte** ogni volta che
  l'uso precedente aveva lasciato qualcosa nella barra di ricerca:
  svuotarla fa partire lo stesso aggiornamento che la riga successiva
  poi richiama. Fino a 80 righe costruite per niente, sul percorso che
  deve essere istantaneo.

### Cambiato
- **L'autore pubblicato è ora `Josemgu`**, l'account GitHub, invece di
  un nome legale completo — nell'installer, nel `CompanyName`
  dell'eseguibile e nei README.

### Corretto nella strumentazione stessa
Queste due cose facevano mentire i log, che è peggio di non misurare.

- **Aprire e chiudere in fretta gonfiava le cifre di disegno.** Più
  timer si accumulavano in attesa dello stesso fotogramma e riportavano
  tutti insieme: tre righe a otto millisecondi l'una dall'altra che
  dichiaravano 588, 322 e 200 ms. Nessuna era vera, e cifre così mandano
  a caccia di una lentezza che non esiste. Adesso si misura solo
  l'apertura davvero a schermo.
- **Il contatore di inattività misurava la cosa sbagliata.** Contava
  qualsiasi attività, e copiare qualsiasi cosa lo azzerava — quindi chi
  lavorava normalmente risultava sempre "appena usato". Una riga diceva
  "0,5 min inattivo" con otto minuti di orologio tra quella e l'apertura
  precedente. Adesso il log riporta tutti e due: i minuti dall'ultima
  apertura del pannello, che è ciò che predice un'apertura lenta, e i
  minuti dall'ultima cosa successa.

## [4.8.0] - 2026-08-14

### Corretto
- **pastepad non cambia più la tua scorciatoia senza dirtelo.** Se la
  combinazione che hai scelto è già presa da un altro programma,
  `RegisterHotKey` la rifiuta —errore 1409— e pastepad ricadeva in
  silenzio su quella di fabbrica e proseguiva. Non si mostrava niente,
  perché il ripiego aveva funzionato. Continui a premere la tua
  scorciatoia per sempre, non succede niente, e il tasto passa al
  programma davanti, che è il segnale che pastepad non l'ha mai
  ricevuto.

  È comune su una macchina di lavoro, dove più programmi si contendono
  le combinazioni. Adesso lo dice **tramite l'icona nell'area di
  notifica**, nominando sia quella presa sia quella usata al suo posto.
  Non si può dire solo nel pannello: il pannello si apre con la
  scorciatoia, quindi chi ha il problema è esattamente chi non può
  arrivare al messaggio che lo spiega. Cliccare quella stessa icona apre
  il pannello per cambiarla.
- **Aggiornare sopra una copia in esecuzione ora la chiude prima, come
  si deve.** L'installer ti consegnava la pagina di Windows
  "Preparazione dell'installazione", che elenca pastepad e chiede se può
  chiuderlo. È una domanda con una sola risposta giusta, e scegliere
  "non chiudere le applicazioni" sostituiva i file sotto una copia
  ancora in esecuzione — motivo per cui installare sopra una versione
  esistente non si comportava come un'installazione pulita.

  Adesso pastepad viene chiuso prima ancora che Setup guardi quali file
  sono in uso, quindi quella pagina non compare mai. Viene chiuso dalla
  stessa porta che usa Windows spegnendosi, quindi riversa ciò che non
  ha ancora scritto, rilascia la scorciatoia globale e toglie la sua
  icona, invece di essere ucciso. A installazione finita viene riaperto
  senza chiedere, anche in un'installazione silenziosa: non l'hai chiuso
  tu, e ciò che hai chiesto era un aggiornamento.

## [4.7.0] - 2026-08-14

**pastepad non partirà più vuoto comportandosi come se fosse normale.**

### Corretto
- **Se i file di pastepad vengono serviti da un posto diverso da quello
  che ha chiesto, adesso lo dice e si rifiuta di scrivere.** È l'errore
  che era aperto dalla 4.0.1 come *"un'istanza partì senza poter leggere
  né scrivere nella sua cartella e, chiudendosi, salvò una cronologia
  vuota sopra quella reale. Non riprodotto."* Si è riprodotto, e adesso
  la causa è nota.

  Quando un'altra applicazione impacchettata lancia pastepad, pastepad
  eredita il contenitore di quell'applicazione e Windows reindirizza
  tutto ciò che sta sotto `%LOCALAPPDATA%` in
  `…\Packages\<pacchetto>\LocalCache\Local`. pastepad calcola
  correttamente il suo percorso, crede di leggerlo, e in realtà legge e
  scrive una copia. Aprilo dopo dal suo collegamento e **la cronologia e
  i testi salvati sono spariti** — mentre su disco sono intatti. È ciò
  che successe il 14 agosto 2026, e partire da zero senza una parola è
  ciò che trasformò un incidente dell'ambiente in "ha cancellato tutto".

  All'avvio pastepad ora scrive una sonda nella sua cartella dati e
  chiede a Windows dove quel file è finito davvero. Se la risposta non è
  dove aveva chiesto, la sessione diventa di sola lettura per sempre:
  **non si scrive niente, in nessun file**, il motivo si registra con
  tutti e due i percorsi, e il pannello si apre con la spiegazione
  invece che con una lista vuota.

  Tre cose andarono misurate per farlo bene, e due sembravano corrette e
  non lo erano:
  - **L'identità di pacchetto non lo rileva.**
    `GetCurrentPackageFullName` restituisce `APPMODEL_ERROR_NO_PACKAGE`:
    il processo figlio eredita il reindirizzamento dei file ma non
    l'identità. Provato per primo, e non scattò mai.
  - **Nemmeno un handle di cartella.** La cartella si risolve in se
    stessa; solo un file rivela il reindirizzamento.
  - **Il file deve essere appena scritto.** Il reindirizzamento è
    copia-alla-scrittura, file per file, quindi un file che c'era già si
    risolve nel percorso reale finché qualcuno non lo scrive.

  Il guardiano è una sola sonda all'avvio, creata con `DELETE_ON_CLOSE`
  così Windows la toglie anche se il processo muore tenendola aperta.
- Da 94 a 99 prove. Le cinque nuove fissano la garanzia che conta: una
  sessione congelata non scrive niente — né le preferenze, né il
  riversamento rimandato della cronologia, né le cose salvate
  all'istante come fissare, eliminare e svuotare, che sono ciò che la
  persona ha fatto apposta.

## [4.6.0] - 2026-08-14

**Due cose che fallivano in silenzio.** Una si vedeva e non ci si
credeva, l'altra non si vedeva affatto.

### Corretto
- **Il pannello non compare più disegnato a metà.** Segnalato dall'uso
  reale: la prima volta che si apriva, l'intestazione usciva vuota —
  niente etichetta della scorciatoia, niente pulsanti — e la volta dopo
  andava bene. Misurato, non era né l'intestazione né la barra del
  titolo: nell'istante in cui la finestra compariva, il contenuto
  mancava **dappertutto**, circa 5.500 pixel dipinti dei 36.500 di un
  pannello finito, che si riempivano in circa 150 ms. Di solito non te
  ne accorgi. Con la macchina occupata si allunga abbastanza da sembrare
  che manchi qualcosa.

  Adesso il pannello si compone una volta all'avvio, fuori dallo schermo
  e senza essere attivato, così la prima volta che lo apri davvero non è
  più il primo fotogramma che abbia mai disegnato. Misurato su sei giri
  sotto carico: il pannello è completo nell'istante in cui compare —
  6.562 pixel nell'intestazione e 36.518 nel corpo, identici a 0 ms e a
  300 ms, dove prima erano circa 800 e 5.500. **Aprire non costa niente
  in più**, perché il lavoro avviene all'avvio, che è rimasto a
  467–493 ms.
- **L'avviso che non si poteva mai leggere.** Incollando, il pannello si
  nasconde prima che il focus venga restituito, quindi nella rara
  occasione in cui la restituzione falliva, "Copiato, ma non sono
  riuscito a tornare alla finestra precedente" si dipingeva su una
  finestra già nascosta. Restavi con il testo copiato, non incollato, e
  senza sapere perché. Adesso arriva dall'icona nell'area di notifica,
  che è l'unico posto che può raggiungerti quando il pannello non è
  davanti a te. Lo stesso per un link che non si riesce ad aprire.
- **Un avviso poteva anche tornare stantio.** Siccome quei messaggi si
  scrivevano su un pannello nascosto, la volta successiva che lo aprivi
  —magari mezz'ora dopo, accanto a qualcosa che non c'entrava— la barra
  rossa era ancora lì. Gli avvisi adesso si conservano come si
  conservava già l'avviso di versione nuova, e chiudere il pannello ne
  segna uno come letto.

## [4.5.0] - 2026-08-14

### Aggiunto
- **I prompt di IA sono il quinto tipo**, accanto a segnalibri, modelli,
  email e note. Chiesto da chi tiene i prompt come parte del proprio
  modo di lavorare: mescolati tra le note non si trovano, e sono tra le
  cose più spesso riusate parola per parola.

  Come le email, questo non si indovina mai. Un prompt è testo normale,
  e dove un'email ha almeno una @ a cui aggrapparsi, un prompt non ha
  niente — che è precisamente il motivo per cui va scelto a mano. Un
  prompt con `[[campi]]` si propone comunque come modello, ed è giusto:
  ciò che decide se ti viene chiesto prima di incollare è il testo, non
  il tipo. Spostalo tra i prompt e chiede ancora i suoi campi.
- **Il primo fotogramma del pannello ora lascia una riga nel log**, una
  volta per esecuzione: la dimensione e il colore misurati
  dell'etichetta della scorciatoia e del pulsante di chiusura, il tema
  risolto, e se la barra del titolo è estesa. Esiste per un errore
  segnalato dall'uso reale — la prima volta che il pannello si apre,
  l'intestazione a volte compare non dipinta, e la volta dopo va bene —
  che nessun controllo automatico può vedere, perché l'albero di
  accessibilità ha gli elementi tutte e due le volte. Si vede solo
  guardando i pixel, e a quel punto è passato.

  Ciò che quella riga ha già escluso: la riga è disposta correttamente
  al primo fotogramma (etichetta 49×15, pulsante di chiusura 20×20) e il
  suo colore è opaco e visibile, quindi né "non è mai stata disposta" né
  "il tema si è risolto tardi" è la spiegazione. Misurare l'intera
  finestra invece della striscia dell'intestazione ha mostrato che il
  primo fotogramma è dipinto parzialmente **dappertutto**, non solo
  nell'intestazione, e questo esclude anche la barra del titolo.
- Da 90 a 94 prove.

## [4.4.0] - 2026-08-13

### Corretto
- **Un link non si poteva incollare da nessuna parte.** Copiare un
  indirizzo web e provare a metterlo in un campo non faceva niente: la
  riga si comportava come un pulsante e apriva il browser. Aprire era
  voluto; ciò che non lo era è che tutte e quattro le strade verso
  l'incolla passavano dallo stesso posto — il clic, l'Invio dalla barra
  di ricerca, e **Incolla** e **Incolla senza formato** nel menu della
  riga. Quindi la voce etichettata Incolla apriva Chrome. Adesso il clic
  incolla, come in ogni altra riga, e aprire si è spostato dove non può
  essere scambiato per incollare: **Apri nel browser**, in cima al menu
  della riga. Verificato da un capo all'altro con il Blocco note.

### Aggiunto
- **Scegli tu cos'è un testo salvato**: segnalibro, modello, email o
  nota. pastepad propone quello adatto a ciò che hai scritto —una URL
  intera è un segnalibro, dei `[[campi]]` fanno un modello— e smette di
  proporre nel momento in cui tocchi la tendina. Sta nella stessa linea
  dell'etichetta Nome, che costa 13 pixel di altezza del dialogo invece
  dei 40 che prenderebbe una riga propria.
- **Le email sono il quarto gruppo**, e quello che non si poteva mai
  indovinare: il corpo di un messaggio è testo normale e dentro non c'è
  niente che lo separi da una nota. Cinque corpi di email erano cinque
  note in più. pastepad non propone mai questo tipo da solo — offrirlo
  perché il testo contiene una @ trasformerebbe in email ogni nota che
  menziona un indirizzo.
- Ciò che scegli si scrive in `snippets.json` solo quando contraddice
  ciò che si sarebbe dedotto comunque, quindi aprire un testo salvato e
  premere Salva non può dargli una chiave `tipo` che non aveva. Nota:
  questo riguarda solo la chiave del tipo; risalvare un testo normalizza
  comunque i suoi a capo a `\r\n`, che è la riparazione introdotta dalla
  4.0.1.

### Cambiato
- **L'installer ora dice quando ha trovato una copia esistente.**
  Sostituiva sempre invece di duplicare —a questo serve l'AppId fisso—
  ma non lo diceva mai, quindi installare la 4.4.0 sopra la 4.3.0
  sembrava esattamente una prima installazione: nessun modo di capire se
  stavi aggiornando o duplicando, e nessuna parola sul destino dei testi
  salvati. Adesso legge prima la versione installata e offre di
  aggiornare a questa, dicendo subito che cronologia, testi salvati,
  scorciatoia e preferenze si conservano perché vivono in una cartella
  separata. Installare di nuovo la stessa versione si offre come
  reinstallazione, e installare una versione più vecchia sopra una più
  nuova lo dice invece di farlo in silenzio. In un'installazione
  silenziosa non si chiede niente, perché una finestra di messaggio
  aspetterebbe per sempre qualcuno che non sta guardando.
- **La voce di avvio non è più l'unico modo in cui pastepad torna.**
  Adesso c'è anche un'attività di accesso, registrata da pastepad stesso
  senza bisogno di diritti di amministratore, e ritardata di mezzo
  minuto così che la voce di registro vinca ogni volta che funziona.

  Segue il caso di una macchina in cui pastepad non partì dopo un
  riavvio e non lasciò niente da leggere. La voce di registro era
  corretta, puntava a un eseguibile esistente, non era disattivata in
  Gestione attività, e Windows elaborò quella chiave in quell'avvio —
  OneDrive, un'altra voce della stessa, partì 15 secondi dopo explorer.
  pastepad non lasciò nessuna riga in nessuno dei due log e nessun
  rapporto di errore, quindi il processo o non fu mai creato o morì
  prima della sua prima istruzione, dove l'host di .NET fallisce in
  silenzio. La causa è ancora ignota; la seconda strada fa sì che non
  serva trovarla perché il programma torni.
- **Ogni avvio ora registra da quanto era acceso Windows.** Finora
  pastepad poteva registrare solo gli avvii avvenuti, quindi un avvio
  non avvenuto non lasciava niente — che è esattamente ciò che rese
  impossibile la diagnosi dell'errore qui sopra. Pochi secondi
  significano che l'ha aperto Windows; nove ore significano che l'hai
  aperto tu, e l'avvio automatico è fallito.
- **La scorciatoia ora si misura lungo tutto il suo percorso**, in tre
  parti: quanto la pressione ha aspettato in coda, quanto il pannello ci
  ha messo a chiedere la sua finestra, e quanto è passato prima che
  fosse davvero disegnato. La cifra vecchia copriva solo quella di mezzo
  e diceva 25 ms mentre il pannello sembrava lento. Ogni riga registra
  anche da quanto il programma era inattivo, così i numeri si possono
  dividere in "appena usato" e "lasciato solo per dieci minuti".
- Da 79 a 90 prove.

## [4.3.0] - 2026-08-13

### Aggiunto
- **Qualsiasi testo salvato può avere un nome**, non solo i segnalibri.
  Lasciarlo vuoto si comporta come prima e si usa la prima linea, quindi
  salvare due frasi non richiede di scrivere niente in più. Conta quando
  costruisci una raccolta invece di annotare cose: cinque corpi di email
  che iniziano tutti con "Hi team," sono cinque righe identiche, e la
  cartella ti dice che sono corpi senza dirti quale è quale.

### Corretto
- **I pulsanti Salva e Annulla potevano finire mezzi sotto la piega**
  nel dialogo Nuovo, alla dimensione predefinita del pannello, quando
  compariva il campo del nome — che finora succedeva solo quando ciò che
  scrivevi era un link. Il dialogo dava l'altezza rimasta prima alla
  casella di testo e ai pulsanti per ultimi. Misurato sulla 4.2.0: 19
  pixel di pulsante invece di 32.

### Cambiato
- La casella di scrittura del dialogo Nuovo mostra 140 pixel alla volta
  invece di 180. È ciò che costa il campo del nome, e la casella scorre
  al suo interno, quindi cambia quanto vedi in una volta e non quanto
  puoi scrivere.
- Da 78 a 79 prove.

## [4.2.0] - 2026-08-13

**Aggiornare pastepad non ti costa più gli appunti che hai appena
copiato, e non lo lascia più chiuso.** Succedevano tutte e due le cose,
e nessuna diceva niente.

### Corretto
- **pastepad non tornava dopo essere stato aggiornato.** Installare
  sopra una copia in esecuzione lo lasciava installato e spento — niente
  scorciatoia, niente cattura, niente a schermo che lo dicesse. Windows
  riapre solo un programma che ha chiesto di essere riaperto, e pastepad
  non lo chiedeva mai. Misurato con la stessa API che usa l'installer:
  `bRestartable` era `False`.
- **Si perdevano fino a tre secondi di copie ogni volta.** La cronologia
  si scrive ogni pochi secondi invece che a ogni copia, e l'unica cosa
  che la forzava era uscire dall'area di notifica. Un installer che
  chiudeva pastepad —o Windows che si spegneva— si portava via ciò che
  non era ancora stato scritto. Adesso salva quando Windows lo avverte,
  cosa che prima non sentiva: la finestra che gestisce la scorciatoia è
  una finestra solo-messaggi, e quelle non si possono enumerare, quindi
  l'avvertimento non le arrivava mai. Una seconda finestra, mai
  disegnata e mai in Alt+Tab, lo ascolta.
- Ciò che succedeva in un aggiornamento dipendeva dal fatto che **il
  pannello fosse aperto**. Con il pannello nell'area di notifica Windows
  classificava pastepad come un programma senza finestra di primo
  livello; con il pannello aperto, come uno normale. Stesso programma,
  due comportamenti, decisi da dove l'avevi lasciato.
- **L'opzione della cartella dati di prova riscriveva la vera voce di
  avvio automatico.** L'opzione esiste per provare senza toccare la tua
  installazione, e teneva separati i dati di ogni prova mentre puntava
  la voce di avvio di Windows a una build che il giorno dopo non ci
  sarebbe più stata.

### Cambiato
- Da 74 a 78 prove.

## [4.1.0] - 2026-08-13

**Questa è l'ultima versione da installare a mano.** Da qui in poi
pastepad controlla le nuove release e te lo dice dentro il pannello. Se
arrivi dalla 4.0.0, ti porti dietro sei errori, due dei quali
distruggevano testo senza dirlo.

### Aggiunto
- **Un'intera cartella si può modificare in una volta**, come una nota
  per linea, nella stessa casella usata per aggiungerle. Togliere cento
  note su tremila richiedeva cento passaggi tra ricerca, menu, modifica
  e salvataggio; adesso è una modifica e un Salva. Le linee che non hai
  toccato conservano la loro nota originale, quindi un segnalibro
  conserva il nome che gli hai dato e una nota formattata conserva il
  suo formato. Le note su più linee restano fuori e intere, e il dialogo
  lo dice. Prima di salvare ti viene detto quante note spariranno, e
  tornare indietro ti riporta a ciò che avevi scritto.
- **Una barra di formato per i testi salvati**: carattere, dimensione,
  grassetto, corsivo, dieci colori di testo, punti elenco, numerazione,
  rientro e rimuovi formattazione. Il formato dei dati portava già tutto
  e l'RTF che arriva a Outlook e Word lo capiva già — semplicemente non
  c'era mai stato un modo di impostarlo.
- **Un avviso quando esce una versione nuova**, mostrato dentro il
  pannello con un link alla release. Si può spegnere in Aspetto. Niente
  viene scaricato o installato da solo: resta una decisione tua.

### Corretto
- I campi di testo disegnavano la sottolineatura di focus nel colore di
  accento di Windows invece che in quello scelto da te. Un'app con
  accento menta mostrava 491 pixel di arancione.
- I colori si perdevano riaprendo un testo formattato, perché l'editor
  veniva caricato prima che il dialogo fosse a schermo.
- Il clic destro su una cartella non faceva niente nell'elenco a discesa
  e offriva solo rinomina ed elimina sulle etichette.

### Cambiato
- Da 51 a 74 prove.

## [4.0.1] - 2026-08-13

Il primo giorno di uso reale fece emergere tre errori, e misurarli ne
fece emergere altri tre. Due dei sei distruggevano testo senza dirlo.

### Corretto
- **Modificare un testo salvato di più linee conservava solo la
  prima.** Aprire l'editor e premere Salva —senza toccare niente—
  tagliava una nota di 100 linee da 5290 caratteri a 77. La causa era
  l'ordine di due righe in un inizializzatore di oggetto: un `TextBox`
  di WinUI con `AcceptsReturn` ancora a `false` tronca ciò che si
  assegna a `Text`, e `Text` veniva prima.
- **Salvare ricalcolava il titolo dalla prima linea**, quindi aprire e
  salvare un segnalibro cancellava il nome che gli era stato dato.
- **Gli a capo si leggevano come `\n` quando un `TextBox` restituisce
  `\r`.** Incollare 60 linee e scegliere "una nota per linea" produceva
  una sola nota con tutte dentro, e il testo salvato arrivava alle altre
  applicazioni su una linea.
- **Un testo lungo spingeva i pulsanti del dialogo fuori dallo schermo**
  senza modo di scorrere fino a loro. Il corpo adesso è una griglia: il
  piede conserva il suo posto e la casella di testo scorre al suo
  interno.
- **Le cartelle non si potevano modificare.** La versione in Python
  aveva quel dialogo e non era stato portato. Rinomina ed Elimina erano
  anche irraggiungibili con "Tutte le cartelle" selezionato, che è il
  valore predefinito.
- **Un pulsante secondario era bianco su bianco nel tema chiaro**,
  perché lì lo sfondo elevato e il riquadro sono tutti e due `#FFFFFF`.

### Aggiunto
- I segnalibri possono avere un nome.
- Gli elementi salvati hanno un contrassegno quando contengono
  `[[campi]]`.
- Salvati si divide in Segnalibri, Modelli e Note in base a cos'è il
  contenuto invece che a dove è stato archiviato. I gruppi partono
  chiusi e ricordano il loro stato tra una sessione e l'altra.
- "Modifica il contenuto di \<cartella\>…" nel menu della cartella.

### Cambiato
- Da 45 a 51 prove.

## [4.0.0] - 2026-08-13

Riscritto in C# con WinUI 3. Stesso programma, stessi dati, stessa
scorciatoia — ma adesso la scorciatoia risponde ogni volta, che è il
motivo per cui la riscrittura è avvenuta.

### Cambiato
- **La scorciatoia globale sta su una finestra solo-messaggi** che vive
  quanto il processo, non quanto il pannello. La versione in Python
  registrava una scorciatoia di thread e la pompava da un lavoratore; un
  messaggio senza finestra associata non viene mai smistato a una
  procedura di finestra, quindi rispondeva qualche volta e poi smetteva.
  Misurato a 100 pressioni su 100, e di nuovo a 30 su 30 sulla build
  installata.
- **Gli appunti avvisano con un evento** invece di essere sondati ogni
  0,7 secondi. `WM_CLIPBOARDUPDATE` arriva più di una volta per copia,
  quindi un filtro sul numero di sequenza separa una copia vera da una
  ripetizione.
- **Un processo invece di due.** Il working set è passato da 207 MB a
  120 MB installato; il pannello si apre in 16–25 ms.
- Il tema segue Windows e cambia a pannello aperto. Mica traspare a meno
  che non si scelga uno dei dodici sfondi.
- Le righe delle impostazioni si impilano invece di dividersi in due
  colonne. Due colonne richiedono che un pannello da 380 pixel sia largo
  455, e la tendina tagliava da sinistra senza puntini —
  `Ctrl + Shift + V` si leggeva come `l + Shift + V`, che sembra
  un'altra scorciatoia invece di una tagliata.
- Il programma si installa in `%LOCALAPPDATA%\Programs\pastepad` e tiene
  i dati in `%LOCALAPPDATA%\pastepad`. Disinstallare non può più toccare
  la cronologia, per costruzione e non perché lo chiede prima.

### Aggiunto
- Un installer: un solo `.exe` da 47 MB, nessuna richiesta di
  amministratore, una voce in Installazione applicazioni, e il suo
  disinstallatore.
- Selettore della lingua per le quattro traduzioni che esistevano già ma
  non si potevano scegliere.
- Nomi accessibili sui pulsanti con icona e sui diciotto campioni di
  accento, che non ne avevano.

### Corretto
- Accenti e punteggiatura di apertura nelle quattro lingue.
- Ogni colore di accento ora supera il contrasto WCAG AA, verificato con
  il calcolo. Il colore del sottotitolo falliva su tutti e dodici gli
  sfondi.
- I titoli salvati si conservano interi. Un riassunto di una linea
  veniva scritto in `snippets.json` con i puntini di sospensione dentro
  i dati.
- Un file che esiste ma non si può leggere viene segnato e mai
  sovrascritto. `File.Exists` restituisce false quando il permesso è
  negato, quindi una cronologia illeggibile poteva passare per un primo
  avvio ed essere sostituita da una vuota.

### Tolto
- L'implementazione in Python. Vive nel tag `ultima-version-python` e in
  `v3.0.1`.
- Le preimpostazioni di dimensione della finestra. Il pannello si
  ridimensiona trascinando e ricorda dove è stato lasciato.

## [2.0.0] - 2026-08-12

### Cambiato
- **Riscritto come pacchetto.** Un file da 2.300 righe con una classe da
  950 righe è diventato otto moduli con un compito ciascuno: `modelo`
  possiede i dati e non importa mai tkinter, `windows` contiene tutto
  ciò che parla con il sistema operativo, `busqueda` fa il punteggio,
  `tema` contiene i colori e i pezzi riusabili, `lista` disegna le
  righe, `dialogos` ha i popup, e `panel` si limita a coordinarli.
- Il salvataggio è atomico — le scritture vanno su un file temporaneo
  che poi viene spostato al suo posto, quindi un crash a metà scrittura
  non può più lasciare un JSON troncato.
- Disposizione più ariosa: righe più grandi, una vera scala tipografica,
  spaziatura su una griglia da 4px, e una tavolozza con toni intermedi
  invece di saltare dal riquadro al testo.

### Aggiunto
- 15 prove che coprono cartelle, fissati, ritaglio, modelli e punteggio
  di ricerca. Girano senza aprire una finestra, e il workflow di release
  le esegue prima di compilare.

## [1.2.0] - 2026-08-12

### Aggiunto
- La scorciatoia è configurabile, e adesso il valore predefinito è
  Ctrl+Shift+V, che la mano sinistra raggiunge senza lasciare il mouse
- Una sua icona, inclusa nell'eseguibile e mostrata nei dialoghi
- Log degli errori: un errore non gestito ora scrive in un file di log e
  lo dice, invece di far sparire la finestra senza traccia
- Illustrazioni nel README, disegnate in SVG così seguono il tema di
  GitHub

### Corretto
- Gli angoli arrotondati si applicano davvero: la regione veniva
  impostata sul widget interno di Tk invece che sulla finestra che
  Windows conosce

### Cambiato
- La build incorpora i metadati di versione, quindi il file ha un nome e
  un autore in Proprietà invece di comparire vuoto

## [1.1.0] - 2026-08-11

### Aggiunto
- Eliminare una cartella e tutto ciò che contiene, da un pulsante con
  etichetta o dal menu del clic destro sull'etichetta della cartella
- Rinominare le cartelle — i testi salvati si spostano con loro
- Modo selezione multipla: spunta più righe ed eliminale insieme
- Quattro dimensioni del pannello — mini, piccolo, medio, grande — dal
  dialogo dell'aspetto. Il trascinamento libero dei bordi fu provato e
  abbandonato: CustomTkinter non ridisegna in modo affidabile i suoi
  widget quando una finestra si ridimensiona decine di volte al secondo
- Rispetta i formati degli appunti di Windows che i gestori di password
  usano per marcare un contenuto come privato — quel contenuto non viene
  nemmeno letto
- Pulsante di pausa che ferma la cattura senza chiudere l'app

### Cambiato
- Gli appunti si leggono solo quando cambia il numero di sequenza di
  Windows, invece che ogni 900 ms comunque
- L'indice di ricerca tiene in cache il testo normalizzato per voce;
  ricostruirlo è sceso da ~27 ms a ~0,02 ms
- Il cursore di ridimensionamento si aggiorna solo quando la zona del
  bordo cambia davvero
- Hover e selezione ricolorano due righe invece di ridisegnare l'intera
  tela — con una finestra grande solo questo consumava quasi un core

## [1.0.0] - 2026-08-11

### Aggiunto
- Cronologia automatica degli appunti con fissati, testo e immagini
- Testi salvati organizzati in cartelle
- Ricerca con punteggio nelle due schede, insensibile agli accenti,
  parole in qualsiasi ordine
- Modelli da riempire con segnaposto `[[campo]]`
- Incolla con formato in Word e Outlook; testo semplice con Ctrl+Invio
- Importazione in blocco: una nota per linea, o tutto in una sola nota
- Sei colori di accento; segue il tema chiaro/scuro di Windows
- Si avvia con Windows
