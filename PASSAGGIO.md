# Passaggio di consegne — riscrittura di pastepad

Documento per avviare la riscrittura senza ripetere ciò che è già stato
imparato. Leggilo prima di scrivere una riga.

Racconta il passaggio dalla versione in Python a quella in C#, scritto
dall'autore originale. I nomi di file e funzioni della versione in
Python sono rimasti in spagnolo, perché quel codice vive così nel tag
`ultima-version-python`.

---

## Che cos'è e cosa deve fare

Un gestore di appunti per Windows. Sostituisce `Win+V`, che conserva
solo 25 voci e le perde al riavvio.

**Requisiti, in ordine di importanza:**

1. **La scorciatoia globale deve funzionare sempre.** Senza questo,
   nient'altro conta. È il requisito che la versione in Python non ha
   mai rispettato.
2. **Aprire il pannello deve essere istantaneo.** È uno strumento di
   produttività: se ci mette tanto, non si usa.
3. **Poco consumo.** Deve poter restare residente tutto il giorno su una
   macchina da 4 GB senza farsi notare.
4. Aspetto pulito e minimalista, al livello di `Win+V`.

---

## Perché si abbandona la versione in Python + Flet

**Misurato, non supposto:**

| | |
|---|---|
| Memoria totale | 207 MB (2 processi) |
| — motore di Flutter | 177 MB, intoccabile |
| — tutta la nostra interfaccia | 26 MB |
| Aprire il pannello (residente) | 12–184 ms |
| Avvio a freddo | 1–2 s |
| Processi | 2 (python.exe + flet.exe) |
| Thread | 13 + 55 |

Un'app Flet **vuota** sono già 177 MB. Togliere ombre, animazioni o temi
non scende sotto: è il motore che si avvia, non il design.

**E l'errore che non fu risolto:** la scorciatoia globale risponde due o
tre volte e poi smette, senza eccezione e senza traccia.

Furono tentate due implementazioni:

1. **Libreria `keyboard`** (hook `WH_KEYBOARD_LL`). Windows sgancia
   l'hook in silenzio se la callback ci mette più di
   `LowLevelHooksTimeout` (300 ms). Scartata.
2. **`RegisterHotKey`** della API di Windows, in un thread proprio con
   il suo ciclo dei messaggi. Provata in isolamento con 45 pressioni
   sintetiche: 45 ricevute, 0 perse. **Ma dentro l'applicazione continua
   a morire.**

**Il sospetto rimasto senza conferma:** il problema non è la
registrazione della scorciatoia ma ciò che succede dopo.
`page.run_thread()` di Flet non crea un thread, consegna il lavoro a un
`ThreadPoolExecutor` condiviso. L'app gli dà due cicli `while True` che
non ritornano mai (`_vigilar` e `_atender_cola`), occupando due
lavoratori per sempre. Se quel pool si esaurisce o il thread si blocca
in un `page.update()` su una finestra nascosta, le pressioni si
accodano e nessuno le serve.

**In una riscrittura questo sparisce da solo** se la scorciatoia e la
finestra vivono nello stesso thread dell'interfaccia, che è come
funziona un'applicazione nativa di Windows.

---

## Il linguaggio: C# con WinUI 3

**Deciso dopo averlo valutato, non per abitudine.** Microsoft lo indica
come la strada preferita per le applicazioni nuove e native di Windows;
WPF resta per le basi di codice già esistenti.

| Perché | |
|---|---|
| **Aspetto** | Mica e Acrilico nativi. Sono i materiali di Fluent Design che usa `Win+V`: non bisogna imitarli, ci sono già |
| **Scalatura DPI** | Automatica e per monitor. WPF, Win32 e WinForms **non** lo fanno: senza lavoro in più escono sfocati |
| **Tipografia e icone** | `Segoe UI Variable` e `Segoe Fluent Icons`, quelli di Windows 11, di serie |
| **Tema chiaro/scuro** | Cambia da solo, senza codice |
| **Memoria** | 15–20% sotto WPF in app equivalenti. Contro i 207 MB di Flet, tra 40 e 60 |
| **Scorciatoia globale** | `RegisterHotKey` su un `HWND`. Senza thread, senza code, senza pool — è lì che si perse la versione precedente |
| **Appunti** | `AddClipboardFormatListener` avvisa **con un evento**: finisce il sondaggio ogni 0,7 s |
| **Native AOT** | La sua compatibilità è più avanti di quella di WPF, che si porta dietro la reflection nel binding |
| **Firma** | Il certificato gratuito di SignPath si applica allo stesso modo |

**Il rischio, detto chiaramente:** WinUI 3 ha un ecosistema meno maturo
di WPF —meno controlli di terze parti, meno risposte per i casi rari—.

Si accetta per un motivo preciso: **la parte che può rompersi (la
scorciatoia e gli appunti) è Win32 puro ed è identica nei due.**
L'immaturità di WinUI non tocca ciò che è rischioso; riguarda solo la
parte visiva, che è proprio dove WinUI vince.

**Alternative valutate e scartate:**

- **WPF** — più maturo, ma l'aspetto di Windows 11 va costruito a mano e
  il DPI richiede lavoro in più. È la rete di sicurezza se WinUI 3 desse
  fastidio.
- **C++/Win32** — il più leggero (~15 MB) e massimo controllo, ma la
  finitura si disegna a mano e sono mesi.
- **Rust/Tauri o Go/Wails** — l'interfaccia è web: tornano il peso
  (~80 MB) e l'avvio lento appena lasciati alle spalle.
- **Avalonia o MAUI** — multipiattaforma che non serve, e non hanno
  l'aspetto di Windows 11.
- **CustomTkinter** — scenderebbe a ~60 MB, ma si deforma
  ridimensionando e i bordi escono ruvidi. Fu già abbandonato una volta
  per questo.

---

## Ciò che FUNZIONA e va conservato

Non partire da zero. Queste decisioni sono provate ed è costato
trovarle:

**Formati privati degli appunti.** Windows ne definisce quattro con cui
un programma dice «non salvare questo»: `Clipboard Viewer Ignore`,
`ExcludeClipboardContentFromMonitorProcessing`,
`CanIncludeInClipboardHistory`, `CanUploadToCloudClipboard`. Li usano
KeePass, Bitwarden, Gestione credenziali e l'incognito di Chrome. Oggi
in `Sistema/Appunti.cs`.

**Restituire il focus prima di incollare.** All'apertura del pannello,
il campo in cui stava il cursore lo perde, e un `Ctrl+V` da solo finisce
nel vuoto. Bisogna salvare l'`hwnd` della finestra attiva nell'istante
della scorciatoia e restituirglielo con `AttachThreadInput` prima di
incollare — è il trucco che Windows esige perché `SetForegroundWindow`
funzioni da un altro processo. Oggi in `Sistema/FocusFinestra.cs`.

**Rilasciare Shift e Alt prima di incollare.** Se la scorciatoia li
contiene e restano premuti, la destinazione riceve `Ctrl+Shift+V`
invece di `Ctrl+V`.

**Leggere gli appunti solo quando cambiano.**
`GetClipboardSequenceNumber` sale a ogni copia e costa una chiamata;
aprire gli appunti costa moltissimo di più. In C# questo migliora:
`AddClipboardFormatListener` avvisa con un evento ed elimina il
sondaggio.

**Salvare in modo rimandato.** Scrivere l'intero JSON a ogni copia
costava 7,8 ms e più di un megabyte per `Ctrl+C`. Accumulare in memoria
e riversare ogni pochi secondi lo portò a 0,020 ms. Ciò che l'utente fa
apposta (fissare, eliminare, svuotare) va invece scritto subito.

**Riconoscere quando l'intero testo è una URL** —un paragrafo che la
menziona di passaggio non conta— per offrire «Apri nel browser».
Attenzione: fino alla 4.3.0 quel link *si apriva invece di incollarsi*,
e risultò essere un errore, non una comodità. Siccome da lì passavano
anche «Incolla» e «Incolla senza formato», un link copiato non si poteva
mettere in nessun campo. Aprire è un'azione del menu; il clic incolla,
come in tutto il resto.

**Installare in `%LOCALAPPDATA%`, non in Programmi.** Lì Windows blocca
la scrittura senza avvisare: il programma si avvierebbe ma non
salverebbe niente.

**Una sola istanza.** Due processi si contendono la scorciatoia globale
e quello che perde resta come una finestra muta. Fu il motivo per cui il
programma sembrò rotto per giorni.

---

## Logica riusabile

Questi moduli della versione in Python non importavano nessuna libreria
grafica. Erano 1.208 righe da tradurre, non da riprogettare:

```
pastepad/modelo.py      i dati e le loro regole, con 19 prove
pastepad/busqueda.py    punteggio: senza accenti, parole in qualsiasi
                        ordine, il titolo pesa più del corpo
pastepad/windows.py     appunti, focus, scorciatoia, avvio automatico
pastepad/idiomas.py     i testi in 4 lingue
pastepad/config.py      limiti e percorsi
```

**Le 19 prove di `prueba.py` erano la specifica eseguibile delle
regole.** Furono tradotte per prime, così la logica era verificata prima
di disegnare qualcosa.

---

## Formato dei dati

In `%LOCALAPPDATA%\pastepad\`. Questi sono i nomi di questa copia in
italiano; nella versione originale file e chiavi erano in spagnolo
(`historial.json`, `titulo`, `texto`...), e i due formati non si leggono
a vicenda.

```jsonc
// snippets.json
{
  "categorie": ["Lavoro", "Note"],
  "snippets": [
    { "titolo": "Modello di email",
      "categoria": "Lavoro",
      "runs": [ { "t": "Ciao [[nome]]", "f": "Calibri", "s": 11,
                  "b": 0, "i": 0, "u": 0, "c": "#000000" } ] }
  ]
}

// cronologia.json — lista, i fissati per primi
[ { "tipo": "testo", "testo": "...", "pin": true },
  { "tipo": "immagine", "percorso": "C:\\...\\immagini\\img_1699.bmp" } ]

// config.json
{ "tema": "auto", "accento": "menta",
  "scorciatoia": "ctrl+shift+v", "larghezza": 380, "altezza": 560,
  "in_pausa": false, "avvio_automatico": "si" }
```

Un `runs` è una lista di frammenti con formato; le chiavi sono di una
lettera perché il JSON non ingrassi.

---

## Design

È già definito e non va reinventato:

- **`docs/mockups/`** — 35 maquette SVG di ogni schermata e stato
- **`docs/SPECIFICHE-UI.md`** — tavolozza, misure, tipografia e
  comportamento, con i valori esatti

Attenzione: le maquette **20 e 33** non sono verificate
sull'applicazione, e la **26 e 27** sono dialoghi di Windows, non
nostri.

Tavolozza di base: sfondo `#0B0B0D`, riquadro `#1B1B1F`, accento
`#2DD4A7`. Righe di 56 px con 6 di separazione. Pannello di 380×560
come predefinito.

---

## Errori da non ripetere

1. **Nessun `catch` muto.** L'errore della scorciatoia richiese giorni
   di diagnosi perché ogni thread ingoiava la propria eccezione in
   silenzio. Tutto ciò che si cattura, si registra.
2. **Registrare gli errori di tutti i thread.** In Python,
   `sys.excepthook` copre solo il principale. Qualsiasi linguaggio ha il
   suo equivalente; bisogna agganciarli tutti.
3. **Misurare prima di decidere.** Il sospetto che il programma fosse
   lento risultò falso: si apriva in 12–184 ms. Lento era l'avvio a
   freddo, e rotto era avere due istanze in lotta.
4. **La versione esce dal tag, non scritta a mano.** Fu pubblicata una
   release con binari vecchi per averla inchiodata in dodici posti.
5. **Provare l'eseguibile, non solo il codice sorgente.** L'errore di
   `python311.dll` compariva solo separando l'`.exe` dalla sua cartella.

---

## Stato al momento del passaggio

- Ramo `main`, tag `v3.0.1` pubblicato con installer e versione
  portatile
- 19 prove in verde, senza codice morto né riferimenti rotti
- README in inglese e spagnolo, 35 maquette, specifiche dell'interfaccia
- **La scorciatoia globale continuava a non essere affidabile. È il
  motivo della riscrittura.**
