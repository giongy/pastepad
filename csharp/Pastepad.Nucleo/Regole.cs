namespace Pastepad.Nucleo;

/// <summary>
/// Le regole dei dati, senza niente di interfaccia. Tutto ciò che sta
/// qui si può provare senza aprire una finestra.
/// </summary>
public static class Regole
{
    public static Frammento CreaFrammento(
        string testo,
        string? carattere = null,
        int? dim = null,
        int grassetto = 0,
        int corsivo = 0,
        int sottolineato = 0,
        string? colore = null) => new()
        {
            T = testo,
            F = carattere ?? Config.CarattereDef,
            S = dim ?? Config.DimDef,
            B = grassetto,
            I = corsivo,
            U = sottolineato,
            C = colore ?? Config.ColoreDef,
        };

    public static string TestoDi(IEnumerable<Frammento> frammenti) =>
        string.Concat(frammenti.Select(f => f.T));

    /// <summary>
    /// I due caratteri con cui un testo può arrivare diviso.
    ///
    /// Il TextBox di WinUI restituisce l'a capo come \r e basta
    /// —verificato leggendo lo snippets.json scritto dal programma—,
    /// mentre ciò che arriva dagli appunti porta \r\n. Dividere solo per
    /// \n lasciava le sessanta linee attaccate in una.
    /// </summary>
    static readonly char[] Acapi = ['\r', '\n'];

    /// <summary>
    /// Le linee con qualcosa di scritto, comunque arrivi diviso il testo.
    /// Quelle vuote cadono: sono separazione, non contenuto.
    /// </summary>
    public static List<string> LineeDi(string testo)
    {
        if (string.IsNullOrEmpty(testo)) return [];

        var uscita = new List<string>();

        foreach (var linea in testo.Split(Acapi))
        {
            string pulito = linea.Trim();
            if (pulito.Length > 0) uscita.Add(pulito);
        }

        return uscita;
    }

    /// <summary>
    /// Porta tutti gli a capo a \r\n, che è come li aspetta Windows.
    ///
    /// Ciò che esce dal TextBox porta \r e basta e così si salvava in
    /// snippets.json; incollato nel Blocco note, un testo di tre linee
    /// usciva su una sola.
    /// </summary>
    public static string NormalizzaAcapi(string testo) =>
        string.IsNullOrEmpty(testo)
            ? testo
            : testo.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\r\n");

    /// <summary>
    /// Una nuova annotazione per il blocco.
    ///
    /// La data entra come parametro e non si legge dall'orologio qui:
    /// così si può provare senza dipendere da che ora è, che è lo stesso
    /// motivo per cui il resto del nucleo non tocca l'orologio.
    ///
    /// Il formato è quello di andata e ritorno di .NET —«O»—, che è
    /// ISO-8601 e ordina allo stesso modo in senso alfabetico e
    /// cronologico. Non serve passargli una cultura: la documentazione
    /// dice che «because the "O" or "o" standard format specifier
    /// conforms to an international standard, the formatting or parsing
    /// operation that uses the specifier always uses the invariant
    /// culture». Con qualsiasi altro —«d», «G»— il file si scriverebbe
    /// diverso in ogni regione.
    /// </summary>
    public static Nota CreaNota(
        string testo, DateTimeOffset quando, string titolo = "") =>
        CreaNota([CreaFrammento(testo)], quando, titolo);

    public static Nota CreaNota(
        IReadOnlyList<Frammento> runs, DateTimeOffset quando,
        string titolo = "") => new()
    {
        Titolo = titolo.Trim(),
        Runs = PulisciRuns(runs),
        Modificata = Timbro(quando),
    };

    /// <summary>
    /// Come si chiama un'annotazione nella lista: il nome che le ha dato
    /// l'utente se ce l'ha, altrimenti la sua prima linea, come nelle
    /// note rapide di Windows.
    /// </summary>
    public static string NomeDi(Nota annotazione) =>
        annotazione.Titolo.Length > 0
            ? annotazione.Titolo
            : PrimaLinea(TestoDi(annotazione.Runs));

    /// <summary>
    /// Una data così come si scrive nel file. In un solo posto perché
    /// creare un'annotazione e ritoccarla non possano usare formati
    /// diversi, cosa che lascerebbe la lista ordinata a caso.
    /// </summary>
    public static string Timbro(DateTimeOffset quando) => quando.ToString("O");

    /// <summary>
    /// Le prime linee di un'annotazione, per vederla nel suo riquadro
    /// senza aprirla.
    ///
    /// Non è <see cref="UnaLinea"/>: una nota rapida non sta in una
    /// riga, e schiacciarla in una lascia riquadri che iniziano tutti
    /// uguali —«Security details», «Security details»— senza niente che
    /// li distingua. Qui si conservano gli a capo.
    ///
    /// Le linee vuote dell'inizio si saltano: un'annotazione che
    /// comincia con un buco non può sprecare la sua prima linea visibile
    /// per niente.
    /// </summary>
    public static string Anteprima(string testo, int linee = 6, int limite = 240)
    {
        if (string.IsNullOrWhiteSpace(testo)) return "";

        var sciolte = NormalizzaAcapi(testo).Split("\r\n");

        int i = 0;
        while (i < sciolte.Length && sciolte[i].Trim().Length == 0) i++;

        var escono = sciolte.Skip(i).Take(linee).Select(l => l.TrimEnd()).ToList();

        // Un buco alla fine di ciò che si vede è una linea sprecata.
        while (escono.Count > 0 && escono[^1].Length == 0)
            escono.RemoveAt(escono.Count - 1);

        var vista = string.Join('\n', escono);

        // Il taglio per caratteri è la seconda rete: sei linee da mille
        // caratteri ciascuna continuano a non stare in un riquadro.
        if (vista.Length > limite)
            vista = vista[..limite].TrimEnd() + "…";

        return vista;
    }

    /// <summary>
    /// La prima linea con qualcosa di scritto, con gli spazi interni
    /// normalizzati. Non taglia e non aggiunge: ciò che restituisce è
    /// contenuto tale e quale nel testo dell'utente.
    ///
    /// È quella che vale per SALVARE. Per lo schermo c'è
    /// <see cref="UnaLinea"/>, che segna il taglio con i puntini di
    /// sospensione — e quei puntini non possono finire in un file.
    /// </summary>
    public static string PrimaLinea(string testo)
    {
        if (string.IsNullOrEmpty(testo)) return "";

        foreach (var linea in testo.Split(Acapi))
        {
            string pulito = string.Join(" ", linea.Split(
                (char[]?)null, StringSplitOptions.RemoveEmptyEntries));

            if (pulito.Length > 0) return pulito;
        }

        return "";
    }

    /// <summary>
    /// Un nuovo salvato a partire da ciò che l'utente ha scritto.
    ///
    /// Vive qui e non in ogni dialogo perché il titolo è un dato che va
    /// in snippets.json e deve decidersi in un solo posto. Si salvava
    /// riassunto con <see cref="UnaLinea"/>, e il riassunto lasciava i
    /// puntini di sospensione DENTRO il file: chi leggeva il titolo
    /// trovava un testo che l'utente non aveva mai scritto. Accorciarlo
    /// perché stia nella riga è compito dell'interfaccia, che è l'unica
    /// a sapere quanto ci sta.
    ///
    /// Il testo si salva con gli a capo normalizzati per lo stesso
    /// motivo: è l'unico posto da cui passa ciò che l'utente ha scritto
    /// prima di arrivare al file.
    ///
    /// <paramref name="titolo"/> è per i segnalibri. In un testo la
    /// prima linea fa da titolo, ma in un link la prima linea È
    /// l'indirizzo, e una lista di indirizzi non si può cercare per
    /// nome. Vuoto o in bianco torna alla prima linea, che è il
    /// comportamento di sempre.
    /// </summary>
    public static Snippet CreaSnippet(
        string testo, string categoria, string? titolo = null,
        string? tipo = null)
    {
        string pulito = NormalizzaAcapi(testo);
        string nome = (titolo ?? "").Trim();

        return new Snippet
        {
            Titolo = nome.Length > 0 ? nome : PrimaLinea(pulito),
            Categoria = categoria,
            Runs = [CreaFrammento(pulito)],
            Tipo = Tipi.PerSalvare(tipo, pulito),
        };
    }

    /// <summary>
    /// Lo stesso, ma conservando il formato di ogni frammento. È da dove
    /// passa ciò che esce dall'editor con barra di formato.
    ///
    /// Il titolo si ricava dal testo completo, non dal primo frammento:
    /// mettere in grassetto le prime tre parole non può cambiare come si
    /// chiama il salvato.
    /// </summary>
    public static Snippet CreaSnippet(
        IReadOnlyList<Frammento> runs, string categoria, string? titolo = null,
        string? tipo = null)
    {
        var puliti = PulisciRuns(runs);

        string nome = (titolo ?? "").Trim();

        return new Snippet
        {
            Titolo = nome.Length > 0
                ? nome
                : PrimaLinea(TestoDi(puliti)),
            Categoria = categoria,
            Runs = puliti,
            Tipo = Tipi.PerSalvare(tipo, TestoDi(puliti)),
        };
    }

    /// <summary>
    /// Riassunto di una linea, da mostrare. Taglia prima di separare in
    /// parole: con testi di migliaia di linee, farlo al contrario costa
    /// quasi un secondo.
    /// </summary>
    public static string UnaLinea(string testo, int limite = 52)
    {
        if (string.IsNullOrEmpty(testo)) return "";

        int taglio = limite * 4;
        string crudo = testo.Length > taglio ? testo[..taglio] : testo;

        string pulito = string.Join(" ", crudo.Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (testo.Length > crudo.Length || pulito.Length > limite)
            return pulito[..Math.Min(limite, pulito.Length)] + "...";

        return pulito;
    }

    // ------------------------------------------------------------- link

    /// <summary>
    /// True se il testo è un indirizzo web e nient'altro. Solo se
    /// l'intero testo è il link: un paragrafo che menziona una url di
    /// passaggio non conta, perché aprirlo non sarebbe ciò che l'utente
    /// si aspetta facendo clic.
    /// </summary>
    public static bool EUnLink(string? testo)
    {
        if (string.IsNullOrEmpty(testo)) return false;

        string t = testo.Trim();

        // I due a capo: ciò che esce dal TextBox arriva diviso con \r e
        // basta, e un testo di più linee non è un link anche se la prima
        // lo sembra.
        if (t.Contains(' ') || t.IndexOfAny(Acapi) >= 0 || t.Length > 2000)
            return false;

        string basso = t.ToLowerInvariant();

        return basso.StartsWith("http://", StringComparison.Ordinal)
            || basso.StartsWith("https://", StringComparison.Ordinal)
            || basso.StartsWith("www.", StringComparison.Ordinal);
    }

    /// <summary>L'indirizzo pronto da aprire nel browser.</summary>
    public static string UrlDi(string testo)
    {
        string t = testo.Trim();

        return t.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? "https://" + t
            : t;
    }

    /// <summary>Il dominio da solo, da mostrare sotto il titolo.</summary>
    public static string DominioDi(string testo)
    {
        string t = UrlDi(testo);

        foreach (var prefisso in new[] { "https://", "http://" })
        {
            if (t.StartsWith(prefisso, StringComparison.Ordinal))
            {
                t = t[prefisso.Length..];
                break;
            }
        }

        if (t.StartsWith("www.", StringComparison.Ordinal))
            t = t[4..];

        string dominio = t.Split('/')[0];

        return dominio.Length > 60 ? dominio[..60] : dominio;
    }

    // ------------------------------------------ punti elenco e rientri

    /// <summary>
    /// Il punto elenco va **nel testo** e non come elenco dell'editor, ed
    /// è una decisione, non una scorciatoia.
    ///
    /// Un elenco vero è formato di paragrafo —ITextParagraphFormat
    /// .ListType—, e ciò che salva un <see cref="Frammento"/> è formato
    /// di carattere: carattere, dimensione, grassetto, corsivo,
    /// sottolineato e colore. Salvare elenchi obbligherebbe a cambiare il
    /// formato di snippets.json e a riscrivere il generatore di RTF, e in
    /// più lascerebbe senza risposta quale testo semplice produce un
    /// elenco incollando senza formato.
    ///
    /// Con il punto elenco dentro il testo, ciò che si vede è ciò che si
    /// salva, ciò che si incolla con formato e ciò che si incolla senza.
    /// E "Togli numeri e punti elenco" del dialogo degli elenchi sa già
    /// come disfarlo.
    /// </summary>
    public const string PuntoElenco = "• ";

    static string[] Linee(string blocco) => blocco.Split('\r');

    static bool ConPuntoElenco(string linea) =>
        linea.StartsWith(PuntoElenco, StringComparison.Ordinal);

    /// <summary>Il "12. " dell'inizio, o 0 se non c'è.</summary>
    static int LunghezzaDelNumero(string linea)
    {
        int i = 0;
        while (i < linea.Length && char.IsAsciiDigit(linea[i])) i++;

        if (i == 0 || i + 1 >= linea.Length) return 0;

        return linea[i] == '.' && linea[i + 1] == ' ' ? i + 2 : 0;
    }

    static string SenzaSegno(string linea) =>
        ConPuntoElenco(linea) ? linea[PuntoElenco.Length..]
                         : linea[LunghezzaDelNumero(linea)..];

    /// <summary>
    /// Mette i punti elenco alle linee del blocco, o li toglie se li
    /// hanno già tutte. Alterna e non solo mette: è ciò che fa il
    /// pulsante dei punti elenco in qualsiasi editor.
    /// </summary>
    public static string AlternaPuntiElenco(string blocco)
    {
        var linee = Linee(blocco);
        var conQualcosa = linee.Where(l => l.Trim().Length > 0).ToList();

        if (conQualcosa.Count == 0) return blocco;

        bool togli = conQualcosa.All(ConPuntoElenco);

        return string.Join('\r', linee.Select(l =>
            l.Trim().Length == 0 ? l : togli ? SenzaSegno(l) : PuntoElenco + SenzaSegno(l)));
    }

    /// <summary>Lo stesso, numerando da 1.</summary>
    public static string AlternaNumeri(string blocco)
    {
        var linee = Linee(blocco);
        var conQualcosa = linee.Where(l => l.Trim().Length > 0).ToList();

        if (conQualcosa.Count == 0) return blocco;

        bool togli = conQualcosa.All(l => LunghezzaDelNumero(l) > 0);

        int n = 0;

        return string.Join('\r', linee.Select(l =>
        {
            if (l.Trim().Length == 0) return l;
            if (togli) return SenzaSegno(l);

            n++;
            return $"{n}. " + SenzaSegno(l);
        }));
    }

    /// <summary>
    /// Una tabulazione all'inizio di ogni linea, o una in meno. Con il
    /// tabulatore e non con gli spazi perché il generatore di RTF lo
    /// converte già in \tab, così il rientro arriva uguale a Outlook e
    /// al Blocco note.
    /// </summary>
    public static string Rientra(string blocco, bool piu) =>
        string.Join('\r', Linee(blocco).Select(l =>
            l.Trim().Length == 0 ? l
            : piu ? "\t" + l
            : l.StartsWith('\t') ? l[1..] : l));

    // --------------------------------------------- la cartella in linee

    /// <summary>
    /// I salvati di una cartella divisi tra quelli che stanno in una
    /// linea e quelli che no.
    ///
    /// L'editor di cartella mostra una nota per linea, quindi una nota
    /// che ha già degli a capo dentro lì non si può rappresentare:
    /// dividerla la trasformerebbe in più note senza che nessuno l'abbia
    /// chiesto. Restano fuori dalla casella e fuori pericolo, e il
    /// dialogo dice quante sono.
    /// </summary>
    public sealed record CartellaInLinee(
        List<Snippet> DiUnaLinea, List<Snippet> DiPiuLinee)
    {
        /// <summary>Ciò che si carica nella casella: una nota per linea.</summary>
        public string Testo =>
            string.Join("\r\n", DiUnaLinea.Select(s => TestoDi(s.Runs).Trim()));
    }

    public static CartellaInLinee DividiCartella(IReadOnlyList<Snippet> dentro)
    {
        var alcune = new List<Snippet>();
        var varie = new List<Snippet>();

        foreach (var s in dentro)
        {
            string t = TestoDi(s.Runs).Trim();
            (t.IndexOfAny(Acapi) >= 0 ? varie : alcune).Add(s);
        }

        return new CartellaInLinee(alcune, varie);
    }

    /// <summary>
    /// Ciò che resterebbe nella cartella con il testo che c'è nella
    /// casella. <paramref name="Tolte"/> sono le note che spariscono: è
    /// il numero da mostrare prima di salvare.
    /// </summary>
    public sealed record FusioneCartella(
        List<Snippet> Risultato,
        List<Snippet> Tolte,
        int Conservate,
        int Nuove);

    /// <summary>
    /// Ricostruisce la cartella dal testo modificato, una nota per linea.
    ///
    /// **Le linee che non sono cambiate riusano la loro stessa nota**,
    /// l'oggetto tale e quale e non una copia. Con 3000 linee, rifarle
    /// tutte vorrebbe dire buttare il nome proprio di ogni segnalibro e
    /// il formato di ciascuna per sistemare le cento che l'utente ha
    /// toccato. Si crea una nota nuova solo per ciò che prima non c'era.
    ///
    /// L'abbinamento va per testo esatto e in coda: due note con lo
    /// stesso testo sono due note, e la seconda linea ripetuta deve
    /// tenersi la seconda nota, non di nuovo la prima.
    /// </summary>
    public static FusioneCartella FondiCartella(
        CartellaInLinee prima, string modificato, string cartella)
    {
        var perTesto = new Dictionary<string, Queue<Snippet>>(StringComparer.Ordinal);

        foreach (var s in prima.DiUnaLinea)
        {
            string t = TestoDi(s.Runs).Trim();

            if (!perTesto.TryGetValue(t, out var coda))
                perTesto[t] = coda = new Queue<Snippet>();

            coda.Enqueue(s);
        }

        var risultato = new List<Snippet>();
        int conservate = 0;
        int nuove = 0;

        foreach (var linea in LineeDi(modificato))
        {
            if (perTesto.TryGetValue(linea, out var coda) && coda.Count > 0)
            {
                risultato.Add(coda.Dequeue());
                conservate++;
                continue;
            }

            risultato.Add(CreaSnippet(linea, cartella));
            nuove++;
        }

        var tolte = perTesto.Values.SelectMany(c => c).ToList();

        // Quelle di più linee non sono mai state nella casella, quindi
        // non possono essere state eliminate lì. Tornano intatte e in
        // fondo.
        risultato.AddRange(prima.DiPiuLinee);

        return new FusioneCartella(risultato, tolte, conservate, nuove);
    }

    // ---------------------------------------------------------- modelli

    /// <summary>
    /// I [[campi]] di un modello, in ordine e senza ripetere.
    /// </summary>
    public static List<string> CampiDi(string testo)
    {
        var campi = new List<string>();
        string resto = testo;

        while (true)
        {
            int i = resto.IndexOf("[[", StringComparison.Ordinal);
            if (i < 0) break;

            int j = resto.IndexOf("]]", i, StringComparison.Ordinal);

            // La versione in Python non lo controllava ed esplodeva con un
            // testo come "]] [[x", dove la chiusura viene prima
            // dell'apertura. Qui semplicemente si smette di cercare.
            if (j < 0) break;

            string nome = resto[(i + 2)..j].Trim();

            if (nome.Length > 0 && !campi.Contains(nome))
                campi.Add(nome);

            resto = resto[(j + 2)..];
        }

        return campi;
    }

    /// <summary>Tutto tranne il testo.</summary>
    /// <summary>
    /// Lascia i frammenti come devono andare nel file: senza pezzetti
    /// vuoti, con gli a capo normalizzati, e unendo quelli consecutivi
    /// che condividono il formato.
    ///
    /// Quest'ultimo passo non è cosmetico: senza, scrivere carattere per
    /// carattere lasciava un frammento per ogni tasto. Un'annotazione di
    /// mille lettere sarebbero mille oggetti in note.json.
    ///
    /// Non restituisce mai la lista vuota: un contenuto senza niente
    /// resta comunque un frammento, e il resto del programma conta sul
    /// fatto che ce ne sia almeno uno.
    /// </summary>
    public static List<Frammento> PulisciRuns(IReadOnlyList<Frammento> runs)
    {
        var puliti = new List<Frammento>();

        foreach (var f in runs)
        {
            string t = NormalizzaAcapi(f.T);
            if (t.Length == 0) continue;

            if (puliti.Count > 0 && StessoFormato(puliti[^1], f))
            {
                puliti[^1].T += t;
                continue;
            }

            puliti.Add(Copia(f, t));
        }

        if (puliti.Count == 0) puliti.Add(CreaFrammento(""));

        return puliti;
    }

    public static bool StessoFormato(Frammento a, Frammento b) =>
        a.F == b.F && a.S == b.S && a.B == b.B
        && a.I == b.I && a.U == b.U
        && string.Equals(a.C, b.C, StringComparison.OrdinalIgnoreCase);

    static Frammento Copia(Frammento f, string testo) => new()
    {
        T = testo,
        F = f.F,
        S = f.S,
        B = f.B,
        I = f.I,
        U = f.U,
        C = f.C,
    };

    /// <summary>
    /// Sostituisce i [[campi]] con i loro valori conservando il formato.
    ///
    /// Lavora sul testo intero e non frammento per frammento. Da quando
    /// c'è la barra di formato, un salvato può avere più frammenti e il
    /// taglio cade dove l'utente ha messo il grassetto: mettere in
    /// grassetto solo "nome" dentro "[[nome]]" divideva il campo in tre,
    /// nessun frammento conteneva "[[nome]]" intero, e ciò che si
    /// incollava portava dentro le parentesi quadre.
    ///
    /// Il valore prende il formato del frammento in cui inizia il campo.
    /// </summary>
    public static List<Frammento> Riempi(
        IEnumerable<Frammento> frammenti,
        IReadOnlyDictionary<string, string> valori)
    {
        // Quelli vuoti intralciano il percorso e non aggiungono niente.
        var pezzetti = frammenti.Where(f => f.T.Length > 0).ToList();
        if (pezzetti.Count == 0) return [];

        string tutto = TestoDi(pezzetti);

        var campi = new List<(int Inizio, int Lungo, string Valore)>();
        int cerca = 0;

        while (true)
        {
            int a = tutto.IndexOf("[[", cerca, StringComparison.Ordinal);
            if (a < 0) break;

            int b = tutto.IndexOf("]]", a, StringComparison.Ordinal);
            if (b < 0) break;

            if (valori.TryGetValue(tutto[(a + 2)..b].Trim(), out var valore))
                campi.Add((a, b + 2 - a, valore));

            cerca = b + 2;
        }

        if (campi.Count == 0) return [.. pezzetti.Select(f => Copia(f, f.T))];

        var pezzi = new List<(int Quale, string Testo)>();

        int quale = 0;    // frammento in cui si trova il percorso
        int usato = 0;   // caratteri già consumati di quel frammento
        int dove = 0;   // posizione dentro `tutto`

        void Avanza(int quanti, bool emetti)
        {
            while (quanti > 0)
            {
                if (usato == pezzetti[quale].T.Length) { quale++; usato = 0; }

                int prende = Math.Min(quanti, pezzetti[quale].T.Length - usato);

                if (emetti) pezzi.Add((quale, pezzetti[quale].T.Substring(usato, prende)));

                usato += prende;
                quanti -= prende;
                dove += prende;
            }
        }

        foreach (var (inizio, lungo, valore) in campi)
        {
            Avanza(inizio - dove, true);

            // Il formato del valore è quello del posto in cui inizia il
            // campo, non di quello in cui finisce.
            if (usato == pezzetti[quale].T.Length && quale + 1 < pezzetti.Count)
            {
                quale++;
                usato = 0;
            }

            pezzi.Add((quale, valore));
            Avanza(lungo, false);
        }

        Avanza(tutto.Length - dove, true);

        var uscita = new List<Frammento>();

        foreach (var (indice, testo) in pezzi)
        {
            if (testo.Length == 0) continue;

            // Pezzi consecutivi con lo stesso formato tornano a essere uno
            // solo: altrimenti ogni incolla moltiplicherebbe i frammenti.
            if (uscita.Count > 0 && StessoFormato(uscita[^1], pezzetti[indice]))
            {
                uscita[^1].T += testo;
                continue;
            }

            uscita.Add(Copia(pezzetti[indice], testo));
        }

        return uscita;
    }
}
