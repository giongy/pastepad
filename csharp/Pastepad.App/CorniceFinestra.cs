using System.Runtime.InteropServices;

namespace Pastepad.App;

/// <summary>
/// Lo scarto tra ciò che Windows chiama "la finestra" e ciò che si vede.
///
/// Una finestra ridimensionabile ha intorno una fascia invisibile da cui
/// la si afferra per allargarla. <c>GetWindowRect</c> —e con lui
/// <c>AppWindow.Size</c>— la contano; lo schermo non la mostra. Misurato
/// su questa macchina al 100%: 7 px per lato e 7 sotto, 0 sopra.
///
/// Senza toglierla, un pannello chiesto di 380x560 si vedeva di 366x553,
/// e tutte le misure delle specifiche uscivano corte.
/// </summary>
static class CorniceFinestra
{
    /// <summary>DWMWA_EXTENDED_FRAME_BOUNDS: ciò che la finestra disegna.</summary>
    const int BordiReali = 9;

    [StructLayout(LayoutKind.Sequential)]
    struct RECT
    {
        public int Sin, Su, Des, Giu;
    }

    [DllImport("dwmapi.dll")]
    static extern int DwmGetWindowAttribute(
        nint finestra, int attributo, out RECT valore, int dimensione);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetWindowRect(nint finestra, out RECT valore);

    /// <summary>
    /// Di quanto il rettangolo della finestra supera ciò che si vede. Si
    /// misura una volta a finestra già creata; restituisce (0,0) se DWM
    /// non risponde, che è il caso in cui non c'è niente da togliere.
    /// </summary>
    public static (int Larghezza, int Altezza) Margine(nint finestra)
    {
        try
        {
            if (!GetWindowRect(finestra, out var fuori)) return (0, 0);

            if (DwmGetWindowAttribute(
                    finestra, BordiReali, out var dentro,
                    Marshal.SizeOf<RECT>()) != 0)
            {
                return (0, 0);
            }

            int larghezza = (fuori.Des - fuori.Sin) - (dentro.Des - dentro.Sin);
            int altezza = (fuori.Giu - fuori.Su) - (dentro.Giu - dentro.Su);

            // Un valore assurdo significa che la misura non vale; meglio non
            // togliere niente che spostare la finestra.
            if (larghezza is < 0 or > 64 || altezza is < 0 or > 64) return (0, 0);

            return (larghezza, altezza);
        }
        catch (Exception e)
        {
            Registro.Errore("misurare la cornice della finestra", e);
            return (0, 0);
        }
    }
}
