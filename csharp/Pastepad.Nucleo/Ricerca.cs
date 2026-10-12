using System.Text;

namespace Pastepad.Nucleo;

public enum TipoRisultato
{
    /// <summary>Uno snippet salvato apposta.</summary>
    Salvato,

    /// <summary>Una voce della cronologia.</summary>
    Cronologia,
}

public readonly record struct Risultato(Elemento Dato, TipoRisultato Tipo);

/// <summary>Ricerca con punteggio. Indipendente dall'interfaccia.</summary>
public static class Ricerca
{
    public const int LimiteTesto = 4000;

    /// <summary>
    /// Minuscole e senza accenti, perché "citta" trovi "città" e
    /// viceversa.
    /// </summary>
    public static string Normalizza(string testo)
    {
        if (string.IsNullOrEmpty(testo)) return "";

        string taglio = testo.Length > LimiteTesto ? testo[..LimiteTesto] : testo;

        var uscita = new StringBuilder(taglio.Length);

        foreach (char c in taglio.ToLowerInvariant())
        {
            uscita.Append(c switch
            {
                'á' or 'à' => 'a',
                'é' or 'è' => 'e',
                'í' or 'ì' => 'i',
                'ó' or 'ò' => 'o',
                'ú' or 'ù' or 'ü' => 'u',
                'ñ' => 'n',
                _ => c,
            });
        }

        return uscita.ToString();
    }

    static readonly char[] _tagliTitolo = [' ', '-', '_', '.', ':', ',', '/', '(', ')'];
    static readonly char[] _tagliCorpo = [' ', '-', '_', '.', ':', ',', '/', '(', ')', '\n'];

    /// <summary>
    /// Quanto somiglia, o null se non coincide. Ogni parola deve stare
    /// da qualche parte. Pesa di più se compare nel titolo, se inizia
    /// una parola e se è vicina all'inizio.
    /// </summary>
    public static int? DaiPunteggio(
        IReadOnlyList<string> parole, string titolo, string corpo)
    {
        int totale = 0;

        foreach (var parola in parole)
        {
            int posT = titolo.IndexOf(parola, StringComparison.Ordinal);
            int posC = corpo.IndexOf(parola, StringComparison.Ordinal);

            if (posT < 0 && posC < 0) return null;

            if (posT >= 0)
            {
                totale += 100;

                if (posT == 0 || _tagliTitolo.Contains(titolo[posT - 1]))
                    totale += 60;

                totale += Math.Max(0, 25 - posT / 2);
            }
            else
            {
                totale += 30;

                if (posC == 0 || _tagliCorpo.Contains(corpo[posC - 1]))
                    totale += 20;

                totale += Math.Max(0, 15 - posC / 40);
            }
        }

        // La frase intera e in ordine vale più delle parole sciolte.
        if (parole.Count > 1 &&
            (titolo + " " + corpo).Contains(
                string.Join(" ", parole), StringComparison.Ordinal))
            totale += 80;

        return totale;
    }

    public static string[] Parole(string query) =>
        Normalizza(query).Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries);
}

/// <summary>
/// Conserva il testo già normalizzato di ogni voce. Normalizzare 80
/// testi lunghi costa circa 25 ms; senza questa cache succedeva ogni
/// volta che si copiava qualcosa.
/// </summary>
public sealed class Indice(Archivio archivio)
{
    readonly record struct Salvato(object? SegnoSpunta, string Titolo, string Corpo);

    readonly record struct Riga(
        Elemento Dato, TipoRisultato Tipo, string Titolo, string Corpo);

    readonly Archivio _archivio = archivio;

    // La chiave è l'identità dell'oggetto, non il suo contenuto: è ciò
    // che permette di notare che una voce ha cambiato testo senza
    // percorrerlo.
    readonly Dictionary<Elemento, Salvato> _cache =
        new(ReferenceEqualityComparer.Instance);

    List<Riga>? _lista;

    public void Invalida() => _lista = null;

    Salvato Normalizzato(Elemento dato, TipoRisultato tipo)
    {
        object? segno = dato switch
        {
            Voce e => e.Testo,
            Snippet s => s.Titolo,
            _ => null,
        };

        if (_cache.TryGetValue(dato, out var salvato) &&
            ReferenceEquals(salvato.SegnoSpunta, segno))
            return salvato;

        Salvato nuovo;

        if (tipo == TipoRisultato.Salvato)
        {
            var s = (Snippet)dato;
            nuovo = new Salvato(
                segno,
                Ricerca.Normalizza(s.Titolo),
                Ricerca.Normalizza(Regole.TestoDi(s.Runs)));
        }
        else
        {
            var e = (Voce)dato;

            if (e.EUnImmagine)
            {
                nuovo = new Salvato(segno, "immagine cattura", "");
            }
            else
            {
                string t = e.Testo ?? "";
                nuovo = new Salvato(
                    segno,
                    Ricerca.Normalizza(Regole.UnaLinea(t, 80)),
                    Ricerca.Normalizza(t));
            }
        }

        _cache[dato] = nuovo;
        return nuovo;
    }

    List<Riga> Voci()
    {
        if (_lista is not null) return _lista;

        var lista = new List<Riga>();

        foreach (var g in _archivio.Snippets)
        {
            var n = Normalizzato(g, TipoRisultato.Salvato);
            lista.Add(new Riga(g, TipoRisultato.Salvato, n.Titolo, n.Corpo));
        }

        foreach (var h in _archivio.CronOrdinata())
        {
            var n = Normalizzato(h, TipoRisultato.Cronologia);
            lista.Add(new Riga(h, TipoRisultato.Cronologia, n.Titolo, n.Corpo));
        }

        // Ciò che non sta più nell'archivio si toglie dalla cache, o
        // crescerebbe senza limite finché il programma resta aperto.
        var vivi = new HashSet<Elemento>(
            lista.Select(f => f.Dato), ReferenceEqualityComparer.Instance);

        foreach (var morto in _cache.Keys.Where(k => !vivi.Contains(k)).ToList())
            _cache.Remove(morto);

        _lista = lista;
        return lista;
    }

    /// <summary>Quante voci sono indicizzate in questo momento.</summary>
    public int Quante() => Voci().Count;

    /// <summary>I risultati ordinati per somiglianza.</summary>
    public List<Risultato> Cerca(string query)
    {
        var parole = Ricerca.Parole(query);
        if (parole.Length == 0) return [];

        var conPunteggio = new List<(int Punteggio, Riga Riga)>();

        foreach (var riga in Voci())
        {
            int? p = Ricerca.DaiPunteggio(parole, riga.Titolo, riga.Corpo);
            if (p is null) continue;

            // Ciò che è salvato apposta pesa più di ciò che è copiato di
            // passaggio.
            if (riga.Tipo == TipoRisultato.Salvato) p += 40;

            conPunteggio.Add((p.Value, riga));
        }

        // OrderByDescending è stabile: a parità di punteggio si conserva
        // l'ordine di ingresso, come faceva il sort di Python.
        return [.. conPunteggio
            .OrderByDescending(x => x.Punteggio)
            .Select(x => new Risultato(x.Riga.Dato, x.Riga.Tipo))];
    }
}
