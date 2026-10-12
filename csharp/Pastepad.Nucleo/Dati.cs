using System.Text.Json.Serialization;

namespace Pastepad.Nucleo;

/// <summary>
/// Base comune di ciò che può comparire in una lista o in un risultato
/// di ricerca: uno snippet salvato o una voce della cronologia.
/// </summary>
public abstract class Elemento
{
}

/// <summary>
/// Un pezzo di testo con il suo formato. Uno snippet è una lista di
/// questi. Le chiavi sono di una lettera perché il JSON non ingrassi, e
/// sono le stesse che scriveva la versione in Python.
/// </summary>
public sealed class Frammento
{
    [JsonPropertyName("t")] public string T { get; set; } = "";
    [JsonPropertyName("f")] public string F { get; set; } = Config.CarattereDef;
    [JsonPropertyName("s")] public int S { get; set; } = Config.DimDef;
    [JsonPropertyName("b")] public int B { get; set; }
    [JsonPropertyName("i")] public int I { get; set; }
    [JsonPropertyName("u")] public int U { get; set; }
    [JsonPropertyName("c")] public string C { get; set; } = Config.ColoreDef;
}

/// <summary>Un testo salvato apposta, dentro una cartella.</summary>
public sealed class Snippet : Elemento
{
    [JsonPropertyName("titolo")] public string Titolo { get; set; } = "";

    [JsonPropertyName("categoria")] public string Categoria { get; set; } = "";

    [JsonPropertyName("runs")] public List<Frammento> Runs { get; set; } = [];

    /// <summary>
    /// Di che tipo è, se l'utente l'ha detto: vedi <see cref="Tipi"/>.
    ///
    /// Nullo finché nessuno lo sceglie, e allora si deduce dal testo come
    /// si è sempre fatto. Per questo non si scrive quando è nullo: uno
    /// snippets.json di prima di questa versione si riscrive tale e
    /// quale, senza una chiave che non c'era.
    /// </summary>
    [JsonPropertyName("tipo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tipo { get; set; }

    /// <summary>
    /// Lo portano solo i file di versioni vecchie, di prima che uno
    /// snippet potesse avere formato. Al caricamento diventa un unico
    /// frammento; non si riscrive.
    /// </summary>
    [JsonPropertyName("testo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Testo { get; set; }
}

/// <summary>Qualcosa che è passato dagli appunti.</summary>
public sealed class Voce : Elemento
{
    public const string Testo_ = "testo";
    public const string Immagine = "immagine";

    [JsonPropertyName("tipo")] public string Tipo { get; set; } = Testo_;

    [JsonPropertyName("testo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Testo { get; set; }

    [JsonPropertyName("percorso")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Percorso { get; set; }

    /// <summary>
    /// Si omette in scrittura quando è false, come faceva il JSON di
    /// Python, dove la chiave semplicemente non c'era.
    /// </summary>
    [JsonPropertyName("pin")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Pin { get; set; }

    /// <summary>
    /// Calcolata, non un campo del file. Senza questo JsonIgnore si
    /// scriveva come una chiave in più e sporcava il JSON della v3.
    /// </summary>
    [JsonIgnore]
    public bool EUnImmagine => Tipo == Immagine;
}

/// <summary>
/// Una nota del blocco. Non si incolla mai: si scrive, si legge e si
/// elimina, come le note rapide di Windows.
///
/// Vive in un file suo e non dentro snippets.json apposta. Ciò che sta
/// in snippets.json esiste per essere incollato altrove; questo esiste
/// per restare qui. Mescolarli obbligherebbe ogni lista, ogni ricerca e
/// ogni menu a chiedersi quale delle due cose è ogni riga.
/// </summary>
public sealed class Nota : Elemento
{
    /// <summary>
    /// Il nome che le ha dato l'utente, per richiamarla dalla barra di
    /// ricerca. Facoltativo: una nota senza nome si riconosce dalle sue
    /// prime linee, che è come funzionano le note rapide.
    ///
    /// Non si scrive quando è vuoto, così un note.json della 4.11.0 si
    /// riscrive uguale a com'era.
    /// </summary>
    [JsonPropertyName("titolo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Titolo { get; set; } = "";

    /// <summary>
    /// Il contenuto, con il suo formato, come un salvato. Dalla 4.15.0
    /// una nota può avere grassetto, corsivo, sottolineato ed elenchi.
    /// </summary>
    [JsonPropertyName("runs")] public List<Frammento> Runs { get; set; } = [];

    /// <summary>
    /// Lo portano solo i note.json di prima della 4.15.0, quando una
    /// nota era testo semplice. Al caricamento diventa un unico
    /// frammento e non si riscrive — la stessa strada che hanno già
    /// fatto i salvati.
    /// </summary>
    [JsonPropertyName("testo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Testo { get; set; }

    /// <summary>
    /// Quando è stata toccata l'ultima volta, in ISO-8601. Stringa e non
    /// DateTime perché il file si legga uguale in qualsiasi regione: con
    /// DateTime, un computer con altre impostazioni regionali scrive
    /// «15/08/2026» e un altro lo legge come l'8 marzo.
    ///
    /// Ordina la lista: l'ultima toccata, in alto.
    /// </summary>
    [JsonPropertyName("modificata")] public string Modificata { get; set; } = "";
}

/// <summary>Il contenuto di snippets.json.</summary>
internal sealed class Raccolta
{
    [JsonPropertyName("categorie")]
    public List<string> Categorie { get; set; } = [];

    [JsonPropertyName("snippets")]
    public List<Snippet> Snippets { get; set; } = [];
}
