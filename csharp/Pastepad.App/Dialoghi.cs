using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Pastepad.Nucleo;
using Windows.System;

namespace Pastepad.App;

/// <summary>
/// I dialoghi delle maquette da 04 a 09. Si costruiscono a mano e non con
/// i pulsanti di ContentDialog: le maquette chiedono pulsanti di 90x32
/// con raggio 10 e un rosso proprio per Elimina, e il piede di
/// ContentDialog non lascia arrivarci senza rifargli l'intero modello.
/// </summary>
public static class Dialoghi
{
    /// <summary>
    /// La casella di uno qualsiasi dei sei dialoghi.
    ///
    /// **Senza raggio proprio.** L'arrotondamento lo mette il modello del
    /// ContentDialog con OverlayCornerRadius. Mettere anche il nostro
    /// lasciava due curve diverse e, tra l'una e l'altra, spuntavano due
    /// cunei scuri negli angoli in alto: ciò che si vede lì è lo strato
    /// di fumo che il dialogo dipinge sopra il pannello. È lo stesso
    /// difetto già corretto nel pannello, e per lo stesso motivo: un solo
    /// arrotondamento, e lo mette il sistema.
    ///
    /// **Larghezza legata a quella del pannello.** Il dialogo vive dentro
    /// la finestra e non può uscirne. Fissando la larghezza qui, il
    /// contenuto può avere lo stesso riempimento ai due lati e resta
    /// centrato per costruzione, non per fortuna.
    /// </summary>
    static ContentDialog Casella(XamlRoot radice, UIElement contenuto)
    {
        var casella = new ContentDialog
        {
            XamlRoot = radice,
            Background = Stile.Pennello(Stile.Attuale.Elevato),
            BorderBrush = Stile.Pennello(Stile.Attuale.Bordo),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(0),
            // Default è seguire Windows; Light o Dark solo se l'utente
            // ha scelto uno sfondo apposta. Stesso criterio del
            // pannello.
            RequestedTheme = Stile.TemaRichiesto,
            Content = contenuto,
        };

        // 8 px di aria per lato perché si legga come un dialogo e non
        // come un'altra schermata. Con il minimo del pannello (300)
        // restano 284, che bastano ancora per le etichette.
        double larghezza = Math.Max(240, radice.Size.Width - 16);

        casella.Resources["ContentDialogMinWidth"] = larghezza;
        casella.Resources["ContentDialogMaxWidth"] = larghezza;

        // Anche l'altezza: il modello porta un limite suo e, con il
        // pannello allungato oltre quello, il dialogo restava corto e
        // tagliava in basso. Stesso criterio della larghezza: comanda
        // la finestra, che è dove il dialogo vive.
        casella.Resources["ContentDialogMaxHeight"] = Disponibile(radice);

        return casella;
    }

    /// <summary>
    /// L'altezza che un dialogo può occupare: quella del pannello meno
    /// l'aria dei bordi. Ciò che non ci sta non si vede e non si
    /// raggiunge.
    /// </summary>
    static double Disponibile(XamlRoot radice) => Math.Max(200, radice.Size.Height - 16);

    static TextBlock Titolo(string testo) => new()
    {
        Text = testo,
        FontSize = Stile.TTitolo,
        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        Foreground = Stile.Pennello(Stile.Attuale.Testo),
        Margin = new Thickness(0, 0, 0, Stile.E4),
    };

    static TextBlock Etichetta(string testo) => new()
    {
        Text = testo,
        FontSize = Stile.TMini,
        Foreground = Stile.Pennello(Stile.Attuale.Medio),
        Margin = new Thickness(0, 0, 0, Stile.E1),
    };

    static TextBlock Nota(string testo) => new()
    {
        Text = testo,
        FontSize = Stile.TMini,
        Foreground = Stile.Pennello(Stile.Attuale.Tenue),
        TextWrapping = TextWrapping.Wrap,
    };

    /// <summary>
    /// Un campo di testo con l'aspetto della tavolozza. Altezza
    /// esplicita: il minimo che WinUI dà a un TextBox di una linea è 32
    /// e le maquette chiedono 36 in quello di cartella e 32 in quelli
    /// dei campi.
    ///
    /// **Il testo si mette alla fine, e non dentro l'inizializzatore.**
    /// Un TextBox con AcceptsReturn a false si tiene la prima linea di
    /// ciò che gli si assegna, e nell'inizializzatore Text si assegnava
    /// prima di AcceptsReturn: aprire un salvato di cento linee
    /// nell'editor ne caricava una. Misurato: 7990 caratteri entravano
    /// come 77, e premere Salva scriveva quei 77 sopra i 7990.
    /// </summary>
    static TextBox Campo(string valore = "", int linee = 1, double altezza = 36)
    {
        bool una = linee <= 1;

        var casella = new TextBox
        {
            AcceptsReturn = !una,
            TextWrapping = una ? TextWrapping.NoWrap : TextWrapping.Wrap,
            FontSize = Stile.TCorpo,
            Foreground = Stile.Pennello(Stile.Attuale.Testo),
            Background = Stile.Pennello(Stile.Attuale.Riquadro),
            BorderBrush = Stile.Pennello(Stile.Attuale.Bordo),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, una ? 0 : 10, 12, 0),
            MinHeight = 0,
            Height = una ? altezza : double.NaN,
            VerticalContentAlignment = una
                ? VerticalAlignment.Center
                : VerticalAlignment.Top,
        };

        // La sottolineatura di focus la dipinge il modello del TextBox
        // con l'accento di **Windows**, non con il nostro. Misurato sulla
        // cattura dell'editor di cartella: 491 px di #F38064 —l'arancione
        // di sistema— sotto la casella, in un'applicazione il cui accento
        // era il menta #2DD4A7. La barra di ricerca del pannello si
        // incornicia già nel nostro; i campi dei dialoghi no.
        casella.Resources["TextControlBorderBrushFocused"] =
            Stile.Pennello(Stile.ColoreAccento.Color);

        if (!una)
        {
            casella.MinHeight = altezza;

            // La casella di più linee scorre al suo interno. Senza questo
            // cresceva con il testo fino a uscire dalla finestra: si vedeva
            // l'inizio del testo e nient'altro, e il resto non c'era modo
            // di raggiungerlo.
            ScrollViewer.SetVerticalScrollBarVisibility(casella, ScrollBarVisibility.Auto);
        }

        // Adesso sì: con AcceptsReturn già impostato, il testo entra intero.
        casella.Text = valore;

        return casella;
    }

    /// <summary>
    /// La casella di testo con formato.
    ///
    /// **Su carta bianca in tutti i temi, e non sul riquadro.** I colori
    /// di un salvato sono assoluti —quello di fabbrica è nero #000000—
    /// perché la sua destinazione è un'email, che si legge su bianco.
    /// Dipinti sullo sfondo scuro del pannello, il testo di fabbrica
    /// resterebbe nero su #1B1B1F: 1.1:1 di contrasto, cioè invisibile.
    /// Qui ciò che si vede è ciò che si incolla, che è il motivo di avere
    /// una barra di formato.
    /// </summary>
    static RichEditBox CasellaRicca()
    {
        var bianco = new SolidColorBrush(Microsoft.UI.Colors.White);

        var casella = new RichEditBox
        {
            // Senza FontSize. Misurato: con FontSize=13 il documento partiva
            // a 10, perché la dimensione del controllo va in pixel e quella
            // del documento in punti —13 px sono 9,75 pt— e la seconda si
            // teneva la prima. Chi comanda è il formato di carattere
            // predefinito, che è Calibri 11 pt, che è ciò che poi viaggia
            // verso Outlook.
            // Il carattere invece si dichiara qui, e non solo nel formato
            // di carattere predefinito del documento: misurato, il nome del
            // controllo vince su quello del documento e la tendina della
            // barra usciva vuota perché leggeva un carattere che non è
            // nessuno dei suoi.
            FontFamily = new FontFamily(Config.CarattereDef),
            Background = bianco,
            BorderBrush = Stile.Pennello(Stile.Attuale.Bordo),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 8, 10, 8),

            // Erano 180 fino alla 4.3.0. Chiedere il nome in tutti i
            // salvati e non solo nei link costa 51 px di altezza, e con
            // 180 qui il pulsante Salva restava mezzo sotto la piega nel
            // pannello di fabbrica: bisognava scorrere per salvare. Questi
            // 40 sono proprio ciò che serviva.
            //
            // La casella scorre al suo interno, quindi ciò che si perde
            // non è spazio per scrivere: è quanto se ne vede in una volta.
            MinHeight = 140,
            IsSpellCheckEnabled = false,
            TextWrapping = TextWrapping.Wrap,
            // Il cursore, la selezione e il menu del tasto destro li
            // dipinge il sistema secondo il tema richiesto, non secondo il
            // nostro sfondo: su carta bianca devono essere quelli chiari.
            RequestedTheme = ElementTheme.Light,
        };

        // Il modello porta i propri sfondi per stato e facevano
        // diventare grigia la carta al passaggio del mouse.
        foreach (var chiave in (string[])[
            "TextControlBackground",
            "TextControlBackgroundPointerOver",
            "TextControlBackgroundFocused"])
        {
            casella.Resources[chiave] = bianco;
        }

        casella.Resources["TextControlBorderBrushFocused"] =
            Stile.Pennello(Stile.ColoreAccento.Color);

        return casella;
    }

    static Button Pulsante(string testo, string stile, double larghezza = 90)
    {
        var b = new Button
        {
            Content = testo,
            Height = 32,
            MinHeight = 32,
            MinWidth = 0,
            Width = larghezza,
            CornerRadius = new CornerRadius(10),
            BorderThickness = new Thickness(0),
            FontSize = Stile.TMinore,
            Padding = new Thickness(Stile.E4, 0, Stile.E4, 0),
        };

        switch (stile)
        {
            case "accento":
                b.Background = Stile.Pennello(Stile.ColoreAccento.Color);
                b.Foreground = Stile.Pennello(Stile.ColoreAccento.Sopra);
                b.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
                break;

            case "pericolo":
                b.Background = Stile.Pennello(Stile.Pericolo);
                b.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
                b.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
                break;

            default:
                b.Background = Stile.Pennello(Stile.Attuale.Riquadro);
                b.Foreground = Stile.Pennello(Stile.Attuale.Testo);

                // Nelle tavolozze chiare il riquadro e lo sfondo elevato del
                // dialogo sono lo stesso bianco —"#FFFFFF" tutti e due nella
                // tavolozza Chiara—, quindi Annulla restava senza superficie
                // e si leggeva come testo sciolto accanto a un Salva pieno.
                // Il bordo è ciò che usano già i riquadri delle impostazioni
                // e le righe per questo stesso motivo.
                b.BorderBrush = Stile.Pennello(Stile.Attuale.Bordo);
                b.BorderThickness = new Thickness(Stile.EChiaro ? 1 : 0);
                break;
        }

        return b;
    }

    /// <summary>Annulla a sinistra, l'azione a destra.</summary>
    static Grid Piede(Button annulla, Button accetta)
    {
        var piede = new Grid { Margin = new Thickness(0, Stile.E4 + Stile.E2, 0, 0) };
        piede.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        piede.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        piede.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(annulla, 0);
        Grid.SetColumn(accetta, 2);

        piede.Children.Add(annulla);
        piede.Children.Add(accetta);
        return piede;
    }

    static StackPanel Corpo() => new()
    {
        Spacing = 0,
        Padding = new Thickness(24, 24, 24, 24),
    };

    /// <summary>
    /// Il corpo dei dialoghi che hanno qualcosa di grande dentro —una
    /// casella di testo, una lista di cartelle—. Va in griglia e non
    /// impilato apposta.
    ///
    /// **Impilato, il piede usciva dalla finestra.** La casella cresceva
    /// con il contenuto e spingeva in basso tutto ciò che veniva dopo:
    /// incollare trenta linee lasciava Annulla e Aggiungi fuori dallo
    /// schermo, e senza nessun posto da cui scorrere fino a loro —questi
    /// dialoghi non avevano ScrollViewer—. L'unica uscita era eliminare
    /// testo o premere Esc e perdere ciò che era incollato. Misurato con
    /// 60 linee: la casella occupava 447 px dei 567 del pannello e i due
    /// pulsanti non arrivavano a disegnarsi.
    ///
    /// Qui ciò che sta sopra e ciò che sta sotto occupano il loro e lo
    /// spazio che avanza è dell'elemento di mezzo, che scorre al suo
    /// interno. Il piede non si muove mai, che il testo abbia una linea
    /// o cento.
    /// </summary>
    static Grid CorpoConBuco(
        double altezza,
        IReadOnlyList<UIElement> sopra,
        FrameworkElement medio,
        IReadOnlyList<UIElement> sotto)
    {
        var griglia = new Grid
        {
            Padding = new Thickness(24, 24, 24, 24),
            MaxHeight = altezza,
        };

        void Metti(UIElement figlio, GridLength suo)
        {
            griglia.RowDefinitions.Add(new RowDefinition { Height = suo });
            Grid.SetRow((FrameworkElement)figlio, griglia.RowDefinitions.Count - 1);
            griglia.Children.Add(figlio);
        }

        foreach (var figlio in sopra) Metti(figlio, GridLength.Auto);

        // Il minimo dell'elemento si rispetta —una casella di una linea
        // non serve per incollare— ma la riga a stella lo taglia quando
        // non ci sta. Misurato con il pannello alla sua altezza minima,
        // 340: la casella si tiene ciò che avanza e il piede continua a
        // disegnarsi.
        medio.VerticalAlignment = VerticalAlignment.Stretch;
        Metti(medio, new GridLength(1, GridUnitType.Star));

        foreach (var figlio in sotto) Metti(figlio, GridLength.Auto);

        return griglia;
    }

    // ------------------------------------------------------ una linea

    /// <summary>
    /// Il dialogo della maquette 05: titolo, etichetta, un campo, e
    /// Annulla / OK. Restituisce null se è stato annullato.
    /// </summary>
    public static async Task<string?> UnaLinea(
        XamlRoot radice, string titolo, string etichetta, string valore = "")
    {
        var campo = Campo(valore);
        campo.SelectAll();

        var corpo = Corpo();
        corpo.Children.Add(Titolo(titolo));
        corpo.Children.Add(Etichetta(etichetta));
        corpo.Children.Add(campo);

        var annulla = Pulsante("Annulla", "normale");
        var accetta = Pulsante("OK", "accento");
        corpo.Children.Add(Piede(annulla, accetta));

        var dialogo = Casella(radice, corpo);

        string? uscita = null;

        annulla.Click += (_, _) => dialogo.Hide();
        accetta.Click += (_, _) => { uscita = campo.Text.Trim(); dialogo.Hide(); };

        // Invio accetta: scrivere il nome e premere Invio è il gesto
        // naturale in un dialogo di una sola linea.
        campo.KeyDown += (_, a) =>
        {
            if (a.Key != VirtualKey.Enter) return;
            a.Handled = true;
            uscita = campo.Text.Trim();
            dialogo.Hide();
        };

        dialogo.Opened += (_, _) => campo.Focus(FocusState.Programmatic);

        await dialogo.ShowAsync();

        return string.IsNullOrEmpty(uscita) ? null : uscita;
    }

    // ------------------------------------------------------- conferma

    /// <summary>La maquette 09: avviso, Annulla e Sì, elimina in rosso.</summary>
    public static async Task<bool> Conferma(XamlRoot radice, string messaggio)
    {
        var corpo = Corpo();
        corpo.Children.Add(Titolo("Conferma"));
        corpo.Children.Add(new TextBlock
        {
            Text = messaggio,
            FontSize = Stile.TMinore,
            Foreground = Stile.Pennello(Stile.Attuale.Testo),
            TextWrapping = TextWrapping.Wrap,
        });

        var annulla = Pulsante("Annulla", "normale");
        var elimina = Pulsante("Sì, elimina", "pericolo", 100);
        corpo.Children.Add(Piede(annulla, elimina));

        var dialogo = Casella(radice, corpo);

        bool si = false;

        annulla.Click += (_, _) => dialogo.Hide();
        elimina.Click += (_, _) => { si = true; dialogo.Hide(); };

        await dialogo.ShowAsync();
        return si;
    }

    // ---------------------------------------------------- testo nuovo

    /// <summary>
    /// La maquette 04: cartella, area di testo e la nota sui modelli.
    /// Restituisce null se è stato annullato.
    /// </summary>
    public static async Task<Snippet?> Testo(
        XamlRoot radice,
        string titolo,
        IReadOnlyList<string> cartelle,
        string cartella,
        Snippet? originale = null)
    {
        var scegliCartella = new ComboBox
        {
            MinWidth = 120,
            Height = 28,
            MinHeight = 28,
            CornerRadius = new CornerRadius(10),
            FontSize = Stile.TMinore,
            Padding = new Thickness(12, 0, 8, 0),
        };

        foreach (var c in cartelle) scegliCartella.Items.Add(c);

        // "I miei testi" è il nome della cartella che si crea, e
        // finisce scritto in snippets.json come categoria di ogni
        // testo. È un dato con valore iniziale, non una dicitura.
        if (cartelle.Count == 0)
        {
            scegliCartella.Items.Add(Config.CartellaDef);
            cartella = Config.CartellaDef;
        }

        scegliCartella.SelectedItem = cartelle.Contains(cartella)
            ? cartella
            : scegliCartella.Items[0];

        var riga = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = Stile.E3,
            Margin = new Thickness(0, 0, 0, Stile.E3),
        };
        riga.Children.Add(new TextBlock
        {
            Text = "Salva in",
            FontSize = Stile.TMini,
            Foreground = Stile.Pennello(Stile.Attuale.Medio),
            VerticalAlignment = VerticalAlignment.Center,
        });
        riga.Children.Add(scegliCartella);

        var casella = CasellaRicca();

        string testo = originale is null ? "" : Regole.TestoDi(originale.Runs);

        // Il nome. Si chiede per qualsiasi salvato, non solo per i
        // link, e lasciarlo in bianco si comporta come sempre: il
        // titolo esce dalla prima linea. Così chi salva due frasi non
        // riempie niente in più —che era il motivo per nasconderlo— e
        // chi costruisce una raccolta può distinguere i suoi pezzi.
        //
        // L'ha chiesto l'utente con un caso concreto: cinque corpi di
        // email che iniziano tutti con "Hi team," sono cinque righe
        // identiche tagliate a 80 caratteri. La cartella dice che sono
        // corpi; non dice quale è quale.
        var nome = Campo();
        nome.PlaceholderText = "Come vuoi chiamarlo";

        var etichettaNome = Etichetta("Nome");
        etichettaNome.VerticalAlignment = VerticalAlignment.Center;
        etichettaNome.Margin = new Thickness(0);

        // Di che tipo è. Si propone leggendo il testo —ciò che pastepad
        // faceva da solo e senza possibilità di discutere— e l'utente lo
        // cambia se non indovina. Il caso che lo richiedeva: il corpo di
        // un'email è testo normale, dentro non c'è niente che lo separi
        // da una nota, e senza poterlo dire cinque email erano cinque
        // note in più.
        var scegliTipo = new ComboBox
        {
            MinWidth = 112,
            Height = 28,
            MinHeight = 28,
            CornerRadius = new CornerRadius(10),
            FontSize = Stile.TMinore,
            Padding = new Thickness(12, 0, 8, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
        };

        foreach (var t in Tipi.Tutti) scegliTipo.Items.Add(DicituraTipo(t));

        // Condivide la linea con l'etichetta del nome, quindi non ha una
        // dicitura propria da leggere. Senza questo, un lettore di schermo
        // annuncia una tendina senza dire di cosa — e con «Segnalibro»
        // dentro, è ragionevole pensare che sia il nome. La cartella
        // accanto ha invece il suo «Salva in» davanti.
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(
            scegliTipo, "Tipo");

        // Il tipo va nella STESSA linea dell'etichetta del nome, e non
        // in una riga propria come la cartella. Una riga intera costa
        // circa 40 px di altezza e qui non avanzano: la 4.3.0 dovette
        // ridurre la casella di scrittura da 180 a 140 perché il pulsante
        // Salva non restasse mezzo sotto la piega. Condividere la linea
        // dell'etichetta costa la differenza tra una dicitura e una
        // tendina, che sono circa 13.
        var intestazioneNome = new Grid();
        intestazioneNome.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        intestazioneNome.ColumnDefinitions.Add(
            new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(scegliTipo, 1);
        intestazioneNome.Children.Add(etichettaNome);
        intestazioneNome.Children.Add(scegliTipo);

        var bloccoNome = new StackPanel
        {
            Margin = new Thickness(0, 0, 0, Stile.E3),
            Spacing = Stile.E1,
        };
        bloccoNome.Children.Add(intestazioneNome);
        bloccoNome.Children.Add(nome);

        // Un titolo che è la prima linea del testo stesso non è un
        // nome: l'ha messo il programma perché non ce n'era un altro.
        // Si offre in bianco perché un salvato che esiste già possa
        // averne uno, e pieno quando gliene hanno davvero dato uno.
        if (originale is not null
            && originale.Titolo != Regole.PrimaLinea(testo))
        {
            nome.Text = originale.Titolo;
        }

        // Finché l'utente non tocca la tendina, il tipo segue ciò che
        // si scrive: incollare un indirizzo lo mette su Segnalibro,
        // scrivere [[campi]] su Modello. Appena la tocca, smette di
        // muoversi — una proposta che si corregge da sola non è una scelta.
        bool loHaSceltoLUtente = Tipi.Vale(originale?.Tipo);
        bool mettendoloNoi = false;

        void MettiTipo(string tipo)
        {
            mettendoloNoi = true;
            scegliTipo.SelectedIndex = Math.Max(0, Array.IndexOf(Tipi.Tutti, tipo));
            mettendoloNoi = false;
        }

        scegliTipo.SelectionChanged += (_, _) =>
        {
            if (!mettendoloNoi) loHaSceltoLUtente = true;
        };

        string TipoScelto() =>
            Tipi.Tutti[Math.Max(0, scegliTipo.SelectedIndex)];

        // In un link il nome non è facoltativo in pratica: la prima
        // linea È l'indirizzo, e una lista di indirizzi non si cerca
        // per nome. L'etichetta lo dice quando serve.
        void RegolaEtichetta()
        {
            string semplice = Formato.TestoSemplice(casella).Trim();

            etichettaNome.Text = Regole.EUnLink(semplice)
                ? "Nome del segnalibro"
                : "Nome";

            if (!loHaSceltoLUtente) MettiTipo(Tipi.Deduci(semplice));
        }

        MettiTipo(originale is not null
            ? Tipi.Di(originale)
            : Tipi.Deduci(testo));

        RegolaEtichetta();
        casella.TextChanged += (_, _) => RegolaEtichetta();

        var annulla = Pulsante("Annulla", "normale");
        var salva = Pulsante("Salva", "accento", 94);

        // La griglia prende la sua altezza naturale SEMPRE, e a tagliare
        // pensa lo ScrollViewer qui sotto. Prima questo succedeva solo
        // sotto i 520 px di spazio, e sopra le si dava l'altezza
        // disponibile perché la griglia la ripartisse.
        //
        // Ripartire non funzionava. La casella ha 180 di minimo, quindi
        // la sua riga non poteva cedere nemmeno un pixel, e a cedere era
        // il PIEDE. Misurato sulla 4.2.0 già pubblicata, con il pannello
        // di fabbrica e un link scritto —che è quando compariva il campo
        // del nome—: Annulla e Salva in 19 px di altezza invece di 32.
        // Con il campo sempre visibile, quello passerebbe da caso raro a
        // caso normale.
        //
        // E ciò che si perdeva ripartendo non era niente: misurato a 560
        // e a 900 di pannello, la casella dava 180 in tutti e due. Non è
        // mai cresciuta con la finestra, anche se questo commento diceva
        // di sì.
        var corpo = CorpoConBuco(
            double.PositiveInfinity,
            [Titolo(titolo), riga, bloccoNome, BarraDiFormato(casella)],
            casella,
            [
                new Border { Height = Stile.E3, Background = null },
                Nota("Scrivi [[qualcosa]] e il programma te lo chiederà prima di incollare"),
                Piede(annulla, salva),
            ]);

        var dialogo = Casella(radice, new ScrollViewer
        {
            Content = corpo,
            MaxHeight = Disponibile(radice),
            // Hidden e non Auto, come in Aspetto: la barra resterebbe
            // sempre presente e la rotella e la tastiera continuano a scorrere.
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
        });

        Snippet? uscita = null;

        annulla.Click += (_, _) => dialogo.Hide();

        salva.Click += (_, _) =>
        {
            var runs = Formato.Leggi(casella);

            string valore = Regole.TestoDi(runs).Trim();
            if (valore.Length == 0) return;

            // Lo compone il nucleo perché finisce in snippets.json. Prima
            // si guardava se il campo era visibile, perché si nascondeva
            // quando smetteva di essere un link e ciò che era scritto
            // restava lì. Ora non si nasconde mai, quindi ciò che è
            // scritto è sempre ciò che ha voluto l'utente.
            string comeSiChiama = nome.Text;

            // Salvare senza aver toccato il testo non può cambiare il
            // titolo. Senza questo, aprire un salvato con nome proprio e
            // premere Salva se lo portava via e lo lasciava alla prima
            // linea del testo.
            if (comeSiChiama.Trim().Length == 0
                && originale is not null
                && Regole.NormalizzaAcapi(valore)
                   == Regole.NormalizzaAcapi(testo).Trim())
            {
                comeSiChiama = originale.Titolo;
            }

            uscita = Regole.CreaSnippet(
                runs,
                scegliCartella.SelectedItem as string ?? Config.CartellaDef,
                comeSiChiama,
                TipoScelto());

            dialogo.Hide();
        };

        // Il testo si carica **a dialogo già aperto**, non alla
        // costruzione della casella. Misurato: caricato prima, il salvato
        // usciva con i suoi grassetti ma senza i suoi colori —una linea
        // salvata in #C00000 si apriva in nero e si salvava in nero—,
        // perché il RichEditBox distribuisce il proprio Foreground nel
        // documento quando entra nell'albero visuale, e questo succede
        // dopo. È lo stesso tipo di trappola del TextBox con AcceptsReturn.
        dialogo.Opened += (_, _) =>
        {
            Formato.Carica(casella, originale?.Runs ?? []);
            RegolaEtichetta();
            casella.Focus(FocusState.Programmatic);
        };

        await dialogo.ShowAsync();
        return uscita;
    }

    /// <summary>
    /// Come si chiama ogni tipo a schermo. La costante che va nel file
    /// è un'altra cosa: è un dato, e cambiarla lascerebbe i salvati
    /// classificati con una parola che non si riconosce più, come
    /// succede con il nome della cartella di fabbrica.
    /// </summary>
    static string DicituraTipo(string tipo) => tipo switch
    {
        Tipi.Segnalibro => "Segnalibro",
        Tipi.Modello => "Modello",
        Tipi.Email => "Email",
        Tipi.Prompt => "Prompt IA",
        _ => "Nota",
    };

    // ------------------------------------------------ barra di formato

    /// <summary>
    /// I dieci colori della tavolozza di testo, verificati con il
    /// calcolo su carta bianca, che è dove finisce l'email: il peggiore
    /// dei dieci è l'oro #8A6D00 con 4.92:1, sopra il 4.5:1 che chiede
    /// WCAG AA per il testo normale. Il migliore è il nero, con 21:1.
    /// </summary>
    static readonly (string Hex, string Nome)[] Inchiostri =
    [
        ("#000000", "Nero"), ("#595959", "Grigio"), ("#C00000", "Rosso"),
        ("#B45309", "Arancione"), ("#8A6D00", "Oro"), ("#2E7D32", "Verde"),
        ("#0F766E", "Turchese"), ("#1F4E79", "Blu"), ("#6A1B9A", "Viola"),
        ("#7F4F24", "Marrone"),
    ];

    /// <summary>Un pulsante quadrato della barra.</summary>
    /// <summary>
    /// La barra corta, per un'annotazione del blocco: grassetto, corsivo,
    /// sottolineato, elenchi e rimuovi formattazione.
    ///
    /// Senza carattere, senza dimensione e senza colore apposta.
    /// Un'annotazione non esce da pastepad —non si incolla da nessuna
    /// parte— quindi scegliere la tipografia lì non serve a niente; ciò
    /// che serve è evidenziare l'importante e fare elenchi. La barra
    /// lunga resta dove conta, nei testi salvati, che viaggiano verso
    /// Word e Outlook.
    ///
    /// Il sottolineato qui C'È, anche se è stato tolto dalla barra dei
    /// salvati: lì fu tolto perché non aggiungeva niente a un testo che
    /// si incolla in un altro programma, e questo non si incolla in
    /// nessuno.
    /// </summary>
    public static FrameworkElement BarraDiAnnotazione(RichEditBox casella)
    {
        var grassetto = PulsanteBarra("Grassetto", Lettera("G", grassetto: true));
        grassetto.Click += (_, _) => Formato.Grassetto(casella);

        var corsivo = PulsanteBarra("Corsivo", Lettera("C", corsivo: true));
        corsivo.Click += (_, _) => Formato.Corsivo(casella);

        var sottolineato = PulsanteBarra(
            "Sottolineato", Lettera("S", sottolineato: true));
        sottolineato.Click += (_, _) => Formato.Sottolineato(casella);

        var puntiElenco = PulsanteBarra("Elenco puntato", Lettera("•", dim: 16));
        puntiElenco.Click += (_, _) => Formato.PuntiElenco(casella);

        var numeri = PulsanteBarra("Elenco numerato", Lettera("1.", dim: 11));
        numeri.Click += (_, _) => Formato.Numeri(casella);

        var pulisci = PulsanteBarra(
            "Rimuovi formattazione", Glifo(Stile.Icone.Scopa));
        pulisci.Click += (_, _) => Formato.Pulisci(casella);

        var barra = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 3,
        };

        foreach (var b in new[]
            { grassetto, corsivo, sottolineato, puntiElenco, numeri, pulisci })
        {
            barra.Children.Add(b);
        }

        return barra;
    }

    static Button PulsanteBarra(string dicitura, UIElement dentro)
    {
        var b = new Button
        {
            Content = dentro,
            // 26 e non 30: i nove pulsanti più la loro separazione devono
            // stare nei 264 px utili del dialogo con il pannello alla sua
            // larghezza di fabbrica. Misurato con 30 px, la barra chiedeva
            // 302 e l'ultimo pulsante —rimuovi formattazione— restava fuori
            // dalla vista. Con 26 e 3 di spazio sono 258 e ci stanno tutti e
            // nove. È anche la misura dei pulsanti del piede.
            Width = 26,
            Height = 26,
            MinWidth = 26,
            MinHeight = 26,
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(Stile.RControl),
            Background = Stile.Pennello(Stile.Attuale.Riquadro),
            BorderBrush = Stile.Pennello(Stile.Attuale.Bordo),
            BorderThickness = new Thickness(Stile.EChiaro ? 1 : 0),
        };

        ToolTipService.SetToolTip(b, dicitura);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(b, dicitura);

        return b;
    }

    /// <summary>
    /// La lettera di un pulsante della barra. G, C e S e non icone: sono
    /// le stesse lettere che usa Word in italiano, si leggono a
    /// qualsiasi dimensione e non dipendono dal fatto che un glifo
    /// esista nel carattere di icone —due volte si è infilata un'icona
    /// sbagliata per averla data per buona senza disegnarla—.
    /// </summary>
    static TextBlock Lettera(
        string testo, bool grassetto = false, bool corsivo = false,
        bool sottolineato = false, double dim = 14)
    {
        var t = new TextBlock
        {
            Text = testo,
            FontSize = dim,
            Foreground = Stile.Pennello(Stile.Attuale.Testo),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        if (grassetto) t.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
        if (corsivo) t.FontStyle = Windows.UI.Text.FontStyle.Italic;

        if (sottolineato)
            t.TextDecorations = Windows.UI.Text.TextDecorations.Underline;

        return t;
    }

    static TextBlock Glifo(string codice) => new()
    {
        Text = codice,
        FontFamily = new FontFamily("Segoe Fluent Icons"),
        FontSize = 15,
        Foreground = Stile.Pennello(Stile.Attuale.Testo),
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
    };

    /// <summary>
    /// La barra di formato della maquette chiesta dall'utente: carattere,
    /// dimensione, grassetto, corsivo, sottolineato, colore, punti
    /// elenco, numerazione, rientri e rimuovi formattazione.
    ///
    /// **Su due righe e con la seconda scorrevole.** Il pannello scende
    /// fino a 300 px di larghezza, che ne lasciano 236 utili dentro il
    /// dialogo, e undici controlli in una riga non ci stanno nemmeno da
    /// lontano. È la stessa uscita che usano già le tessere di cartella:
    /// si scorre in orizzontale invece di stringersi fino a non poterli
    /// premere.
    /// </summary>
    static FrameworkElement BarraDiFormato(RichEditBox casella)
    {
        bool sincronizzando = false;

        var caratteri = new ComboBox
        {
            Height = 32,
            MinHeight = 32,
            MinWidth = 110,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            CornerRadius = new CornerRadius(Stile.RControl),
            FontSize = Stile.TMinore,
        };

        foreach (var f in Formato.Caratteri) caratteri.Items.Add(f);
        caratteri.SelectedItem = Config.CarattereDef;
        ToolTipService.SetToolTip(caratteri, "Carattere");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(
            caratteri, "Carattere");

        var dimensioni = new ComboBox
        {
            Width = 64,
            Height = 32,
            MinHeight = 32,
            CornerRadius = new CornerRadius(Stile.RControl),
            FontSize = Stile.TMinore,
        };

        foreach (var t in Formato.Dimensioni) dimensioni.Items.Add(t);
        dimensioni.SelectedItem = Config.DimDef;
        ToolTipService.SetToolTip(dimensioni, "Dimensione");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(
            dimensioni, "Dimensione");

        caratteri.SelectionChanged += (_, _) =>
        {
            if (!sincronizzando && caratteri.SelectedItem is string n)
                Formato.Carattere(casella, n);
        };

        dimensioni.SelectionChanged += (_, _) =>
        {
            if (!sincronizzando && dimensioni.SelectedItem is int t)
                Formato.Dimensione(casella, t);
        };

        var sopra = new Grid { ColumnSpacing = Stile.E2 };
        sopra.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star),
        });
        sopra.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(dimensioni, 1);
        sopra.Children.Add(caratteri);
        sopra.Children.Add(dimensioni);

        // ------------------------------------------------ i pulsanti
        var grassetto = PulsanteBarra("Grassetto", Lettera("G", grassetto: true));
        grassetto.Click += (_, _) => Formato.Grassetto(casella);

        var corsivo = PulsanteBarra("Corsivo", Lettera("C", corsivo: true));
        corsivo.Click += (_, _) => Formato.Corsivo(casella);

        // Senza pulsante di sottolineato: l'ha tolto l'utente. Il supporto
        // di Formato e la chiave "u" del file RESTANO — un salvato che ce
        // l'ha già deve conservarlo aprendosi e salvandosi, e togliere la
        // lettura lo cancellerebbe in silenzio alla prima modifica. Ciò
        // che sparisce è il modo di metterlo, non di conservarlo.

        // La "A" con la sua fascia di colore sotto, come in Word: dice
        // insieme che è il colore del testo e quale è impostato.
        var fascia = new Border
        {
            Height = 3,
            Width = 14,
            CornerRadius = new CornerRadius(1),
            Background = Stile.Pennello(Inchiostri[0].Hex),
        };

        var campione = new StackPanel
        {
            Spacing = 1,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        campione.Children.Add(Lettera("A", dim: 13));
        campione.Children.Add(fascia);

        var colore = PulsanteBarra("Colore del testo", campione);
        colore.Flyout = InchiostriFlyout(casella, fascia);

        var puntiElenco = PulsanteBarra("Elenco puntato", Lettera("•", dim: 17));
        puntiElenco.Click += (_, _) => Formato.PuntiElenco(casella);

        var numeri = PulsanteBarra("Elenco numerato", Lettera("1.", dim: 12));
        numeri.Click += (_, _) => Formato.Numeri(casella);

        var meno = PulsanteBarra("Riduci rientro", Glifo(""));
        meno.Click += (_, _) => Formato.Rientro(casella, false);

        var piu = PulsanteBarra("Aumenta rientro", Glifo(""));
        piu.Click += (_, _) => Formato.Rientro(casella, true);

        var pulisci = PulsanteBarra(
            "Rimuovi formattazione", Glifo(Stile.Icone.Scopa));
        pulisci.Click += (_, _) => Formato.Pulisci(casella);

        var pulsanti = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 3,
        };

        foreach (var b in new[]
        {
            grassetto, corsivo, colore,
            puntiElenco, numeri, meno, piu, pulisci,
        })
        {
            pulsanti.Children.Add(b);
        }

        var sotto = new ScrollViewer
        {
            Content = pulsanti,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            HorizontalScrollMode = ScrollMode.Enabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollMode = ScrollMode.Disabled,
            Margin = new Thickness(0, Stile.E1, 0, 0),
        };

        // Le tendine seguono il cursore: con il cursore dentro una parola
        // in Arial 14, la barra non può continuare a dire Calibri 11 — è
        // la differenza tra informare e mentire.
        casella.SelectionChanged += (_, _) =>
        {
            var cf = Formato.Selezione(casella)?.CharacterFormat;
            if (cf is null) return;

            sincronizzando = true;

            try
            {
                string nome = cf.Name;

                caratteri.SelectedItem =
                    Formato.Caratteri.Contains(nome) ? nome : null;

                int dim = (int)Math.Round(cf.Size);
                dimensioni.SelectedItem =
                    Formato.Dimensioni.Contains(dim) ? dim : (object?)null;

                fascia.Background = Stile.Pennello(Formato.AHex(cf.ForegroundColor));
            }
            finally
            {
                sincronizzando = false;
            }
        };

        var tutto = new StackPanel { Margin = new Thickness(0, 0, 0, Stile.E3) };
        tutto.Children.Add(sopra);
        tutto.Children.Add(sotto);

        return tutto;
    }

    static Flyout InchiostriFlyout(RichEditBox casella, Border fascia)
    {
        var griglia = new VariableSizedWrapGrid
        {
            Orientation = Orientation.Horizontal,
            MaximumRowsOrColumns = 5,
        };

        var flyout = new Flyout
        {
            Content = griglia,
            Placement = FlyoutPlacementMode.Bottom,
        };

        foreach (var (hex, nome) in Inchiostri)
        {
            var pallino = new Button
            {
                Width = 28,
                Height = 28,
                MinWidth = 28,
                MinHeight = 28,
                Margin = new Thickness(0, 0, Stile.E1, Stile.E1),
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(14),
                Background = Stile.Pennello(hex),
                BorderBrush = Stile.Pennello(Stile.Attuale.Bordo),
                BorderThickness = new Thickness(1),
            };

            ToolTipService.SetToolTip(pallino, nome);
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(
                pallino, nome);

            string quale = hex;

            pallino.Click += (_, _) =>
            {
                Formato.Colore(casella, quale);
                fascia.Background = Stile.Pennello(quale);
                flyout.Hide();
            };

            griglia.Children.Add(pallino);
        }

        return flyout;
    }

    // --------------------------------------------------------- campi

    /// <summary>
    /// La maquette 06: un campo per ogni [[segnaposto]] del modello.
    /// Restituisce null se è stato annullato.
    /// </summary>
    public static async Task<Dictionary<string, string>?> Campi(
        XamlRoot radice, IReadOnlyList<string> campi)
    {
        var corpo = Corpo();
        corpo.Children.Add(Titolo("Completa prima di incollare"));

        var caselle = new Dictionary<string, TextBox>();

        foreach (var nome in campi)
        {
            corpo.Children.Add(Etichetta(nome));

            var casella = Campo(altezza: 32);
            casella.Margin = new Thickness(0, 0, 0, Stile.E3);

            caselle[nome] = casella;
            corpo.Children.Add(casella);
        }

        var annulla = Pulsante("Annulla", "normale");
        var incolla = Pulsante("Incolla", "accento", 82);
        corpo.Children.Add(Piede(annulla, incolla));

        var dialogo = Casella(radice, corpo);

        Dictionary<string, string>? uscita = null;

        annulla.Click += (_, _) => dialogo.Hide();

        incolla.Click += (_, _) =>
        {
            uscita = caselle.ToDictionary(p => p.Key, p => p.Value.Text);
            dialogo.Hide();
        };

        dialogo.Opened += (_, _) =>
        {
            if (caselle.Count > 0)
                caselle.Values.First().Focus(FocusState.Programmatic);
        };

        await dialogo.ShowAsync();
        return uscita;
    }

    // -------------------------------------------------- elenco in blocco

    /// <summary>
    /// La maquette 07: incollare più linee e creare una nota per
    /// ciascuna o una sola con tutto. Restituisce null se è stato
    /// annullato.
    /// </summary>
    public static async Task<List<string>?> Lista(XamlRoot radice, string cartella)
    {
        var casella = Campo(linee: 8, altezza: 150);

        var perLinea = new RadioButton
        {
            Content = "Una nota per ogni riga",
            IsChecked = true,
            FontSize = Stile.TMinore,
            MinHeight = 26,
        };

        var insieme = new RadioButton
        {
            Content = "Tutto insieme in una sola nota",
            FontSize = Stile.TMinore,
            MinHeight = 26,
        };

        var pulisci = new CheckBox
        {
            Content = "Togli numeri e punti elenco",
            IsChecked = true,
            FontSize = Stile.TMinore,
            MinHeight = 26,
        };

        var conteggio = new TextBlock
        {
            Text = Testi.Componi("%d note", 0),
            FontSize = Stile.TMinore,
            Foreground = Stile.Pennello(Stile.Attuale.Tenue),
            VerticalAlignment = VerticalAlignment.Center,
        };

        List<string> Spezzetta()
        {
            string grezzo = casella.Text;
            if (grezzo.Trim().Length == 0) return [];

            if (insieme.IsChecked == true) return [grezzo.Trim()];

            var uscita = new List<string>();

            // Le linee le divide il nucleo: ciò che restituisce il TextBox
            // arriva separato da \r e basta, e dividerlo per \n lasciava le
            // sessanta linee trasformate in una sola nota.
            foreach (var linea in Regole.LineeDi(grezzo))
            {
                string l = pulisci.IsChecked == true ? SenzaPuntoElenco(linea) : linea;
                if (l.Length > 0) uscita.Add(l);
            }

            return uscita;
        }

        void Riconta()
        {
            int n = Spezzetta().Count;
            conteggio.Text = Testi.Componi(n == 1 ? "%d nota" : "%d note", n);
        }

        casella.TextChanged += (_, _) => Riconta();
        perLinea.Checked += (_, _) => Riconta();
        insieme.Checked += (_, _) => Riconta();
        pulisci.Checked += (_, _) => Riconta();
        pulisci.Unchecked += (_, _) => Riconta();

        var annulla = Pulsante("Annulla", "normale");
        var aggiungi = Pulsante("Aggiungi", "accento", 86);

        var corpo = CorpoConBuco(
            Disponibile(radice),
            [Titolo(Testi.Componi("Aggiungi a %s", cartella))],
            casella,
            [
                new Border { Height = Stile.E3 },
                perLinea,
                insieme,
                pulisci,
                conteggio,
                Piede(annulla, aggiungi),
            ]);

        var dialogo = Casella(radice, corpo);

        List<string>? uscita = null;

        annulla.Click += (_, _) => dialogo.Hide();

        aggiungi.Click += (_, _) =>
        {
            var pezzetti = Spezzetta();
            if (pezzetti.Count == 0) return;

            uscita = pezzetti;
            dialogo.Hide();
        };

        dialogo.Opened += (_, _) => casella.Focus(FocusState.Programmatic);

        await dialogo.ShowAsync();
        return uscita;
    }

    /// <summary>Toglie "1. ", "- ", "• " e simili dall'inizio.</summary>
    static string SenzaPuntoElenco(string linea)
    {
        int i = 0;

        while (i < linea.Length && char.IsDigit(linea[i])) i++;

        if (i > 0 && i < linea.Length && (linea[i] == '.' || linea[i] == ')'))
            return linea[(i + 1)..].TrimStart();

        if (linea.Length > 1 && (linea[0] is '-' or '*' or '•' or '·'))
            return linea[1..].TrimStart();

        return linea;
    }

    // ------------------------------------------------------- cartelle

    /// <summary>
    /// Ciò che l'utente ha deciso su una cartella: come si chiama adesso
    /// e se va via. Nome è come si chiamava all'apertura del dialogo,
    /// che è da dove l'archivio la trova.
    /// </summary>
    public sealed record CambioCartella(string Nome, string Nuovo, bool Tolta);

    /// <summary>Un pulsante di sola icona, per le azioni di una riga.</summary>
    static Button IconaRiga(string glifo, string dicitura, string colore)
    {
        var b = new Button
        {
            Content = new TextBlock
            {
                Text = glifo,
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 14,
                Foreground = Stile.Pennello(colore),
            },
            Width = 32,
            Height = 32,
            MinWidth = 32,
            MinHeight = 32,
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(Stile.RControl),
            Background = Stile.Pennello(Stile.Attuale.Riquadro),
            BorderBrush = Stile.Pennello(Stile.Attuale.Bordo),
            // Come il pulsante secondario: in chiaro il riquadro è dello
            // stesso bianco del dialogo e senza bordo non si vede.
            BorderThickness = new Thickness(Stile.EChiaro ? 1 : 0),
        };

        ToolTipService.SetToolTip(b, dicitura);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(b, dicitura);

        return b;
    }

    /// <summary>
    /// Modifica le cartelle: rinominarle e togliere quelle di troppo,
    /// tutte nella stessa schermata.
    ///
    /// Esiste perché non c'era modo di arrivarci. Rinomina ed elimina
    /// stavano nell'elenco a discesa delle cartelle, ma solo per la
    /// cartella selezionata: con "Tutte le cartelle" —che è ciò che c'è
    /// all'apertura— non usciva nessuna delle due, e una volta scelta
    /// una bisogna riaprire l'elenco per vederle. L'utente lo raccontò
    /// come «le cartelle non hanno un pulsante di modifica».
    ///
    /// Niente si perde finché non si preme Salva: togliere segna la riga
    /// e si può annullare. È ciò che faceva la versione precedente, e un
    /// clic in più non costa niente.
    /// </summary>
    public static async Task<List<CambioCartella>?> Cartelle(
        XamlRoot radice, IReadOnlyList<(string Nome, int Quanti)> cartelle)
    {
        var campi = new List<(string Nome, TextBox Campo)>();
        var tolte = new HashSet<string>();

        var lista = new StackPanel { Spacing = Stile.E1 };

        foreach (var (nome, quanti) in cartelle)
        {
            var campo = Campo(nome, altezza: 32);
            campi.Add((nome, campo));

            var conteggio = new TextBlock
            {
                Text = Testi.Componi(quanti == 1 ? "%d testo" : "%d testi", quanti),
                FontSize = Stile.TMini,
                Foreground = Stile.Pennello(Stile.Attuale.Tenue),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(Stile.E2, 0, Stile.E2, 0),
            };

            var togli = IconaRiga(
                Stile.Icone.Cestino, "Togli", Stile.Rosso);

            var riga = new Grid();
            riga.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star),
            });
            riga.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            riga.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(conteggio, 1);
            Grid.SetColumn(togli, 2);

            riga.Children.Add(campo);
            riga.Children.Add(conteggio);
            riga.Children.Add(togli);

            string quale = nome;

            togli.Click += (_, _) =>
            {
                bool fuori = !tolte.Remove(quale);
                if (fuori) tolte.Add(quale);

                campo.IsEnabled = !fuori;
                riga.Opacity = fuori ? 0.45 : 1.0;

                if (togli.Content is TextBlock icona)
                {
                    icona.Text = fuori
                        ? Stile.Icone.AnnullaModifica
                        : Stile.Icone.Cestino;

                    icona.Foreground = Stile.Pennello(
                        fuori ? Stile.Attuale.Medio : Stile.Rosso);
                }

                ToolTipService.SetToolTip(
                    togli, fuori ? "Ripristina" : "Togli");
            };

            lista.Children.Add(riga);
        }

        if (cartelle.Count == 0)
            lista.Children.Add(Nota("Non ci sono ancora cartelle."));

        var annulla = Pulsante("Annulla", "normale");
        var salva = Pulsante("Salva", "accento", 94);

        // La lista va dentro lo spazio che avanza: con venti cartelle
        // scorre al suo interno e il piede resta dov'era. La barra, in
        // Hidden come nel resto dei dialoghi; misurato, la rotella e la
        // tastiera continuano a scorrere.
        var scorrimento = new ScrollViewer
        {
            Content = lista,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
        };

        var corpo = CorpoConBuco(
            Disponibile(radice),
            [Titolo("Cartelle")],
            scorrimento,
            [
                new Border { Height = Stile.E3 },
                Nota("Salvando si eliminano le cartelle tolte "
                            + "e i testi che contengono."),
                Piede(annulla, salva),
            ]);

        var dialogo = Casella(radice, corpo);

        List<CambioCartella>? uscita = null;

        annulla.Click += (_, _) => dialogo.Hide();

        salva.Click += (_, _) =>
        {
            var cambi = new List<CambioCartella>();

            foreach (var (nome, campo) in campi)
            {
                bool fuori = tolte.Contains(nome);
                string nuovo = campo.Text.Trim();

                // Un nome in bianco non elimina la cartella: quello si
                // chiede con il cestino, che avvisa di ciò che si porta
                // via.
                if (nuovo.Length == 0) nuovo = nome;

                if (!fuori && nuovo == nome) continue;

                cambi.Add(new CambioCartella(nome, nuovo, fuori));
            }

            uscita = cambi;
            dialogo.Hide();
        };

        await dialogo.ShowAsync();
        return uscita;
    }

    /// <summary>
    /// L'intera cartella in una sola casella, una nota per linea,
    /// caricata con ciò che c'è già. Restituisce il testo che l'utente
    /// lascia, o null se è stato annullato.
    ///
    /// Sostituisce la lista con una matita per nota della 4.0.1, che era
    /// ciò che era stato capito male: con 3000 note, toglierne cento una
    /// alla volta sono trecento clic. Qui è lo stesso gesto di "Aggiungi
    /// a questa cartella" —la casella che l'utente conosce già— ma
    /// precaricata.
    ///
    /// **Senza le opzioni di "Aggiungi".** Lì si sceglie tra una nota per
    /// linea e tutto insieme in una sola; qui "tutto insieme"
    /// trasformerebbe 3000 note in una e non c'è modo di annullarlo. E
    /// togliere numeri e punti elenco serve per materiale appena
    /// incollato da fuori, non per ciò che il programma stesso ha
    /// salvato.
    ///
    /// La conferma va in ciclo e non annidata: due ContentDialog non
    /// possono essere aperti insieme, quindi questo si chiude, chiede,
    /// e se l'utente ci ripensa si riapre **con ciò che aveva scritto**.
    /// Perdere cento modifiche per aver detto di no sarebbe lo stesso
    /// errore che si veniva a sistemare.
    /// </summary>
    public static async Task<string?> ModificaCartella(
        XamlRoot radice, string cartella, Regole.CartellaInLinee prima)
    {
        string valore = prima.Testo;

        while (true)
        {
            var (testo, seNeVanno) = await PassataCartella(radice, cartella, prima, valore);

            if (testo is null) return null;
            if (seNeVanno == 0) return testo;

            string avviso = Testi.Componi(
                seNeVanno == 1
                    ? "Salvare? Verrà eliminata %d nota da %s. Non si può annullare."
                    : "Salvare? Verranno eliminate %d note da %s. Non si può annullare.",
                seNeVanno, cartella);

            if (await Conferma(radice, avviso)) return testo;

            valore = testo;
        }
    }

    /// <summary>
    /// Un'apertura dell'editor di cartella. Restituisce ciò che è scritto
    /// e quante note spariranno, o null se è stato annullato.
    /// </summary>
    static async Task<(string? Testo, int SeNeVanno)> PassataCartella(
        XamlRoot radice, string cartella, Regole.CartellaInLinee prima, string valore)
    {
        var casella = Campo(valore, linee: 10, altezza: 200);

        var conteggio = new TextBlock
        {
            FontSize = Stile.TMinore,
            Foreground = Stile.Pennello(Stile.Attuale.Tenue),
        };

        // Il numero di eliminazioni va a parte e in rosso. È il dato per
        // cui questo dialogo può fare danni: salvare qui elimina di colpo
        // tutto ciò che non è più scritto, e va visto prima di premere,
        // non dopo.
        var basse = new TextBlock
        {
            FontSize = Stile.TMinore,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = Stile.Pennello(Stile.Rosso),
            TextWrapping = TextWrapping.Wrap,
            Visibility = Visibility.Collapsed,
        };

        var annulla = Pulsante("Annulla", "normale");
        var salva = Pulsante("Salva", "accento", 94);

        int seNeVanno = 0;

        void Riconta()
        {
            var f = Regole.FondiCartella(prima, casella.Text, cartella);

            seNeVanno = f.Tolte.Count;

            conteggio.Text = Testi.Componi(
                f.Risultato.Count == 1 ? "%d nota" : "%d note", f.Risultato.Count);

            basse.Visibility = seNeVanno == 0 ? Visibility.Collapsed : Visibility.Visible;

            if (seNeVanno > 0)
            {
                basse.Text = Testi.Componi(
                    seNeVanno == 1
                        ? "Salvando verrà eliminata %d nota"
                        : "Salvando verranno eliminate %d note",
                    seNeVanno);
            }

            // E il pulsante diventa rosso quando salvare elimina: è lo stesso
            // segnale che ha già "Sì, elimina", senza dipendere dalla lettura.
            salva.Background = Stile.Pennello(
                seNeVanno > 0 ? Stile.Pericolo : Stile.ColoreAccento.Color);

            salva.Foreground = seNeVanno > 0
                ? new SolidColorBrush(Microsoft.UI.Colors.White)
                : Stile.Pennello(Stile.ColoreAccento.Sopra);
        }

        Riconta();
        casella.TextChanged += (_, _) => Riconta();

        var sotto = new List<UIElement>
        {
            new Border { Height = Stile.E3 },
            Nota("Una nota per riga. Salvando, la cartella "
                        + "resta come la lasci qui."),
        };

        // Solo quando ce ne sono: una nota a piè di pagina che dice
        // quasi sempre "zero" è rumore che si smette di leggere.
        if (prima.DiPiuLinee.Count > 0)
        {
            sotto.Add(Nota(Testi.Componi(
                "Le note di più righe (%d) restano come sono.",
                prima.DiPiuLinee.Count)));
        }

        sotto.Add(conteggio);
        sotto.Add(basse);
        sotto.Add(Piede(annulla, salva));

        var corpo = CorpoConBuco(
            Disponibile(radice),
            [Titolo(Testi.Componi("Contenuto di %s", cartella))],
            casella,
            sotto);

        var dialogo = Casella(radice, corpo);

        string? uscita = null;

        annulla.Click += (_, _) => dialogo.Hide();

        salva.Click += (_, _) =>
        {
            uscita = casella.Text;
            dialogo.Hide();
        };

        dialogo.Opened += (_, _) => casella.Focus(FocusState.Programmatic);

        await dialogo.ShowAsync();

        return (uscita, seNeVanno);
    }

    // --------------------------------------------------------- blocco

    /// <summary>
    /// Scrivere o ritoccare un'annotazione. Restituisce null se è stato
    /// annullato, e la stringa vuota non si distingue: salvare
    /// un'annotazione in bianco non ha senso, quindi il pulsante si
    /// spegne finché non c'è niente.
    ///
    /// È apposta il dialogo più semplice del programma: senza cartella,
    /// senza tipo, senza nome e senza barra di formato. Un'annotazione
    /// non viaggia da nessuna parte —non si incolla— quindi non ha
    /// bisogno di niente di ciò che quelle cose risolvono. È il blocco
    /// note, non un salvato.
    /// </summary>
    public static async Task<string?> Annotazione(
        XamlRoot radice, string titolo, string valore)
    {
        var casella = Campo(valore, linee: 10, altezza: 200);

        var annulla = Pulsante("Annulla", "normale");
        var salva = Pulsante("Salva", "accento", 94);

        void Riconta() =>
            salva.IsEnabled = casella.Text.Trim().Length > 0;

        Riconta();
        casella.TextChanged += (_, _) => Riconta();

        var corpo = CorpoConBuco(
            Disponibile(radice),
            [Titolo(titolo)],
            casella,
            [
                new Border { Height = Stile.E3 },
                Nota("Le note restano qui: non si incollano."),
                Piede(annulla, salva),
            ]);

        var dialogo = Casella(radice, corpo);

        string? uscita = null;

        annulla.Click += (_, _) => dialogo.Hide();

        salva.Click += (_, _) =>
        {
            uscita = casella.Text;
            dialogo.Hide();
        };

        // A dialogo già aperto, come nell'editor di testo: chiedere il
        // focus prima che il controllo sia nell'albero visuale non fa
        // niente, e l'utente si ritrova a scrivere nel vuoto.
        dialogo.Opened += (_, _) =>
        {
            casella.Focus(FocusState.Programmatic);
            casella.SelectionStart = casella.Text.Length;
        };

        await dialogo.ShowAsync();

        return uscita;
    }

    // -------------------------------------------------------- aspetto

    public sealed record Preferenze(
        string Accento, string Tema, string Scorciatoia, string Cartelle,
        bool AvvisaAggiornamenti);

    /// <summary>
    /// Una riga di impostazione nello stile delle Impostazioni di
    /// Windows 11: dicitura sopra, controllo sotto, su un riquadro. È il
    /// linguaggio a cui somiglia il resto del pannello, e lascia leggere
    /// il dialogo dall'alto in basso senza cercare dove sta ogni cosa.
    ///
    /// **Sempre impilato, anche con il pannello largo.** Ci fu un
    /// tentativo di metterlo su due colonne quando ci stava, e fu
    /// ritirato dopo averlo misurato: la riga a due colonne non entra
    /// senza rompere qualcosa fino a un pannello di circa 455, molto
    /// sopra i 380 di fabbrica. Sotto, o la dicitura si spezza su due e
    /// tre linee, o il ComboBox taglia il suo valore **da sinistra** —"l
    /// + Shift + V" invece di "Ctrl + Shift + V"—, che è perdere il dato
    /// senza avvisare. Una sola disposizione in tutto l'intervallo
    /// 300-720 è anche un'intera classe di errori che non bisogna
    /// ricontrollare a sette larghezze diverse.
    /// </summary>
    static Border Impostazione(string dicitura, string? dettaglio, FrameworkElement controllo)
    {
        var testo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

        // La dicitura si spezza su più linee piuttosto che tagliarsi: con
        // il pannello alla larghezza minima, 300, la colonna
        // dell'etichetta resta a circa 70 px e "Scorciatoia per aprire"
        // non ci sta in un pezzo. Un'etichetta su due linee si legge; una
        // tagliata, no.
        testo.Children.Add(new TextBlock
        {
            Text = dicitura,
            FontSize = Stile.TCorpo,
            Foreground = Stile.Pennello(Stile.Attuale.Testo),
            TextWrapping = TextWrapping.WrapWholeWords,
        });

        if (dettaglio is not null)
        {
            testo.Children.Add(new TextBlock
            {
                Text = dettaglio,
                FontSize = Stile.TMini,
                Foreground = Stile.Pennello(Stile.Attuale.Medio),
                TextWrapping = TextWrapping.Wrap,
            });
        }

        // Il controllo si allarga a tutta la larghezza del riquadro,
        // quindi perde la larghezza fissa con cui arriva.
        controllo.Width = double.NaN;
        controllo.HorizontalAlignment = HorizontalAlignment.Stretch;

        testo.Margin = new Thickness(0, 0, 0, Stile.E2);

        var dentro = new StackPanel();
        dentro.Children.Add(testo);
        dentro.Children.Add(controllo);

        return new Border
        {
            Background = Stile.Pennello(Stile.Attuale.Riquadro),
            BorderBrush = Stile.Pennello(Stile.Attuale.Bordo),
            BorderThickness = new Thickness(Stile.EChiaro ? 1 : 0),
            CornerRadius = new CornerRadius(Stile.RControl),
            Padding = new Thickness(Stile.E4, Stile.E3, Stile.E4, Stile.E3),
            Margin = new Thickness(0, 0, 0, Stile.E1),
            MinHeight = 52,
            Child = dentro,
        };
    }

    /// <summary>La dicitura di un gruppo di impostazioni.</summary>
    static TextBlock Sezione(string testo, bool laPrima = false) => new()
    {
        Text = testo,
        FontSize = Stile.TMinore,
        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        Foreground = Stile.Pennello(Stile.Attuale.Medio),
        Margin = new Thickness(0, laPrima ? 0 : Stile.E4 + Stile.E1, 0, Stile.E2),
    };

    /// <summary>
    /// 190 è ciò che chiede il valore più lungo che può comparire
    /// dentro, "Ctrl + Shift + Spazio", con la sua freccia e il suo
    /// riempimento.
    /// </summary>
    static ComboBox Tendina(int larghezza = 190) => new()
    {
        Width = larghezza,
        Height = 32,
        MinHeight = 32,
        CornerRadius = new CornerRadius(Stile.RControl),
        FontSize = Stile.TMinore,
        VerticalAlignment = VerticalAlignment.Center,
    };

    /// <summary>
    /// La maquette 08, riorganizzata. Tre gruppi, nell'ordine in cui si
    /// toccano: ciò che si vede, come si ordinano le cartelle, e ciò che
    /// dipende dal sistema.
    ///
    /// Non c'è più il selettore di dimensione: il pannello si allarga
    /// trascinandone i bordi e se lo ricorda da solo, quindi delle
    /// preimpostazioni fisse accanto a qualcosa che già si adatta sono
    /// di troppo e confondono.
    /// </summary>
    public static async Task<Preferenze?> Aspetto(
        XamlRoot radice, Preferenze attuale)
    {
        var scelto = attuale;

        var corpo = Corpo();

        corpo.Children.Add(Titolo("Aspetto"));

        // -------------------------------------------------------- colori
        corpo.Children.Add(Sezione("Colore di accento", laPrima: true));

        var pallini = new VariableSizedWrapGrid
        {
            Orientation = Orientation.Horizontal,
            MaximumRowsOrColumns = 9,
            Margin = new Thickness(0, 0, 0, Stile.E1),
        };

        var segni = new Dictionary<string, TextBlock>();

        foreach (var (nome, coppia) in Stile.Accenti)
        {
            var segno = new TextBlock
            {
                Text = Stile.Icone.SegnoSpunta,
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 13,
                Foreground = Stile.Pennello(coppia.Sopra),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = nome == attuale.Accento
                    ? Visibility.Visible
                    : Visibility.Collapsed,
            };

            segni[nome] = segno;

            var pallino = new Button
            {
                Width = 34,
                Height = 34,
                MinWidth = 34,
                MinHeight = 34,
                Margin = new Thickness(0, 0, Stile.E2, Stile.E2),
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(17),
                BorderThickness = new Thickness(0),
                Background = Stile.Pennello(coppia.Color),
                Content = segno,
                Tag = nome,
            };

            // Il suggerimento e il nome accessibile, dallo stesso testo: un
            // pallino di colore senza dicitura non si può scegliere a orecchio.
            ToolTipService.SetToolTip(pallino, nome);
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(pallino, nome);

            pallino.Click += (r, _) =>
            {
                if (r is not Button b || b.Tag is not string quale) return;

                foreach (var m in segni) m.Value.Visibility = Visibility.Collapsed;
                segni[quale].Visibility = Visibility.Visible;

                scelto = scelto with { Accento = quale };
            };

            pallini.Children.Add(pallino);
        }

        corpo.Children.Add(pallini);

        var sfondi = Tendina();

        foreach (var chiave in Stile.Temi.Keys)
            sfondi.Items.Add(Stile.NomeTema(chiave));

        sfondi.SelectedItem = Stile.NomeTema(attuale.Tema);

        sfondi.SelectionChanged += (_, _) =>
        {
            foreach (var chiave in Stile.Temi.Keys)
            {
                if (Stile.NomeTema(chiave)
                    == sfondi.SelectedItem as string)
                {
                    scelto = scelto with { Tema = chiave };
                    return;
                }
            }
        };

        corpo.Children.Add(Impostazione(
            "Sfondo",
            null,
            sfondi));

        // ------------------------------------------------------ cartelle
        corpo.Children.Add(Sezione("Cartelle"));

        var modi = Tendina();
        modi.Items.Add("Elenco a discesa");
        modi.Items.Add("Etichette in fila");
        modi.SelectedIndex = attuale.Cartelle == "tessere" ? 1 : 0;

        modi.SelectionChanged += (_, _) =>
            scelto = scelto with
            {
                Cartelle = modi.SelectedIndex == 1 ? "tessere" : "menu",
            };

        corpo.Children.Add(Impostazione(
            "Come si mostrano",
            null,
            modi));

        // ------------------------------------------------------- sistema
        corpo.Children.Add(Sezione("Sistema"));

        var scorciatoie = Tendina();

        foreach (var coppia in Config.Scorciatoie) scorciatoie.Items.Add(coppia.Value);

        scorciatoie.SelectedItem = Config.Scorciatoie.TryGetValue(attuale.Scorciatoia, out var leggibile)
            ? leggibile
            : Config.Scorciatoie[Config.ScorciatoiaDef];

        scorciatoie.SelectionChanged += (_, _) =>
        {
            foreach (var coppia in Config.Scorciatoie)
            {
                if (coppia.Value == scorciatoie.SelectedItem as string)
                {
                    scelto = scelto with { Scorciatoia = coppia.Key };
                    return;
                }
            }
        };

        corpo.Children.Add(
            Impostazione("Scorciatoia per aprire", null, scorciatoie));

        // L'interruttore dell'avviso di versioni, visibile dal primo
        // giorno in cui esiste l'avviso. Un programma che chiama casa
        // senza che si possa dire di no è un programma che non rispetta
        // chi lo usa, e il dettaglio dice dove chiama.
        var aggiornamenti = new ToggleSwitch
        {
            IsOn = attuale.AvvisaAggiornamenti,
            OnContent = null,
            OffContent = null,
            MinWidth = 0,
            VerticalAlignment = VerticalAlignment.Center,
        };

        aggiornamenti.Toggled += (_, _) =>
            scelto = scelto with { AvvisaAggiornamenti = aggiornamenti.IsOn };

        corpo.Children.Add(Impostazione(
            "Avvisa delle nuove versioni",
            "Controlla una volta al giorno su GitHub",
            aggiornamenti));

        var annulla = Pulsante("Annulla", "normale");
        var applica = Pulsante("Applica", "accento", 94);
        corpo.Children.Add(Piede(annulla, applica));

        // Hidden e non Auto: con Auto la barra resta presente finché il
        // contenuto non ci sta, cioè sempre in questo dialogo. Misurato:
        // era ancora lì 5,5 s dopo l'apertura, senza toccare niente.
        // Provato anche con ScrollView, il controllo nuovo, ed è peggio:
        // oltre a restare, la sua barra occupa larghezza reale e
        // restringeva il contenuto.
        //
        // Con Hidden **lo scorrimento si conserva** —rotella, tastiera e
        // tocco continuano a funzionare, lo dice la tabella di visibilità
        // della documentazione— e l'unica cosa che sparisce è la barra.
        //
        // L'altezza si lega a quella del pannello e non a un numero
        // fisso: il dialogo vive dentro la finestra e non può essere più
        // alto di lei.
        var dialogo = Casella(radice, new ScrollViewer
        {
            Content = corpo,
            MaxHeight = Math.Max(200, radice.Size.Height - 16),
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
        });

        Preferenze? uscita = null;

        annulla.Click += (_, _) => dialogo.Hide();
        applica.Click += (_, _) => { uscita = scelto; dialogo.Hide(); };

        await dialogo.ShowAsync();
        return uscita;
    }
}
