# Specifiche dell'interfaccia — pastepad

Documento di riferimento per implementare l'interfaccia sulle maquette
di `docs/mockups/`. I valori della tavolozza devono coincidere con
`csharp/Pastepad.App/Stile.cs`.

**La tecnologia non è più Flet.** Questo documento fu scritto per la v3;
la riscrittura è in C# con WinUI 3 (vedi `PIANO.md`). I numeri valgono
ancora tutti; ciò che cambia è dove si scrivono. Dove il testo cita
`estilo.py`, `app.py` o `config.py` parla dei file della versione in
Python, che vive nel tag `ultima-version-python`: `estilo.py` era la
fonte dei dati della tavolozza, e la sua traduzione è `Stile.cs`.

**Cosa mette il sistema e cosa mettiamo noi.** WinUI 3 porta Mica, il
cambio di tema chiaro/scuro dal vivo, `Segoe UI Variable`,
`Segoe Fluent Icons` e gli angoli arrotondati della finestra. Tutto
questo si usa così com'è. Questo documento comanda solo dove dice
apposta qualcosa di diverso: la tavolozza degli accenti, le misure di
riga e la barra bianca della riga attiva.

---

## 1. Mappa delle maquette

| File | Cosa mostra | Stato del codice |
|---|---|---|
| `01_pannello_recenti.svg` | Pannello completo, scheda Recenti, una riga fissata e attiva | Implementato |
| `02_pannello_salvati.svg` | Pannello completo, scheda Salvati, cartella a discesa | Implementato |
| `03_pannello_seleziona.svg` | Modo selezione multipla, caselle e piede con Elimina (n) | Implementato |
| `04_dialogo_testo_nuovo.svg` | Dialogo per creare/modificare un testo salvato | Implementato |
| `05_dialogo_cartella.svg` | Dialogo di una sola linea (nome della cartella) | Implementato |
| `06_dialogo_campi.svg` | Dialogo di modello con `[[campi]]` | Implementato |
| `07_dialogo_elenco.svg` | Dialogo di importazione in blocco | Implementato |
| `08_dialogo_aspetto.svg` | Colore, dimensione, stile delle cartelle, scorciatoia | Implementato |
| `09_dialogo_conferma.svg` | Conferma di eliminazione | Implementato |
| `10_stato_vuoto_recenti.svg` | "Copia qualcosa e apparirà qui" | Implementato |
| `11_stato_vuoto_salvati.svg` | "Vuoto. Usa Nuovo per salvare un testo" | Implementato |
| `12_ricerca_senza_risultati.svg` | "Niente corrisponde a questa ricerca" | Implementato |
| `13_riga_link.svg` | Icona di link + dominio al posto del contatore | Implementato |
| `14_menu_tre_punti.svg` | Menu contestuale di una riga | Implementato |
| `15_menu_cartelle.svg` | Menu a discesa delle cartelle | Implementato |
| `16_dimensione_mini.svg` | Pannello a 300×380 | Implementato |
| `17_dimensione_grande.svg` | Pannello a 470×700 | Implementato |
| `18_tema_chiaro.svg` | Tavolozza chiara completa | Implementato |
| `19_pausa_attiva.svg` | Intestazione con cattura in pausa | Implementato |
| `20_allungando_finestra.svg` | **Speculativo** — non verificato in esecuzione | Non verificato |

**La 20 è diversa dalle altre 19.** Le prime diciannove descrivono stati
che il codice definisce già in modo esplicito (testi, colori,
condizioni). La 20 disegna un'intenzione di come dovrebbe vedersi il
ridimensionamento nativo, ma nessuno l'ha ancora vista girare su
Windows. Trattala come un'ipotesi da validare, non come una specifica.

---

## 2. Tavolozza dei colori

Fonte di verità: `Stile.cs`. Storicamente i valori venivano da
`pastepad/estilo.py`, dizionari `OSCURA` e `CLARA`, **tranne `medio` e
`tenue`**, che furono corretti per il contrasto — vedi la sezione 6. La
versione in Python aveva anche una copia vecchia in `config.py` con
valori diversi: era peso morto.

### Scura (predefinita)

| Token | Valore | Uso |
|---|---|---|
| `sfondo` | `#0B0B0D` | Sfondo del pannello |
| `elevato` | `#141417` | Barra di ricerca, dialoghi |
| `riquadro` | `#1B1B1F` | Righe non attive, pulsanti secondari |
| `hover` | `#26262B` | Riga sotto il cursore |
| `bordo` | `#242429` | Bordo di 1px del pannello |
| `testo` | `#F4F4F5` | Testo principale |
| `medio` | `#9C9CA6` | Icone, testo secondario |
| `tenue` | `#86868E` | Sottotitoli, testo disabilitato |

### Chiara

| Token | Valore |
|---|---|
| `sfondo` | `#F6F6F4` |
| `elevato` / `riquadro` | `#FFFFFF` |
| `hover` | `#EFEFEC` |
| `bordo` | `#E6E6E2` |
| `testo` | `#141416` |
| `medio` | `#5C5C66` |
| `tenue` | `#707079` |

In tema chiaro, i riquadri non attivi hanno un bordo di 1px invece di
uno sfondo differenziato, e un'ombra leggera (`#00000012` spostata 2px
in basso) per staccarsi dallo sfondo senza dipendere solo dal colore.
Vedi `18_tema_chiaro.svg`.

### Gli altri dieci sfondi

Oltre a Scura e Chiara, `Stile.cs` definisce altre dieci tavolozze. Con
"auto" sono dodici opzioni nel selettore di Aspetto.

**Quattro scure:** `mezzanotte` (`#0A0F1E`), `grafite` (`#141414`),
`bosco` (`#0A140F`) e la stessa `scuro`.
**Sette chiare:** `carta` (`#F5F1E8`), `nebbia` (`#EEF1F5`), `sabbia`
(`#F7F4EF`), `lilla` (`#F4F1FA`), `salvia` (`#EEF7F3`), `rossore`
(`#FBF2F4`) e la stessa `chiaro`.

Gli otto token di ciascuna stanno in `Stile.cs`. **Scura e Chiara le
mette il sistema** (`ElementTheme.Default` segue Windows e cambia dal
vivo); le altre dieci le mette l'utente apposta e comandano sul sistema.

### Accenti (selezionabili da Aspetto)

Sono **diciotto**, non sei. I sei della tabella vecchia erano quelli che
disegna la maquette 08.

| Nome | Colore | Testo sopra | Contrasto |
|---|---|---|---|
| `menta` (predefinito) | `#2DD4A7` | `#052E23` | 7.80 |
| `blu` | `#4B8DF8` | `#04183C` | 5.38 |
| `viola` | `#9B7BF7` | `#1E1046` | 5.40 |
| `ambra` | `#F5A524` | `#3A2606` | 7.05 |
| `corallo` | `#F76B5C` | `#3D0F09` | 5.68 |
| `rosa` | `#F472B6` | `#3A1128` | 6.14 |
| `ciano` | `#22D3EE` | `#062E36` | 8.00 |
| `lime` | `#A3E635` | `#1A2E05` | 9.69 |
| `indaco` | `#818CF8` | `#111539` | 5.92 |
| `turchese` | `#2DD4BF` | `#042F2A` | 7.81 |
| `pesca` | `#FB923C` | `#3B1D06` | 6.80 |
| `lavanda` | `#C084FC` | `#2E1065` | 5.77 |
| `smeraldo` | `#34D399` | `#04291C` | 8.16 |
| `cielo` | `#38BDF8` | `#052F42` | 6.57 |
| `oro` | `#FCD34D` | `#3B2606` | 9.94 |
| `fragola` | `#FB7185` | `#3F0A16` | 6.16 |
| `menta_fredda` | `#5EEAD4` | `#032F2A` | 9.83 |
| `sabbia` | `#D6BC8A` | `#332612` | 8.00 |

Il colore "testo sopra" cambia secondo l'accento perché alcuni sono
chiari e altri scuri — mai usare il bianco fisso su ambra o rosa, il
contrasto fallisce. La colonna del contrasto è **calcolata** con la
formula di WCAG 2.1, non stimata: i diciotto passano AA per il testo
normale (4.5:1), e il peggiore è `blu` con 5.38.

Tre coppie cambiarono rispetto alla tabella vecchia di questo documento:
`blu`, `viola` e `corallo` avevano `#FFFFFF` sopra e restavano a 3:1.

Rosso di pericolo (fisso, non cambia con il tema): `#DC2626`, hover
`#B91C1C` — riservato al pulsante "Elimina" e agli stati di errore.

---

## 3. Misure

### Dimensione della finestra

**Il pannello è adattabile e non ha preimpostazioni.** Si allarga
trascinandone i bordi, ricorda la dimensione tra una sessione e l'altra,
e tutto ciò che c'è dentro —righe, barra di ricerca, schede, cartelle e
piede— si adatta con lui. Il selettore di dimensione che disegnava la
maquette 08 fu tolto: delle preimpostazioni fisse accanto a qualcosa che
già si adatta sono di troppo e confondono.

| | |
|---|---|
| Dimensione iniziale | 380 × 560 |
| Minimo | 300 × 340 |
| Massimo | 720 × 1100 |
| Soglia di riga compatta | sotto i **340** di larghezza |

Le maquette **16 e 17** non descrivono più un'opzione: sono due esempi
di come deve comportarsi il pannello adattabile a 300 e a 470 di
larghezza. A 300 la riga scende a 42px e perde il sottotitolo;
"Seleziona" sparisce dal piede e "Nuovo" resta con il segno più.

La dimensione si misura su **ciò che si vede**, non sul rettangolo della
finestra: una finestra ridimensionabile ha intorno una cornice
invisibile —misurata, 7px per lato e 7 sotto— che `GetWindowRect` e
`AppWindow.Size` contano e lo schermo non mostra. Senza toglierla, un
pannello chiesto di 380×560 si vedeva di 366×553.

Le quattro preimpostazioni furono tolte dal nucleo. La dimensione
iniziale sono `Config.LarghezzaDef` e `Config.AltezzaDef`, due misure
sciolte accanto ai minimi e ai massimi, che invece restano come limiti
del ridimensionamento.

### Raggi e cornice della finestra

| Elemento | Raggio |
|---|---|
| Pannello completo | **8px, e lo mette Windows** (vedi sotto) |
| Dialoghi | 20px |
| Righe / riquadri | 14px |
| Pulsanti, campi di testo | 10–12px |
| Schede, tessere di cartella | 15–18px (capsula) |
| Pulsanti circolari con icona | cerchio completo (raggio = metà del lato) |

**Il pannello non disegna angoli né bordo propri.** Il `Border` radice
ha `CornerRadius="0"` e `BorderThickness="0"` apposta.

Avere due arrotondamenti —quello della finestra e il nostro— fu un
difetto reale e visibile: se il nostro è maggiore, la fascia tra le due
curve resta non dipinta e lascia vedere ciò che c'è dietro. Su sfondo
scuro si notava appena; in tema chiaro uscivano **due cunei scuri negli
angoli in alto**. Un solo arrotondamento, e lo mette il sistema. Il
bordo di 1px lo disegna Windows con la finestra, quindi il nostro lo
raddoppiava soltanto.

La finestra va con `SetBorderAndTitleBar(true, false)`,
`ExtendsContentIntoTitleBar` e `DWMWCP_ROUND` chiesto in modo esplicito.

**Raggio misurato sul render**, contando i pixel nell'angolo in alto a
sinistra al 100% di scala: 8, 5, 3, 1, 1 px di rientro nelle righe y =
1, 2, 4, 8 e 12. È un quarto di cerchio di **raggio 8px**. Scala con il
DPI: al 125% sono 10px.

**Non si può arrotondare di più, e non è una nostra decisione.**
`DWM_WINDOW_CORNER_PREFERENCE` ha solo quattro valori —`DEFAULT`,
`DONOTROUND`, `ROUND` e `ROUNDSMALL`— e **nessuno accetta un raggio in
pixel**; `ROUND` è il massimo che si può chiedere ed è quello che si
chiede. Fonte: la pagina dell'enumerazione su learn.microsoft.com.

Ottenere un raggio proprio richiederebbe una finestra senza cornice di
sistema e angoli dipinti da noi, e questo riporta le due cose già
sistemate: la fascia che il sistema riserva in alto per la barra del
titolo, e i cunei —perché l'area fuori dalla nostra curva continua a non
essere trasparente—. I 20px che chiedeva questo documento restano
**superati da ciò che il sistema permette**.

### Spaziatura

Griglia di 4px. I valori che si ripetono nel codice:
`4, 8, 12, 16, 24`. Non usare numeri sciolti come 15 o 18 per il
padding — arrotondare al multiplo di 4 più vicino.

### Tipografia

Carattere: `Segoe UI Variable Display` se disponibile (Windows 11),
altrimenti `Segoe UI` (Windows 10). Quattro dimensioni, niente di
intermedio:

| Token | Dimensione | Uso |
|---|---|---|
| `TTitolo` | 15px | Titoli di dialogo |
| `TCorpo` | 13px | Testo di riga, campi di input |
| `TMinore` | 12px | Pulsanti, schede |
| `TMini` | 11px | Sottotitoli, contatore di caratteri |

### Righe della lista

- Altezza normale: **56px**
- Altezza in modo mini: **42px**, una sola linea di testo (senza
  sottotitolo)
- Separazione tra le righe: 6px
- Padding interno: 16px a sinistra quando non c'è l'icona di tipo,
  22px quando c'è

---

## 4. Comportamento (non solo visivo)

Queste regole stanno nel codice e le maquette le danno per scontate; se
l'implementazione non le rispetta, non è un problema di stile ma di
logica.

**Barra di accento.** Solo la riga selezionata ha una barretta bianca
verticale di 3px a sinistra (vedi `01`, `13`, `17`). Non è solo
decorativa — è il segnale di focus che non dipende solo dal colore di
sfondo, per chi ha difficoltà a distinguere i colori.

**Icone per riga, solo al passaggio del cursore.** Il pulsante dei tre
punti (`···`) e la puntina di "fissa" non sono sempre visibili —
compaiono all'hover o quando la riga è attiva. Eccezione: se qualcosa è
già fissato, la puntina resta sempre visibile, per sapere cosa è fissato
senza passare il mouse su ogni riga.

**Riga di link.** Solo se l'intero testo copiato è una URL
(`Regole.EUnLink`) — non se il testo *contiene* una URL in mezzo a una
frase. Quando si applica: il dominio (`Regole.DominioDi`) al posto del
contatore di caratteri, e «Apri nel browser» nel menu della riga. **Il
clic incolla**, come in qualsiasi altra riga.

Fino alla 4.3.0 il clic apriva il browser. Fu tolto perché dallo stesso
posto passavano le quattro strade che portano a incollare —il clic,
l'Invio della barra di ricerca, e «Incolla» e «Incolla senza formato»
del menu—, quindi un link non si poteva incollare in nessun campo: la
voce che diceva «Incolla» apriva Chrome. Verificato da un capo all'altro
con il Blocco note.

**Salvati va in cinque gruppi richiudibili: segnalibri, modelli, email,
prompt di IA e note.** Nessuno è un altro tipo di dato: sono salvati che
si separano perché non si usano allo stesso modo, e mescolarli obbliga a
leggere l'intera lista per trovarne uno qualsiasi.

Icone per tipo, tutte di Segoe Fluent Icons e verificate nella lista
ufficiale: segnalibro `E71B` (Link), modello `E943` (Code), email `E715`
(Mail), prompt `EAB7` (ChatSparkle — la famiglia «sparkle» con cui
Windows segna le cose di IA). La nota non ha icona: è la maggioranza, e
una colonna di icone in tutte le righe sarebbe rumore.

**Il tipo lo sceglie l'utente** nella tendina del dialogo di
salvataggio, che condivide la linea con l'etichetta «Nome». pastepad
propone quello che si deduce dal testo —una URL intera è un segnalibro,
dei `[[campi]]` un modello, il resto nota— e smette di proporre appena
l'utente lo tocca. Ciò che è scelto si scrive in `snippets.json` solo se
contraddice il dedotto, quindi un file di prima della 4.4.0 si riscrive
senza una chiave in più.

**Un segnalibro salvato si apre al clic; tutto il resto si incolla.** Lo
decide il TIPO, non che il testo sembri un link: un indirizzo nella
cronologia si incolla —copiarlo e non poterlo mettere in nessun campo fu
l'errore della 4.3.0— e anche un salvato che l'utente cambia in Nota.
Copiare e incollare un segnalibro restano nel menu dei tre punti, e
«Incolla» di quel menu incolla sempre, anche in un segnalibro.

**Né email né prompt di IA si deducono mai.** Nel corpo di un'email non
c'è niente che lo separi da una nota, e proporlo perché contiene una
chiocciola trasformerebbe in email qualsiasi testo che menzioni un
indirizzo. Con un prompt è peggio: non c'è nemmeno una chiocciola a cui
aggrapparsi. Sono i due tipi che esistono proprio per essere scelti a
mano.

Un prompt con `[[campi]]` si propone come modello, e va bene così: ciò
che decide se chiede prima di incollare è il testo, non il tipo. Chi lo
vuole tra i suoi prompt lo cambia e continua a chiedergli i campi lo
stesso.

L'intestazione di gruppo misura 32px, ha chevron, icona e contatore, e
**compare solo quando c'è più di un gruppo con qualcosa dentro**: con
uno solo non c'è niente da separare.

**Cartelle: due modi intercambiabili.** `menu` (elenco a discesa, un
solo pulsante) o `tessere` (una capsula per cartella in fila
orizzontale). Si sceglie da Aspetto e si ricorda tra una sessione e
l'altra nella preferenza `"cartelle"`. L'elenco a discesa è il modo
predefinito perché con molte cartelle le tessere non stanno nella
larghezza del pannello.

Attenzione: la v3 in Python **aveva tolto il modo a tessere** perché non
si usava. Fu recuperato perché l'utente lo chiese espressamente, e
perché questa sezione l'ha sempre descritto.

**Lingua.** La versione originale aveva quattro lingue (`es`, `en`,
`pt`, `fr`) con un selettore in Aspetto. Questa copia è solo in
italiano: tabelle di traduzione e selettore sono stati tolti.

**Come si raggruppa Aspetto.** Tre blocchi, nell'ordine in cui si
toccano, nel formato delle Impostazioni di Windows 11: un riquadro per
impostazione, **dicitura sopra e controllo sotto**, a tutte le
larghezze.

1. **Colore di accento** — i 18 pallini, e sotto lo sfondo.
2. **Cartelle** — elenco a discesa o tessere.
3. **Sistema** — scorciatoia globale e avviso delle nuove versioni.

Senza sezione di dimensione.

**Una sola disposizione, e perché non ce ne sono due.** Ci fu un
tentativo di mettere la dicitura a sinistra e il controllo a destra
quando il pannello era largo, e fu ritirato dopo averlo misurato a sette
larghezze. La riga a due colonne non entra senza rompere qualcosa fino a
un pannello di circa **455 px**, molto sopra i 380 di fabbrica:

- sotto, la dicitura si spezza su due e tre linee con il pannello a 366;
- e se si dà priorità alla dicitura, a perdere è la tendina, che **non
  mette i puntini di sospensione: taglia da sinistra**. Misurato:
  "l + Shift + V" invece di "Ctrl + Shift + V", che è perdere il dato
  senza avvisare.

Impilare sempre evita le due cose in tutto l'intervallo 300–720 e toglie
un'intera classe di errori che dipendono dalla larghezza.

**Margini del dialogo.** Il contenuto è centrato per costruzione: la
larghezza del dialogo si fissa a quella del pannello meno 16, e il
riempimento è lo stesso ai due lati. Misurato sul render:

| Larghezza del pannello | Dialogo | Margine sin. | Margine des. |
|---|---|---|---|
| 300 | 280 | 49 | 50 |
| 380 | 360 | 49 | 50 |
| 720 | 700 | 49 | 49 |

Costante e simmetrico. Nel caso più stretto, 300, al riquadro restano
**149 px all'interno**; le misure furono prese con i testi originali, e
con quelli in italiano vanno ricontrollate.

**Pausa.** Il pulsante cambia icona (pausa ↔ play) e colore (grigio ↔
rosso `#EF4444`) secondo lo stato. Compare inoltre un testo "In pausa"
in rosso a sinistra dell'intestazione quando è attiva. Vedi `19`.

---

## 5. Cosa confrontare per primo

Se devi rivedere l'implementazione sulle maquette, quest'ordine dà più
segnale con meno sforzo:

1. **Colori esatti** — apri `Stile.cs` accanto a una qualsiasi maquette
   e confronta i valori esadecimali uno a uno. È meccanico e trova
   errori in fretta.
2. **`13`, `14`, `15`** — sono quelle che mostrano comportamento
   (rilevamento del link, menu contestuale, menu delle cartelle), non
   solo struttura. Lì è più probabile che il codice reale diverga
   dall'intenzione.
3. **`10`, `11`, `12`** — conferma che i tre messaggi di stato vuoto
   stanno esattamente dove li mette `Pannello.Aggiorna()`, non
   inventati.
4. **`20` alla fine, con scetticismo** — è l'unica maquette non
   verificata in esecuzione reale. Se il ridimensionamento si vede
   diverso, fidati di ciò che Windows fa davvero, non del disegno.

---

## 6. Ciò che questo documento non copre

- Animazioni e transizioni (durata, curva) — non c'è una maquette
  statica che le catturi.
- Comportamento della scorciatoia globale — vedi `CLAUDE.md`.
- Navigazione da tastiera in modo sistematico.

### Il contrasto, verificato e corretto

Il contrasto fu calcolato con la formula di WCAG 2.1 per l'intera
tavolozza, non guardato. Uscirono quattro cose.

**1. I 18 accenti passano** AA per il testo normale. Il peggiore è `blu`
con 5.38:1. La tabella sta nella sezione 2.

**2. `tenue` non arrivava in nessuno dei 12 sfondi, ed è stato alzato.**

`tenue` è il colore del sottotitolo di ogni riga —"29 caratteri", il
nome della cartella— e del messaggio degli stati vuoti: testo che
informa, non ornamento. Dava tra 2.64:1 e 3.31:1 quando AA chiede 4.5:1.

Ogni sfondo ha il suo valore: la correzione che sistema `salvia` non
sistema `lilla`. Fu ottenuto mescolando `tenue` verso il `testo` della
propria tavolozza —il minimo per arrivare a 4.5:1 e non un passo di più,
per spostare l'aspetto il meno possibile— e misurando contro il peggiore
dei suoi due sfondi (`sfondo` e `riquadro`; nella scura, anche contro il
grigio di Mica, `#202020`, che è ciò che si vede davvero dietro il
pannello).

| Sfondo | `tenue` prima | rapporto | `tenue` ora | rapporto |
|---|---|---|---|---|
| scura | `#6B6B75` | 3.09 | `#86868E` | **4.51** |
| chiara | `#8E8E98` | 3.00 | `#707079` | **4.53** |
| mezzanotte | `#64708C` | 3.22 | `#7E89A1` | **4.54** |
| grafite | `#6E6E6E` | 3.04 | `#8B8B8B` | **4.56** |
| bosco | `#5E7A6B` | 3.31 | `#779082` | **4.51** |
| carta | `#96897A` | 3.03 | `#776C60` | **4.55** |
| nebbia | `#8A95A6` | 2.67 | `#636E80` | **4.55** |
| sabbia | `#948B7E` | 3.06 | `#776F65` | **4.51** |
| lilla | `#9086AE` | 3.03 | `#73698E` | **4.54** |
| salvia | `#849E90` | 2.64 | `#5D766A` | **4.51** |
| rossore | `#A5858E` | 3.01 | `#866871` | **4.52** |

Verificato anche sul render, non solo sul codice: il pixel del
sottotitolo misura `#86868E` in scuro e `#707079` in chiaro.

**3. Alzando `tenue` si perdeva il gradino con `medio`, ed è stato
recuperato alzando `medio` in sei tavolozze.**

Con `tenue` a 4.5:1, in quattro sfondi chiari restava attaccato a
`medio` —fino a 1.07:1 in `salvia`— e il sottotitolo smetteva di
leggersi come secondario: si scambiava un problema con un altro. Siccome
`tenue` non può più scendere senza rompere AA, il gradino si recupera
solo alzando `medio`. Fu fissato a 1.30:1, che è quello che avevano già
le tavolozze non rovinate.

| Sfondo | `medio` prima | `medio` ora | gradino prima | gradino ora |
|---|---|---|---|---|
| carta | `#6B6155` | `#645B4F` | 1.18 | **1.30** |
| nebbia | `#5A6779` | `#505C6E` | 1.12 | **1.31** |
| sabbia | `#6E675C` | `#645D53` | 1.13 | **1.31** |
| lilla | `#655885` | `#635682` | 1.26 | **1.30** |
| salvia | `#557264` | `#496458` | 1.07 | **1.31** |
| rossore | `#77565F` | `#76555E` | 1.29 | **1.31** |

Le altre cinque non si toccano. In `lilla` e `rossore` la correzione è
del 4% e dell'1%: esiste perché la regola valga in tutte e undici, non
perché si veda. I sei `medio` nuovi restano inoltre sopra 5.9:1, quindi
superano con margine anche la soglia di AA.

**Questi valori comandano su quelli ereditati dalla versione in
Python.** È deliberato: leggibilità prima dell'aspetto ereditato.

**4. La barra bianca di focus di 3px dà 1.44:1 su `oro` e 1.89:1 su
`menta`**, sotto il 3:1 che chiede WCAG 1.4.11 per un elemento grafico.
Resta bianca perché così dicono le maquette e perché il suo compito è
essere un segnale *di forma*, non di colore, su una riga già
differenziata dallo sfondo di accento. Detto con il numero davanti
perché si decida consapevolmente.
