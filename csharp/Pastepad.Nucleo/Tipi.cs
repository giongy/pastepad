namespace Pastepad.Nucleo;

/// <summary>
/// Di che tipo è ogni salvato: un segnalibro, un modello, un'email o
/// una nota.
///
/// Fino alla 4.3.0 questo non si sceglieva, si deduceva dal testo —una
/// url era un segnalibro, dei [[campi]] un modello, e il resto una nota—
/// e non c'era modo di contraddirlo. Funzionava finché i quattro gruppi
/// si potevano leggere nel testo stesso, e smise di funzionare appena ne
/// comparve uno che no: il corpo di un'email è testo normale e dentro
/// non c'è niente che lo distingua da una nota.
///
/// Quindi il tipo continua a proporsi da solo, ma adesso è una
/// proposta. L'utente la accetta o la cambia, e ciò che sceglie comanda.
///
/// **Ciò che non si sceglie non si scrive.** Il campo è nullo finché
/// nessuno lo tocca, e allora vale il dedotto, che è ciò che pastepad
/// faceva da sempre: uno snippets.json di oggi continua a leggersi
/// uguale e si riscrive uguale, senza una chiave in più.
/// </summary>
public static class Tipi
{
    public const string Segnalibro = "segnalibro";
    public const string Modello = "modello";
    public const string Email = "email";

    /// <summary>
    /// Un testo che si dà a una IA. Lo ha chiesto chi salva prompt come
    /// parte del suo lavoro: mescolati tra le note non si trovano, e sono
    /// tra le cose che più si riusano tali e quali.
    /// </summary>
    public const string Prompt = "prompt";

    public const string Nota = "nota";

    /// <summary>
    /// Nell'ordine in cui si mostrano, che è anche quello dei gruppi del
    /// pannello: dal più specifico al più generale. La nota va per ultima
    /// perché è il cassetto di tutto il resto — ciò che non è nessuna
    /// delle altre quattro.
    /// </summary>
    public static readonly string[] Tutti =
        [Segnalibro, Modello, Email, Prompt, Nota];

    /// <summary>
    /// Il tipo che si propone leggendo il testo. È esattamente ciò che
    /// l'interfaccia faceva per conto suo, portato qui perché ci sia un
    /// solo posto che lo decide e perché si possa provare.
    ///
    /// Un link con [[campi]] conta come segnalibro: tra i due vince
    /// quello che si riconosce con più certezza.
    ///
    /// **Né «email» né «prompt» si deducono mai.** Tutti e due sono testo
    /// normale e dentro non hanno niente che li separi da una nota.
    /// Proporre email per una chiocciola trasformerebbe in email
    /// qualsiasi testo che menzioni un indirizzo, e con un prompt è
    /// ancora peggio: non c'è nemmeno una chiocciola a cui aggrapparsi.
    /// Sono proprio i due tipi che esistono per essere scelti a mano.
    ///
    /// Un prompt con [[campi]] si proporrà come modello, e va bene così:
    /// ciò che decide se ti chiede prima di incollare è il testo, non il
    /// tipo. Chi lo vuole tra i suoi prompt lo cambia, e continua a
    /// chiedergli i campi lo stesso.
    /// </summary>
    public static string Deduci(string? testo) => DeduciDa(
        Regole.EUnLink(testo),
        Regole.CampiDi(testo ?? "").Count > 0);

    /// <summary>
    /// Lo stesso, per chi sa già le due risposte.
    ///
    /// Esiste per prestazioni e non per gusto: la riga deve sapere se è
    /// un link e se ha campi per disegnarsi, e chiamare
    /// <see cref="Deduci"/> dopo ripeteva le due ricerche sul testo
    /// INTERO — fino a ottanta volte per apertura, e di nuovo a ogni
    /// tasto nella barra di ricerca. La regola continua a vivere qui e in
    /// un solo posto; l'unica cosa che cambia è chi porta i dati.
    /// </summary>
    public static string DeduciDa(bool eUnLink, bool haCampi) =>
        eUnLink ? Segnalibro : haCampi ? Modello : Nota;

    /// <summary>
    /// Il tipo che vale: quello scelto, se ne è stato scelto uno ed è
    /// tra quelli validi; altrimenti quello dedotto.
    ///
    /// Si controlla che sia tra quelli validi perché config.json e
    /// snippets.json si modificano a mano e arrivano da versioni che non
    /// esistono ancora. Un tipo sconosciuto non può lasciare il salvato
    /// fuori dai gruppi, che è come sparire dalla lista.
    /// </summary>
    public static string Di(string? scelto, string? testo) =>
        Vale(scelto) ? scelto! : Deduci(testo);

    /// <summary>Lo stesso, per un salvato.</summary>
    public static string Di(Snippet snippet) =>
        Di(snippet.Tipo, Regole.TestoDi(snippet.Runs));

    public static bool Vale(string? tipo) =>
        tipo is not null && Array.IndexOf(Tutti, tipo) >= 0;

    /// <summary>
    /// Ciò che si salva nel file: nullo quando lo scelto coincide con
    /// ciò che si sarebbe dedotto comunque.
    ///
    /// Senza questo, aprire un salvato e premere Salva senza toccare
    /// niente gli aggiungerebbe una chiave "tipo" che prima non aveva.
    /// Non cambia il comportamento, ma sporca il file di chiunque
    /// modifichi qualcosa e fa rumore in qualsiasi confronto.
    /// </summary>
    public static string? PerSalvare(string? scelto, string? testo) =>
        Vale(scelto) && scelto != Deduci(testo) ? scelto : null;
}
