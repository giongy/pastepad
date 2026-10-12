using System.Runtime.InteropServices;

namespace Pastepad.App.Sistema;

/// <summary>
/// Finestra solo-messaggi: non si disegna, non compare in Alt+Tab, non
/// ha pixel. È il cuore dell'applicazione — da qui entrano la
/// scorciatoia globale, gli avvisi degli appunti, il clic nell'area di
/// notifica e la chiamata di una seconda istanza.
///
/// Vive quanto il processo, non quanto la finestra visibile. Legarla
/// alla finestra del pannello, che si nasconde nell'area di notifica, è
/// l'accoppiamento che uccise la versione in Python.
///
/// Verificato il 12 ago 2026: il ciclo dei messaggi di WinUI 3 smista i
/// messaggi di questa finestra purché sia creata nel suo stesso thread.
/// </summary>
internal sealed class Cassetta : IDisposable
{
    public const string Classe = "pastepad_cassetta_3ff1c0de";

    const int ID_SCORCIATOIA = 1;
    const nuint MENU_APRI = 1;
    const nuint MENU_ESCI = 2;

    /// <summary>È stata premuta la scorciatoia globale.</summary>
    public event Action? Scorciatoia;

    /// <summary>Gli appunti sono cambiati.</summary>
    public event Action? Appunti;

    /// <summary>Un'altra istanza, o l'area di notifica, chiedono il pannello.</summary>
    public event Action? Mostrati;

    /// <summary>È stata chiesta la chiusura dal menu dell'area di notifica.</summary>
    public event Action? Esci;

    public nint Handle { get; private set; }
    public bool ScorciatoiaImpostata { get; private set; }
    public string? Problema { get; private set; }

    /// <summary>
    /// Millisecondi che l'ultima scorciatoia ha aspettato in coda da
    /// quando Windows l'ha messa fino a quando questo thread l'ha
    /// raccolta.
    ///
    /// Si calcola qui e non dove si usa perché vale solo dentro lo
    /// smistamento del messaggio: GetMessageTime parla dell'ultimo
    /// messaggio recuperato da GetMessage, e appena se ne recupera un
    /// altro non è più il nostro.
    ///
    /// La sottrazione va in int e non in long apposta. Il contatore
    /// «wraps to the minimum value for a long integer» ogni 49 giorni, e
    /// la documentazione dice che per misurare i ritardi bisogna
    /// sottrarre «ignoring overflow» — che è proprio ciò che fa l'int.
    /// </summary>
    public int AttesaDellaScorciatoia { get; private set; }

    // Il delegato si tiene in un campo apposta: se vivesse solo nella
    // chiamata a RegisterClass, il garbage collector potrebbe
    // portarselo via e Windows salterebbe in memoria liberata.
    readonly Procedura _procedura;

    bool _scartato;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate nint Procedura(nint hwnd, uint messaggio, nint wParam, nint lParam);

    /// <summary>
    /// Va costruita nel thread dell'interfaccia: RegisterHotKey fallisce
    /// se l'HWND è stato creato da un altro thread.
    /// </summary>
    public Cassetta()
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
                Problema = $"RegisterClassW fallita con errore {error}";
                Registro.Annota(Problema);
                return;
            }
        }

        Handle = Nativo.CreateWindowExW(
            0, Classe, Classe, 0, 0, 0, 0, 0,
            Nativo.HWND_MESSAGE, 0, istanza, 0);

        if (Handle == 0)
        {
            Problema = "CreateWindowExW fallita con errore " +
                       Marshal.GetLastWin32Error();
            Registro.Annota(Problema);
            return;
        }

        Registro.Annota($"cassetta creata, hwnd 0x{Handle:X}");

        if (!Nativo.AddClipboardFormatListener(Handle))
        {
            Problema = "AddClipboardFormatListener fallita con errore " +
                       Marshal.GetLastWin32Error();
            Registro.Annota(Problema);
        }
    }

    /// <summary>
    /// Registra o cambia la scorciatoia. False se Windows non la
    /// accetta, che quasi sempre significa che un'altra applicazione ce
    /// l'ha già.
    /// </summary>
    public bool MettiScorciatoia(string combinazione)
    {
        if (Handle == 0) return false;

        var parti = Combinazione.Scomponi(combinazione);

        if (parti is null)
        {
            Problema = $"la combinazione '{combinazione}' non è valida";
            Registro.Annota(Problema);
            return false;
        }

        if (ScorciatoiaImpostata)
        {
            Nativo.UnregisterHotKey(Handle, ID_SCORCIATOIA);
            ScorciatoiaImpostata = false;
        }

        var (mods, vk) = parti.Value;

        // MOD_NOREPEAT: senza, tenere premuta la combinazione la fa
        // partire in ciclo e il pannello lampeggia. Misurato: una
        // pressione prolungata sono 31 ripetizioni della tastiera e un
        // solo avviso.
        ScorciatoiaImpostata = Nativo.RegisterHotKey(
            Handle, ID_SCORCIATOIA, mods | Nativo.MOD_NOREPEAT, vk);

        if (!ScorciatoiaImpostata)
        {
            Problema = $"Windows ha rifiutato {combinazione} (errore " +
                       $"{Marshal.GetLastWin32Error()}). " +
                       "Probabilmente un'altra applicazione la usa già.";
            Registro.Annota(Problema);
        }
        else
        {
            Problema = null;
            Registro.Annota($"scorciatoia {combinazione} registrata");
        }

        return ScorciatoiaImpostata;
    }

    /// <summary>
    /// Chiede all'istanza già aperta di mostrare il pannello. La si
    /// cerca per il nome di classe della sua cassetta.
    /// </summary>
    public static bool ChiediDiMostrarsi()
    {
        nint hwnd = Nativo.FindWindowExW(Nativo.HWND_MESSAGE, 0, Classe, null);
        if (hwnd == 0) return false;

        return Nativo.PostMessageW(hwnd, Nativo.WM_MOSTRATI, 0, 0);
    }

    /// <summary>
    /// La procedura di finestra. Nessuna eccezione può scappare da qui:
    /// tornerebbe nel codice nativo, che è comportamento indefinito e si
    /// manifesta come una chiusura senza traccia.
    /// </summary>
    nint Smista(nint hwnd, uint messaggio, nint wParam, nint lParam)
    {
        try
        {
            switch (messaggio)
            {
                case Nativo.WM_HOTKEY when (int)wParam == ID_SCORCIATOIA:
                    AttesaDellaScorciatoia =
                        unchecked(Environment.TickCount - Nativo.GetMessageTime());
                    Scorciatoia?.Invoke();
                    return 0;

                case Nativo.WM_CLIPBOARDUPDATE:
                    Appunti?.Invoke();
                    return 0;

                case Nativo.WM_MOSTRATI:
                    Mostrati?.Invoke();
                    return 0;

                case Nativo.WM_AREANOTIFICA:
                    GestisciAreaNotifica((uint)lParam);
                    return 0;

                case Nativo.WM_COMMAND:
                    GestisciMenu((nuint)(wParam & 0xFFFF));
                    return 0;
            }
        }
        catch (Exception e)
        {
            Registro.Errore("Cassetta.Smista", e);
        }

        return Nativo.DefWindowProcW(hwnd, messaggio, wParam, lParam);
    }

    void GestisciAreaNotifica(uint evento)
    {
        switch (evento)
        {
            case Nativo.WM_LBUTTONUP:
                Mostrati?.Invoke();
                break;

            case Nativo.WM_RBUTTONUP:
                ApriMenu();
                break;
        }
    }

    /// <summary>
    /// Il menu del clic destro. Windows esige il SetForegroundWindow
    /// prima: senza, il menu resta aperto cliccando fuori.
    /// </summary>
    void ApriMenu()
    {
        nint menu = Nativo.CreatePopupMenu();
        if (menu == 0) return;

        try
        {
            Nativo.AppendMenuW(menu, Nativo.MF_STRING, MENU_APRI, "Apri pastepad");
            Nativo.AppendMenuW(menu, Nativo.MF_SEPARATOR, 0, null);
            Nativo.AppendMenuW(menu, Nativo.MF_STRING, MENU_ESCI, "Esci");

            Nativo.GetCursorPos(out var punto);
            Nativo.SetForegroundWindow(Handle);

            int scelto = Nativo.TrackPopupMenuEx(
                menu,
                Nativo.TPM_RIGHTBUTTON | Nativo.TPM_RETURNCMD,
                punto.x, punto.y, Handle, 0);

            if (scelto > 0) GestisciMenu((nuint)scelto);
        }
        finally
        {
            Nativo.DestroyMenu(menu);
        }
    }

    void GestisciMenu(nuint id)
    {
        switch (id)
        {
            case MENU_APRI: Mostrati?.Invoke(); break;
            case MENU_ESCI: Esci?.Invoke(); break;
        }
    }

    public void Dispose()
    {
        if (_scartato) return;
        _scartato = true;

        if (Handle == 0) return;

        if (ScorciatoiaImpostata) Nativo.UnregisterHotKey(Handle, ID_SCORCIATOIA);
        Nativo.RemoveClipboardFormatListener(Handle);
        Nativo.DestroyWindow(Handle);

        Handle = 0;
        Registro.Annota("cassetta chiusa");
    }
}
