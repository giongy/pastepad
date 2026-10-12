namespace Pastepad.Nucleo;

/// <summary>
/// Comporre la riga di comando con cui Windows deve riaprirci dopo un
/// aggiornamento.
///
/// Vive nel nucleo e non accanto alla chiamata di sistema per il solito
/// motivo: è la parte che può sbagliare in silenzio. Se le virgolette
/// falliscono, pastepad non esplode —si riapre tranquillamente su
/// UN'ALTRA cartella di dati—, e la cartella accanto è la cronologia
/// reale dell'utente.
///
/// Il percorso di prova con cui si verificò tutto questo era quello
/// corto in formato 8.3 e non aveva nemmeno uno spazio, quindi il ramo
/// che conta non fu eseguito nemmeno una volta. Il desktop dell'utente
/// invece gli spazi li ha.
/// </summary>
public static class Argomenti
{
    /// <summary>
    /// Unisce gli argomenti in una sola riga, mettendo tra virgolette
    /// quelli che contengono spazi. Il nome dell'eseguibile resta fuori
    /// apposta: RegisterApplicationRestart dice "do not include the name
    /// of the executable in the command line; this function adds it for
    /// you".
    ///
    /// Restituisce null se non c'è niente da registrare, perché per
    /// quella API una stringa vuota e null significano la stessa cosa
    /// —elimina ciò che è registrato— e così chi chiama non deve
    /// ricordarselo.
    /// </summary>
    public static string? Componi(IEnumerable<string> parti)
    {
        var linea = string.Join(' ', parti.Select(MettiTraVirgolette));
        return linea.Length == 0 ? null : linea;
    }

    static string MettiTraVirgolette(string parte)
    {
        // Senza spazi non si tocca: nemmeno mettere virgolette in più è
        // gratis, ci sono programmi che le virgolette se le mangiano alla
        // lettera.
        if (!parte.Contains(' ')) return parte;

        // Se arrivava già tra virgolette, non si avvolge due volte.
        if (parte.Length >= 2 && parte[0] == '"' && parte[^1] == '"')
            return parte;

        return '"' + parte + '"';
    }
}
