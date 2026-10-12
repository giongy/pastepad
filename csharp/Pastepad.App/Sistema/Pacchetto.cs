namespace Pastepad.App.Sistema;

/// <summary>
/// Se i file di pastepad stanno finendo in un posto diverso da quello
/// che pastepad crede.
///
/// **Non dovrebbe succedere mai e per questo va rilevato.** Quando
/// pastepad viene aperto da un'altra applicazione impacchettata, ne
/// eredita il contenitore e Windows reindirizza le sue scritture in
/// <c>%LOCALAPPDATA%</c> verso
/// <c>...\Packages\&lt;pacchetto&gt;\LocalCache\Local</c>. pastepad
/// calcola bene il suo percorso, crede di leggerlo, e in realtà legge e
/// scrive su una copia. L'utente lo apre dopo dal suo posto e **la sua
/// cronologia e i suoi testi sono spariti**, anche se sul disco sono
/// intatti.
///
/// È successo davvero il 14 agosto 2026: pastepad fu lanciato da una
/// sessione che viveva dentro un'altra applicazione impacchettata,
/// passò ore a catturare gli appunti dentro la copia reindirizzata, e
/// quando l'utente lo aprì dal menu Start trovò la cartella vera senza
/// <c>snippets.json</c>. Partì da zero senza dire niente, che è ciò che
/// trasformò un incidente dell'ambiente in «si è cancellato tutto».
///
/// **L'identità di pacchetto non serve a rilevarlo, anche se sembra.**
/// Si provò prima con <c>GetCurrentPackageFullName</c> e restituisce
/// <c>APPMODEL_ERROR_NO_PACKAGE</c>: il processo figlio eredita il
/// reindirizzamento dei file ma NON l'identità. Misurato lanciando
/// pastepad da dentro il contenitore — l'avviso non uscì.
///
/// Ciò che funziona è chiedere di un FILE, non del processo né della
/// cartella. <c>GetFinalPathNameByHandle</c> restituisce il percorso
/// reale dietro un handle, e lì il reindirizzamento si vede intero.
///
/// **E deve essere un file appena scritto.** Misurati, i tre casi
/// nello stesso posto reindirizzato:
///
/// - la cartella              -> si risolve in se stessa. Non rivela niente.
/// - un file che c'era già    -> nemmeno, finché qualcuno non lo scrive:
///   il reindirizzamento è copia-alla-scrittura, file per file.
/// - una sonda appena creata  -> esce sotto <c>...\Packages\...</c>
///
/// Che si debba scrivere per vederlo non è un inconveniente: scrivere è
/// proprio ciò che si vuole controllare prima di lasciare che il
/// programma salvi qualcosa.
/// </summary>
internal static class Pacchetto
{
    /// <summary>
    /// Come si chiama la sonda. Inizia con un punto perché non dia
    /// fastidio se mai sopravvivesse, anche se non dovrebbe: si crea con
    /// <c>DELETE_ON_CLOSE</c>, quindi Windows la cancella al rilascio
    /// dell'handle anche se il processo muore di colpo.
    /// </summary>
    const string Sonda = ".pastepad-sonda";

    /// <summary>
    /// Dove finiscono davvero i file della cartella dati, se non è dove
    /// sono stati chiesti. Null quando tutto è al suo posto, che è il
    /// caso normale.
    /// </summary>
    public static string? PercorsoDeviato(string cartella)
    {
        try
        {
            if (!Directory.Exists(cartella)) return null;

            nint handle = Nativo.CreateFileW(
                Path.Combine(cartella, Sonda),
                Nativo.GENERIC_WRITE,
                0,
                0,
                Nativo.CREATE_ALWAYS,
                Nativo.FILE_FLAG_DELETE_ON_CLOSE,
                0);

            if (handle == Nativo.HANDLE_NON_VALIDO)
            {
                // Che non si riesca a scrivere la sonda è già di per sé un
                // problema, ma di altro tipo: di quello si occupa l'archivio
                // quando non riesce a scrivere i suoi file.
                Registro.Annota(
                    "impossibile creare la sonda nella cartella dei dati, errore "
                    + System.Runtime.InteropServices.Marshal.GetLastWin32Error());

                return null;
            }

            try
            {
                var buffer = new char[1024];
                uint lungo = Nativo.GetFinalPathNameByHandleW(
                    handle, buffer, (uint)buffer.Length, 0);

                if (lungo == 0 || lungo > buffer.Length)
                {
                    Registro.Annota(
                        "GetFinalPathNameByHandle non ha detto il percorso reale, errore "
                        + System.Runtime.InteropServices.Marshal.GetLastWin32Error());

                    return null;
                }

                string reale = new(buffer, 0, (int)lungo);

                // Arriva in forma lunga: \\?\C:\... Si toglie per poterlo
                // confrontare con quello chiesto dal programma.
                if (reale.StartsWith(@"\\?\", StringComparison.Ordinal))
                    reale = reale[4..];

                // Si confronta la CARTELLA della sonda, non la sonda: ciò che
                // conta è dove finisce ciò che si scrive, non il nome del
                // file con cui lo si è scoperto.
                string dove = Path.GetDirectoryName(reale) ?? reale;

                return SonoLaStessa(dove, cartella) ? null : dove;
            }
            finally
            {
                Nativo.CloseHandle(handle);
            }
        }
        catch (Exception e)
        {
            // Largo apposta: è un controllo di diagnosi nella prima riga
            // dell'avvio, e non può impedire al programma di aprirsi.
            Registro.Errore("controllare il percorso reale della cartella dei dati", e);
            return null;
        }
    }

    /// <summary>
    /// Senza barra finale e senza distinguere maiuscole, che in Windows
    /// è lo stesso nome.
    /// </summary>
    static bool SonoLaStessa(string a, string b) =>
        string.Equals(
            Path.TrimEndingDirectorySeparator(a),
            Path.TrimEndingDirectorySeparator(b),
            StringComparison.OrdinalIgnoreCase);
}
