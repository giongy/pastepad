using System.Runtime.InteropServices;

namespace Pastepad.App;

/// <summary>
/// L'arrotondamento degli angoli della finestra.
///
/// Lo disegna Windows, non noi. Gli si chiede il raggio grande in modo
/// esplicito invece di lasciarlo a "decida il sistema": così il valore è
/// scritto e non dipende dal fatto che il criterio predefinito non
/// cambi.
///
/// **Non esiste un raggio su misura.** L'enumerazione ufficiale ha solo
/// quattro valori —DEFAULT, DONOTROUND, ROUND e ROUNDSMALL— e nessuno
/// accetta un numero di pixel. Fonte: DWM_WINDOW_CORNER_PREFERENCE su
/// learn.microsoft.com. ROUND è il massimo che si può chiedere.
///
/// Sta in un file suo e non accanto a <see cref="CorniceFinestra"/>
/// perché quello è in attesa di essere sostituito da API di AppWindow e
/// questo non c'entra niente.
/// </summary>
static class AngoliFinestra
{
    /// <summary>DWMWA_WINDOW_CORNER_PREFERENCE.</summary>
    const int Preferenza = 33;

    /// <summary>DWMWCP_ROUND: il raggio grande, il massimo offerto.</summary>
    const int Rotonde = 2;

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(
        nint finestra, int attributo, ref int valore, int dimensione);

    /// <summary>
    /// Chiede l'arrotondamento grande. Su Windows 10 la chiamata fallisce
    /// e non succede niente: lì le finestre non si arrotondano e il
    /// pannello esce con angoli retti, come il resto del sistema.
    /// </summary>
    public static void Arrotonda(nint finestra)
    {
        try
        {
            int valore = Rotonde;
            DwmSetWindowAttribute(finestra, Preferenza, ref valore, sizeof(int));
        }
        catch (Exception e)
        {
            Registro.Errore("arrotondare gli angoli della finestra", e);
        }
    }
}
