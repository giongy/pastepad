using System.Collections.ObjectModel;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Pastepad.App.Sistema;
using Pastepad.Nucleo;
using Windows.System;
using WinRT.Interop;

namespace Pastepad.App;

/// <summary>
/// Il pannello. Senza cornice, sempre in primo piano e fuori da Alt+Tab,
/// come il Win+V che sostituisce.
///
/// Non si chiude mai: si nasconde. Chiuderlo ucciderebbe il processo e
/// con lui la scorciatoia globale.
/// </summary>
public sealed partial class Pannello : Window
{
    const string Recenti = "recenti";
    const string Salvati = "salvati";

    /// <summary>
    /// Il blocco. È l'unica scheda dove niente si incolla: si scrive, si
    /// legge e si elimina.
    /// </summary>
    const string Note = "note";

    readonly ObservableCollection<ItemLista> _items = [];

    /// <summary>Solo i riquadri, senza intestazioni: è ciò che si incolla.</summary>
    readonly List<Riga> _righe = [];

    /// <summary>
    /// Ciò che è spuntato in selezione multipla. Per riferimento e non
    /// per contenuto: due voci con lo stesso testo sono due voci.
    /// </summary>
    readonly HashSet<Elemento> _spuntati =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Quali gruppi di Salvati sono aperti. Si inizializza dall'archivio
    /// all'avvio e si salva alla chiusura del gruppo: l'utente ha chiesto
    /// di vederli "in un elenco a discesa, non esteso", e una preferenza
    /// che si perde chiudendo il pannello non è una preferenza.
    /// </summary>
    readonly Dictionary<string, bool> _gruppi = [];

    readonly nint _hwnd;
    readonly OverlappedPresenter _presentatore;

    string _scheda = Recenti;

    /// <summary>
    /// L'annotazione aperta nella lavagna, o null se si sta vedendo la
    /// lista. Sopravvive al nascondere il pannello apposta: è ciò che fa
    /// sì che richiamare pastepad restituisca la nota dove era stata
    /// lasciata, con ciò che c'era scritto.
    /// </summary>
    Nota? _lavagna;

    /// <summary>null è "tutte le cartelle".</summary>
    string? _cartella;

    /// <summary>
    /// Il menu del clic destro sull'elenco a discesa delle cartelle. È
    /// un oggetto diverso da quello del clic sinistro perché un
    /// MenuFlyout non può essere appeso a due proprietà insieme.
    /// </summary>
    MenuFlyout? _menuDestro;

    bool _spuntando;
    bool _compatta;

    public bool EVisibile { get; private set; }

    public Pannello()
    {
        InitializeComponent();

        _hwnd = WindowNative.GetWindowHandle(this);

        _presentatore = OverlappedPresenter.Create();
        // Con bordo e senza barra del titolo. Misurato: con (false, false)
        // la finestra conservava una fascia non-client di 9 px in alto
        // dipinta dal sistema —la fascia chiara che si vedeva attraversare
        // il pannello— e l'area client restava 9 px più corta della
        // cornice. Con (true, false) il contenuto arriva da bordo a bordo
        // e gli angoli arrotondati e l'ombra li mette Windows.
        _presentatore.SetBorderAndTitleBar(true, false);
        _presentatore.IsAlwaysOnTop = true;
        _presentatore.IsResizable = true;
        _presentatore.IsMinimizable = false;
        _presentatore.IsMaximizable = false;

        AppWindow.SetPresenter(_presentatore);

        MettiLimiti();

        // Senza questo WinUI riserva in alto la fascia della barra del
        // titolo e la dipinge il sistema: si vedeva una fascia chiara
        // attraversare il pannello da un lato all'altro. Con il contenuto
        // esteso, la finestra è una sola superficie da bordo a bordo.
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(ZonaTrascinamento);

        // L'arrotondamento si chiede una volta sola e solo qui: il
        // pannello non disegna angoli propri. Due arrotondamenti diversi
        // lasciavano in vista la fascia tra l'uno e l'altro.
        AngoliFinestra.Arrotonda(_hwnd);

        // Un gestore di appunti non è un'applicazione che si visita: si
        // invoca. Non disegna niente in Alt+Tab né nella barra delle
        // applicazioni.
        AppWindow.IsShownInSwitchers = false;

        AppWindow.Hide();

        Lista.ItemsSource = _items;

        ApplicaStile();
        DisegnaSchede();
        DisegnaPiede();

        // Ciò che dipingiamo da codice —tutto ciò che ha l'accento— non
        // lo rivaluta nessuno quando Windows passa da chiaro a scuro. I
        // pennelli della tavolozza sì: vanno con ThemeResource.
        Cornice.ActualThemeChanged += AlCambioDelTema;

        // Perdere il focus lo chiude, come fa lo stesso Win+V.
        Activated += AllAttivazione;

        AppWindow.Changed += AlCambioDellaFinestra;
    }

    void AlCambioDelTema(FrameworkElement mittente, object args)
    {
        Stile.Sincronizza(Cornice.ActualTheme);
        Ridisegna();
    }

    Archivio Archivio => App.Attuale.Archivio;

    /// <summary>Il modo delle cartelle: "menu" o "tessere".</summary>
    string ModoCartelle => Archivio.Pref("cartelle", "menu") ?? "menu";

    // ---------------------------------------------------------- stile

    /// <summary>
    /// Rilegge accento e sfondo dalle preferenze.
    ///
    /// Decide solo cosa chiedere a WinUI: con "auto" chiede
    /// <see cref="ElementTheme.Default"/>, che è seguire Windows, e da
    /// lì il cambio di tema a caldo lo fa il framework con i
    /// ThemeDictionaries. Qui non si sonda il registro né si forza un
    /// tema.
    /// </summary>
    public void ApplicaStile()
    {
        Stile.Applica(
            Archivio.Pref("accento", Stile.AccentoDef),
            Archivio.Pref("tema", Stile.TemaDef));

        Cornice.RequestedTheme = Stile.TemaRichiesto;

        // Mica è il materiale che usa lo stesso Win+V, ed è metà del
        // motivo per stare su WinUI 3: coprirlo con un colore pieno
        // sarebbe pagare il costo senza incassare il beneficio. Si copre
        // solo quando l'utente ha scelto uno dei dieci sfondi propri, che
        // è chiedere apposta un colore preciso.
        // Con uno sfondo proprio Mica dà fastidio: si vedrebbe il materiale
        // di sistema dietro un colore che l'utente ha scelto a mano.
        SystemBackdrop = Stile.UsaMica
            ? new Microsoft.UI.Xaml.Media.MicaBackdrop()
            : null;

        Stile.Sincronizza(Cornice.ActualTheme);
        Ridisegna();
    }

    /// <summary>
    /// Ridisegna ciò che si dipinge da codice. I pennelli della
    /// tavolozza vanno con ThemeResource e si aggiornano da soli;
    /// l'accento e tutto ciò che si costruisce a mano —schede, cartelle,
    /// piede, righe— no.
    /// </summary>
    void Ridisegna()
    {
        // Trasparente e non null: null non risponde al mouse e il
        // pannello smetterebbe di ricevere clic negli spazi vuoti.
        Cornice.Background = Stile.UsaMica
            ? new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.Colors.Transparent)
            : Stile.Pennello(Stile.Attuale.Sfondo);

        string scorciatoia = Archivio.Pref("scorciatoia", Config.ScorciatoiaDef) ?? Config.ScorciatoiaDef;

        EtichettaScorciatoia.Text = Config.Leggibile(scorciatoia);

        Traduci();

        DisegnaPausa();
        DisegnaSchede();
        DisegnaCartelle();
        DisegnaPiede();

        // Le righe leggono la tavolozza quando costruiscono i loro
        // pennelli, quindi bisogna dire loro di riguardarla.
        foreach (var f in _righe) f.Aggiorna();

        // Anche la fascia della novità va in accento: se non si
        // ridisegna, resta con il colore di prima quando lo si cambia in
        // Aspetto.
        DisegnaNovita();
    }

    /// <summary>
    /// I testi che lo XAML dichiara sciolti. Sono rimasti nel codice da
    /// quando la lingua si sceglieva a caldo.
    /// </summary>
    void Traduci()
    {
        BarraRicerca.PlaceholderText = "Cerca ovunque";
        TabRecenti.Content = "Recenti";
        TabSalvati.Content = "Salvati";
        TabNote.Content = "Note";
        AvvisoPausa.Text = "● " + "In pausa";

        Intitola(PulsanteAspetto, "Aspetto");
        Intitola(PulsanteChiudi, "Chiudi");
    }

    /// <summary>
    /// La dicitura di un pulsante che ha solo un'icona: il suggerimento
    /// e il nome che legge un lettore di schermo, presi dallo stesso
    /// testo.
    ///
    /// Vanno insieme apposta. Con un glifo come unico contenuto,
    /// l'albero di accessibilità dava un nome vuoto e quei pulsanti non
    /// si potevano identificare a orecchio; e messi separati, il giorno
    /// che uno cambia l'altro resta indietro.
    /// </summary>
    static void Intitola(UIElement controllo, string testo)
    {
        ToolTipService.SetToolTip(controllo, testo);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(controllo, testo);
    }

    void DisegnaPausa()
    {
        bool inPausa = Archivio.Pref("in_pausa", false);

        AvvisoPausa.Visibility = inPausa ? Visibility.Visible : Visibility.Collapsed;

        PulsantePausa.Content = inPausa
            ? Stile.Icone.Riprendi
            : Stile.Icone.Pausa;

        PulsantePausa.Foreground = inPausa
            ? Stile.Pennello(Stile.Rosso)
            : Stile.Pennello(Stile.Attuale.Medio);

        PulsantePausa.BorderBrush = Stile.Pennello(Stile.Rosso);
        PulsantePausa.BorderThickness = new Thickness(inPausa ? 1 : 0);

        Intitola(
            PulsantePausa,
            inPausa ? "Riprendi la cattura" : "Metti in pausa la cattura");
    }

    // -------------------------------------------------------- finestra

    void AllAttivazione(object mittente, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated
            && EVisibile)
        {
            // Con un dialogo aperto la finestra perde l'attivazione senza
            // che l'utente sia andato da nessuna parte. Nasconderla lì
            // lasciava il dialogo orfano in mezzo allo schermo.
            if (CeUnDialogo()) return;

            Nascondi();
        }
    }

    bool CeUnDialogo() =>
        Cornice.XamlRoot is { } radice
        && Microsoft.UI.Xaml.Media.VisualTreeHelper
            .GetOpenPopupsForXamlRoot(radice).Count > 0;

    /// <summary>
    /// Salva la dimensione che l'utente lascia. Va in unità logiche,
    /// non fisiche: altrimenti cambiando monitor il pannello cresceva o
    /// si restringeva.
    /// </summary>
    void AlCambioDellaFinestra(AppWindow finestra, AppWindowChangedEventArgs args)
    {
        if (!args.DidSizeChange || !EVisibile) return;

        double scala = Scala();

        // Si salva ciò che si vede, non il rettangolo della finestra:
        // salvare quest'ultimo faceva crescere il pannello di qualche
        // pixel a ogni apertura, perché riaprendolo gli si sommava di
        // nuovo la cornice.
        var margine = CorniceFinestra.Margine(_hwnd);

        int larghezza = (int)Math.Round(
            (finestra.Size.Width - margine.Larghezza) / scala);
        int altezza = (int)Math.Round(
            (finestra.Size.Height - margine.Altezza) / scala);

        if (larghezza < Config.MinLarghezza || altezza < Config.MinAltezza) return;

        // La riga passa a una sola linea sotto i 340 di larghezza. Si
        // guarda la larghezza reale e non la preimpostazione: da quando
        // il bordo si trascina, la preimpostazione non dice più la verità.
        if (_compatta != larghezza < Stile.LarghezzaCompatta)
        {
            _compatta = larghezza < Stile.LarghezzaCompatta;
            Aggiorna();
            DisegnaPiede();
        }

        if (Archivio.Pref("larghezza", 0) == larghezza && Archivio.Pref("altezza", 0) == altezza)
            return;

        Archivio.MettiPref("larghezza", larghezza);
        Archivio.MettiPref("altezza", altezza);
    }

    double Scala()
    {
        double scala = Nativo.GetDpiForWindow(_hwnd) / 96.0;
        return scala > 0 ? scala : 1;
    }

    /// <summary>
    /// I limiti del trascinamento. Si dichiarano su ciò che si vede, che
    /// è ciò di cui parlano le specifiche, ma il presentatore li applica
    /// al rettangolo della finestra — e quello contiene la cornice
    /// invisibile di trascinamento. Senza sommarla, un massimo di 720
    /// lasciava il pannello a 706 di larghezza visibile, e il minimo di
    /// 300 gli permetteva di scendere a 286.
    ///
    /// Si reimposta a ogni apertura perché il margine non si può
    /// misurare finché la finestra non esiste, e può cambiare passando a
    /// un monitor con un'altra scala.
    /// </summary>
    void MettiLimiti()
    {
        var margine = CorniceFinestra.Margine(_hwnd);

        _presentatore.PreferredMinimumWidth = Config.MinLarghezza + margine.Larghezza;
        _presentatore.PreferredMinimumHeight = Config.MinAltezza + margine.Altezza;
        _presentatore.PreferredMaximumWidth = Config.MaxLarghezza + margine.Larghezza;
        _presentatore.PreferredMaximumHeight = Config.MaxAltezza + margine.Altezza;
    }

    /// <summary>Fa comparire il pannello vicino al puntatore.</summary>
    public void Affiora()
    {
        MettiLimiti();

        var (larghezza, altezza) = DimensioneSalvata();

        // AppWindow lavora in pixel fisici; le misure di config sono
        // logiche. Senza questa conversione il pannello esce piccolo sugli
        // schermi scalati, che è proprio ciò che WinUI evita nel resto.
        double scala = Scala();

        int larghezzaFisica = (int)Math.Round(larghezza * scala);
        int altezzaFisica = (int)Math.Round(altezza * scala);

        var (x, y) = Schermo.VicinoAlPuntatore(larghezzaFisica, altezzaFisica);

        // Al rettangolo della finestra va sommata la cornice invisibile
        // di trascinamento, che si conta ma non si vede. Senza, un
        // pannello chiesto di 380x560 si vedeva di 366x553.
        var margine = CorniceFinestra.Margine(_hwnd);

        AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(
            x, y, larghezzaFisica + margine.Larghezza, altezzaFisica + margine.Altezza));

        _compatta = larghezza < Stile.LarghezzaCompatta;

        // Svuotare la barra di ricerca fa partire
        // BarraRicerca_TextChanged, che chiama già Aggiorna. Senza
        // zittirlo, **ogni apertura dopo una ricerca ricostruisce la
        // lista due volte** — fino a 80 righe in più, nel percorso che
        // deve essere istantaneo.
        _zitto = true;
        BarraRicerca.Text = "";
        _zitto = false;

        Aggiorna();

        // Dopo Aggiorna, che non le tocca. Tutte e due sopravvivono alla
        // chiusura del pannello e si rimettono finché non vengono guardate.
        DisegnaAvviso();
        DisegnaNovita();

        AppWindow.Show();
        EVisibile = true;

        Activate();

        // Activate() non basta: Windows non lascia che un processo in
        // secondo piano si metta davanti, e il pannello usciva visibile
        // ma senza focus, con la barra di ricerca che non riceveva ciò
        // che si scriveva. Si usa lo stesso aggancio di thread usato per
        // restituire il focus quando si incolla.
        if (!FocusFinestra.PortaInPrimoPiano(_hwnd))
            Registro.Annota("il pannello non ha ottenuto il primo piano");

        // Con un'annotazione aperta il cursore va DENTRO di lei, non
        // nella barra di ricerca. È la lavagna: si chiama con la
        // scorciatoia e si continua a scrivere dove si era rimasti,
        // senza dover premere niente. La barra di ricerca resta sopra,
        // in vista e a un clic.
        if (_lavagna is not null)
        {
            LavagnaTesto.Focus(FocusState.Programmatic);
            AllaFine();
        }
        else
        {
            BarraRicerca.Focus(FocusState.Programmatic);
        }
    }

    /// <summary>
    /// Compone il pannello una volta, fuori dallo schermo, perché la
    /// prima apertura vera non sia anche il primo fotogramma.
    ///
    /// Misurato: nell'istante in cui la finestra compare il contenuto è
    /// dipinto a metà —circa 5.500 pixel dei 36.500 che ha quando è
    /// completa— e finisce di comporsi in circa 150 ms. Di solito non
    /// si nota; con la macchina carica si allunga abbastanza perché
    /// l'utente veda il pannello senza intestazione e creda che manchi.
    /// Non riguarda la barra del titolo, che era il primo sospetto:
    /// manca contenuto in TUTTA la finestra, non solo in quella riga.
    ///
    /// Va nell'avvio e non in Affiora, quindi **non costa un solo
    /// millisecondo all'apertura del pannello**, che è il requisito
    /// numero due. Ciò che costa è un fotogramma all'avvio.
    ///
    /// Tre dettagli che non sono un caso:
    ///
    /// - <c>Show(false)</c>, non <c>Show()</c>: «Shows the window with an
    ///   option to activate it or not». Attivarlo ruberebbe il primo
    ///   piano a ciò che l'utente stava facendo all'accesso.
    /// - Molto lontano e in alto, in coordinate negative grandi, perché
    ///   non cada dentro nessun monitor per quanto strana sia la
    ///   disposizione. Un fotogramma che spunta all'avvio sarebbe peggio
    ///   dell'errore.
    /// - <c>EVisibile</c> non si tocca. Vale false tutto il tempo, quindi
    ///   il gestore di attivazione —che nasconde il pannello quando
    ///   perde il focus— esce dalla sua prima riga e non se ne accorge.
    /// </summary>
    public void Riscalda()
    {
        try
        {
            AppWindow.MoveAndResize(
                new Windows.Graphics.RectInt32(-32000, -32000, 380, 560));

            AppWindow.Show(false);

            AlPrimoFotogramma(() =>
            {
                AppWindow.Hide();
                Registro.Annota("pannello composto in anticipo");
            });
        }
        catch (Exception e)
        {
            // Che non si componga in anticipo è un fastidio; che cada
            // l'avvio per averci provato, no.
            Registro.Errore("riscaldare il pannello", e);
        }
    }

    bool _mancaDiagnostica = true;

    /// <summary>
    /// Ciò che c'è da sapere sul PRIMO fotogramma, e solo su quello.
    ///
    /// Esiste per un errore di cui non si conosce la causa: ci sono
    /// prime aperture in cui la riga dell'intestazione esce non dipinta
    /// —né l'etichetta della scorciatoia né i tre pulsanti— e la
    /// successiva esce bene. È intermittente e compare con la macchina
    /// carica.
    ///
    /// **Nessun controllo per automazione lo vede**: l'albero di
    /// accessibilità ha gli elementi tutte e due le volte. Si vede solo
    /// guardando i pixel, e a quel punto è già passato. Queste cinque
    /// cifre sono ciò che separa le ipotesi senza dover essere presenti:
    ///
    /// - larghezze a 0    -> la riga non è arrivata a disporsi
    /// - larghezze buone  -> si è disposta ed è fallita la pittura
    /// - colore uguale allo sfondo -> è il tema che si risolve tardi
    ///
    /// Si annota una volta per processo: si indaga sul primo fotogramma,
    /// e pagarlo a ogni apertura sarebbe gravare proprio sul percorso
    /// che deve essere istantaneo.
    /// </summary>
    public string? Diagnostica()
    {
        if (!_mancaDiagnostica) return null;
        _mancaDiagnostica = false;

        var inchiostro = (EtichettaScorciatoia.Foreground as
            Microsoft.UI.Xaml.Media.SolidColorBrush)?.Color;

        return string.Format(
            "primo fotogramma: scorciatoia {0:F0}x{1:F0} '{2}' inchiostro {3}, "
            + "pulsante chiudi {4:F0}x{5:F0}, tema {6}, barra estesa {7}",
            EtichettaScorciatoia.ActualWidth, EtichettaScorciatoia.ActualHeight,
            EtichettaScorciatoia.Text,
            inchiostro is { } c ? $"#{c.R:X2}{c.G:X2}{c.B:X2} alfa {c.A}" : "(non è un colore pieno)",
            PulsanteChiudi.ActualWidth, PulsanteChiudi.ActualHeight,
            Cornice.ActualTheme,
            ExtendsContentIntoTitleBar);
    }

    /// <summary>
    /// Avvisa una volta sola, quando il pannello si è disegnato davvero.
    ///
    /// <see cref="Affiora"/> torna appena ha chiesto la finestra, non
    /// quando c'è qualcosa a schermo. Ciò che l'utente chiama «metterci
    /// tanto» è fino a vedere il pannello, e quel tratto restava fuori
    /// dalla misura: il log diceva 25 ms mentre si notava una lunga
    /// attesa.
    ///
    /// Rendering scatta a ogni fotogramma, quindi bisogna sganciarsi al
    /// primo. Altrimenti si paga a ogni fotogramma per tutta la vita del
    /// programma.
    /// </summary>
    Action? _aspettandoFotogramma;

    public void AlPrimoFotogramma(Action avviso)
    {
        // **Può essercene uno solo in attesa.** Aprendo e chiudendo in
        // fretta si accumulavano più iscrizioni che scattavano tutte
        // nello stesso fotogramma, ognuna misurando dal proprio segno: nel
        // log dell'utente uscirono tre righe a 8 ms l'una dall'altra che
        // dicevano 588, 322 e 200 ms di disegno. Nessuna era vera, e
        // quelle cifre mandano a cercare una lentezza che non esiste.
        //
        // Resta l'ultima, che è l'apertura che l'utente sta vedendo
        // davvero; le precedenti non interessano più perché il loro
        // pannello si è chiuso prima di arrivare a dipingersi.
        bool laPrima = _aspettandoFotogramma is null;
        _aspettandoFotogramma = avviso;

        if (!laPrima) return;

        void UnaVolta(object? _, object __)
        {
            Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= UnaVolta;

            var inSospeso = _aspettandoFotogramma;
            _aspettandoFotogramma = null;
            inSospeso?.Invoke();
        }

        Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += UnaVolta;
    }

    public void Nascondi()
    {
        if (!EVisibile) return;

        EVisibile = false;

        // Prima di nascondersi, non dopo: l'annotazione si salva a ogni
        // tasto, ma nascondere è il momento in cui l'utente smette di
        // guardare e bisogna essere sicuri.
        SalvaLavagna();

        AppWindow.Hide();

        // Chiudere il pannello dà l'avviso per letto. La novità di
        // versione non si tocca qui: quella si conserva a parte e torna
        // finché non viene guardata.
        _avvisoInSospeso = null;

        Avviso.Visibility = Visibility.Collapsed;
        AvvisoAzione.Visibility = Visibility.Collapsed;

        // Il modo selezione non sopravvive alla chiusura: tornare e
        // trovare le spunte messe la volta prima disorienta.
        if (_spuntando)
        {
            _spuntando = false;
            _spuntati.Clear();
        }
    }

    /// <summary>
    /// Ciò che l'utente ha lasciato trascinando i bordi. Sotto il minimo
    /// si ignora: al primo avvio non c'è niente di salvato, e un
    /// config.json modificato a mano può contenere qualsiasi cosa.
    /// </summary>
    (int Larghezza, int Altezza) DimensioneSalvata()
    {
        int larghezza = Archivio.Pref("larghezza", 0);
        int altezza = Archivio.Pref("altezza", 0);

        return larghezza >= Config.MinLarghezza && altezza >= Config.MinAltezza
            ? (larghezza, altezza)
            : (Config.LarghezzaDef, Config.AltezzaDef);
    }

    /// <summary>Dove porta il pulsante dell'avviso, se c'è.</summary>
    string? _avvisoLink;

    /// <summary>
    /// Un problema che l'utente deve vedere. Niente errori muti: se la
    /// scorciatoia non si è potuta registrare, si dice quale e perché.
    /// </summary>
    /// <summary>
    /// L'avviso in attesa che l'utente lo veda, se c'è.
    ///
    /// Si conserva come la novità di versione, e per lo stesso motivo:
    /// ci sono avvisi che si emettono con il pannello nascosto —l'avvio
    /// degradato esce PRIMA del primo Affiora— e senza conservarli si
    /// dipingevano su una finestra invisibile e si perdevano.
    ///
    /// E serve conservarlo, non solo dipingerlo: chiudere la fascia a
    /// ogni apertura sistemerebbe l'avviso stantio ma si porterebbe via
    /// quello dell'avvio degradato, che è l'unico che davvero non può
    /// perdersi — dice che la cronologia non si è potuta leggere.
    /// </summary>
    string? _avvisoInSospeso;

    public void Avvisa(string testo)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _avvisoInSospeso = testo;
            DisegnaAvviso();
        });
    }

    void DisegnaAvviso()
    {
        if (_avvisoInSospeso is not { } testo) return;

        _avvisoLink = null;

        Avviso.Text = testo;
        Avviso.Foreground = Stile.Pennello(Stile.Rosso);
        Avviso.Visibility = Visibility.Visible;

        AvvisoAzione.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// La versione nuova in attesa che l'utente la veda, se c'è.
    ///
    /// Si conserva e si ridipinge a ogni apertura invece di mostrarsi
    /// una volta e basta: il controllo cade quando cade —può essere a
    /// pannello chiuso— e nascondere il pannello pulisce la fascia.
    /// Senza questo, l'avviso può non riceverlo nessuno.
    /// </summary>
    (string Version, string Pagina)? _novita;

    /// <summary>
    /// C'è una versione nuova. In accento e non in rosso: non è un
    /// errore, è una notizia. E con un pulsante che apre la pagina della
    /// release — non scarica niente.
    /// </summary>
    public void AvvisaNovita(string versione, string pagina)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _novita = (versione, pagina);
            DisegnaNovita();
        });
    }

    void DisegnaNovita()
    {
        if (_novita is not { } novita) return;

        _avvisoLink = novita.Pagina;

        Avviso.Text = Testi.Componi("C'è una versione nuova: %s", novita.Version);
        Avviso.Foreground = Stile.Pennello(Stile.ColoreAccento.Color);
        Avviso.Visibility = Visibility.Visible;

        AvvisoAzione.Content = "Vedi la novità";
        AvvisoAzione.Visibility = Visibility.Visible;
    }

    void AvvisoAzione_Click(object mittente, RoutedEventArgs args)
    {
        if (_avvisoLink is not { Length: > 0 } pagina) return;

        // L'ha già vista ed è andato a prenderla: non gli si rimette
        // davanti a ogni apertura.
        _novita = null;

        // ApriLink nasconde già il pannello prima di lanciare il browser.
        App.Attuale.ApriLink(pagina);
    }

    // ---------------------------------------------------------- lista

    /// <summary>Rifà la lista con ciò che c'è nell'archivio.</summary>
    public void Aggiorna()
    {
        string query = (BarraRicerca?.Text ?? "").Trim();

        _items.Clear();
        _righe.Clear();

        string vuoto;
        string iconaVuoto;

        if (query.Length > 0 && _scheda == Note)
        {
            // Le annotazioni si cercano qui e solo qui. Nella ricerca
            // generale non escono: lì tutto ciò che compare si incolla, e
            // un'annotazione no.
            foreach (var n in Archivio.CercaNote(query))
                _righe.Add(new Riga(n, _compatta));

            vuoto = "Nessuna nota corrisponde";
            iconaVuoto = Stile.Icone.Cerca;
        }
        else if (query.Length > 0)
        {
            foreach (var r in App.Attuale.Indice.Cerca(query))
                _righe.Add(new Riga(r.Dato, _compatta));

            vuoto = "Niente corrisponde a questa ricerca";
            iconaVuoto = Stile.Icone.Cerca;
        }
        else if (_scheda == Salvati)
        {
            foreach (var s in Archivio.Snippets)
            {
                if (_cartella is null || s.Categoria == _cartella)
                    _righe.Add(new Riga(s, _compatta));
            }

            vuoto = "Vuoto. Usa Nuovo per salvare un testo";
            iconaVuoto = Stile.Icone.CartellaAperta;
        }
        else if (_scheda == Note)
        {
            // Arrivano già ordinate dall'archivio, l'ultima toccata per prima.
            foreach (var n in Archivio.Note)
                _righe.Add(new Riga(n, _compatta));

            vuoto = "Vuoto. Usa Nuovo per scrivere una nota";
            iconaVuoto = Stile.Icone.Nota;
        }
        else
        {
            foreach (var e in Archivio.CronOrdinata())
                _righe.Add(new Riga(e, _compatta));

            vuoto = "Copia qualcosa e apparirà qui";
            iconaVuoto = Stile.Icone.Appunti;
        }

        foreach (var f in _righe)
        {
            f.Spuntando = _spuntando;
            f.Spuntata = _spuntati.Contains(f.Dato);
        }

        if (_righe.Count == 0)
        {
            TestoVuoto.Text = vuoto;
            IconaVuoto.Glyph = iconaVuoto;
            Vuoto.Visibility = Visibility.Visible;
        }
        else
        {
            Vuoto.Visibility = Visibility.Collapsed;

            if (_scheda == Salvati && query.Length == 0)
                Raggruppa();
            else
                foreach (var f in _righe) _items.Add(f);
        }

        DisegnaSchede();
        DisegnaCartelle();
        DisegnaPiede();
        DisegnaLavagna(query.Length > 0);

        // La prima riga resta selezionata, come nella maquette 01. Senza
        // questo, Invio non avrebbe su cosa agire e la barra bianca di
        // focus non si vedrebbe finché non si tocca il mouse.
        if (!_spuntando) Lista.SelectedIndex = PrimaRiga();
    }

    /// <summary>
    /// Salvati in cinque gruppi richiudibili: segnalibri, modelli,
    /// email, prompt di IA e note.
    ///
    /// Nessuno è un altro tipo di dato: sono salvati che si separano
    /// perché non si usano allo stesso modo. Il gruppo lo sceglie
    /// l'utente salvando, e finché non sceglie si propone leggendo il
    /// testo —un indirizzo, dei [[campi]], o né l'uno né l'altro—, che è
    /// ciò che si faceva prima che si potesse scegliere.
    ///
    /// I due che non si deducono, email e prompt, sono proprio quelli
    /// che servivano: tutti e due sono testo normale e dentro non c'è
    /// niente che li distingua da una nota. Senza poter scegliere,
    /// cinque corpi di email erano cinque note in più, e una raccolta di
    /// prompt si perdeva intera in mezzo a loro.
    ///
    /// La cartella resta un'altra cosa: il gruppo dice COSA è, la
    /// cartella dice DOVE sta.
    /// </summary>
    void Raggruppa()
    {
        var perTipo = Tipi.Tutti.ToDictionary(
            t => t,
            t => _righe.Where(f => f.Tipo == t).ToList());

        // Con un solo gruppo l'intestazione è di troppo: non c'è niente
        // da separare, e chiuderlo lascerebbe la scheda vuota.
        bool conIntestazione = perTipo.Values.Count(g => g.Count > 0) > 1;

        Riversa("segnalibri", "Segnalibri", perTipo[Tipi.Segnalibro],
               Stile.Icone.Link, true, conIntestazione);

        Riversa("modelli", "Modelli", perTipo[Tipi.Modello],
               Stile.Icone.Modello, true, conIntestazione);

        Riversa("posta", "Email", perTipo[Tipi.Email],
               Stile.Icone.Email, true, conIntestazione);

        Riversa("prompts", "Prompt IA", perTipo[Tipi.Prompt],
               Stile.Icone.Prompt, true, conIntestazione);

        // «Testi» e non «Note» dalla 4.11.0. Il tipo continua a
        // chiamarsi nota all'interno e nel file —cambiarlo obbligherebbe
        // a riscrivere lo snippets.json di tutti— ma a schermo non può
        // più chiamarsi come la scheda del blocco, che è un'altra cosa e
        // non si incolla. Nemmeno la chiave del gruppo cambia: è quella
        // che ricorda se era aperto o chiuso.
        Riversa("note", "Testi", perTipo[Tipi.Nota],
               Stile.Icone.Nota, false, conIntestazione);
    }

    /// <summary>
    /// Se un gruppo è aperto. Di fabbrica chiuso: con le intestazioni in
    /// vista si vede a colpo d'occhio cosa c'è di ogni tipo e si apre
    /// ciò che si cerca, che è come l'ha chiesto l'utente.
    /// </summary>
    bool GruppoAperto(string chiave)
    {
        if (_gruppi.TryGetValue(chiave, out bool aperto)) return aperto;

        aperto = Archivio.Pref("gruppo." + chiave, false);
        _gruppi[chiave] = aperto;

        return aperto;
    }

    void Riversa(string chiave, string etichetta, List<Riga> gruppo,
                string icona, bool inAccento, bool conIntestazione)
    {
        if (gruppo.Count == 0) return;

        bool aperto = GruppoAperto(chiave);

        if (conIntestazione)
        {
            _items.Add(new Gruppo(chiave, etichetta, gruppo.Count, aperto,
                                 icona, inAccento));

            if (!aperto) return;
        }

        foreach (var f in gruppo) _items.Add(f);
    }

    // --------------------------------------------------------- schede

    void DisegnaSchede()
    {
        // Cercando non se ne evidenzia nessuna: la ricerca le attraversa
        // tutte, e lasciarne una accesa faceva credere che guardasse
        // solo lì.
        bool cercando = (BarraRicerca?.Text ?? "").Trim().Length > 0;

        Disegna(TabRecenti, _scheda == Recenti && !cercando);
        Disegna(TabSalvati, _scheda == Salvati && !cercando);
        Disegna(TabNote, _scheda == Note && !cercando);
    }

    static void Disegna(Button scheda, bool attiva)
    {
        Vesti(
            scheda,
            attiva ? Stile.ColoreAccento.Color : Stile.Attuale.Riquadro,
            attiva ? Stile.ColoreAccento.Sopra : Stile.Attuale.Medio);

        scheda.FontWeight = attiva
            ? Microsoft.UI.Text.FontWeights.SemiBold
            : Microsoft.UI.Text.FontWeights.Normal;
    }

    /// <summary>
    /// Dipinge un pulsante con un nostro colore, stati compresi.
    ///
    /// Mettere solo Background non basta: il modello di Button cambia
    /// quel pennello al passaggio del mouse e alla pressione, e una
    /// scheda in accento diventava grigio di sistema finché il puntatore
    /// era sopra. I pennelli di stato vivono nel dizionario del pulsante
    /// stesso, che è dove il modello li cerca prima di salire a quello
    /// dell'applicazione.
    /// </summary>
    static void Vesti(Button pulsante, string sfondo, string lettera)
    {
        var riempimento = Stile.Da(sfondo);
        var inchiostro = Stile.Da(lettera);

        // Sopra il colore proprio, l'hover e la pressione si fanno con lo
        // stesso inchiostro a bassa opacità: è come li fa WinUI, ma senza
        // perdere il nostro colore.
        pulsante.Resources["ButtonBackground"] = new SolidColorBrush(riempimento);
        pulsante.Resources["ButtonBackgroundPointerOver"] =
            Miscela(riempimento, inchiostro, 0.10);
        pulsante.Resources["ButtonBackgroundPressed"] =
            Miscela(riempimento, inchiostro, 0.18);

        pulsante.Resources["ButtonForeground"] = new SolidColorBrush(inchiostro);
        pulsante.Resources["ButtonForegroundPointerOver"] = new SolidColorBrush(inchiostro);
        pulsante.Resources["ButtonForegroundPressed"] = new SolidColorBrush(inchiostro);

        pulsante.Resources["ButtonBorderBrush"] =
            new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        pulsante.Resources["ButtonBorderBrushPointerOver"] =
            new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        pulsante.Resources["ButtonBorderBrushPressed"] =
            new SolidColorBrush(Microsoft.UI.Colors.Transparent);

        pulsante.Background = new SolidColorBrush(riempimento);
        pulsante.Foreground = new SolidColorBrush(inchiostro);
    }

    static SolidColorBrush Miscela(
        Windows.UI.Color uno, Windows.UI.Color altro, double parte)
    {
        byte Meta(byte a, byte b) =>
            (byte)Math.Round(a + ((b - a) * parte));

        return new SolidColorBrush(Windows.UI.Color.FromArgb(
            255,
            Meta(uno.R, altro.R),
            Meta(uno.G, altro.G),
            Meta(uno.B, altro.B)));
    }

    void Tab_Recenti(object mittente, RoutedEventArgs args) => Cambia(Recenti);

    void Tab_Salvati(object mittente, RoutedEventArgs args) => Cambia(Salvati);

    void Tab_Note(object mittente, RoutedEventArgs args) => Cambia(Note);

    void Cambia(string quale)
    {
        _scheda = quale;

        if (_spuntando)
        {
            _spuntando = false;
            _spuntati.Clear();
        }

        Aggiorna();
    }

    // ------------------------------------------------------- cartelle

    void DisegnaCartelle()
    {
        if (_scheda != Salvati)
        {
            BarraCartelle.Visibility = Visibility.Collapsed;
            return;
        }

        BarraCartelle.Visibility = Visibility.Visible;

        bool tessere = ModoCartelle == "tessere";

        PulsanteCartella.Visibility = tessere ? Visibility.Collapsed : Visibility.Visible;
        TessereCartelle.Visibility = tessere ? Visibility.Visible : Visibility.Collapsed;

        if (tessere) DisegnaTessere();
        else DisegnaPulsanteCartella();
    }

    void DisegnaPulsanteCartella()
    {
        bool impostata = _cartella is not null;

        NomeCartella.Text = _cartella ?? "Tutte le cartelle";

        Vesti(
            PulsanteCartella,
            impostata ? Stile.ColoreAccento.Color : Stile.Attuale.Riquadro,
            impostata ? Stile.ColoreAccento.Sopra : Stile.Attuale.Medio);

        var lettera = impostata
            ? Stile.Pennello(Stile.ColoreAccento.Sopra)
            : Stile.Pennello(Stile.Attuale.Medio);

        NomeCartella.Foreground = lettera;
        ChevronCartella.Foreground = lettera;

        // La cartella va sempre in ambra quando non è accesa: è il
        // segnale da cui si riconosce a colpo d'occhio. Sopra l'accento
        // non si leggerebbe, quindi lì prende il colore del testo sopra.
        IconaCartella.Foreground = impostata ? lettera : Stile.Pennello(Stile.Ambra);
        IconaCartella.Glyph = impostata
            ? Stile.Icone.Cartella
            : Stile.Icone.CartellaAperta;

        RiempiMenuCartelle(MenuCartelle);

        // Lo stesso menu con il tasto destro. L'utente lo cerca lì
        // —"dovrebbe essere con clic destro sulla cartella"— e finora
        // l'elenco a discesa rispondeva solo al sinistro. Va in un
        // MenuFlyout a parte apposta: uno stesso flyout non può essere
        // appeso a Flyout e a ContextFlyout insieme.
        _menuDestro ??= new MenuFlyout { Placement = FlyoutPlacementMode.Bottom };

        RiempiMenuCartelle(_menuDestro);
        PulsanteCartella.ContextFlyout = _menuDestro;
    }

    void RiempiMenuCartelle(MenuFlyout menu)
    {
        menu.Items.Clear();

        var tutte = new MenuFlyoutItem { Text = "Tutte le cartelle" };
        tutte.Click += (_, _) => ScegliCartella(null);
        menu.Items.Add(tutte);

        if (Archivio.Cartelle.Count > 0)
            menu.Items.Add(new MenuFlyoutSeparator());

        foreach (var nome in Archivio.Cartelle)
        {
            var item = new ToggleMenuFlyoutItem
            {
                Text = nome,
                IsChecked = nome == _cartella,
            };

            string quale = nome;
            item.Click += (_, _) => ScegliCartella(quale);

            menu.Items.Add(item);
        }

        menu.Items.Add(new MenuFlyoutSeparator());

        var nuova = new MenuFlyoutItem { Text = "Nuova cartella..." };
        nuova.Click += async (_, _) => await NuovaCartella();
        menu.Items.Add(nuova);

        // Modifica non dipende dal fatto che ci sia una cartella
        // selezionata: con "Tutte le cartelle" —che è ciò che c'è
        // all'apertura— non usciva nessun modo di rinominare né di
        // eliminare, e l'utente lo raccontò come «le cartelle non hanno
        // un pulsante di modifica».
        if (Archivio.Cartelle.Count > 0)
        {
            var modifica = new MenuFlyoutItem { Text = "Modifica cartelle..." };
            modifica.Click += async (_, _) => await ModificaCartelle();
            menu.Items.Add(modifica);
        }

        if (_cartella is { } impostata) AzioniDiCartella(menu, impostata);
    }

    /// <summary>
    /// Le tre cose che si fanno su una cartella precisa. Le stesse
    /// nell'elenco a discesa e nel clic destro della tessera: due menu
    /// con contenuti diversi per la stessa cosa era il motivo per cui
    /// "modifica il contenuto" non si trovava nel modo a tessere.
    /// </summary>
    void AzioniDiCartella(MenuFlyout menu, string cartella)
    {
        var contenuto = new MenuFlyoutItem
        {
            Text = Testi.Componi("Modifica il contenuto di %s...", cartella),
        };
        contenuto.Click += async (_, _) =>
        {
            ScegliCartella(cartella);
            await ModificaContenuto(cartella);
        };
        menu.Items.Add(contenuto);

        var rinomina = new MenuFlyoutItem { Text = Testi.Componi("Rinomina %s", cartella) };
        rinomina.Click += async (_, _) =>
        {
            ScegliCartella(cartella);
            await RinominaCartella();
        };
        menu.Items.Add(rinomina);

        var elimina = new MenuFlyoutItem
        {
            Text = Testi.Componi("Elimina %s e il suo contenuto", cartella),
            Foreground = Stile.Pennello(Stile.Rosso),
        };
        elimina.Click += async (_, _) =>
        {
            ScegliCartella(cartella);
            await EliminaCartella();
        };
        menu.Items.Add(elimina);
    }

    /// <summary>
    /// L'altro modo: una capsula per cartella in fila orizzontale. Con
    /// molte cartelle non stanno nella larghezza del pannello, e per
    /// questo l'elenco a discesa è il modo di fabbrica.
    /// </summary>
    void DisegnaTessere()
    {
        ListaTessere.Children.Clear();

        ListaTessere.Children.Add(Tessera("Tutti", null));

        foreach (var nome in Archivio.Cartelle)
            ListaTessere.Children.Add(Tessera(nome, nome));

        var piu = new Button
        {
            Content = Stile.Icone.CartellaNuova,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
            FontSize = 14,
            Height = Stile.AltezzaScheda,
            MinHeight = Stile.AltezzaScheda,
            MinWidth = 0,
            Width = 34,
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(Stile.RCapsula),
            BorderThickness = new Thickness(0),
            Background = Stile.Pennello(Stile.Attuale.Riquadro),
            Foreground = Stile.Pennello(Stile.Ambra),
        };

        Intitola(piu, "Nuova cartella");
        piu.Click += async (_, _) => await NuovaCartella();

        ListaTessere.Children.Add(piu);

        if (Archivio.Cartelle.Count == 0) return;

        // La matita accanto al più: in questo modo il clic destro della
        // tessera era l'unica cosa che c'era per rinominare, e un menu
        // che compare solo con il tasto destro non lo trova nessuno.
        var modifica = new Button
        {
            Content = Stile.Icone.Modifica,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
            FontSize = 14,
            Height = Stile.AltezzaScheda,
            MinHeight = Stile.AltezzaScheda,
            MinWidth = 0,
            Width = 34,
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(Stile.RCapsula),
            BorderThickness = new Thickness(0),
            Background = Stile.Pennello(Stile.Attuale.Riquadro),
            Foreground = Stile.Pennello(Stile.Attuale.Medio),
        };

        Intitola(modifica, "Modifica cartelle");
        modifica.Click += async (_, _) => await ModificaCartelle();

        ListaTessere.Children.Add(modifica);
    }

    Button Tessera(string etichetta, string? cartella)
    {
        bool attiva = _cartella == cartella;

        var tessera = new Button
        {
            Content = etichetta,
            Height = Stile.AltezzaScheda,
            MinHeight = Stile.AltezzaScheda,
            MinWidth = 0,
            Padding = new Thickness(Stile.E4, 0, Stile.E4, 0),
            CornerRadius = new CornerRadius(Stile.RCapsula),
            BorderThickness = new Thickness(0),
            FontSize = Stile.TMinore,
            FontWeight = attiva
                ? Microsoft.UI.Text.FontWeights.SemiBold
                : Microsoft.UI.Text.FontWeights.Normal,
        };

        Vesti(
            tessera,
            attiva ? Stile.ColoreAccento.Color : Stile.Attuale.Riquadro,
            attiva ? Stile.ColoreAccento.Sopra : Stile.Attuale.Medio);

        tessera.Click += (_, _) => ScegliCartella(cartella);

        // Modifica il contenuto, rinomina ed elimina vivono nel clic
        // destro della tessera: in questo modo non c'è un elenco a
        // discesa dove metterli, ed è dove l'utente li cerca.
        if (cartella is not null)
        {
            var menu = new MenuFlyout();
            AzioniDiCartella(menu, cartella);
            tessera.ContextFlyout = menu;
        }

        return tessera;
    }

    void ScegliCartella(string? nome)
    {
        _cartella = nome;
        Aggiorna();
    }

    async Task NuovaCartella()
    {
        string? nome = await Dialoghi.UnaLinea(
            Cornice.XamlRoot, "Nuova cartella", "Nome della cartella");

        if (nome is null) return;

        if (!Archivio.CreaCartella(nome))
        {
            Avvisa(Testi.Componi("Esiste già una cartella chiamata %s.", nome));
            return;
        }

        _cartella = nome;
        _scheda = Salvati;

        App.Attuale.AggiornaLista();
    }

    /// <summary>
    /// Rinominare e togliere cartelle, tutte in una volta e senza
    /// dipendere da quale è selezionata.
    /// </summary>
    async Task ModificaCartelle()
    {
        var prima = Archivio.Cartelle
            .Select(c => (c, Archivio.ContenutoDi(c).Count))
            .ToList();

        var cambi = await Dialoghi.Cartelle(Cornice.XamlRoot, prima);
        if (cambi is null || cambi.Count == 0) return;

        foreach (var cambio in cambi)
        {
            if (cambio.Tolta)
            {
                Archivio.EliminaCartella(cambio.Nome);
                if (_cartella == cambio.Nome) _cartella = null;
                continue;
            }

            if (!Archivio.RinominaCartella(cambio.Nome, cambio.Nuovo))
            {
                Avvisa(Testi.Componi(
                    "Impossibile rinominare: esiste già una cartella %s.", cambio.Nuovo));
                continue;
            }

            if (_cartella == cambio.Nome) _cartella = cambio.Nuovo;
        }

        App.Attuale.AggiornaLista();
    }

    /// <summary>
    /// L'intera cartella in una casella, una nota per linea.
    ///
    /// Ciò che sopravvive lo decide il nucleo: le linee che non sono
    /// cambiate riusano la loro stessa nota, con il suo nome di
    /// segnalibro e il suo formato. Qui si scrive solo il risultato, e
    /// in una volta sola.
    /// </summary>
    async Task ModificaContenuto(string cartella)
    {
        var prima = Regole.DividiCartella(Archivio.ContenutoDi(cartella));

        string? testo = await Dialoghi.ModificaCartella(Cornice.XamlRoot, cartella, prima);
        if (testo is null) return;

        var fusione = Regole.FondiCartella(prima, testo, cartella);

        // Salvare senza aver cambiato niente non scrive. Senza questo,
        // aprire l'editor e premere Salva riordinava snippets.json —le
        // note di più linee vanno in fondo— senza che l'utente toccasse
        // niente.
        var adesso = Archivio.ContenutoDi(cartella);

        bool uguale = fusione.Risultato.Count == adesso.Count
            && !fusione.Risultato.Where((s, i) => !ReferenceEquals(s, adesso[i])).Any();

        if (uguale) return;

        Archivio.SostituisciContenuto(cartella, fusione.Risultato);

        _scheda = Salvati;
        _cartella = cartella;

        App.Attuale.AggiornaLista();
    }

    async Task RinominaCartella()
    {
        if (_cartella is not { } vecchio) return;

        string? nuovo = await Dialoghi.UnaLinea(
            Cornice.XamlRoot, "Rinomina cartella", "Nuovo nome", vecchio);

        if (nuovo is null || nuovo == vecchio) return;

        if (!Archivio.RinominaCartella(vecchio, nuovo))
        {
            Avvisa(Testi.Componi("Impossibile rinominare: esiste già una cartella %s.", nuovo));
            return;
        }

        _cartella = nuovo;
        App.Attuale.AggiornaLista();
    }

    async Task EliminaCartella()
    {
        if (_cartella is not { } nome) return;

        int quanti = Archivio.ContenutoDi(nome).Count;

        string avviso = quanti switch
        {
            0 => Testi.Componi("Eliminare la cartella %s?", nome),

            1 => Testi.Componi(
                "Eliminare la cartella %s e il suo testo? Non si può annullare.",
                nome),

            _ => Testi.Componi(
                "Eliminare la cartella %s e i suoi %d testi? "
                + "Non si può annullare.",
                nome, quanti),
        };

        if (!await Dialoghi.Conferma(Cornice.XamlRoot, avviso)) return;

        Archivio.EliminaCartella(nome);
        _cartella = null;

        App.Attuale.AggiornaLista();
    }

    // ----------------------------------------------------------- piede

    void DisegnaPiede()
    {
        Piede.Children.Clear();
        Piede.ColumnDefinitions.Clear();

        Piede.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Piede.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star),
        });
        Piede.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var sinistra = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = Stile.E2,
            VerticalAlignment = VerticalAlignment.Center,
        };

        Grid.SetColumn(sinistra, 0);
        Piede.Children.Add(sinistra);

        if (_spuntando)
        {
            sinistra.Children.Add(PulsantePiede("Tutti", "normale", SpuntaTutti));
            sinistra.Children.Add(PulsantePiede(
                Testi.Componi("Elimina (%d)", _spuntati.Count), "pericolo",
                async () => await EliminaSpuntati()));

            var annulla = PulsantePiede("Annulla", "normale", AlternaSpunta);
            Grid.SetColumn(annulla, 2);
            Piede.Children.Add(annulla);
            return;
        }

        if (_scheda == Salvati)
        {
            sinistra.Children.Add(IconaPiede(
                Stile.Icone.CartellaNuova, "Nuova cartella",
                async () => await NuovaCartella(), Stile.Ambra));

            sinistra.Children.Add(IconaPiede(
                Stile.Icone.Lista, "Aggiungi un elenco",
                async () => await AggiungiLista()));
        }
        else if (_scheda == Note)
        {
            // Senza scopa e senza cartelle. La scopa svuota la cronologia,
            // che si riempie da sola; il blocco non si riempie da solo e
            // non c'è niente da spazzare. Le annotazioni si eliminano una
            // alla volta o spuntandole.
        }
        else
        {
            sinistra.Children.Add(IconaPiede(
                Stile.Icone.Scopa, "Svuota la cronologia",
                async () => await Svuota()));
        }

        // Con la finestra stretta non ci stanno icona e due pulsanti
        // con testo: l'ultimo usciva a destra. Stringendo, "Seleziona"
        // sparisce e "Nuovo" resta con il segno più.
        if (!_compatta)
            sinistra.Children.Add(PulsantePiede("Seleziona", "normale", AlternaSpunta));

        var nuovo = new Button
        {
            Style = (Style)Application.Current.Resources["PpPulsanteAccento"],
        };

        Vesti(nuovo, Stile.ColoreAccento.Color, Stile.ColoreAccento.Sopra);

        var dentro = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
        };

        dentro.Children.Add(new TextBlock
        {
            Text = Stile.Icone.Piu,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
        });

        if (!_compatta)
        {
            dentro.Children.Add(new TextBlock
            {
                Text = "Nuovo",
                FontSize = Stile.TMinore,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
            });
        }

        nuovo.Content = dentro;

        // Ha la dicitura anche se di solito si vede la parola: a
        // pannello stretto il testo sparisce e resta solo il segno più.
        Intitola(nuovo, "Nuovo");

        nuovo.Click += async (_, _) => await Nuovo();

        Grid.SetColumn(nuovo, 2);
        Piede.Children.Add(nuovo);
    }

    Button PulsantePiede(string testo, string stile, Action allaPressione)
    {
        var b = new Button
        {
            Content = testo,
            Style = (Style)Application.Current.Resources["PpPulsante"],
        };

        if (stile == "pericolo")
        {
            Vesti(b, Stile.Pericolo, "#FFFFFF");
        }
        else
        {
            Vesti(b, Stile.Attuale.Riquadro, Stile.Attuale.Medio);
            b.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
        }

        b.Click += (_, _) => allaPressione();
        return b;
    }

    Button IconaPiede(string glifo, string tip, Action allaPressione, string? colore = null)
    {
        var b = new Button
        {
            Content = glifo,
            Style = (Style)Application.Current.Resources["PpIconaPiede"],
            Foreground = Stile.Pennello(colore ?? Stile.Attuale.Medio),
        };

        Intitola(b, tip);
        b.Click += (_, _) => allaPressione();
        return b;
    }

    // -------------------------------------------- selezione multipla

    void AlternaSpunta()
    {
        _spuntando = !_spuntando;
        _spuntati.Clear();
        Aggiorna();
    }

    void SpuntaTutti()
    {
        if (_spuntati.Count == _righe.Count) _spuntati.Clear();
        else foreach (var f in _righe) _spuntati.Add(f.Dato);

        foreach (var f in _righe) f.Spuntata = _spuntati.Contains(f.Dato);

        DisegnaPiede();
    }

    async Task EliminaSpuntati()
    {
        if (_spuntati.Count == 0) return;

        string avviso = Testi.Componi(
            _spuntati.Count == 1
                ? "Eliminare %d elemento? Non si può annullare."
                : "Eliminare %d elementi? Non si può annullare.",
            _spuntati.Count);

        if (!await Dialoghi.Conferma(Cornice.XamlRoot, avviso)) return;

        Archivio.EliminaVari([.. _spuntati]);

        _spuntati.Clear();
        _spuntando = false;

        App.Attuale.AggiornaLista();
    }

    // -------------------------------------------------------- lavagna

    /// <summary>
    /// Chi occupa lo spazio della lista: la lavagna o la lista.
    ///
    /// Cercare la copre senza chiuderla —la barra di ricerca resta sopra
    /// e serve per qualsiasi cosa—, e svuotando la ricerca l'annotazione
    /// torna tale e quale. Per questo si guarda la query e non si tocca
    /// <c>_lavagna</c>.
    /// </summary>
    void DisegnaLavagna(bool cercando)
    {
        bool vederla = _lavagna is not null && !cercando;

        Lavagna.Visibility = vederla ? Visibility.Visible : Visibility.Collapsed;
        Lista.Visibility = vederla ? Visibility.Collapsed : Visibility.Visible;

        // Lo spazio di «non c'è niente» non può comparire sotto l'annotazione.
        if (vederla) Vuoto.Visibility = Visibility.Collapsed;
    }

    void ApriLavagna(Nota annotazione)
    {
        _lavagna = annotazione;

        // Zitto: riempire le caselle fa partire TextChanged, e quello salva.
        // Senza questo, aprire un'annotazione la segnerebbe come modificata
        // e la porterebbe in cima alla lista senza che nessuno abbia scritto
        // niente.
        _zitto = true;
        LavagnaNome.Text = annotazione.Titolo;
        Formato.Carica(LavagnaTesto, annotazione.Runs);
        _zitto = false;

        // La barra si aggancia una volta sola, non a ogni apertura: sono
        // sei pulsanti con i loro gestori, e rifarli ogni volta che si
        // apre un'annotazione è spazzatura che si accumula.
        LavagnaBarra.Content ??= Dialoghi.BarraDiAnnotazione(LavagnaTesto);

        LavagnaNome.PlaceholderText = "Dagli un nome";

        DisegnaLavagna(false);

        LavagnaTesto.Focus(FocusState.Programmatic);
        AllaFine();
    }

    /// <summary>
    /// Lascia il cursore alla fine di ciò che è scritto. In un
    /// RichEditBox non vale SelectionStart: si chiede al documento.
    /// </summary>
    void AllaFine()
    {
        var fine = LavagnaTesto.Document.GetRange(
            int.MaxValue / 2, int.MaxValue / 2);

        fine.SetRange(fine.EndPosition, fine.EndPosition);
        fine.SetRange(fine.EndPosition, fine.EndPosition);
        LavagnaTesto.Document.Selection.SetRange(
            fine.EndPosition, fine.EndPosition);
    }

    /// <summary>
    /// Torna alla lista. Ciò che è scritto è già salvato —si salva a
    /// ogni tasto—, quindi qui si rilascia soltanto l'annotazione.
    /// </summary>
    void ChiudiLavagna()
    {
        SalvaLavagna();
        _lavagna = null;
        Aggiorna();
    }

    /// <summary>
    /// Scrive ciò che c'è nelle caselle. Si chiama a ogni tasto e quando
    /// si nasconde il pannello: una lavagna che si chiama con una
    /// scorciatoia e si nasconde con un'altra non può avere un pulsante
    /// di salvataggio che bisogna ricordarsi di premere.
    ///
    /// Un'annotazione che resta senza niente dentro si elimina, come
    /// nelle note rapide di Windows. Altrimenti aprirne una nuova e non
    /// scrivere niente lascerebbe un riquadro vuoto per sempre.
    /// </summary>
    void SalvaLavagna()
    {
        if (_lavagna is null) return;

        var runs = Formato.Leggi(LavagnaTesto);
        string nome = LavagnaNome.Text;

        if (Regole.TestoDi(runs).Trim().Length == 0
            && nome.Trim().Length == 0)
        {
            Archivio.EliminaNota(_lavagna);
            _lavagna = null;
            return;
        }

        Archivio.CambiaNota(
            _lavagna, runs, Regole.Timbro(DateTimeOffset.Now), nome);
    }

    /// <summary>
    /// Due gestori per la stessa cosa perché i due controlli non
    /// dichiarano lo stesso evento: TextBox.TextChanged porta
    /// TextChangedEventHandler e RichEditBox.TextChanged porta
    /// RoutedEventHandler. Non c'è una firma che valga per tutti e due.
    /// </summary>
    void Lavagna_Cambio(object mittente, TextChangedEventArgs args) =>
        SalvaSeServe();

    void Lavagna_Cambio(object mittente, RoutedEventArgs args) =>
        SalvaSeServe();

    void SalvaSeServe()
    {
        if (_zitto) return;
        SalvaLavagna();
    }

    void Lavagna_Indietro(object mittente, RoutedEventArgs args) =>
        ChiudiLavagna();

    async void Lavagna_Elimina(object mittente, RoutedEventArgs args)
    {
        if (_lavagna is null) return;

        // Senza chiedere quando non c'è niente da perdere.
        if (Formato.TestoSemplice(LavagnaTesto).Trim().Length > 0
            && !await Dialoghi.Conferma(
                Cornice.XamlRoot,
                "Eliminare questa nota? Non si può annullare."))
            return;

        Archivio.EliminaNota(_lavagna);
        _lavagna = null;
        Aggiorna();
    }

    // ---------------------------------------------------------- azioni

    async Task Nuovo()
    {
        if (_scheda == Note)
        {
            // Nasce vuota e si apre per scriverci dentro, come nelle
            // note rapide. Se resta senza niente, SalvaLavagna la
            // elimina: non serve chiedere né lasciare spazzatura.
            var annotazione = Regole.CreaNota("", DateTimeOffset.Now);
            Archivio.AggiungiNota(annotazione);

            Aggiorna();
            ApriLavagna(annotazione);
            return;
        }

        var snippet = await Dialoghi.Testo(
            Cornice.XamlRoot,
            "Nuovo testo",
            Archivio.Cartelle,
            _cartella ?? Archivio.Cartelle.FirstOrDefault() ?? Config.CartellaDef);

        if (snippet is null) return;

        Archivio.AggiungiSnippet(snippet);

        _scheda = Salvati;
        App.Attuale.AggiornaLista();
    }

    async Task AggiungiLista()
    {
        string cartella = _cartella
            ?? Archivio.Cartelle.FirstOrDefault()
            ?? Config.CartellaDef;

        var linee = await Dialoghi.Lista(Cornice.XamlRoot, cartella);
        if (linee is null) return;

        foreach (var linea in linee)
        {
            Archivio.AggiungiSnippet(Regole.CreaSnippet(linea, cartella));
        }

        _scheda = Salvati;
        _cartella = cartella;

        App.Attuale.AggiornaLista();
    }

    async Task Svuota()
    {
        if (!await Dialoghi.Conferma(
                Cornice.XamlRoot,
                "Svuotare la cronologia? Gli elementi fissati restano."))
        {
            return;
        }

        Archivio.SvuotaCronologia();
        App.Attuale.AggiornaLista();
    }

    async Task Modifica(Riga riga)
    {
        if (riga.Dato is Nota annotazione)
        {
            // Modificare un'annotazione è aprirla nella lavagna, come
            // premerla. Due modi di arrivare nello stesso posto; ciò
            // che non può esserci sono due editor diversi per la stessa cosa.
            ApriLavagna(annotazione);
            return;
        }

        if (riga.Dato is Snippet vecchio)
        {
            var nuovo = await Dialoghi.Testo(
                Cornice.XamlRoot, "Modifica testo", Archivio.Cartelle,
                vecchio.Categoria, vecchio);

            if (nuovo is null) return;

            Archivio.SostituisciSnippet(vecchio, nuovo);
            App.Attuale.AggiornaLista();
            return;
        }

        // Una voce della cronologia si modifica salvandola: è ciò che
        // faceva la versione precedente con "Modifica e salva...".
        var salvato = await Dialoghi.Testo(
            Cornice.XamlRoot,
            "Modifica e salva...",
            Archivio.Cartelle,
            _cartella ?? Archivio.Cartelle.FirstOrDefault() ?? Config.CartellaDef,
            new Snippet { Runs = [Regole.CreaFrammento(riga.Testo)] });

        if (salvato is null) return;

        Archivio.AggiungiSnippet(salvato);

        _scheda = Salvati;
        App.Attuale.AggiornaLista();
    }

    /// <summary>
    /// Incolla la riga. Se è un modello con [[campi]], prima chiede.
    ///
    /// Un link si incolla come qualsiasi altra cosa. Fino alla 4.3.0 si
    /// apriva nel browser, e da qui passano le quattro strade che
    /// portano a incollare —il clic, l'Invio della barra di ricerca, e
    /// «Incolla» e «Incolla senza formato» del menu—, quindi un link non
    /// si poteva incollare da nessuna parte: la voce che dice «Incolla»
    /// apriva Chrome. Copiare un indirizzo e non poterlo mettere in
    /// nessun campo fu ciò che lo fece emergere.
    ///
    /// Aprire c'è ancora, ma dove non può confondersi con incollare:
    /// «Apri nel browser», in cima al menu della riga.
    /// </summary>
    /// <param name="apriSegnalibro">
    /// False quando l'ordine è incollare e nient'altro. **È ciò che
    /// evita di ripetere l'errore della 4.3.0**: da qui passano anche
    /// «Incolla» e «Incolla senza formato» del menu, e se un segnalibro
    /// aprisse il browser anche da lì, la voce che dice «Incolla» non
    /// incollerebbe — che è esattamente ciò che fu sistemato nella 4.4.0.
    /// </param>
    async Task Usa(Riga riga, bool senzaFormato = false, bool apriSegnalibro = true)
    {
        // Un'annotazione non si incolla mai. Va per prima cosa e qui
        // dentro —e non nel clic— perché valga anche per l'Invio della
        // barra di ricerca e per qualsiasi strada che arrivi a usare una
        // riga.
        if (riga.Dato is Nota annotazione)
        {
            // La lavagna, non un dialogo: deve potersi nascondere con
            // il pannello e tornare aperta dove era stata lasciata.
            ApriLavagna(annotazione);
            return;
        }

        // La cronologia incolla sempre, anche se ciò che è copiato è un
        // indirizzo. Copiare un link e non poterlo mettere in nessun
        // campo fu l'errore sistemato nella 4.4.0 e non si torna indietro.
        if (riga.Dato is Voce voce)
        {
            App.Attuale.Incolla(voce);
            return;
        }

        if (riga.Dato is not Snippet snippet) return;

        // **Un segnalibro salvato invece si apre.** Salvare un indirizzo
        // come segnalibro è dire «questo lo voglio visitare», non «questo
        // lo voglio scrivere»; l'ha detto l'utente con queste parole.
        // Copiarlo c'è ancora, nel menu dei tre punti.
        //
        // I due casi sono diversi e per questo convivono: ciò che comanda
        // qui è il TIPO del salvato, non che il testo sembri un link. Un
        // indirizzo nella cronologia si incolla, e anche un salvato che
        // l'utente cambia in Nota — il tipo lo sceglie lui.
        if (apriSegnalibro && riga.Tipo == Tipi.Segnalibro)
        {
            App.Attuale.ApriLink(riga.Testo);
            return;
        }

        var campi = Regole.CampiDi(riga.Testo);

        var runs = snippet.Runs;

        if (campi.Count > 0)
        {
            var valori = await Dialoghi.Campi(Cornice.XamlRoot, campi);
            if (valori is null) return;

            runs = Regole.Riempi(snippet.Runs, valori);
        }

        App.Attuale.IncollaFrammenti(runs, senzaFormato);
    }

    // -------------------------------------------------------- tastiera

    void Escape_Invoked(
        KeyboardAccelerator mittente, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;

        if (_spuntando)
        {
            AlternaSpunta();
            return;
        }

        Nascondi();
    }

    /// <summary>
    /// Finché è impostato, cambiare il testo della barra di ricerca non
    /// aggiorna. Lo usa <see cref="Affiora"/> per svuotarla senza pagare
    /// una ricostruzione della lista che farà lei stessa subito dopo.
    /// </summary>
    bool _zitto;

    void BarraRicerca_TextChanged(object mittente, TextChangedEventArgs args)
    {
        if (_zitto) return;

        Aggiorna();
    }

    /// <summary>
    /// La barra di ricerca incorniciata in accento quando ha il focus,
    /// come la maquette 12. La casella è un Border a parte perché il
    /// TextBox va senza bordo proprio.
    /// </summary>
    void BarraRicerca_Focus(object mittente, RoutedEventArgs args)
    {
        bool ha = BarraRicerca.FocusState != FocusState.Unfocused;

        // Senza focus il bordo prende il colore del riempimento e non
        // trasparente: misurato, un bordo trasparente di 1 px lasciava la
        // barra di ricerca a 40 px dipinti dove la maquette ne chiede 42.
        CorniceBarraRicerca.BorderBrush = ha
            ? Stile.Pennello(Stile.ColoreAccento.Color)
            : Stile.Pennello(Stile.Attuale.Elevato);
    }

    /// <summary>
    /// Scrivere e premere Invio usa il primo risultato: è la strada
    /// veloce, ed evita di dover lasciare la tastiera per il mouse.
    /// </summary>
    async void BarraRicerca_KeyDown(object mittente, KeyRoutedEventArgs args)
    {
        switch (args.Key)
        {
            case VirtualKey.Enter when _righe.Count > 0:
                args.Handled = true;
                await Usa(_righe[0]);
                break;

            case VirtualKey.Down when _items.Count > 0:
                args.Handled = true;
                Lista.SelectedIndex = PrimaRiga();
                Lista.Focus(FocusState.Programmatic);
                break;
        }
    }

    int PrimaRiga()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] is Riga) return i;
        }

        return -1;
    }

    async void Lista_KeyDown(object mittente, KeyRoutedEventArgs args)
    {
        if (args.Key == VirtualKey.Enter && Lista.SelectedItem is Riga riga)
        {
            args.Handled = true;
            await Usa(riga);
        }
    }

    // ----------------------------------------------------------- mouse

    void Lista_SelectionChanged(object mittente, SelectionChangedEventArgs args)
    {
        foreach (var tolta in args.RemovedItems)
        {
            if (tolta is Riga f) f.Attiva = false;
        }

        // Un'intestazione di gruppo non si seleziona: non c'è niente da incollare.
        if (Lista.SelectedItem is Gruppo)
        {
            Lista.SelectedItem = null;
            return;
        }

        if (Lista.SelectedItem is Riga riga) riga.Attiva = true;
    }

    async void Lista_ItemClick(object mittente, ItemClickEventArgs args)
    {
        switch (args.ClickedItem)
        {
            // L'unico posto dove si chiude un gruppo. L'intestazione non
            // aggancia in più un suo Tapped: con tutti e due, il secondo
            // arrivava quando l'Aggiorna del primo aveva già riciclato
            // il contenitore, leggeva il DataContext del gruppo successivo
            // e chiudeva quello. Misurato: un clic su "Segnalibri" chiudeva
            // i due gruppi e uno su "Note" non faceva niente.
            case Gruppo gruppo:
                bool aperto = !GruppoAperto(gruppo.Chiave);

                _gruppi[gruppo.Chiave] = aperto;
                Archivio.MettiPref("gruppo." + gruppo.Chiave, aperto);

                Aggiorna();
                break;

            case Riga riga when _spuntando:
                if (!_spuntati.Remove(riga.Dato)) _spuntati.Add(riga.Dato);
                riga.Spuntata = _spuntati.Contains(riga.Dato);
                DisegnaPiede();
                break;

            case Riga riga:
                await Usa(riga);
                break;
        }
    }

    void Riga_Entra(object mittente, PointerRoutedEventArgs args)
    {
        if (mittente is FrameworkElement { DataContext: Riga riga })
            riga.InPrimoPiano = true;
    }

    void Riga_Esce(object mittente, PointerRoutedEventArgs args)
    {
        if (mittente is FrameworkElement { DataContext: Riga riga })
            riga.InPrimoPiano = false;
    }

    // ------------------------------------------- menu dei tre punti

    static Riga? DaMenu(object mittente) =>
        (mittente as FrameworkElement)?.DataContext as Riga;

    void Menu_Apri(object mittente, RoutedEventArgs args)
    {
        if (DaMenu(mittente) is { } riga) App.Attuale.ApriLink(riga.Testo);
    }

    async void Menu_Incolla(object mittente, RoutedEventArgs args)
    {
        if (DaMenu(mittente) is { } riga)
            await Usa(riga, apriSegnalibro: false);
    }

    async void Menu_IncollaSemplice(object mittente, RoutedEventArgs args)
    {
        if (DaMenu(mittente) is { } riga)
            await Usa(riga, senzaFormato: true, apriSegnalibro: false);
    }

    void Menu_Copia(object mittente, RoutedEventArgs args)
    {
        if (DaMenu(mittente) is { } riga) App.Attuale.CopiaSoltanto(riga.Dato);
    }

    void Menu_Fissa(object mittente, RoutedEventArgs args)
    {
        if (DaMenu(mittente) is not { Dato: Voce voce }) return;

        // Fissare è qualcosa che l'utente fa apposta, quindi si scrive
        // su disco subito e non con il riversamento rimandato.
        Archivio.Fissa(voce);
        App.Attuale.AggiornaLista();
    }

    async void Menu_Modifica(object mittente, RoutedEventArgs args)
    {
        if (DaMenu(mittente) is { } riga) await Modifica(riga);
    }

    void Menu_Elimina(object mittente, RoutedEventArgs args)
    {
        if (DaMenu(mittente) is not { } riga) return;

        Archivio.Elimina(riga.Dato);
        App.Attuale.AggiornaLista();
    }

    // ---------------------------------------------------- intestazione

    void Pausa_Click(object mittente, RoutedEventArgs args)
    {
        Archivio.MettiPref("in_pausa", !Archivio.Pref("in_pausa", false));
        DisegnaPausa();
    }

    void Chiudi_Click(object mittente, RoutedEventArgs args) => Nascondi();

    async void Aspetto_Click(object mittente, RoutedEventArgs args)
    {
        var prima = new Dialoghi.Preferenze(
            Archivio.Pref("accento", Stile.AccentoDef) ?? Stile.AccentoDef,
            Archivio.Pref("tema", Stile.TemaDef) ?? Stile.TemaDef,
            Archivio.Pref("scorciatoia", Config.ScorciatoiaDef) ?? Config.ScorciatoiaDef,
            ModoCartelle,
            Archivio.Pref(Versioni.ChiaveAvvisa, Versioni.AvvisaDef));

        var scelto = await Dialoghi.Aspetto(Cornice.XamlRoot, prima);
        if (scelto is null) return;

        Archivio.MettiPref("accento", scelto.Accento);
        Archivio.MettiPref("tema", scelto.Tema);
        Archivio.MettiPref("cartelle", scelto.Cartelle);
        Archivio.MettiPref(Versioni.ChiaveAvvisa, scelto.AvvisaAggiornamenti);

        if (scelto.Scorciatoia != prima.Scorciatoia)
        {
            Archivio.MettiPref("scorciatoia", scelto.Scorciatoia);

            if (!App.Attuale.MettiScorciatoia(scelto.Scorciatoia))
            {
                Avvisa(Testi.Componi(
                    "%s è già usata da un altro programma. È rimasta la precedente.",
                    Config.Scorciatoie.GetValueOrDefault(scelto.Scorciatoia, scelto.Scorciatoia)));

                Archivio.MettiPref("scorciatoia", prima.Scorciatoia);
                App.Attuale.MettiScorciatoia(prima.Scorciatoia);
            }
        }

        ApplicaStile();
        Aggiorna();
    }
}
