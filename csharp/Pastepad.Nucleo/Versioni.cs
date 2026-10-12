using System.Globalization;

namespace Pastepad.Nucleo;

/// <summary>
/// Confrontare versioni e decidere quando tocca controllare e quando
/// tocca avvisare.
///
/// Vive nel nucleo e separato dalla parte che parla con GitHub perché è
/// l'unica cosa che si può provare senza rete: il confronto è dove
/// davvero si può sbagliare, e in silenzio.
/// </summary>
public static class Versioni
{
    /// <summary>Come si chiamano le preferenze in config.json.</summary>
    public const string ChiaveAvvisa = "avvisa_aggiornamenti";
    public const string ChiaveControllo = "ultimo_controllo";
    public const string ChiaveAvvisata = "versione_avvisata";

    /// <summary>
    /// Di fabbrica si avvisa. Chi non lo vuole lo spegne in Aspetto: un
    /// programma che chiama casa senza che si possa dire di no è un
    /// programma che non rispetta chi lo usa.
    /// </summary>
    public const bool AvvisaDef = true;

    /// <summary>Come si salva la data: ISO, senza dipendere dalla lingua.</summary>
    public const string FormatoData = "yyyy-MM-dd";

    /// <summary>
    /// Il tag di GitHub arriva come "v4.0.1"; la versione dell'assembly
    /// è "4.0.1". Si toglie la v e nient'altro.
    /// </summary>
    public static string SenzaLaV(string? tag)
    {
        string t = (tag ?? "").Trim();

        return t.StartsWith('v') || t.StartsWith('V') ? t[1..] : t;
    }

    /// <summary>
    /// Confronta come numeri e non come testo. È la trappola: come
    /// stringhe, "4.0.10" esce MINORE di "4.0.9", perché confronta l'"1"
    /// con il "9". Bastano dieci pubblicazioni di correzioni perché
    /// l'avviso smetta di comparire senza che nessuno se ne accorga.
    ///
    /// Ciò che non si riesce a interpretare —una versione con lettere,
    /// un tag con suffisso di prova— restituisce false: meglio non
    /// avvisare che avvisare di qualsiasi cosa.
    /// </summary>
    public static bool CeNovita(string? installata, string? pubblicata)
    {
        if (!Interpreta(installata, out var mia)) return false;
        if (!Interpreta(pubblicata, out var sua)) return false;

        return sua > mia;
    }

    /// <summary>
    /// Con le quattro parti sempre presenti. System.Version tratta
    /// quelle che mancano come -1, quindi "4.0.1" gli esce minore di
    /// "4.0.1.0" e si avviserebbe di una versione che è la stessa.
    /// </summary>
    static bool Interpreta(string? testo, out Version versione)
    {
        versione = new Version(0, 0);

        if (!Version.TryParse(SenzaLaV(testo), out var v)) return false;

        versione = new Version(
            v.Major, v.Minor, Math.Max(v.Build, 0), Math.Max(v.Revision, 0));

        return true;
    }

    /// <summary>
    /// Una volta al giorno. Se la data salvata non si capisce o non
    /// esiste, tocca: è il primo avvio o qualcuno ha modificato il file a
    /// mano.
    /// </summary>
    public static bool ToccaControllare(string? ultima, DateTimeOffset adesso)
    {
        if (!DateTime.TryParseExact(ultima, FormatoData,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var data))
        {
            return true;
        }

        return data.Date < adesso.Date;
    }

    public static string Oggi(DateTimeOffset adesso) =>
        adesso.ToString(FormatoData, CultureInfo.InvariantCulture);

    /// <summary>
    /// Si avvisa di ogni versione una sola volta. Senza questo, chi non
    /// vuole ancora aggiornare si becca la stessa fascia ogni giorno, e
    /// una fascia che esce sempre si smette di leggerla.
    /// </summary>
    public static bool ToccaAvvisare(
        string? installata, string? pubblicata, string? giaAvvisata)
    {
        if (!CeNovita(installata, pubblicata)) return false;

        return !Interpreta(giaAvvisata, out var avvisata)
               || !Interpreta(pubblicata, out var sua)
               || sua != avvisata;
    }
}
