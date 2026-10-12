namespace Pastepad.Nucleo;

/// <summary>
/// Limiti e valori predefiniti. I colori e le misure dell'interfaccia
/// NON stanno qui: vivono nelle specifiche dell'interfaccia, che sono la
/// loro unica fonte. Questo file arrivò ad averne una copia con valori
/// diversi e serviva solo a confondere.
/// </summary>
public static class Config
{
    public const string App = "pastepad";

    /// <summary>
    /// Esce dall'assembly, che a sua volta esce dal tag di git. Mai
    /// scritta a mano: fu pubblicata una release con binari vecchi per
    /// averla inchiodata in dodici posti.
    /// </summary>
    public static string Version { get; } =
        typeof(Config).Assembly
            .GetCustomAttributes(
                typeof(System.Reflection.AssemblyInformationalVersionAttribute),
                false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()?.InformationalVersion
            .Split('+')[0]
        ?? "0.0.0";

    // --- finestra

    /// <summary>
    /// Quanto misura il pannello la prima volta, prima che l'utente abbia
    /// trascinato qualcosa.
    ///
    /// È un valore sciolto e non una preimpostazione selezionabile: le
    /// preimpostazioni (mini, piccolo, medio, grande) furono tolte perché
    /// il pannello è adattabile —si allarga dai bordi e ricorda la
    /// dimensione—, e delle misure fisse accanto a qualcosa che si regola
    /// già da solo erano di troppo. Vive accanto ai limiti qui sotto
    /// perché è della stessa specie: una misura del pannello, non
    /// un'opzione.
    /// </summary>
    public const int LarghezzaDef = 380;
    public const int AltezzaDef = 560;

    public const int MinLarghezza = 300;
    public const int MinAltezza = 340;
    public const int MaxLarghezza = 720;
    public const int MaxAltezza = 1100;

    /// <summary>Scorciatoie che si fanno con una mano sola.</summary>
    public static readonly IReadOnlyDictionary<string, string> Scorciatoie =
        new Dictionary<string, string>
        {
            ["ctrl+shift+v"] = "Ctrl + Shift + V",
            ["ctrl+q"] = "Ctrl + Q",
            ["ctrl+space"] = "Ctrl + Spazio",
            ["ctrl+shift+space"] = "Ctrl + Shift + Spazio",
            ["alt+q"] = "Alt + Q",
            ["ctrl+alt+v"] = "Ctrl + Alt + V",
        };

    public const string ScorciatoiaDef = "ctrl+shift+v";

    /// <summary>
    /// La scorciatoia così come si mostra all'utente. Se non è una di
    /// quelle della lista esce tale e quale: config.json si modifica a
    /// mano, ed è meglio mostrare «ctrl+j» che non mostrare niente.
    /// </summary>
    public static string Leggibile(string scorciatoia) =>
        Scorciatoie.TryGetValue(scorciatoia, out var bello) ? bello : scorciatoia;

    /// <summary>
    /// La cartella che si crea da sola quando non ce n'è nessuna.
    ///
    /// Vive qui e non nell'interfaccia perché finisce scritta in
    /// snippets.json come categoria di ogni testo: è un dato con valore
    /// iniziale, non una dicitura.
    /// </summary>
    public const string CartellaDef = "I miei testi";

    // --- limiti
    public const int MaxCron = 80;
    public const int MaxCaratteri = 200_000;

    // --- tipografia
    public const string CarattereAlt = "Segoe UI";   // Windows 10
    public const string CarattereDef = "Calibri";
    public const int DimDef = 11;
    public const string ColoreDef = "#000000";
}

/// <summary>
/// Dove vive tutto. Si passa all'<see cref="Archivio"/> invece di vivere
/// in variabili globali: così le prove usano una cartella temporanea
/// senza toccare stato condiviso, cosa che nella versione in Python
/// obbligava a riassegnare l'intero modulo di configurazione.
/// </summary>
public sealed record Percorsi(
    string Dati,
    string Cronologia,
    string Preferenze,
    string Immagini,
    string Note)
{
    /// <summary>
    /// %LOCALAPPDATA%\pastepad. In Programmi Windows blocca la scrittura
    /// senza avvisare: il programma si avvierebbe ma non salverebbe
    /// niente.
    /// </summary>
    public static Percorsi Predefinite()
    {
        var baseDir = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            Config.App);

        return InCartella(baseDir);
    }

    public static Percorsi InCartella(string cartella) => new(
        Path.Combine(cartella, "snippets.json"),
        Path.Combine(cartella, "cronologia.json"),
        Path.Combine(cartella, "config.json"),
        Path.Combine(cartella, "immagini"),
        Path.Combine(cartella, "note.json"));

    /// <summary>
    /// La cartella che li contiene tutti. Si deduce e non si salva a
    /// parte perché non possano discordare: tutti i percorsi escono
    /// sempre dalla stessa, anche quando arrivano da <c>--dati</c>.
    /// </summary>
    public string Cartella => Path.GetDirectoryName(Dati) ?? "";
}
