using System.Security.Cryptography;
using System.Text;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Pastepad.App.Sistema;

namespace Pastepad.App;

/// <summary>
/// Main proprio, al posto di quello generato da XAML. L'istanza unica
/// va decisa prima di creare qualsiasi finestra: due processi che si
/// contendevano la scorciatoia globale furono il motivo per cui il
/// programma sembrò rotto per giorni — Windows la dà a uno solo, e chi
/// perde resta come una finestra muta.
/// </summary>
public static class Program
{
    const string CHIAVE_ISTANZA = "pastepad-3ff1c0de";

    /// <summary>
    /// Cartella dati alternativa, da `--dati &lt;percorso&gt;`. Esiste per
    /// poter provare senza toccare la cronologia vera dell'utente: la
    /// cartella di sempre è quella della versione installata, e una
    /// sessione di prove si è già portata via delle sue voci.
    /// </summary>
    internal static string? CartellaDati { get; private set; }

    /// <summary>
    /// Ciò che è fallito risolvendo il percorso di <c>--dati</c>, se è
    /// fallito. Non si annota sul momento: registrare prima di conoscere
    /// la cartella dati inchioderebbe il log in quella di sempre, che è
    /// proprio ciò che <c>--dati</c> vuole evitare.
    /// </summary>
    static Exception? _percorsoErrato;

    /// <summary>
    /// L'istante della prima riga di <c>Main</c>. Il requisito numero due
    /// del progetto è che aprire il pannello sia istantaneo, e da tre
    /// versioni non c'era un solo numero dietro. Si assegna in
    /// un'istruzione e non in un inizializzatore di campo perché sia
    /// davvero la prima riga.
    /// </summary>
    internal static long Avvio { get; private set; }

    [STAThread]
    static void Main(string[] args)
    {
        Avvio = System.Diagnostics.Stopwatch.GetTimestamp();

        // Prima di tutto: il registro e l'istanza unica dipendono dalla
        // cartella dati.
        LeggiArgomenti(args);

        Registro.AgganciaGlobali();
        Registro.AnnotaAvvio();

        if (_percorsoErrato is not null)
            Registro.Errore("--dati: non è stato possibile risolvere il percorso", _percorsoErrato);

        if (CartellaDati is not null)
            Registro.Annota($"dati in una cartella alternativa: {CartellaDati}");

        if (!SonoLIstanzaBuona())
        {
            // Che l'utente lanci di nuovo il programma si intende come
            // "voglio vederlo", non come "aprine un altro".
            bool avvisato = Cassetta.ChiediDiMostrarsi();

            Registro.Annota(avvisato
                ? "c'era già un'altra istanza; le è stato chiesto di mostrarsi"
                : "c'era già un'altra istanza, ma non ha risposto alla richiesta");

            return;
        }

        WinRT.ComWrappersSupport.InitializeComWrappers();

        Application.Start(p =>
        {
            var coda = DispatcherQueue.GetForCurrentThread();
            SynchronizationContext.SetSynchronizationContext(
                new DispatcherQueueSynchronizationContext(coda));
            _ = new App();
        });
    }

    static void LeggiArgomenti(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals("--dati", StringComparison.OrdinalIgnoreCase))
            {
                CartellaDati = Normalizza(args[i + 1]);
                return;
            }
        }
    }

    /// <summary>
    /// In percorso assoluto e senza barra finale. Due avvii che scrivono
    /// nella stessa cartella devono dare la stessa chiave di istanza, e
    /// <c>dati</c>, <c>.\dati\</c> e <c>C:\...\dati</c> sono la stessa
    /// cartella scritta in tre modi.
    /// </summary>
    static string Normalizza(string percorso)
    {
        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(percorso));
        }
        catch (Exception e) when (e is ArgumentException
                                    or NotSupportedException
                                    or PathTooLongException)
        {
            _percorsoErrato = e;

            // Si prosegue con ciò che è arrivato: l'archivio dirà cosa
            // succede provando a scrivere lì, e quello l'utente lo vede.
            return percorso;
        }
    }

    /// <summary>
    /// La chiave di istanza porta dentro la cartella dati: altrimenti
    /// un'istanza di prova cederebbe il controllo a quella vera e non
    /// arriverebbe a partire.
    /// </summary>
    static string Chiave() => CartellaDati is null
        ? CHIAVE_ISTANZA
        : CHIAVE_ISTANZA + "-" + Riassunto(CartellaDati);

    /// <summary>
    /// Riassunto stabile del percorso: 16 cifre esadecimali di SHA-256
    /// su di esso in minuscolo, che in Windows è lo stesso nome.
    ///
    /// Non è String.GetHashCode e non può esserlo: in .NET Core è
    /// randomizzato per processo, quindi ogni avvio produceva una chiave
    /// diversa e con <c>--dati</c> l'istanza unica non funzionava — due
    /// processi che si contendevano la scorciatoia globale, che è
    /// esattamente l'errore per cui il programma sembrò rotto per giorni.
    /// Qui non gli si chiede niente di crittografico, solo che sia lo
    /// stesso numero in tutti i processi e in tutti gli avvii.
    /// </summary>
    static string Riassunto(string percorso) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(percorso.ToLowerInvariant())))[..16];

    static bool SonoLIstanzaBuona()
    {
        try
        {
            var proprietaria = AppInstance.FindOrRegisterForKey(Chiave());
            if (proprietaria.IsCurrent) return true;

            // Se la cessione non va a buon fine, quella che era registrata
            // non esiste più: partire è meglio che restare a metà.
            return !Reindirizza(proprietaria, AppInstance.GetCurrent().GetActivatedEventArgs());
        }
        catch (Exception e)
        {
            Registro.Errore("istanza unica", e);

            // Senza questo meccanismo è preferibile partire che non
            // partire: la cassetta qui sotto continua a rilevare chi c'era
            // già.
            return !Cassetta.ChiediDiMostrarsi();
        }
    }

    /// <summary>
    /// Cede l'attivazione all'istanza che già comanda. Va in un altro
    /// thread perché la chiamata ha bisogno che questo non sia bloccato,
    /// e qui non c'è ancora un ciclo dei messaggi.
    /// </summary>
    static bool Reindirizza(AppInstance proprietaria, AppActivationArguments args)
    {
        using var aspettando = new SemaphoreSlim(0, 1);
        bool ceduto = false;

        _ = Task.Run(() =>
        {
            try
            {
                proprietaria.RedirectActivationToAsync(args).AsTask().Wait();
                ceduto = true;
            }
            catch (Exception e)
            {
                Registro.Errore("RedirectActivationToAsync", e);
            }
            finally
            {
                aspettando.Release();
            }
        });

        // Con attesa limitata: se l'istanza registrata è morta di colpo
        // —uccisa con Stop-Process, per esempio— la cessione non risponde
        // mai, e senza questo limite il programma restava appeso senza
        // finestra e senza dire niente. Da fuori sembra che "non parte".
        if (!aspettando.Wait(TimeSpan.FromSeconds(3)))
        {
            Registro.Annota(
                "l'istanza registrata non ha risposto in 3 s; "
                + "si prosegue come istanza principale");
            return false;
        }

        return ceduto;
    }
}
