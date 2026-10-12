namespace Pastepad.Nucleo;

/// <summary>
/// Le frasi dell'interfaccia che portano un valore dentro.
///
/// L'applicazione parla una sola lingua, l'italiano, e le frasi stanno
/// scritte dove si usano. Qui resta solo ciò che non è un semplice
/// testo: riempire i buchi %s e %d di una frase.
///
/// Vive nel nucleo e non nell'interfaccia perché non ha bisogno di
/// niente di WinUI —è uno string.Format— e così le prove possono
/// controllarlo senza aprire nessuna finestra.
/// </summary>
public static class Testi
{
    /// <summary>
    /// Riempie i buchi %s e %d, numerandoli nell'ordine in cui
    /// compaiono.
    ///
    /// Numerarli conta: una versione precedente metteva {0} in tutti, e
    /// "Eliminare la cartella %s e i suoi %d testi?" usciva come
    /// "Eliminare la cartella 3 e i suoi 3 testi?" — i due buchi
    /// ricevevano lo stesso valore. Con un solo buco nessuno se ne
    /// accorse.
    /// </summary>
    public static string Componi(string frase, params object[] valori)
    {
        var stampo = new System.Text.StringBuilder(frase.Length + 8);
        int buco = 0;

        for (int i = 0; i < frase.Length; i++)
        {
            if (frase[i] == '%' && i + 1 < frase.Length
                && frase[i + 1] is 's' or 'd')
            {
                stampo.Append('{').Append(buco++).Append('}');
                i++;
                continue;
            }

            // Una graffa che arrivi nel testo è letterale e va
            // raddoppiata, o string.Format la prende per un buco suo.
            if (frase[i] is '{' or '}') stampo.Append(frase[i]);

            stampo.Append(frase[i]);
        }

        return string.Format(stampo.ToString(), valori);
    }
}
