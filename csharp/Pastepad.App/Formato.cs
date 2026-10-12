using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Pastepad.Nucleo;
using Windows.UI;

namespace Pastepad.App;

/// <summary>
/// Il ponte tra il <see cref="RichEditBox"/> e i frammenti che vanno in
/// snippets.json.
///
/// L'editor di testo formattato lo mette WinUI: <c>RichEditBox</c> con
/// il suo <c>ITextDocument</c>. La barra dei pulsanti no, e non è una
/// dimenticanza — lo dice la documentazione stessa del controllo: "i
/// pulsanti di formato non fanno parte del rich edit box; dovresti
/// offrire almeno un insieme minimo e programmarne le azioni".
///
/// Ciò che si salva resta quello di sempre: una lista di
/// <see cref="Frammento"/> con carattere, dimensione, grassetto,
/// corsivo, sottolineato e colore. Non serve toccare il formato del
/// file.
/// </summary>
internal static class Formato
{
    /// <summary>
    /// Caratteri presenti in qualsiasi Windows e che Outlook capisce
    /// senza sostituire niente. Calibri per primo: è quello di fabbrica.
    /// </summary>
    public static readonly string[] Caratteri =
    [
        "Calibri", "Arial", "Segoe UI", "Times New Roman",
        "Georgia", "Verdana", "Tahoma", "Courier New",
    ];

    public static readonly int[] Dimensioni =
        [8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 36];

    /// <summary>
    /// L'editor separa i paragrafi con \r e basta e il salvato porta
    /// \r\n. Senza convertirlo, le posizioni slittano di un carattere per
    /// ogni a capo e il formato si applica spostato.
    /// </summary>
    static string PerLEditor(string t) =>
        Regole.NormalizzaAcapi(t).Replace("\r\n", "\r");

    public static string AHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    // ------------------------------------------------------ caricare

    public static void Carica(RichEditBox casella, IReadOnlyList<Frammento> runs)
    {
        var doc = casella.Document;

        var pezzetti = runs
            .Select(f => (Frag: f, Testo: PerLEditor(f.T)))
            .Where(x => x.Testo.Length > 0)
            .ToList();

        // Ciò che si scrive in un documento vuoto esce con questo: senza
        // fissarlo, una nota nuova si salvava con il carattere che il
        // controllo porta di serie e non con il Calibri 11 di sempre.
        var comePredefinito = doc.GetDefaultCharacterFormat();
        Vesti(comePredefinito, Regole.CreaFrammento(""));
        doc.SetDefaultCharacterFormat(comePredefinito);

        doc.BatchDisplayUpdates();

        try
        {
            doc.SetText(TextSetOptions.None,
                        string.Concat(pezzetti.Select(x => x.Testo)));

            int i = 0;

            foreach (var (frag, testo) in pezzetti)
            {
                var intervallo = doc.GetRange(i, i + testo.Length);

                var cf = intervallo.CharacterFormat;
                Vesti(cf, frag);
                intervallo.CharacterFormat = cf;

                i += testo.Length;
            }
        }
        finally
        {
            doc.ApplyDisplayUpdates();
        }

        // Il cursore all'inizio e senza selezionare niente: aprire un
        // salvato con tutto il testo selezionato invita a cancellarlo con
        // un tasto.
        doc.Selection.SetRange(0, 0);
    }

    static void Vesti(ITextCharacterFormat cf, Frammento f)
    {
        cf.Name = f.F;
        cf.Size = f.S;
        cf.Bold = f.B != 0 ? FormatEffect.On : FormatEffect.Off;
        cf.Italic = f.I != 0 ? FormatEffect.On : FormatEffect.Off;
        cf.Underline = f.U != 0 ? UnderlineType.Single : UnderlineType.None;
        cf.ForegroundColor = Stile.Da(f.C);
    }

    // ------------------------------------------------------- leggere

    /// <summary>
    /// Il testo semplice, che è ciò che si incolla senza formato, ciò che
    /// si cerca e da cui esce il titolo.
    /// </summary>
    public static string TestoSemplice(RichEditBox casella)
    {
        casella.Document.GetText(TextGetOptions.None, out string t);

        // Il documento finisce sempre con un a capo di paragrafo che
        // l'utente non ha scritto.
        return t.TrimEnd('\r');
    }

    /// <summary>
    /// Il documento convertito in frammenti. Si percorre per tratti di
    /// formato uguale —<c>TextRangeUnit.CharacterFormat</c>, che la
    /// documentazione definisce come "un tratto di caratteri con le
    /// stesse proprietà di formato"—, non carattere per carattere: con
    /// un modello di cento linee sarebbero migliaia di chiamate.
    /// </summary>
    public static List<Frammento> Leggi(RichEditBox casella)
    {
        var doc = casella.Document;
        string testo = TestoSemplice(casella);

        var uscita = new List<Frammento>();
        if (testo.Length == 0) return uscita;

        int i = 0;

        // Limite di giri: se il percorso per tratti non avanzasse, questo
        // sarebbe un ciclo infinito con la finestra bloccata.
        for (int giro = 0; i < testo.Length && giro <= testo.Length; giro++)
        {
            var segno = doc.GetRange(i, i);
            segno.MoveEnd(TextRangeUnit.CharacterFormat, 1);

            int fine = segno.EndPosition;
            if (fine <= i || fine > testo.Length) fine = testo.Length;

            var f = Di(doc.GetRange(i, fine).CharacterFormat, testo[i..fine]);

            if (uscita.Count > 0 && Regole.StessoFormato(uscita[^1], f))
                uscita[^1].T += f.T;
            else
                uscita.Add(f);

            i = fine;
        }

        // Il formato è un ornamento; il testo no. Se il percorso non ha
        // riprodotto il documento carattere per carattere, si salva
        // semplice piuttosto che salvare male — e si lascia detto perché.
        string riarmato = Regole.TestoDi(uscita);

        if (riarmato != testo)
        {
            Registro.Annota(
                $"il percorso per tratti di formato ha dato {riarmato.Length} "
                + $"caratteri e il documento ne ha {testo.Length}; "
                + "si salva senza formato per non perdere testo");

            return [Regole.CreaFrammento(testo)];
        }

        return uscita;
    }

    static Frammento Di(ITextCharacterFormat cf, string testo)
    {
        string carattere = cf.Name;
        if (string.IsNullOrWhiteSpace(carattere)) carattere = Config.CarattereDef;

        int dim = (int)Math.Round(cf.Size);
        if (dim <= 0) dim = Config.DimDef;

        return Regole.CreaFrammento(
            testo,
            carattere,
            dim,
            cf.Bold == FormatEffect.On ? 1 : 0,
            cf.Italic == FormatEffect.On ? 1 : 0,
            cf.Underline is UnderlineType.None or UnderlineType.Undefined ? 0 : 1,
            AHex(cf.ForegroundColor));
    }

    // ------------------------------------------------- la selezione

    /// <summary>Ciò che è selezionato, o dove si trova il cursore.</summary>
    public static ITextSelection Selezione(RichEditBox casella) =>
        casella.Document.Selection;

    public static void Grassetto(RichEditBox casella) =>
        Cambia(casella, cf => cf.Bold = Alterna(cf.Bold));

    public static void Corsivo(RichEditBox casella) =>
        Cambia(casella, cf => cf.Italic = Alterna(cf.Italic));

    public static void Sottolineato(RichEditBox casella) =>
        Cambia(casella, cf => cf.Underline =
            cf.Underline == UnderlineType.Single
                ? UnderlineType.None
                : UnderlineType.Single);

    public static void Carattere(RichEditBox casella, string nome) =>
        Cambia(casella, cf => cf.Name = nome);

    public static void Dimensione(RichEditBox casella, int dim) =>
        Cambia(casella, cf => cf.Size = dim);

    public static void Colore(RichEditBox casella, string hex) =>
        Cambia(casella, cf => cf.ForegroundColor = Stile.Da(hex));

    /// <summary>Riporta tutto a Calibri 11 nero e senza ornamenti.</summary>
    public static void Pulisci(RichEditBox casella) => Cambia(casella, cf =>
    {
        cf.Name = Config.CarattereDef;
        cf.Size = Config.DimDef;
        cf.Bold = FormatEffect.Off;
        cf.Italic = FormatEffect.Off;
        cf.Underline = UnderlineType.None;
        cf.ForegroundColor = Stile.Da(Config.ColoreDef);
    });

    /// <summary>
    /// <c>FormatEffect.Undefined</c> esce quando la selezione mescola
    /// pezzetti in grassetto e pezzetti senza. Lì si mette il grassetto
    /// a tutto, che è ciò che fa Word.
    /// </summary>
    static FormatEffect Alterna(FormatEffect attuale) =>
        attuale == FormatEffect.On ? FormatEffect.Off : FormatEffect.On;

    static void Cambia(RichEditBox casella, Action<ITextCharacterFormat> che)
    {
        var sel = casella.Document.Selection;
        if (sel is null) return;

        var cf = sel.CharacterFormat;
        che(cf);
        sel.CharacterFormat = cf;

        casella.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
    }

    // ------------------------------------------ punti elenco e rientri

    public static void PuntiElenco(RichEditBox casella) =>
        PerLinee(casella, Regole.AlternaPuntiElenco);

    public static void Numeri(RichEditBox casella) =>
        PerLinee(casella, Regole.AlternaNumeri);

    public static void Rientro(RichEditBox casella, bool piu) =>
        PerLinee(casella, b => Regole.Rientra(b, piu));

    /// <summary>
    /// Applica un cambio di inizio linea ai paragrafi toccati.
    ///
    /// **Si riscrive solo il pezzetto che cambia**, non l'intera linea.
    /// Sostituire il paragrafo completo dà a tutto il formato del primo
    /// carattere: mettere un punto elenco a una linea con una parola in
    /// grassetto si portava via il grassetto.
    /// </summary>
    static void PerLinee(RichEditBox casella, Func<string, string> cambio)
    {
        var doc = casella.Document;
        var sel = doc.Selection;
        if (sel is null) return;

        int a = Math.Min(sel.StartPosition, sel.EndPosition);
        int b = Math.Max(sel.StartPosition, sel.EndPosition);

        var blocco = doc.GetRange(a, b);
        blocco.Expand(TextRangeUnit.Paragraph);

        int inizio = blocco.StartPosition;
        string prima = blocco.Text;

        // Expand si porta dietro anche l'a capo finale dell'ultimo
        // paragrafo, e con quello compariva una linea vuota in più alla
        // fine.
        bool acapo = prima.EndsWith('\r');
        if (acapo) prima = prima[..^1];

        string dopo = cambio(prima);
        if (dopo == prima) return;

        var vecchie = prima.Split('\r');
        var nuove = dopo.Split('\r');

        if (vecchie.Length != nuove.Length) return;

        doc.BatchDisplayUpdates();

        try
        {
            // Da dietro in avanti: ogni cambio sposta ciò che viene dopo, e
            // le posizioni davanti restano valide.
            int da = inizio + prima.Length;

            for (int i = vecchie.Length - 1; i >= 0; i--)
            {
                da -= vecchie[i].Length;

                int comune = 0;

                while (comune < vecchie[i].Length && comune < nuove[i].Length
                       && vecchie[i][^(comune + 1)] == nuove[i][^(comune + 1)])
                {
                    comune++;
                }

                int toglie = vecchie[i].Length - comune;
                string metti = nuove[i][..(nuove[i].Length - comune)];

                if (toglie > 0 || metti.Length > 0)
                    doc.GetRange(da, da + toglie).Text = metti;

                da--;   // il \r che separa questa linea dalla precedente
            }
        }
        finally
        {
            doc.ApplyDisplayUpdates();
        }

        casella.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
    }
}
