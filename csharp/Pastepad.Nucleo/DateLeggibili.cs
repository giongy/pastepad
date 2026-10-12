namespace Pastepad.Nucleo;

/// <summary>
/// Mette una data salvata in parole, per la riga sotto il titolo di una
/// nota.
///
/// Vive nel nucleo e riceve l'«adesso» come parametro per il solito
/// motivo: così si può provare senza dipendere da che ora è quando
/// girano le prove.
/// </summary>
public static class DateLeggibili
{
    /// <summary>
    /// «proprio ora», «5 min fa», «ieri 09:30», o la data corta se sono
    /// già passati dei giorni.
    ///
    /// Una stringa che non si riesce a interpretare restituisce vuoto
    /// invece di lanciare: note.json si può modificare a mano, e una data
    /// rotta non può far cadere l'intera lista — al massimo quella riga
    /// resta senza la sua riga di sotto.
    /// </summary>
    public static string Leggibile(string? iso, DateTimeOffset adesso)
    {
        if (!DateTimeOffset.TryParse(iso, out var quando)) return "";

        var passato = adesso - quando;

        // Una data nel futuro non è un caso che valga la pena distinguere
        // —un orologio regolato male, un file copiato da un'altra macchina—,
        // ma bisogna evitare che esca «-40 minuti fa».
        if (passato < TimeSpan.FromMinutes(1)) return "proprio ora";

        if (passato < TimeSpan.FromHours(1))
            return Testi.Componi("%d min fa", (int)passato.TotalMinutes);

        var giorno = quando.ToLocalTime().Date;
        var oggi = adesso.ToLocalTime().Date;

        // Per giorni di calendario e non per ore trascorse: alle 00:30,
        // qualcosa delle 23:00 è «ieri» anche se è passata un'ora e mezza.
        if (giorno == oggi) return Testi.Componi("oggi %s", Ora(quando));
        if (giorno == oggi.AddDays(-1)) return Testi.Componi("ieri %s", Ora(quando));

        return quando.ToLocalTime().ToString("d");
    }

    static string Ora(DateTimeOffset quando) =>
        quando.ToLocalTime().ToString("t");
}
