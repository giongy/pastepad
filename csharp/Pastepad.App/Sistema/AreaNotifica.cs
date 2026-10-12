using System.Runtime.InteropServices;

namespace Pastepad.App.Sistema;

/// <summary>
/// L'icona accanto all'orologio. WinUI 3 non ne porta nessuna —la
/// proposta è aperta da anni nel suo repository— quindi si chiama
/// Shell_NotifyIcon direttamente, sulla stessa finestra solo-messaggi
/// che già gestisce la scorciatoia. Costa una quarantina di righe e zero
/// dipendenze.
/// </summary>
internal sealed class AreaNotifica : IDisposable
{
    const uint ID = 1;

    Nativo.NOTIFYICONDATAW _dati;
    bool _impostato;

    public AreaNotifica(nint hwnd, string consiglio)
    {
        _dati = new Nativo.NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<Nativo.NOTIFYICONDATAW>(),
            hWnd = hwnd,
            uID = ID,
            uFlags = Nativo.NIF_ICON | Nativo.NIF_MESSAGE | Nativo.NIF_TIP,
            uCallbackMessage = Nativo.WM_AREANOTIFICA,
            hIcon = CaricaIcona(),
            szTip = consiglio,
            szInfo = "",
            szInfoTitle = "",
        };

        _impostato = Nativo.Shell_NotifyIconW(Nativo.NIM_ADD, ref _dati);

        if (!_impostato)
            Registro.Annota("Shell_NotifyIcon fallita con errore " +
                            Marshal.GetLastWin32Error());
    }

    static nint CaricaIcona()
    {
        string percorso = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");

        if (File.Exists(percorso))
        {
            nint icona = Nativo.LoadImageW(
                0, percorso, Nativo.IMAGE_ICON, 0, 0,
                Nativo.LR_LOADFROMFILE | Nativo.LR_DEFAULTSIZE);

            if (icona != 0) return icona;

            Registro.Annota("impossibile caricare AppIcon.ico, errore " +
                            Marshal.GetLastWin32Error());
        }
        else
        {
            Registro.Annota($"non esiste {percorso}");
        }

        // Senza icona non c'è voce nell'area di notifica, e senza quella
        // non c'è modo di recuperare l'applicazione. Quella generica di
        // Windows è brutta ma lascia il programma usabile.
        return Nativo.LoadIconW(0, Nativo.IDI_APPLICATION);
    }

    /// <summary>
    /// Un fumetto accanto all'orologio. È l'unico posto dove si può dire
    /// qualcosa all'utente quando il pannello non è davanti.
    ///
    /// Esiste per un errore silenzioso: incollando, il pannello si
    /// nasconde PRIMA di restituire il focus, quindi quando quel passo
    /// falliva l'avviso «Copiato, ma non sono riuscito a tornare alla
    /// finestra precedente» si disegnava in una finestra già nascosta.
    /// L'utente restava con il testo copiato, non incollato e senza
    /// sapere perché. Lo trovò il qa, e gli successe una quindicina di
    /// volte di fila.
    ///
    /// Non porta NIIF_RESPECT_QUIET_TIME apposta. La documentazione
    /// raccomanda di metterlo «on all appropriate notifications», ma
    /// dice anche che in quel periodo «certain notifications should
    /// still be sent because they are expected by the user as feedback
    /// in response to a user action». Questo è esattamente quel caso:
    /// l'utente ha appena premuto per incollare e ciò che riceve è la
    /// risposta. Ingoiarlo lascerebbe l'errore muto come prima.
    /// </summary>
    public void Avvisa(string titolo, string testo)
    {
        if (!_impostato) return;

        // Il titolo non è un ornamento: senza, Windows non disegna
        // l'icona del fumetto —«if the szInfoTitle member is
        // zero-length, the icon is not shown»— e l'avviso perde metà
        // della sua forza.
        _dati.uFlags = Nativo.NIF_ICON | Nativo.NIF_MESSAGE
                      | Nativo.NIF_TIP | Nativo.NIF_INFO;

        // I limiti sono quelli dello struct, contando il nullo finale.
        // Un testo più lungo non si taglia da solo: fallisce l'intera
        // chiamata.
        _dati.szInfoTitle = Ritaglia(titolo, 63);
        _dati.szInfo = Ritaglia(testo, 255);
        _dati.dwInfoFlags = Nativo.NIIF_WARNING;

        if (!Nativo.Shell_NotifyIconW(Nativo.NIM_MODIFY, ref _dati))
            Registro.Annota("impossibile mostrare il fumetto dell'area di notifica, errore "
                            + Marshal.GetLastWin32Error());

        // Si lascia lo struct com'era. Altrimenti qualsiasi NIM_MODIFY
        // successivo —cambiare il suggerimento, per esempio— farebbe
        // uscire di nuovo lo stesso fumetto, perché NIF_INFO resterebbe
        // impostato con il suo testo.
        _dati.uFlags = Nativo.NIF_ICON | Nativo.NIF_MESSAGE | Nativo.NIF_TIP;
        _dati.szInfo = "";
        _dati.szInfoTitle = "";
        _dati.dwInfoFlags = 0;
    }

    static string Ritaglia(string testo, int limite) =>
        testo.Length <= limite ? testo : testo[..limite];

    public void Dispose()
    {
        if (!_impostato) return;

        Nativo.Shell_NotifyIconW(Nativo.NIM_DELETE, ref _dati);
        _impostato = false;
    }
}
