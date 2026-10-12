using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Pastepad.App;

/// <summary>
/// La tavolozza e le misure, portate dalla versione in Python, che è la
/// fonte vera dichiarata nelle specifiche dell'interfaccia.
///
/// I pennelli non si ricreano al cambio di tema: gli si cambia il
/// colore sul posto. XAML risolve una StaticResource una volta sola,
/// quindi sostituire l'oggetto non ridisegnerebbe niente; mutarlo sì.
/// </summary>
public static class Stile
{
    /// <summary>Gli otto colori che definiscono uno sfondo.</summary>
    public sealed record Tavolozza(
        string Sfondo,
        string Elevato,
        string Riquadro,
        string Hover,
        string Bordo,
        string Testo,
        string Medio,
        string Tenue,
        string Ombra);

    // Fissi in tutti i temi: il pericolo deve leggersi ugualmente
    // rosso in chiaro e in scuro, e la cartella ugualmente ambra.
    public const string Rosso = "#EF4444";
    public const string Ambra = "#F5A524";

    /// <summary>Rosso del pulsante Elimina, diverso da quello di avviso.</summary>
    public const string Pericolo = "#DC2626";

    /// <summary>
    /// I 18 accenti. Il secondo colore è quello del testo sopra: il
    /// bianco fisso su ambra o lime non arriva al 4.5:1 di WCAG AA. Le
    /// 18 coppie sono verificate con il calcolo, non a occhio: la
    /// peggiore è blu, con 5.38:1.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (string Color, string Sopra)>
        Accenti = new Dictionary<string, (string, string)>
        {
            ["menta"] = ("#2DD4A7", "#052E23"),
            ["blu"] = ("#4B8DF8", "#04183C"),
            ["viola"] = ("#9B7BF7", "#1E1046"),
            ["ambra"] = ("#F5A524", "#3A2606"),
            ["corallo"] = ("#F76B5C", "#3D0F09"),
            ["rosa"] = ("#F472B6", "#3A1128"),
            ["ciano"] = ("#22D3EE", "#062E36"),
            ["lime"] = ("#A3E635", "#1A2E05"),
            ["indaco"] = ("#818CF8", "#111539"),
            ["turchese"] = ("#2DD4BF", "#042F2A"),
            ["pesca"] = ("#FB923C", "#3B1D06"),
            ["lavanda"] = ("#C084FC", "#2E1065"),
            ["smeraldo"] = ("#34D399", "#04291C"),
            ["cielo"] = ("#38BDF8", "#052F42"),
            ["oro"] = ("#FCD34D", "#3B2606"),
            ["fragola"] = ("#FB7185", "#3F0A16"),
            ["menta_fredda"] = ("#5EEAD4", "#032F2A"),
            ["sabbia"] = ("#D6BC8A", "#332612"),
        };

    public const string AccentoDef = "menta";

    public static readonly Tavolozza Scura = new(
        "#0B0B0D", "#141417", "#1B1B1F", "#26262B", "#242429",
        "#F4F4F5", "#9C9CA6", "#86868E", "#000000");

    public static readonly Tavolozza Chiara = new(
        "#F6F6F4", "#FFFFFF", "#FFFFFF", "#EFEFEC", "#E6E6E2",
        "#141416", "#5C5C66", "#707079", "#94949E");

    static readonly Tavolozza Mezzanotte = new(
        "#0A0F1E", "#111A30", "#16213C", "#1F2D4D", "#1C2942",
        "#E8ECF5", "#93A0BC", "#7E89A1", "#000000");

    static readonly Tavolozza Grafite = new(
        "#141414", "#1C1C1C", "#242424", "#2E2E2E", "#2A2A2A",
        "#F0F0F0", "#A0A0A0", "#8B8B8B", "#000000");

    static readonly Tavolozza Bosco = new(
        "#0A140F", "#0F1D16", "#14281E", "#1D3A2B", "#1A3327",
        "#E8F2EC", "#8FB3A0", "#779082", "#000000");

    static readonly Tavolozza Carta = new(
        "#F5F1E8", "#FFFDF7", "#FFFDF7", "#EDE7D9", "#E2DACA",
        "#2B2620", "#645B4F", "#776C60", "#A89B87");

    static readonly Tavolozza Nebbia = new(
        "#EEF1F5", "#FFFFFF", "#FFFFFF", "#E2E8F0", "#DBE2EA",
        "#1E293B", "#505C6E", "#636E80", "#94A3B8");

    static readonly Tavolozza SabbiaSfondo = new(
        "#F7F4EF", "#FFFFFF", "#FFFFFF", "#EEE9E0", "#E5DED2",
        "#33302B", "#645D53", "#776F65", "#A8A093");

    static readonly Tavolozza Lilla = new(
        "#F4F1FA", "#FFFFFF", "#FFFFFF", "#E9E3F6", "#E1D9F2",
        "#2A2340", "#635682", "#73698E", "#A296C4");

    static readonly Tavolozza Salvia = new(
        "#EEF7F3", "#FFFFFF", "#FFFFFF", "#DFF0E8", "#D5E9DF",
        "#1E332B", "#496458", "#5D766A", "#93B3A4");

    static readonly Tavolozza Rossore = new(
        "#FBF2F4", "#FFFFFF", "#FFFFFF", "#F5E3E8", "#EEDAE0",
        "#3A2229", "#76555E", "#866871", "#B79AA2");

    /// <summary>
    /// I 12 sfondi. "auto" è null e segue il tema di Windows; gli altri
    /// comandano sul sistema.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Tavolozza?> Temi =
        new Dictionary<string, Tavolozza?>
        {
            ["auto"] = null,
            ["scuro"] = Scura,
            ["mezzanotte"] = Mezzanotte,
            ["grafite"] = Grafite,
            ["bosco"] = Bosco,
            ["chiaro"] = Chiara,
            ["carta"] = Carta,
            ["nebbia"] = Nebbia,
            ["sabbia"] = SabbiaSfondo,
            ["lilla"] = Lilla,
            ["salvia"] = Salvia,
            ["rossore"] = Rossore,
        };

    public const string TemaDef = "auto";

    /// <summary>
    /// Quelli che si leggono su sfondo chiaro. Bisogna dirlo a WinUI
    /// oltre a dipingere: i menu e il cursore della barra di ricerca li
    /// disegna il sistema secondo ElementTheme, non secondo i nostri
    /// pennelli.
    /// </summary>
    static readonly HashSet<string> Chiari =
    [
        "chiaro", "carta", "nebbia", "sabbia", "lilla", "salvia", "rossore",
    ];

    // --- misure. Escono dalle maquette SVG e dalle specifiche.
    public const int RPannello = 20;
    public const int RRiquadro = 14;
    public const int RControl = 12;
    public const int RCapsula = 15;

    public const int E1 = 4;
    public const int E2 = 8;
    public const int E3 = 12;
    public const int E4 = 16;

    public const int AltezzaRiga = 56;

    /// <summary>
    /// Il riquadro di un'annotazione. Più alto di una riga perché mostra
    /// più linee: in una nota rapida, il titolo da solo non dice niente.
    /// </summary>
    public const int AltezzaAnnotazione = 76;
    public const int AltezzaRigaMini = 42;
    public const int SepRiga = 6;
    public const int BarraAttiva = 3;

    // Le quattro dimensioni delle specifiche, sezione 3. Niente di
    // intermedio: le maquette disegnano 12.5 e 10 in alcuni testi,
    // ma comandano le specifiche.
    public const int TTitolo = 15;
    public const int TCorpo = 13;
    public const int TMinore = 12;
    public const int TMini = 11;

    // Altezze esplicite e non dedotte dal padding: lasciarle al
    // framework fu ciò che gonfiò le schede a 49 px dove la maquette
    // chiedeva 30, e la barra di ricerca a 66 dove chiedeva 42.
    public const int AltezzaBarraRicerca = 42;
    public const int AltezzaScheda = 30;
    public const int AltezzaPulsante = 26;
    public const int AltezzaCartella = 34;
    public const int AltezzaGruppo = 32;
    public const int LatoIntestazione = 20;

    /// <summary>Sotto questa larghezza la riga passa a una sola linea.</summary>
    public const int LarghezzaCompatta = 340;

    /// <summary>Nome dell'accento e del tema in uso.</summary>
    public static string Accento { get; private set; } = AccentoDef;
    public static string Tema { get; private set; } = TemaDef;

    /// <summary>True se lo sfondo che si vede adesso è chiaro.</summary>
    public static bool EChiaro { get; private set; }

    /// <summary>La tavolozza che si vede adesso.</summary>
    public static Tavolozza Attuale { get; private set; } = Scura;

    public static (string Color, string Sopra) ColoreAccento { get; private set; } =
        Accenti[AccentoDef];

    /// <summary>
    /// Quale tema chiedere a WinUI. <c>Default</c> significa seguire
    /// quello di Windows e cambiare con lui a caldo: è ciò che fa
    /// "auto", ed è il motivo per stare su WinUI 3. Light e Dark solo
    /// quando l'utente sceglie apposta.
    /// </summary>
    public static ElementTheme TemaRichiesto { get; private set; } =
        ElementTheme.Default;

    /// <summary>
    /// True quando lo sfondo lo mette il sistema e bisogna lasciar
    /// vedere Mica. Con uno dei dieci sfondi propri no: lì l'utente ha
    /// chiesto un colore preciso e coprirlo con il materiale di sistema
    /// sarebbe non darglielo.
    /// </summary>
    public static bool UsaMica { get; private set; } = true;

    /// <summary>
    /// Lascia le due tavolozze nei dizionari di tema. Non decide quale
    /// si vede: lo fa WinUI secondo <see cref="TemaRichiesto"/>, e
    /// cambia da solo quando cambia Windows.
    /// </summary>
    public static void Applica(string? accento, string? tema)
    {
        if (!string.IsNullOrEmpty(tema)) Tema = tema;
        if (!string.IsNullOrEmpty(accento)) Accento = accento;

        ColoreAccento = Accenti.TryGetValue(Accento, out var a)
            ? a
            : Accenti[AccentoDef];

        // Di fabbrica, ogni dizionario ha la tavolozza che gli spetta.
        var chiaraEffettiva = Chiara;
        var scuraEffettiva = Scura;

        if (Temi.TryGetValue(Tema, out var propria) && propria is not null)
        {
            if (Chiari.Contains(Tema))
            {
                chiaraEffettiva = propria;
                TemaRichiesto = ElementTheme.Light;
            }
            else
            {
                scuraEffettiva = propria;
                TemaRichiesto = ElementTheme.Dark;
            }

            // "chiaro" e "scuro" sono le tavolozze di sistema scelte a
            // mano: meritano ancora Mica. Le altre nove no.
            UsaMica = Tema is "chiaro" or "scuro";
        }
        else
        {
            TemaRichiesto = ElementTheme.Default;
            UsaMica = true;
        }

        Riversa("Light", chiaraEffettiva);
        Riversa("Dark", scuraEffettiva);
    }

    static void Riversa(string chiave, Tavolozza tavolozza)
    {
        if (Application.Current.Resources.ThemeDictionaries[chiave]
            is not ResourceDictionary dic)
        {
            return;
        }

        Metti(dic, "PpSfondo", tavolozza.Sfondo);
        Metti(dic, "PpElevato", tavolozza.Elevato);
        Metti(dic, "PpRiquadro", tavolozza.Riquadro);
        Metti(dic, "PpHover", tavolozza.Hover);
        Metti(dic, "PpBordo", tavolozza.Bordo);
        Metti(dic, "PpTesto", tavolozza.Testo);
        Metti(dic, "PpMedio", tavolozza.Medio);
        Metti(dic, "PpTenue", tavolozza.Tenue);

        // L'accento è nostro, non di sistema, quindi va uguale nelle due
        // tavolozze. In alto contrasto non si tocca: lì comanda Windows.
        Metti(dic, "PpAccento", ColoreAccento.Color);
        Metti(dic, "PpSopra", ColoreAccento.Sopra);
    }

    /// <summary>
    /// Punta <see cref="Attuale"/> alla tavolozza che si sta vedendo.
    /// Si chiama all'avvio e a ogni ActualThemeChanged: ciò che
    /// dipingiamo da codice non lo rivaluta nessuno per noi.
    /// </summary>
    public static void Sincronizza(ElementTheme effettivo)
    {
        EChiaro = effettivo == ElementTheme.Light;

        if (Temi.TryGetValue(Tema, out var propria) && propria is not null)
        {
            // Con uno sfondo proprio, l'effettivo e il richiesto coincidono
            // sempre: TemaRichiesto l'ha fissato Applica().
            Attuale = propria;
            return;
        }

        Attuale = EChiaro ? Chiara : Scura;
    }

    static void Metti(ResourceDictionary risorse, string chiave, string hex)
    {
        var colore = Da(hex);

        if (risorse.TryGetValue(chiave, out var valore)
            && valore is SolidColorBrush pennello)
        {
            pennello.Color = colore;
            return;
        }

        risorse[chiave] = new SolidColorBrush(colore);
    }

    /// <summary>"#RRGGBB" o "#AARRGGBB" in Color.</summary>
    public static Color Da(string hex)
    {
        string h = hex.TrimStart('#');

        byte a = 255;
        int i = 0;

        if (h.Length == 8)
        {
            a = Convert.ToByte(h[..2], 16);
            i = 2;
        }

        return Color.FromArgb(
            a,
            Convert.ToByte(h.Substring(i, 2), 16),
            Convert.ToByte(h.Substring(i + 2, 2), 16),
            Convert.ToByte(h.Substring(i + 4, 2), 16));
    }

    /// <summary>
    /// I pennelli già fatti, per colore.
    ///
    /// Si condividono tra tutti quelli che chiedono lo stesso colore
    /// invece di crearne uno nuovo ogni volta. Non è
    /// micro-ottimizzazione: ogni aggiornamento della lista ne chiede
    /// decine —sfondo, bordo, titolo, sottotitolo e icona per riga, fino
    /// a ottanta righe— e succede a ogni apertura e a ogni tasto nella
    /// barra di ricerca.
    ///
    /// Condividerli è sicuro **finché nessuno li modifica**: qui si
    /// assegnano a proprietà e non gli si tocca né Color né Opacity. Se
    /// mai servisse mutarne uno, va creato a parte con new, perché
    /// cambiarlo qui lo cambierebbe in tutta l'interfaccia di colpo.
    ///
    /// Il dizionario non cresce senza controllo: le chiavi sono i colori
    /// della tavolozza, che sono una manciata.
    /// </summary>
    static readonly Dictionary<string, SolidColorBrush> _pennelli = [];

    public static SolidColorBrush Pennello(string hex)
    {
        if (_pennelli.TryGetValue(hex, out var fatto)) return fatto;

        var nuovo = new SolidColorBrush(Da(hex));
        _pennelli[hex] = nuovo;

        return nuovo;
    }

    /// <summary>
    /// Un pennello dello stesso colore con meno opacità. Si usa per il
    /// sottotitolo della riga attiva, che va sopra l'accento: abbassare
    /// l'opacità del colore "sopra" lo separa dal titolo senza inventare
    /// un colore che non sta nella tavolozza.
    /// </summary>
    public static SolidColorBrush Pennello(string hex, double opacita)
    {
        var colore = Da(hex);
        return new SolidColorBrush(
            Color.FromArgb((byte)Math.Round(opacita * 255), colore.R, colore.G, colore.B));
    }

    /// <summary>Il nome leggibile di uno sfondo, per il dialogo.</summary>
    public static string NomeTema(string codice) => codice switch
    {
        "auto" => "Come Windows",
        "scuro" => "Scuro",
        "mezzanotte" => "Mezzanotte",
        "grafite" => "Grafite",
        "bosco" => "Bosco",
        "chiaro" => "Chiaro",
        "carta" => "Carta",
        "nebbia" => "Nebbia",
        "sabbia" => "Sabbia",
        "lilla" => "Lilla",
        "salvia" => "Salvia",
        "rossore" => "Rossore",
        _ => codice,
    };

    /// <summary>
    /// Glifi di Segoe Fluent Icons, il carattere di icone di Windows 11.
    /// Ognuno è verificato disegnandolo, non leggendone il nome: quelli
    /// di prima venivano a memoria e uno di loro (E8C8) era l'icona di
    /// "copia" che faceva da icona di "testo".
    /// </summary>
    public static class Icone
    {
        public const string Cerca = "\uE721";       // Search
        public const string Pausa = "\uE769";        // Pause
        public const string Riprendi = "\uE768";     // Play
        public const string Tavolozza = "\uE790";       // Color
        public const string Chiudi = "\uE8BB";       // ChromeClose
        public const string Link = "\uE71B";       // Link
        public const string Immagine = "\uEB9F";       // Photo2
        public const string Fissa = "\uE718";        // Pin
        public const string Sblocca = "\uE77A";       // Unpin
        public const string TrePunti = "\uE712";   // More
        public const string Cartella = "\uE8B7";      // Folder
        public const string CartellaAperta = "\uE838";
        public const string CartellaNuova = "\uE8F4"; // NewFolder
        public const string Lista = "\uE8FD";        // BulletedList
        public const string Scopa = "\uEA99";       // Broom
        public const string Spunta = "\uE739";      // Checkbox
        public const string SpuntaAttiva = "\uE73A";
        public const string Piu = "\uE710";          // Add
        public const string SottoV = "\uE70D";       // ChevronDown
        public const string SopraV = "\uE70E";      // ChevronUp
        public const string DestraV = "\uE76C";     // ChevronRight
        public const string Nota = "\uE70B";         // QuickNote
        public const string Appunti = "\uE77F"; // Paste
        public const string Seleziona = "\uE762";  // CheckList
        public const string SegnoSpunta = "\uE73E";           // CheckMark
        public const string Modifica = "\uE70F";       // Edit
        public const string Modello = "\uE943";    // Code
        public const string Email = "\uE715";       // Mail

        /// <summary>
        /// ChatSparkle. È la famiglia «sparkle» con cui Windows
        /// segna le cose di IA, quindi si legge come tale senza doverlo
        /// spiegare. Verificato nella lista ufficiale di Segoe Fluent Icons.
        /// </summary>
        public const string Prompt = "\uEAB7";       // ChatSparkle
        public const string Cestino = "\uE74D";     // Delete
        public const string AnnullaModifica = "\uE7A7";     // Undo
    }
}
