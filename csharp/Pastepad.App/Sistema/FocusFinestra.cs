namespace Pastepad.App.Sistema;

/// <summary>
/// Restituire il focus dove stava il cursore e incollare lì. È il pezzo
/// che rende utile pastepad: senza, il Ctrl+V finisce nel vuoto.
/// </summary>
internal static class FocusFinestra
{
    static readonly uint _mioPid = (uint)Environment.ProcessId;

    /// <summary>
    /// Handle della finestra con il focus, a meno che sia la nostra. Va
    /// chiamato nell'istante della scorciatoia, prima di mostrare il
    /// pannello: appena il pannello compare, questa informazione è persa.
    /// </summary>
    public static nint FinestraAttiva()
    {
        nint hwnd = Nativo.GetForegroundWindow();
        if (hwnd == 0) return 0;

        Nativo.GetWindowThreadProcessId(hwnd, out uint pid);
        return pid == _mioPid ? 0 : hwnd;
    }

    /// <summary>
    /// Riporta quella finestra in primo piano, con il cursore dov'era.
    ///
    /// Windows non lascia che un processo qualsiasi rubi il primo piano.
    /// Il trucco è agganciarsi per un istante al thread di quella
    /// finestra: finché dura l'aggancio, SetForegroundWindow funziona.
    /// </summary>
    public static bool Restituisci(nint hwnd)
    {
        if (hwnd == 0 || !Nativo.IsWindow(hwnd)) return false;

        if (Nativo.IsIconic(hwnd))
            Nativo.ShowWindow(hwnd, Nativo.SW_RESTORE);

        // Prima con le buone. Vedi ConLeBuone: l'aggancio è ciò che può
        // costare secondi, e molte volte non serve.
        if (ConLeBuone(hwnd)) return true;

        uint threadDestinazione = Nativo.GetWindowThreadProcessId(hwnd, out _);
        uint threadProprio = Nativo.GetCurrentThreadId();

        bool agganciato = false;

        if (threadDestinazione != 0 && threadDestinazione != threadProprio)
            agganciato = Nativo.AttachThreadInput(threadProprio, threadDestinazione, true);

        try
        {
            if (Nativo.IsIconic(hwnd))
                Nativo.ShowWindow(hwnd, Nativo.SW_RESTORE);

            if (!Nativo.SetForegroundWindow(hwnd))
            {
                Registro.Annota($"SetForegroundWindow rifiutata per 0x{hwnd:X}");
                return false;
            }

            Nativo.SetFocus(hwnd);
            return true;
        }
        finally
        {
            if (agganciato)
                Nativo.AttachThreadInput(threadProprio, threadDestinazione, false);
        }
    }

    /// <summary>
    /// Porta in primo piano una nostra finestra. Windows applica la
    /// stessa restrizione che con quelle altrui: un processo in secondo
    /// piano non può mettersi davanti con le buone, e senza questo il
    /// pannello esce visibile ma senza focus.
    ///
    /// L'aggancio si fa al thread di chi comanda adesso, che è chi ha il
    /// permesso che a noi manca.
    /// </summary>
    public static bool PortaInPrimoPiano(nint propria)
    {
        if (propria == 0) return false;

        nint davanti = Nativo.GetForegroundWindow();

        if (davanti == propria) return true;

        if (ConLeBuone(propria)) return true;

        uint threadDavanti = davanti != 0
            ? Nativo.GetWindowThreadProcessId(davanti, out _)
            : 0;

        uint threadProprio = Nativo.GetCurrentThreadId();

        bool agganciato = false;

        if (threadDavanti != 0 && threadDavanti != threadProprio)
            agganciato = Nativo.AttachThreadInput(threadProprio, threadDavanti, true);

        try
        {
            Nativo.SetForegroundWindow(propria);
            Nativo.SetFocus(propria);

            return Nativo.GetForegroundWindow() == propria;
        }
        finally
        {
            if (agganciato)
                Nativo.AttachThreadInput(threadProprio, threadDavanti, false);
        }
    }

    /// <summary>
    /// Mette una finestra davanti **senza agganciarsi al thread di
    /// nessuno**.
    ///
    /// È la strada veloce, e soprattutto l'unica che non può restare in
    /// attesa. L'aggancio usato dai due metodi qui sopra fa sì che
    /// «keyboard and mouse events received by both threads are processed
    /// in the order they were received», cioè ci mettiamo in coda al
    /// programma che sta davanti: se quello è occupato, aspettiamo.
    /// Misurato con una finestra dal thread bloccato apposta,
    /// <c>SetForegroundWindow</c> ci mise **5.399 ms** e in più restituì
    /// false. E sulla macchina di lavoro dell'utente —satura di agenti
    /// di sicurezza— questo smette di essere un caso da laboratorio.
    ///
    /// Senza aggancio può funzionare lo stesso: tra le condizioni che
    /// Windows accetta per cedere il primo piano c'è che «the process
    /// received the last input event», e pastepad è appena stato
    /// svegliato da una scorciatoia globale. Quando passa, ci
    /// risparmiamo l'intero aggancio; quando no, si prosegue per la
    /// strada di sempre e non si perde niente.
    ///
    /// Il risultato si controlla leggendo chi comanda davvero e non
    /// fidandosi del valore restituito: la chiamata può dire true e non
    /// aver cambiato niente.
    /// </summary>
    static bool ConLeBuone(nint hwnd)
    {
        Nativo.SetForegroundWindow(hwnd);

        if (Nativo.GetForegroundWindow() != hwnd) return false;

        Nativo.SetFocus(hwnd);
        return true;
    }

    /// <summary>
    /// Aspetta che quella finestra sia davvero davanti, e restituisce
    /// quanto ci ha messo. Sostituisce un'attesa fissa di 60 ms scritta
    /// a mano.
    ///
    /// I 60 ms erano alla cieca: su una macchina scarica avanzavano —si
    /// pagavano interi anche se il focus era già a posto in 5— e su una
    /// carica potevano non bastare, e allora il Ctrl+V usciva prima che
    /// la destinazione potesse riceverlo. Questo è più veloce nel caso
    /// normale e più affidabile in quello cattivo.
    /// </summary>
    public static int AspettaInPrimoPiano(nint hwnd, int limiteMs = 400)
    {
        var orologio = System.Diagnostics.Stopwatch.StartNew();

        while (orologio.ElapsedMilliseconds < limiteMs)
        {
            if (Nativo.GetForegroundWindow() == hwnd)
                return (int)orologio.ElapsedMilliseconds;

            Thread.Sleep(5);
        }

        Registro.Annota(
            $"la finestra 0x{hwnd:X} non è arrivata in primo piano in {limiteMs} ms; "
            + "si incolla lo stesso");

        return -1;
    }

    /// <summary>Se quel tasto è premuto in questo momento.</summary>
    static bool Premuta(byte tasto) =>
        (Nativo.GetAsyncKeyState(tasto) & 0x8000) != 0;

    /// <summary>
    /// Manda Ctrl+V con la API di Windows.
    /// </summary>
    public static void IncollaConTastiera()
    {
        // Ctrl scende per PRIMO, e non dopo aver rilasciato gli altri
        // modificatori come si faceva fino alla 4.11.0. Rilasciare Alt da
        // solo non è innocuo: la documentazione di WM_SYSKEYUP dice che
        // «DefWindowProc sends a WM_SYSCOMMAND message to the top-level
        // window if the F10 key or the ALT key was released. The wParam
        // parameter of the message is set to SC_KEYMENU», e SC_KEYMENU è
        // aprire il menu. In Outlook questo fa comparire le lettere su
        // tutti i pulsanti della barra multifunzione, e il Ctrl+V che
        // segue se lo mangia il menu invece di incollare. Con Ctrl già
        // premuto, quell'Alt che risale non va da solo.
        Nativo.keybd_event(Nativo.VK_CONTROL, 0, 0, 0);

        // E si rilasciano solo quelli che sono davvero premuti. Prima si
        // rilasciavano sempre tutti e due, anche con una scorciatoia come
        // ctrl+q che non ne ha nessuno: si mandava un Alt che l'utente
        // non aveva mai toccato, a ogni incolla.
        //
        // Rilasciarli quando CI sono continua a servire: se la
        // scorciatoia ha Shift e non lo si rilascia, la destinazione
        // riceve Ctrl+Shift+V, che in molte applicazioni è "incolla senza
        // formato" e in altre non è niente.
        if (Premuta(Nativo.VK_SHIFT))
            Nativo.keybd_event(Nativo.VK_SHIFT, 0, Nativo.KEYEVENTF_KEYUP, 0);

        if (Premuta(Nativo.VK_MENU))
            Nativo.keybd_event(Nativo.VK_MENU, 0, Nativo.KEYEVENTF_KEYUP, 0);

        Thread.Sleep(20);

        Nativo.keybd_event(Nativo.VK_V, 0, 0, 0);

        Thread.Sleep(30);

        Nativo.keybd_event(Nativo.VK_V, 0, Nativo.KEYEVENTF_KEYUP, 0);
        Nativo.keybd_event(Nativo.VK_CONTROL, 0, Nativo.KEYEVENTF_KEYUP, 0);
    }
}
