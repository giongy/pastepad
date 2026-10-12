namespace Pastepad.Nucleo;

/// <summary>
/// Se pastepad deve avviarsi con Windows, secondo la preferenza
/// <c>avvio_automatico</c> di config.json.
///
/// La decisione vive nel nucleo e non accanto al registro di Windows per
/// due motivi: si può provare senza toccare HKCU —che è globale ed è
/// dell'utente, non della prova— e lascia in un solo posto la regola che
/// la versione precedente aveva sparsa nell'avvio.
///
/// Comanda l'applicazione e solo lei. L'installer non scrive più in
/// HKCU\...\Run: con due padroni dello stesso valore, uno lo mette e
/// l'altro lo cancella, e in più disinstallare si portava via una
/// preferenza che l'utente aveva scelto.
/// </summary>
public static class AvvioAutomatico
{
    /// <summary>
    /// Di fabbrica ci si avvia con Windows. È ciò che faceva la versione
    /// precedente ed è ragionevole per qualcosa che vive nell'area di
    /// notifica: un gestore di appunti che bisogna aprire a mano non
    /// sostituisce Win+V.
    /// </summary>
    public const string ComePredefinito = "si";

    /// <summary>Come si chiama la preferenza in config.json.</summary>
    public const string Chiave = "avvio_automatico";

    /// <summary>
    /// Solo un "si" accende l'avvio automatico, come nella versione
    /// precedente, che confrontava la preferenza con "si" tale e quale.
    /// Qui si ignorano maiuscole e spazi di troppo —un config.json si
    /// modifica a mano e "Si " non dovrebbe significare altro—, ma il
    /// criterio non cambia: ciò che non è un si, non lo è.
    /// </summary>
    public static bool Vuole(string? preferenza) =>
        (preferenza ?? ComePredefinito).Trim()
            .Equals(ComePredefinito, StringComparison.OrdinalIgnoreCase);
}
