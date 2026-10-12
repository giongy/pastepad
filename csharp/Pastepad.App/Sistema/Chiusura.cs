using System.Runtime.InteropServices;
using Pastepad.Nucleo;

namespace Pastepad.App.Sistema;

/// <summary>
/// La finestra che ascolta quando stanno per chiuderci senza chiedercelo:
/// un installer che si aggiorna sopra, o Windows che si spegne.
///
/// È una finestra separata dalla cassetta e non è un capriccio. La
/// cassetta è una finestra solo-messaggi, e di quelle la documentazione
/// di Windows dice che "is not visible, has no z-order, cannot be
/// enumerated, and does not receive broadcast messages". Ciò che non si
/// può enumerare non riceve WM_QUERYENDSESSION: la cassetta è sorda a
/// questo per costruzione, e non si tocca perché è ciò che fa funzionare
/// la scorciatoia globale.
///
/// Nemmeno si fa il subclass della finestra XAML —quello si porta dietro
/// l'ExecutionEngineException aperto in WinUI—. Quindi questa: una
/// finestra popup di primo livello, senza cornice, senza WS_VISIBLE e
/// con WS_EX_TOOLWINDOW. Non si disegna mai e non compare in Alt+Tab.
/// Ascolta soltanto.
///
/// Misurato prima di scriverla, con la stessa API che usa Inno Setup: il
/// Restart Manager vedeva pastepad come RmOtherWindow con il pannello
/// nell'area di notifica e come RmMainWindow con il pannello aperto, e
/// bRestartable a False nei due casi. Cioè, ciò che succedeva ai dati
/// aggiornando dipendeva dal fatto che il pannello fosse aperto.
/// </summary>
internal sealed class Chiusura : IDisposable
{
    const string Classe = "pastepad_chiusura_3ff1c0de";

    /// <summary>
    /// Salva subito, perché può non esserci un'altra occasione. Può
    /// arrivare più di una volta e deve reggerlo.
    /// </summary>
    public event Action? Riversa;

    /// <summary>È finita: chiudere del tutto.</summary>
    public event Action? Termina;

    public nint Handle { get; private set; }

    // Stesso motivo di Cassetta: se il delegato vivesse solo nella
    // chiamata, il garbage collector potrebbe portarselo via e Windows
    // salterebbe in memoria liberata.
    readonly Procedura _procedura;

    bool _scartato;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate nint Procedura(nint hwnd, uint messaggio, nint wParam, nint lParam);

    /// <summary>
    /// Si costruisce nel thread dell'interfaccia, come la cassetta: i
    /// messaggi li smista il ciclo di WinUI, e il riversamento tocca
    /// l'archivio, che in tutti gli altri posti si tocca da quel thread.
    /// </summary>
    public Chiusura()
    {
        _procedura = Smista;

        var istanza = Nativo.GetModuleHandleW(null);

        var classe = new Nativo.WNDCLASSW
        {
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_procedura),
            hInstance = istanza,
            lpszClassName = Classe,
        };

        if (Nativo.RegisterClassW(ref classe) == 0)
        {
            int error = Marshal.GetLastWin32Error();
            if (error != Nativo.ERROR_CLASS_ALREADY_EXISTS)
            {
                Registro.Annota($"chiusura: RegisterClassW fallita con errore {error}");
                return;
            }
        }

        // Padre 0, non HWND_MESSAGE: deve essere di primo livello per
        // poter essere enumerata. È tutta la differenza con la cassetta
        // e tutto il motivo per cui esiste.
        Handle = Nativo.CreateWindowExW(
            Nativo.WS_EX_TOOLWINDOW, Classe, Classe, Nativo.WS_POPUP,
            0, 0, 0, 0, 0, 0, istanza, 0);

        if (Handle == 0)
        {
            Registro.Annota("chiusura: CreateWindowExW fallita con errore " +
                            Marshal.GetLastWin32Error());
            return;
        }

        Registro.Annota($"ascolto di chiusura creato, hwnd 0x{Handle:X}");
    }

    /// <summary>
    /// Dà a Windows la riga con cui riaprirci. Senza, il Restart Manager
    /// chiude pastepad per aggiornarlo e non lo riapre: l'utente resta
    /// senza scorciatoia e senza cattura delle copie, e senza niente a
    /// schermo che glielo dica.
    ///
    /// Si escludono apposta la chiusura inattesa e il blocco. Che
    /// Windows riapra un programma che è appena esploso è come nasce un
    /// ciclo di avvii.
    /// </summary>
    public static void ChiediDiRiaprirci()
    {
        // Gli argomenti vanno dati: passare null NON li conserva, li
        // cancella —"if this parameter is NULL or an empty string, the
        // previously registered command line is removed"—. Misurato:
        // un'istanza lanciata con --dati tornò senza e aprì l'archivio
        // di sempre. In produzione pastepad non ha argomenti, ma una
        // sessione di prova sì, ed è proprio il caso in cui sbagliare
        // cartella costa dati all'utente.
        //
        // Il nome dell'eseguibile si omette apposta: "Do not include the
        // name of the executable in the command line; this function adds
        // it for you".
        string? linea = Argomenti.Componi(
            Environment.GetCommandLineArgs().Skip(1));

        int hr = Nativo.RegisterApplicationRestart(
            linea, Nativo.RESTART_NO_CRASH | Nativo.RESTART_NO_HANG);

        // E_INVALIDARG qui significa riga troppo lunga. Non è fatale
        // —pastepad continua a funzionare—, ma smette di riaprirsi da
        // solo, che è una di quelle cose che falliscono senza che
        // nessuno se ne accorga.
        Registro.Annota(hr == 0
            ? "registrati per essere riaperti dopo l'aggiornamento" +
              (linea is null ? "" : $", con {linea}")
            : $"RegisterApplicationRestart fallita con HRESULT 0x{hr:X8}");
    }

    /// <summary>
    /// Nessuna eccezione può scappare da qui: tornerebbe nel codice
    /// nativo, che è comportamento indefinito.
    /// </summary>
    nint Smista(nint hwnd, uint messaggio, nint wParam, nint lParam)
    {
        try
        {
            switch (messaggio)
            {
                // La documentazione è esplicita sul fatto che qui NON ci si
                // chiude —"another application may not be ready to shut
                // down"— e che bisogna rispondere TRUE per non annullare lo
                // spegnimento di tutti. Ma riversare sì: sono circa 8 ms, la
                // sessione può ancora essere annullata senza conseguenze, ed
                // è l'ultimo momento in cui sappiamo per certo di essere
                // vivi. Se poi la chiusura viene forzata, i dati ci sono già.
                case Nativo.WM_QUERYENDSESSION:
                    Registro.Annota(
                        (lParam & Nativo.ENDSESSION_CLOSEAPP) != 0
                            ? "ci chiudono per aggiornarci; riverso i dati"
                            : "Windows si spegne; riverso i dati");
                    Riversa?.Invoke();
                    return 1;

                // wParam a 0 significa che qualcuno ha annullato la sessione e
                // siamo ancora vivi. Chiudersi lì sarebbe chiudersi di troppo.
                case Nativo.WM_ENDSESSION:
                    if (wParam == 0)
                    {
                        Registro.Annota("la chiusura è stata annullata; si prosegue");
                        return 0;
                    }

                    Riversa?.Invoke();
                    Termina?.Invoke();
                    return 0;

                // Lo manda il Restart Manager a chi non si è chiuso con
                // WM_ENDSESSION. Che arrivi qui significa che prima qualcosa
                // è andato storto, quindi resta annotato.
                case Nativo.WM_CLOSE:
                    Registro.Annota("è arrivato WM_CLOSE dopo l'avviso di chiusura");
                    Riversa?.Invoke();
                    Termina?.Invoke();
                    return 0;
            }
        }
        catch (Exception e)
        {
            Registro.Errore("Chiusura.Smista", e);
        }

        return Nativo.DefWindowProcW(hwnd, messaggio, wParam, lParam);
    }

    public void Dispose()
    {
        if (_scartato) return;
        _scartato = true;

        if (Handle == 0) return;

        Nativo.DestroyWindow(Handle);
        Handle = 0;
    }
}
