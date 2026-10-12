using System.Runtime.InteropServices;

namespace Pastepad.App.Sistema;

/// <summary>
/// Tutto ciò che parla con Windows, in un solo posto. Separato dal resto
/// perché il codice dell'interfaccia non debba sapere niente di handle
/// né di strutture di sistema.
///
/// Le firme sono scritte a mano e non generate: sono poche e così si
/// vedono. Se questa lista cresce molto, l'alternativa è CsWin32.
/// </summary>
internal static partial class Nativo
{
    // --- messaggi ---------------------------------------------------

    public const uint WM_DESTROY = 0x0002;
    public const uint WM_HOTKEY = 0x0312;
    public const uint WM_CLIPBOARDUPDATE = 0x031D;

    /// <summary>Lo manda una seconda istanza per chiedere il pannello.</summary>
    public const uint WM_MOSTRATI = 0x0400 + 7;   // WM_USER + 7

    /// <summary>Lo manda l'icona dell'area di notifica al clic.</summary>
    public const uint WM_AREANOTIFICA = 0x0400 + 8;     // WM_USER + 8

    public const uint WM_LBUTTONUP = 0x0202;
    public const uint WM_RBUTTONUP = 0x0205;
    public const uint WM_COMMAND = 0x0111;

    // --- l'avviso che stanno per chiuderci ---------------------------

    public const uint WM_CLOSE = 0x0010;
    public const uint WM_QUERYENDSESSION = 0x0011;
    public const uint WM_ENDSESSION = 0x0016;

    /// <summary>
    /// Arriva nell'lParam di WM_QUERYENDSESSION quando a chiudere non è
    /// Windows che si spegne, ma il Restart Manager che fa posto a un
    /// installer. Serve a distinguere "mi aggiornano" da "si spegne il
    /// computer", che nel registro non è la stessa cosa.
    /// </summary>
    public const nint ENDSESSION_CLOSEAPP = 0x1;

    // --- modificatori della scorciatoia ------------------------------

    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    // --- tasti ------------------------------------------------------

    public const byte VK_SHIFT = 0x10;
    public const byte VK_CONTROL = 0x11;
    public const byte VK_MENU = 0x12;      // Alt
    public const byte VK_V = 0x56;

    public const uint KEYEVENTF_KEYUP = 0x0002;

    // --- appunti ----------------------------------------------------

    public const uint CF_UNICODETEXT = 13;
    public const uint CF_DIB = 8;

    // --- vari -------------------------------------------------------

    public const nint HWND_MESSAGE = -3;

    /// <summary>
    /// Finestra popup senza cornice. Senza WS_VISIBLE: nasce nascosta e
    /// non si mostra mai.
    /// </summary>
    public const uint WS_POPUP = 0x80000000;

    /// <summary>Fuori da Alt+Tab e dalla barra delle applicazioni.</summary>
    public const uint WS_EX_TOOLWINDOW = 0x00000080;

    public const int SW_RESTORE = 9;
    public const uint GMEM_MOVEABLE = 0x0002;
    public const uint MONITOR_DEFAULTTONEAREST = 2;
    public const int ERROR_CLASS_ALREADY_EXISTS = 1410;

    // --- strutture --------------------------------------------------

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WNDCLASSW
    {
        public uint style;
        public nint lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)] public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public readonly int Larghezza => right - left;
        public readonly int Altezza => bottom - top;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    public const uint NIM_ADD = 0x00000000;
    public const uint NIM_MODIFY = 0x00000001;
    public const uint NIM_DELETE = 0x00000002;

    public const uint NIF_MESSAGE = 0x00000001;
    public const uint NIF_ICON = 0x00000002;
    public const uint NIF_TIP = 0x00000004;

    /// <summary>
    /// «Display a balloon notification. The szInfo, szInfoTitle,
    /// dwInfoFlags, and uTimeout members are valid.» E per toglierlo,
    /// lo stesso avviso con szInfo vuoto.
    /// </summary>
    public const uint NIF_INFO = 0x00000010;

    /// <summary>
    /// Il triangolo di avvertimento del fumetto. Senza titolo non se ne
    /// disegna nessuno: «if the szInfoTitle member is zero-length, the
    /// icon is not shown».
    /// </summary>
    public const uint NIIF_WARNING = 0x00000002;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct NOTIFYICONDATAW
    {
        public uint cbSize;
        public nint hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public nint hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
        public uint uVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public nint hBalloonIcon;
    }

    // --- finestre ---------------------------------------------------

    // DllImport e non LibraryImport: il generatore non sa convertire
    // struct con stringhe dentro (SYSLIB1051), e WNDCLASSW le ha.
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern ushort RegisterClassW(ref WNDCLASSW classe);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16,
        SetLastError = true)]
    public static partial nint CreateWindowExW(
        uint stileExtra, string classe, string titolo, uint stile,
        int x, int y, int larghezza, int altezza,
        nint padre, nint menu, nint istanza, nint parametro);

    [LibraryImport("user32.dll")]
    public static partial nint DefWindowProcW(
        nint hwnd, uint messaggio, nint wParam, nint lParam);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyWindow(nint hwnd);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16,
        SetLastError = true)]
    public static partial nint FindWindowExW(
        nint padre, nint dietro, string? classe, string? titolo);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PostMessageW(
        nint hwnd, uint messaggio, nint wParam, nint lParam);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16,
        SetLastError = true)]
    public static partial nint GetModuleHandleW(string? nome);

    // --- farci riaprire dopo un aggiornamento --------------------

    /// <summary>
    /// Dice a Windows con quale riga di comando riaprirci se ci chiude.
    /// Senza questa chiamata il Restart Manager NON riapre niente:
    /// "Restart Manager can only restart applications that have been
    /// registered for restart. This is the only way that the Restart
    /// Manager can determine the command-line command to use".
    ///
    /// Restituisce un HRESULT, non un booleano.
    /// </summary>
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int RegisterApplicationRestart(
        string? rigaDiComando, uint flag);

    /// <summary>Non riaprirci se c'è stata una chiusura inattesa.</summary>
    public const uint RESTART_NO_CRASH = 1;

    /// <summary>Né se Windows ci considera bloccati.</summary>
    public const uint RESTART_NO_HANG = 2;

    // --- scorciatoia globale ----------------------------------------

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RegisterHotKey(
        nint hwnd, int id, uint modificatori, uint tasto);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnregisterHotKey(nint hwnd, int id);

    // --- appunti ----------------------------------------------------

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AddClipboardFormatListener(nint hwnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RemoveClipboardFormatListener(nint hwnd);

    [LibraryImport("user32.dll")]
    public static partial uint GetClipboardSequenceNumber();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool OpenClipboard(nint proprietaria);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseClipboard();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EmptyClipboard();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsClipboardFormatAvailable(uint formato);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint GetClipboardData(uint formato);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint SetClipboardData(uint formato, nint dati);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16,
        SetLastError = true)]
    public static partial uint RegisterClipboardFormatW(string nome);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint GlobalAlloc(uint flag, nuint bytes);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint GlobalLock(nint memoria);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GlobalUnlock(nint memoria);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nuint GlobalSize(nint memoria);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint GlobalFree(nint memoria);

    // --- focus ------------------------------------------------------

    [LibraryImport("user32.dll")]
    public static partial nint GetForegroundWindow();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetForegroundWindow(nint hwnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint SetFocus(nint hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWindow(nint hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsIconic(nint hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ShowWindow(nint hwnd, int ordine);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial uint GetWindowThreadProcessId(nint hwnd, out uint pid);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AttachThreadInput(
        uint threadProprio, uint threadDestinazione, [MarshalAs(UnmanagedType.Bool)] bool aggancia);

    [LibraryImport("kernel32.dll")]
    public static partial uint GetCurrentThreadId();

    [LibraryImport("user32.dll")]
    public static partial void keybd_event(
        byte tasto, byte codice, uint flag, nuint extra);

    /// <summary>
    /// Se il tasto è fisicamente premuto ADESSO. Lo dice il bit alto
    /// del risultato; quello basso dice se è stato premuto dall'ultima
    /// chiamata, e non ci serve.
    ///
    /// Serve per non rilasciare tasti che nessuno ha premuto: vedi
    /// FocusFinestra.IncollaConTastiera.
    /// </summary>
    [LibraryImport("user32.dll")]
    public static partial short GetAsyncKeyState(int tasto);

    /// <summary>
    /// Quando è stato MESSO in coda il messaggio che si sta smistando,
    /// non quando viene servito: «the elapsed time, in milliseconds,
    /// from the time the system was started to the time the message was
    /// created (that is, placed in the thread's message queue)».
    ///
    /// Sottraendolo a GetTickCount esce quanto il messaggio ha aspettato
    /// che qualcuno lo raccogliesse, che è l'unico tratto del percorso
    /// della scorciatoia che finora non si misurava e dove deve stare il
    /// ritardo che l'utente nota dopo dieci minuti di inattività.
    /// </summary>
    [LibraryImport("user32.dll")]
    public static partial int GetMessageTime();

    // --- schermo ----------------------------------------------------

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetCursorPos(out POINT punto);

    [LibraryImport("user32.dll")]
    public static partial nint MonitorFromPoint(POINT punto, uint flag);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetMonitorInfoW(nint monitor, ref MONITORINFO info);

    [LibraryImport("user32.dll")]
    public static partial int GetDpiForWindow(nint hwnd);

    // --- dove finiscono davvero i file ------------------------------

    public const uint GENERIC_WRITE = 0x40000000;
    public const uint CREATE_ALWAYS = 2;

    /// <summary>
    /// Il file si cancella da solo alla chiusura dell'handle. Così la
    /// sonda non lascia traccia nemmeno se il programma muore con lei
    /// aperta.
    /// </summary>
    public const uint FILE_FLAG_DELETE_ON_CLOSE = 0x04000000;

    public static readonly nint HANDLE_NON_VALIDO = -1;

    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW",
                   StringMarshalling = StringMarshalling.Utf16,
                   SetLastError = true)]
    public static partial nint CreateFileW(
        string nome, uint accesso, uint condivide, nint sicurezza,
        uint creazione, uint flag, nint modello);

    /// <summary>
    /// Il percorso REALE del file o della cartella dietro un handle,
    /// che non è per forza quello chiesto: quando il processo vive
    /// dentro il contenitore di un'applicazione impacchettata, Windows
    /// reindirizza %LOCALAPPDATA% e questo è l'unico modo di vederlo da
    /// dentro. Restituisce la lunghezza, o 0 se fallisce.
    /// </summary>
    [LibraryImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW",
                   StringMarshalling = StringMarshalling.Utf16,
                   SetLastError = true)]
    public static partial uint GetFinalPathNameByHandleW(
        nint handle, [Out] char[] buffer, uint lungo, uint flag);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(nint handle);

    // --- area di notifica -------------------------------------------

    // Come RegisterClassW: NOTIFYICONDATAW ha stringhe di dimensione
    // fissa, che il generatore nemmeno converte.
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool Shell_NotifyIconW(uint messaggio, ref NOTIFYICONDATAW dati);

    public const uint IMAGE_ICON = 1;
    public const uint LR_LOADFROMFILE = 0x00000010;
    public const uint LR_DEFAULTSIZE = 0x00000040;

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16,
        SetLastError = true)]
    public static partial nint LoadImageW(
        nint istanza, string nome, uint tipo, int larghezza, int altezza, uint flag);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint LoadIconW(nint istanza, nint nome);

    public static readonly nint IDI_APPLICATION = 32512;

    // --- menu dell'area di notifica ---------------------------------

    public const uint MF_STRING = 0x00000000;
    public const uint MF_SEPARATOR = 0x00000800;
    public const uint TPM_RIGHTBUTTON = 0x0002;
    public const uint TPM_RETURNCMD = 0x0100;

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint CreatePopupMenu();

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16,
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AppendMenuW(
        nint menu, uint flag, nuint id, string? testo);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyMenu(nint menu);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial int TrackPopupMenuEx(
        nint menu, uint flag, int x, int y, nint hwnd, nint parametri);
}
