using Microsoft.UI.Xaml;
using Pastepad.App.Sistema;
using Pastepad.Nucleo;

namespace Pastepad.App;

/// <summary>
/// Coordina tutto: l'archivio, la cassetta di Windows, l'area di notifica
/// e il pannello. È l'unico posto dove queste quattro cose si conoscono.
/// </summary>
public partial class App : Application
{
    internal static App Attuale => (App)Current;

    internal Archivio Archivio { get; private set; } = null!;
    internal Indice Indice { get; private set; } = null!;

    Cassetta? _cassetta;
    AreaNotifica? _areaNotifica;
    Chiusura? _chiusura;
    Pannello? _pannello;
    Timer? _riversamento;
    Timer? _aggiornamenti;

    /// <summary>
    /// Quanto si aspetta prima del primo controllo di versione, e ogni
    /// quanto dopo.
    ///
    /// Il ritardo non è per prudenza: l'avvio è misurato in ~420 ms e non
    /// lo si tocca per una richiesta a internet che nessuno ha chiesto e
    /// per cui nessuno ha fretta.
    /// </summary>
    static readonly TimeSpan PrimoControllo = TimeSpan.FromSeconds(45);

    static readonly TimeSpan OgniGiorno = TimeSpan.FromHours(24);

    /// <summary>
    /// La finestra che aveva il focus quando è stata premuta la
    /// scorciatoia. Si salva in quell'istante e non dopo: appena il
    /// pannello compare, questa informazione è persa.
    /// </summary>
    nint _finestraPrecedente;

    /// <summary>
    /// L'ultima sequenza degli appunti scritta da noi. Senza, incollare
    /// rimetterebbe nella cronologia ciò che si è appena incollato.
    /// </summary>
    uint _sequenzaPropria;

    uint _ultimaSequenza;

    public App()
    {
        InitializeComponent();

        UnhandledException += (_, a) =>
        {
            Registro.Errore("Application.UnhandledException", a.Exception);

            // Non si segna come gestita: un errore nell'albero XAML che
            // non sappiamo trattare non va lasciato passare in silenzio.
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Archivio = new Archivio(
            Program.CartellaDati is { } cartella ? Percorsi.InCartella(cartella) : null,
            Registro.Errore,
            Registro.Annota);
        // Prima che scriva qualcosa. Se i file stanno finendo altrove,
        // ciò che leggiamo non è dell'utente e nemmeno ciò che
        // scrivessimo andrebbe dove lui crede: meglio una sessione
        // inutile che uno di quegli avvii in bianco che sembrano «si
        // è cancellato tutto».
        if (Pacchetto.PercorsoDeviato(Archivio.Percorsi.Cartella) is { } reale)
        {
            Registro.Annota(
                $"ATTENZIONE: è stata chiesta la cartella {Archivio.Percorsi.Cartella} e "
                + $"Windows la sta servendo da {reale}. I file che "
                + "si vedono NON sono quelli dell'utente. Sessione in sola lettura.");

            Archivio.Congela("pastepad si è aperto dentro un'altra applicazione e Windows "
              + "sta deviando i suoi file, quindi non vede i tuoi dati reali. "
              + "Per non danneggiarli, questa sessione non salverà nulla. Chiudilo e "
              + "riaprilo dal menu Start.");
        }

        Indice = new Indice(Archivio);

        _pannello = new Pannello();

        // Se i dati non si sono potuti leggere, l'utente deve
        // saperlo adesso e non quando scopre che manca mezza
        // cronologia. Il pannello si apre apposta: un avviso che
        // nessuno guarda non è un avviso.
        if (Archivio.Problema is not null)
        {
            Registro.Annota("AVVIO DEGRADATO: " + Archivio.Problema);
            _pannello.Avvisa(Archivio.Problema);
            _pannello.Affiora();
        }

        // La cassetta si crea nel thread dell'interfaccia, che è dove
        // RegisterHotKey accetta l'HWND e dove il ciclo di WinUI smista
        // i suoi messaggi.
        _cassetta = new Cassetta();
        _cassetta.Scorciatoia += AllaScorciatoia;
        _cassetta.Appunti += NegliAppunti;
        _cassetta.Mostrati += () => MostraPannello();
        _cassetta.Esci += Chiudi;

        string scorciatoia = Archivio.Pref("scorciatoia", Config.ScorciatoiaDef) ?? Config.ScorciatoiaDef;

        bool suo = _cassetta.MettiScorciatoia(scorciatoia);
        bool diFabbrica = !suo && _cassetta.MettiScorciatoia(Config.ScorciatoiaDef);

        if (diFabbrica)
            Registro.Annota($"'{scorciatoia}' non si è potuta impostare; si è usata quella di fabbrica");

        _areaNotifica = new AreaNotifica(_cassetta.Handle, "pastepad");

        // L'ascolto della chiusura va a parte dalla cassetta perché la
        // cassetta non può sentirla: vedi il commento di Chiusura.cs. E la
        // registrazione per farci riaprire va qui, non prima, perché la
        // riga del log esca dopo quella dell'avvio.
        _chiusura = new Chiusura();
        _chiusura.Riversa += () => Archivio.Riversa(forza: true);
        _chiusura.Termina += Chiudi;

        Chiusura.ChiediDiRiaprirci();

        ApplicaAvvioAutomatico();

        _ultimaSequenza = Appunti.Sequenza();

        // La cronologia si riversa ogni pochi secondi invece che a ogni
        // copia: scrivere l'intero JSON costava 7,8 ms e più di un mega
        // per ogni Ctrl+C.
        _riversamento = new Timer(
            _ => _pannello?.DispatcherQueue.TryEnqueue(() => Archivio.Riversa()),
            null, Archivio.IntervalloRiversamento, Archivio.IntervalloRiversamento);

        // Se a fallire è la SCORCIATOIA, l'avviso non può vivere solo nel
        // pannello: il pannello si apre con la scorciatoia. Chi ha il
        // problema è esattamente chi non può arrivare al messaggio che
        // glielo spiega, e da fuori si vede come «è avviato e non fa
        // niente».
        //
        // È successo su una macchina di lavoro, dove un altro programma
        // aveva già preso Ctrl+Q: il tasto finiva al programma davanti
        // —che è il segnale che pastepad non l'ha registrato— e non c'era
        // modo di accorgersene. Il fumetto dell'area di notifica invece
        // arriva, e l'icona stessa è la strada per aprire il pannello e
        // cambiarla.
        if (_cassetta.Problema is not null) _pannello.Avvisa(_cassetta.Problema);

        // **Il caso peggiore non è restare senza scorciatoia: è cambiarla
        // senza dirlo.** Quando la scorciatoia dell'utente è presa da un
        // altro programma, pastepad ricade su quella di fabbrica e finora
        // restava tranquillo — senza un Problema da mostrare, perché
        // quella di fabbrica era entrata—. L'utente continua a premere
        // la sua per sempre, non succede niente, e in più il tasto
        // finisce al programma che ha davanti, che è il segnale che
        // pastepad non l'ha ricevuto.
        //
        // Succede sulle macchine di lavoro, dove ci sono più programmi
        // che si contendono le combinazioni. E non si può avvisare solo
        // nel pannello: il pannello si apre con la scorciatoia.
        if (diFabbrica)
        {
            _areaNotifica?.Avvisa(
                "La tua scorciatoia era occupata",
                Testi.Componi("Un altro programma usa già %s, quindi pastepad sta "
                       + "usando %s. Fai clic su questa icona per aprire il "
                       + "pannello e sceglierne un'altra.",
                    Config.Leggibile(scorciatoia), Config.Leggibile(Config.ScorciatoiaDef)));
        }
        else if (!suo)
        {
            _areaNotifica?.Avvisa(
                "pastepad è rimasto senza scorciatoia",
                "Non è stato possibile registrare nessuna combinazione. "
                       + "Fai clic su questa icona per aprire il pannello e "
                       + "sceglierne un'altra.");
        }

        // Tutto ciò che tocca l'archivio passa dal thread dell'interfaccia,
        // che è da dove lo si tocca in tutti gli altri posti. L'unica cosa
        // che ne esce è la richiesta, che si aspetta con await.
        _aggiornamenti = new Timer(
            _ => _pannello?.DispatcherQueue.TryEnqueue(async () =>
                await ControllaAggiornamenti()),
            null, PrimoControllo, OgniGiorno);

        // L'ultima cosa dell'avvio, e solo se non abbiamo già dovuto
        // mostrare il pannello: se l'archivio era rotto è aperto davanti
        // all'utente, e spostarlo a -32000 sarebbe farlo sparire.
        if (!_pannello.EVisibile) _pannello.Riscalda();

        // Quanto ci mette a essere pronto, dalla prima riga di Main.
        // A caldo esce una cosa e a freddo un'altra molto diversa:
        // l'avvio a freddo vero sono 476 file che nessuno ha ancora
        // toccato, e si misura solo riavviando la macchina e leggendo
        // questa riga.
        Registro.Annota(string.Format(
            "pronto in {0:F0} ms",
            System.Diagnostics.Stopwatch
                .GetElapsedTime(Program.Avvio).TotalMilliseconds));
    }

    /// <summary>
    /// Avviarsi con Windows, secondo la preferenza dell'utente. La
    /// decide l'applicazione a ogni avvio e nessun altro: l'installer
    /// non scrive più in HKCU\...\Run.
    ///
    /// Si riscrive sempre, che esista già o no. È il modo di correggere
    /// un percorso vecchio, e con un installer di mezzo succede davvero:
    /// chi aveva la versione precedente in %LOCALAPPDATA%\pastepad e
    /// mette la nuova in \Programs\pastepad resta con la voce che punta
    /// a un eseguibile che non c'è più. La versione precedente aveva lo
    /// stesso problema e si risolveva a mano — "se sposti la cartella,
    /// aprilo una volta dal posto nuovo".
    /// </summary>
    void ApplicaAvvioAutomatico()
    {
        // --dati esiste per provare senza toccare le cose dell'utente, e
        // fino alla 4.2.0 non lo rispettava: isolava i dati ma riscriveva
        // lo stesso la voce di registro, lasciando l'avvio automatico
        // puntato a una build di prova che domani non c'è più. È successo
        // due volte nella stessa sessione.
        //
        // Il registro è dell'installazione vera. Una sessione con
        // --dati lì non ha niente da dire.
        if (Program.CartellaDati is not null)
        {
            Registro.Annota("sessione con --dati: l'avvio automatico non si tocca");
            return;
        }

        string preferenza =
            Archivio.Pref(AvvioAutomatico.Chiave, AvvioAutomatico.ComePredefinito)
            ?? AvvioAutomatico.ComePredefinito;

        bool vuole = AvvioAutomatico.Vuole(preferenza);

        // L'attività pianificata, in un thread in sottofondo e senza
        // aspettarla: lancia schtasks, che costa più di tutto l'avvio
        // messo insieme. Niente di ciò che segue dipende da lei.
        _ = Task.Run(() => Avvio.AssicuraAttivitaPianificata(vuole));

        // Il valore che c'era, per poter dire da cosa a cosa. Si legge e
        // si rilegge dopo aver scritto invece di comporre il percorso a
        // mano: così i due lati si confrontano nello stesso formato, con
        // le virgolette che Windows salva.
        string? prima = Avvio.ValoreAttuale();

        if (vuole)
        {
            Avvio.Metti(true);

            string adesso = Avvio.ValoreAttuale()
                           ?? Environment.ProcessPath
                           ?? "(percorso sconosciuto)";

            // Riscrivere la voce di un altro programma non può passare
            // senza lasciare traccia: chi la guarda dopo ha diritto di
            // sapere cosa c'era prima e quando è cambiato.
            Registro.Annota(
                prima is null
                    ? $"avvio con Windows: registrato in {adesso}"
                    : prima == adesso
                        ? $"avvio con Windows: era già impostato in {adesso}"
                        : $"avvio con Windows: riscritto da {prima} a {adesso}");

            return;
        }

        if (prima is not null)
        {
            Avvio.Metti(false);

            // Con il valore che aveva: se l'utente cambia idea, lì c'è
            // ciò che andava rimesso.
            Registro.Annota(
                $"avvio con Windows: tolto ({AvvioAutomatico.Chiave}"
                + $"='{preferenza}'); puntava a {prima}");

            return;
        }

        Registro.Annota(
            $"avvio con Windows: non si registra ({AvvioAutomatico.Chiave}"
            + $"='{preferenza}')");
    }

    // ------------------------------------------- versione nuova

    /// <summary>
    /// Guarda se c'è una versione nuova e, se c'è e non si è già
    /// avvisato di quella, la mostra nella fascia del pannello.
    ///
    /// Non lancia mai: è ciò che si chiede a qualcosa che gira da solo,
    /// in sottofondo, e che l'utente non ha chiesto. Un errore qui non
    /// può far cadere l'applicazione né comparire a schermo; si annota e
    /// si riprova domani.
    /// </summary>
    async Task ControllaAggiornamenti()
    {
        try
        {
            if (!Archivio.Pref(Versioni.ChiaveAvvisa, Versioni.AvvisaDef))
                return;

            var adesso = DateTimeOffset.Now;
            string? ultima = Archivio.Pref<string>(Versioni.ChiaveControllo);

            if (!Versioni.ToccaControllare(ultima, adesso)) return;

            var pubblicata = await Aggiornamento.Consulta();

            // La data si salva solo se la richiesta è andata bene. Se è
            // fallita, che si riprovi e non si perda un giorno.
            if (pubblicata is null) return;

            Archivio.MettiPref(Versioni.ChiaveControllo, Versioni.Oggi(adesso));

            string? avvisata = Archivio.Pref<string>(Versioni.ChiaveAvvisata);

            if (!Versioni.ToccaAvvisare(Config.Version, pubblicata.Version, avvisata))
            {
                // Si annota anche ciò che va bene: il giorno in cui l'avviso
                // smette di funzionare, l'errore è che nessuno si accorge
                // che nessuno se ne accorge.
                Registro.Annota(
                    $"aggiornamenti: installata {Config.Version}, pubblicata "
                    + $"{pubblicata.Version}; niente da segnalare");

                return;
            }

            Archivio.MettiPref(Versioni.ChiaveAvvisata, pubblicata.Version);

            Registro.Annota(
                $"aggiornamenti: c'è la {pubblicata.Version} e si ha la "
                + $"{Config.Version}; si avvisa");

            _pannello?.AvvisaNovita(pubblicata.Version, pubblicata.Pagina);
        }
        catch (Exception e)
        {
            Registro.Errore("controllare se c'è una versione nuova", e);
        }
    }

    // ---------------------------------------------------- la scorciatoia

    void AllaScorciatoia()
    {
        // Prima di tutto, prima di toccare qualsiasi finestra.
        _finestraPrecedente = FocusFinestra.FinestraAttiva();

        if (_pannello is null) return;

        if (_pannello.EVisibile)
        {
            _pannello.Nascondi();
            return;
        }

        // Il percorso intero, in tre pezzetti, perché finora si misurava
        // solo quello di mezzo —«pannello in 25 ms»— e l'utente diceva
        // che ci metteva tanto. I due che mancavano sono proprio dove può
        // stare il ritardo:
        //
        //   coda      quanto la pressione ha aspettato che la raccogliessimo.
        //             Se il processo è fermo da dieci minuti e Windows l'ha
        //             tolto dalla memoria o l'ha rallentato perché è in
        //             secondo piano, il ritardo compare qui e in nessun
        //             altro posto.
        //   comparsa  il solito: chiedere la finestra e darle il focus.
        //   disegno   da quando Affiora torna a quando ci sono pixel. Show()
        //             non aspetta il primo fotogramma.
        //
        // E accanto, da quanto il programma non faceva niente. Senza quel
        // dato le cifre non si possono raggruppare in «appena usato» e
        // «dieci minuti fermo», che è il confronto che serve.
        int coda = _cassetta?.AttesaDellaScorciatoia ?? 0;
        var fermo = System.Diagnostics.Stopwatch.GetElapsedTime(_ultimaAttivita);
        var senzaAprire = System.Diagnostics.Stopwatch.GetElapsedTime(_ultimaApertura);

        long segno = System.Diagnostics.Stopwatch.GetTimestamp();

        MostraPannello();

        double affiora = System.Diagnostics.Stopwatch
            .GetElapsedTime(segno).TotalMilliseconds;

        _pannello.AlPrimoFotogramma(() =>
        {
            Registro.Annota(string.Format(
                "pannello: coda {0} ms + comparsa {1:F1} ms + disegno {2:F1} ms "
                + "= {3:F1} ms ({4:F1} min senza aprirlo, {5:F1} min senza niente)",
                coda,
                affiora,
                System.Diagnostics.Stopwatch.GetElapsedTime(segno).TotalMilliseconds
                    - affiora,
                coda + System.Diagnostics.Stopwatch
                    .GetElapsedTime(segno).TotalMilliseconds,

                // Il primo è quello che predice se ci metterà tanto; il
                // secondo resta perché distingue «freddo» da «in più addormentato».
                senzaAprire.TotalMinutes,
                fermo.TotalMinutes));

            // Solo alla prima apertura del processo, che è l'unica in
            // cui si è vista l'intestazione non disegnata.
            if (_pannello?.Diagnostica() is { } dettaglio) Registro.Annota(dettaglio);
        });

        Attivita();
        _ultimaApertura = System.Diagnostics.Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Quando il programma ha fatto qualcosa l'ultima volta. Lo muovono
    /// la scorciatoia e ogni copia: sono le due cose che lo svegliano.
    /// </summary>
    long _ultimaAttivita = System.Diagnostics.Stopwatch.GetTimestamp();

    /// <summary>
    /// Quando è stato aperto il pannello l'ultima volta, che **non è la
    /// stessa cosa** ed è ciò che predice davvero se ci metterà tanto.
    ///
    /// Fu separato leggendo il log della macchina dell'utente:
    /// un'apertura diceva «era fermo da 0,5 min» e tra quella e la
    /// precedente c'erano otto minuti di orologio. Aveva copiato mentre
    /// lavorava, e ogni copia azzerava il contatore — ma catturare una
    /// copia costa poco e non tiene caldo niente di ciò che disegna il
    /// pannello. La cifra diceva «appena usato» di qualcosa che era
    /// freddo da otto minuti, e con lei la lentezza che l'utente nota
    /// era invisibile.
    /// </summary>
    long _ultimaApertura = System.Diagnostics.Stopwatch.GetTimestamp();

    void Attivita() =>
        _ultimaAttivita = System.Diagnostics.Stopwatch.GetTimestamp();

    void MostraPannello()
    {
        _pannello?.Affiora();
    }

    // ------------------------------------------------------ gli appunti

    void NegliAppunti()
    {
        uint sequenza = Appunti.Sequenza();

        // WM_CLIPBOARDUPDATE non arriva una volta per copia ma una per
        // ogni sessione aperta da chi copia: PowerShell ne fa partire
        // tre. Il contatore di Windows è ciò che distingue una copia
        // vera da un avviso ripetuto.
        if (sequenza == _ultimaSequenza) return;
        _ultimaSequenza = sequenza;

        // Anche una copia sveglia il programma: se l'utente ha copiato
        // dieci secondi fa, non era fermo da dieci minuti per quanto
        // non abbia toccato la scorciatoia.
        Attivita();

        // Ciò che abbiamo appena messo noi per incollarlo non si riannota.
        if (sequenza == _sequenzaPropria) return;

        if (Archivio.Pref("in_pausa", false)) return;

        var contenuto = Appunti.Leggi();

        switch (contenuto.Tipo)
        {
            case TipoContenuto.Privato:
                Registro.Annota("copia segnata come privata: non si salva");
                break;

            case TipoContenuto.Testo when contenuto.Testo is not null:
                if (Archivio.Annota(new Voce
                {
                    Tipo = Voce.Testo_,
                    Testo = contenuto.Testo,
                }))
                {
                    AggiornaLista();
                }
                break;

            case TipoContenuto.Immagine when contenuto.Immagine is not null:
                if (Archivio.SalvaImmagine(Appunti.DibABmp(contenuto.Immagine)))
                    AggiornaLista();
                break;
        }
    }

    /// <summary>
    /// L'indice tiene in cache il testo normalizzato di ogni voce,
    /// quindi va avvisato quando l'archivio cambia.
    /// </summary>
    internal void AggiornaLista()
    {
        Indice.Invalida();
        _pannello?.Aggiorna();
    }

    // -------------------------------------------------------- l'incolla

    /// <summary>
    /// Copia la voce, restituisce il focus dov'era e incolla lì.
    /// </summary>
    internal async void Incolla(Voce voce)
    {
        try
        {
            bool copiato = voce.EUnImmagine
                ? voce.Percorso is not null && Appunti.CopiaImmagine(voce.Percorso)
                : Appunti.Copia(
                    [Regole.CreaFrammento(voce.Testo ?? "")], senzaFormato: true);

            await DopoLaCopia(copiato);
        }
        catch (Exception e)
        {
            Registro.Errore("Incolla", e);
        }
    }

    /// <summary>
    /// Incolla un testo con formato: è ciò che distingue un salvato da
    /// una voce della cronologia, che va sempre semplice.
    /// </summary>
    internal async void IncollaFrammenti(
        IReadOnlyList<Frammento> frammenti, bool senzaFormato)
    {
        try
        {
            await DopoLaCopia(Appunti.Copia(frammenti, senzaFormato));
        }
        catch (Exception e)
        {
            Registro.Errore("IncollaFrammenti", e);
        }
    }

    /// <summary>
    /// Restituisce il focus dov'era e incolla lì. È la parte delicata
    /// del ciclo e per questo vive in un solo posto: salvare l'hwnd
    /// precedente, agganciarne il thread e mandare Ctrl+V.
    /// </summary>
    async Task DopoLaCopia(bool copiato)
    {
        if (!copiato)
        {
            _pannello?.Avvisa("Impossibile copiare negli appunti.");
            return;
        }

        _sequenzaPropria = Appunti.Sequenza();
        _ultimaSequenza = _sequenzaPropria;

        _pannello?.Nascondi();

        if (!FocusFinestra.Restituisci(_finestraPrecedente))
        {
            // Resta copiato: l'utente può incollare a mano. È peggio non
            // dirglielo che dirlo.
            //
            // E passa dall'area di notifica, non dal pannello. Il pannello
            // si è appena nascosto tre righe più sopra, quindi disegnare
            // l'avviso lì era scriverlo in una finestra invisibile:
            // l'utente restava con il testo copiato, non incollato e senza
            // sapere perché. In più l'avviso sopravviveva nascosto e
            // ricompariva all'apertura successiva, ormai senza niente a
            // che vedere con ciò che stava facendo in quel momento.
            _areaNotifica?.Avvisa(
                "Copiato, ma non incollato",
                "Non sono riuscito a tornare alla finestra precedente. "
                       + "È negli appunti: incolla con Ctrl+V.");

            return;
        }

        // Windows ha bisogno di un istante per assestare il primo piano
        // prima di accettare i tasti, ma **quanto non si sa in anticipo**.
        // Prima si aspettavano 60 ms fissi: troppi su una macchina scarica
        // —e si pagavano sempre— e forse pochi su una carica, dove il
        // Ctrl+V usciva prima che la destinazione potesse riceverlo.
        // Adesso si aspetta che la finestra sia davvero davanti, che è
        // ciò che si voleva sapere.
        long prima = System.Diagnostics.Stopwatch.GetTimestamp();

        int attesa = await Task.Run(() => FocusFinestra.AspettaInPrimoPiano(_finestraPrecedente));

        FocusFinestra.IncollaConTastiera();

        Registro.Annota(string.Format(
            "incollato: focus in {0} ms + totale {1:F1} ms",
            attesa,
            System.Diagnostics.Stopwatch.GetElapsedTime(prima).TotalMilliseconds));
    }

    /// <summary>Lascia il contenuto negli appunti senza incollarlo.</summary>
    internal void CopiaSoltanto(Elemento elemento)
    {
        try
        {
            bool copiato = elemento switch
            {
                Voce { EUnImmagine: true, Percorso: { } percorso } =>
                    Appunti.CopiaImmagine(percorso),

                Voce voce => Appunti.Copia(
                    [Regole.CreaFrammento(voce.Testo ?? "")], senzaFormato: true),

                Snippet snippet => Appunti.Copia(snippet.Runs),

                _ => false,
            };

            if (!copiato)
            {
                _pannello?.Avvisa("Impossibile copiare negli appunti.");
                return;
            }

            _sequenzaPropria = Appunti.Sequenza();
            _ultimaSequenza = _sequenzaPropria;

            _pannello?.Nascondi();
        }
        catch (Exception e)
        {
            Registro.Errore("CopiaSoltanto", e);
        }
    }

    /// <summary>
    /// Apre il link nel browser. Si offre solo quando l'intero testo è
    /// l'indirizzo: un paragrafo che lo menziona di passaggio non conta.
    ///
    /// È un'azione in più del menu della riga, non ciò che fa il clic.
    /// Quando lo era, un link non si poteva incollare da nessuna parte.
    /// </summary>
    internal void ApriLink(string testo)
    {
        _pannello?.Nascondi();

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = Regole.UrlDi(testo),
                UseShellExecute = true,
            });
        }
        catch (Exception e)
        {
            Registro.Errore("ApriLink", e);

            // Dall'area di notifica per lo stesso motivo dell'avviso di
            // incolla: il pannello si nasconde in cima a questo metodo,
            // quindi qui non c'è più una finestra dove scrivere.
            _areaNotifica?.Avvisa(
                "Impossibile aprire il link",
                "Windows non ha trovato con cosa aprirlo.");
        }
    }

    /// <summary>
    /// Cambia la scorciatoia globale a caldo. False se Windows non la
    /// concede —di solito perché se l'è presa un altro programma—, e
    /// allora l'interfaccia deve dirlo invece di tacere.
    /// </summary>
    internal bool MettiScorciatoia(string scorciatoia) => _cassetta?.MettiScorciatoia(scorciatoia) ?? false;

    // ------------------------------------------------------- la chiusura

    bool _chiudendo;

    void Chiudi()
    {
        // Non la chiede più solo l'area di notifica: anche il Restart
        // Manager, e quello può mandare WM_ENDSESSION e poi WM_CLOSE.
        // Entrare due volte scarterebbe due volte e riverserebbe su un
        // archivio rilasciato a metà.
        if (_chiudendo) return;
        _chiudendo = true;

        Registro.Annota("chiusura in corso");

        Archivio.Riversa(forza: true);

        _riversamento?.Dispose();
        _aggiornamenti?.Dispose();
        _areaNotifica?.Dispose();
        _chiusura?.Dispose();
        _cassetta?.Dispose();

        Exit();
    }
}
