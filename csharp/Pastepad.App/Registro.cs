using System.Text;

namespace Pastepad.App;

/// <summary>
/// errori.log: l'unico che scrive. Nessun catch muto — l'errore della
/// scorciatoia nella versione precedente richiese giorni di diagnosi
/// perché ogni thread ingoiava la propria eccezione in silenzio.
/// </summary>
internal static class Registro
{
    static readonly Lock _lucchetto = new();

    static string? _percorso;

    /// <summary>
    /// Dove dovrebbe andare il registro: la cartella dati in uso, che con
    /// <c>--dati</c> non è quella di sempre. Un'istanza di prova che
    /// scrive il suo log nella cronologia vera dell'utente non serve a
    /// niente e in più sporca ciò che si stava proteggendo.
    ///
    /// Si risolve la prima volta che viene chiesto e non in un
    /// inizializzatore statico: gli argomenti si leggono nella prima
    /// riga di <c>Main</c>, e un campo statico sarebbe stato fissato
    /// prima. Per lo stesso motivo nessuno deve annotare niente prima di
    /// leggere gli argomenti: il percorso resta quello che si risolve
    /// per primo.
    /// </summary>
    public static string Percorso =>
        _percorso ??= Path.Combine(Program.CartellaDati ?? ComePredefinito(), "errori.log");

    static string ComePredefinito() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        Nucleo.Config.App);

    static string? _percorsoUsato;

    /// <summary>
    /// Dove si scrive davvero, che può non essere <see cref="Percorso"/>.
    /// </summary>
    public static string PercorsoUsato => _percorsoUsato ?? Percorso;

    /// <summary>
    /// Il secondo tentativo, accanto all'eseguibile. Esiste perché ci fu
    /// un avvio che non lasciò nemmeno una riga: se il posto di sempre
    /// non accetta scritture, scrivere altrove è meglio che non
    /// scrivere, e di certo meglio di un Debug.WriteLine che nessuno
    /// vede.
    /// </summary>
    static readonly string _riserva =
        Path.Combine(AppContext.BaseDirectory, "errori.log");

    public static void Annota(string testo)
    {
        var linea = string.Format(
            "{0:yyyy-MM-dd HH:mm:ss.fff}  [thread {1,3}]  {2}{3}",
            DateTime.Now, Environment.CurrentManagedThreadId, testo,
            Environment.NewLine);

        lock (_lucchetto)
        {
            if (Tenta(Percorso, linea)) { _percorsoUsato = Percorso; return; }

            if (Tenta(_riserva, linea)) { _percorsoUsato = _riserva; return; }

            // Senza un posto dove scrivere non resta nessuno da avvisare.
            // Almeno il debugger lo raccoglie.
            System.Diagnostics.Debug.WriteLine("senza log: " + linea);
        }
    }

    static bool Tenta(string percorso, string linea)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(percorso)!);
            File.AppendAllText(percorso, linea, Encoding.UTF8);
            return true;
        }
        // ArgumentException entra qui apposta: con --dati il percorso
        // lo scrive una persona, e un percorso impossibile deve finire
        // nella riserva qui sotto, non in un'eccezione dall'interno di
        // ciò che dovrebbe registrare le eccezioni.
        catch (Exception e) when (e is IOException
                                    or UnauthorizedAccessException
                                    or NotSupportedException
                                    or ArgumentException)
        {
            System.Diagnostics.Debug.WriteLine(
                $"impossibile scrivere in {percorso}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// La prima riga di tutte, scritta nei DUE posti insieme e con
    /// l'ambiente davanti.
    ///
    /// Esiste per un errore aperto: ci sono avvii che non lasciano
    /// nemmeno una riga in %LOCALAPPDATA%, mentre altri dello stesso
    /// binario e dello stesso utente sì. Finché non si sa perché,
    /// l'avvio si annota anche accanto all'eseguibile, che è sempre
    /// scrivibile.
    /// </summary>
    public static void AnnotaAvvio()
    {
        var io = System.Security.Principal.WindowsIdentity.GetCurrent();

        var ambiente =
            $"=== avvio === pastepad {Nucleo.Config.Version}, "
            + $"pid {Environment.ProcessId}, "
            + $"utente {io.Name}, "
            + $"elevato {new System.Security.Principal.WindowsPrincipal(io)
                .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator)}, "
            + $"exe {Environment.ProcessPath}, "
            + $"localappdata {Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData)}, "

            // La variabile d'ambiente e la cartella che l'archivio usa
            // davvero, una accanto all'altra. Se divergono —profilo
            // temporaneo, reindirizzamento, un --dati che non è quello che
            // si credeva— si vede a colpo d'occhio, ed era ciò che mancava
            // per capire perché alcuni avvii lasciavano righe e altri dello
            // stesso binario no.
            + $"LOCALAPPDATA={Environment.GetEnvironmentVariable("LOCALAPPDATA")}, "
            + $"dati {Path.GetDirectoryName(Percorso)}, "

            // Da quanto era acceso Windows quando siamo partiti. È l'unico
            // modo di vedere un avvio automatico che NON è avvenuto: finché
            // pastepad sa annotare solo gli avvii che avvengono, un errore
            // dell'avvio automatico non lascia niente da leggere, ed è
            // esattamente ciò che si trovò indagando. Pochi secondi qui
            // significano che ci ha aperto Windows; alcune ore, che l'avvio
            // automatico è fallito e l'ha aperto l'utente a mano.
            //
            // GetTickCount64 —da cui esce TickCount64— conta
            // «milliseconds that have elapsed since the system was
            // started», sospensione inclusa: la documentazione rimanda a
            // QueryUnbiasedInterruptTime per il tempo in attività. Qui
            // interessa quello dell'orologio a muro, non quello di lavoro.
            + $"windows acceso da {DallAvvio()}";

        var linea = string.Format(
            "{0:yyyy-MM-dd HH:mm:ss.fff}  [thread {1,3}]  {2}{3}",
            DateTime.Now, Environment.CurrentManagedThreadId, ambiente,
            Environment.NewLine);

        lock (_lucchetto)
        {
            bool a = Tenta(Percorso, linea);
            bool b = Tenta(_riserva, linea);

            // Senza questo indizio, chi fa la diagnosi guardando il log
            // accanto all'eseguibile vede una lista di avvii e nessun
            // segno di attività, e conclude che il programma non fa
            // niente. Tutto il resto si scrive nell'altro.
            if (a && b)
            {
                Tenta(_riserva,
                    $"    (il dettaglio di questa sessione va in {Percorso}){Environment.NewLine}");
            }

            if (!a && !b)
                System.Diagnostics.Debug.WriteLine("senza log: " + linea);

            if (!a && b)
            {
                Tenta(_riserva,
                    "    ATTENZIONE: impossibile scrivere in " + Percorso
                    + "; questa sessione si registra solo qui."
                    + Environment.NewLine);
            }
        }
    }

    /// <summary>
    /// Leggibile a colpo d'occhio, che è il motivo per cui si legge:
    /// «4 s» dice «ci ha aperto Windows» e «9 h 12 min» dice «l'ha
    /// aperto l'utente».
    /// </summary>
    static string DallAvvio()
    {
        var t = TimeSpan.FromMilliseconds(Environment.TickCount64);

        if (t.TotalMinutes < 1) return $"{t.TotalSeconds:F0} s";

        if (t.TotalHours < 1) return $"{t.Minutes} min {t.Seconds} s";

        return $"{(int)t.TotalHours} h {t.Minutes} min";
    }

    /// <summary>
    /// Con l'HResult in esadecimale. È ciò che separa una violazione di
    /// condivisione (0x80070020, qualcuno ha il file aperto) da un
    /// accesso negato (0x80070005, non abbiamo il permesso): tutte e due
    /// arrivano come IOException o con un messaggio simile, e senza
    /// questo numero la diagnosi resta a "impossibile leggere il file".
    /// </summary>
    public static void Errore(string dove, Exception e)
    {
        Annota($"ERRORE in {dove}: {e.GetType().Name} "
             + $"(0x{e.HResult:X8}): {e.Message}");

        Annota(e.StackTrace ?? "(senza traccia)");
    }

    /// <summary>
    /// I tre gestori globali. Uno solo non basta: ognuno copre una
    /// strada diversa da cui un'eccezione può scappare.
    /// </summary>
    public static void AgganciaGlobali()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, a) =>
        {
            if (a.ExceptionObject is Exception e)
                Errore("AppDomain.UnhandledException", e);
            else
                Annota($"ERRORE non tipizzato: {a.ExceptionObject}");
        };

        TaskScheduler.UnobservedTaskException += (_, a) =>
        {
            Errore("TaskScheduler.UnobservedTaskException", a.Exception);
            a.SetObserved();
        };
    }
}
