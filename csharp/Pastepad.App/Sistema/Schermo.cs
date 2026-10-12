using System.Runtime.InteropServices;

namespace Pastepad.App.Sistema;

/// <summary>
/// Dove sta il mouse e dove ci sta il pannello.
/// </summary>
internal static class Schermo
{
    /// <summary>Dove sta il mouse, in pixel fisici.</summary>
    public static (int X, int Y) Puntatore()
    {
        if (Nativo.GetCursorPos(out var punto)) return (punto.x, punto.y);

        Registro.Annota("GetCursorPos fallita con errore " +
                        Marshal.GetLastWin32Error());

        return (0, 0);
    }

    /// <summary>
    /// L'area del monitor in cui si trova il punto, senza la barra delle
    /// applicazioni.
    /// </summary>
    public static Nativo.RECT AreaUtile(int x, int y)
    {
        nint monitor = Nativo.MonitorFromPoint(
            new Nativo.POINT { x = x, y = y }, Nativo.MONITOR_DEFAULTTONEAREST);

        var info = new Nativo.MONITORINFO
        {
            cbSize = (uint)Marshal.SizeOf<Nativo.MONITORINFO>(),
        };

        if (monitor != 0 && Nativo.GetMonitorInfoW(monitor, ref info))
            return info.rcWork;

        Registro.Annota("GetMonitorInfo fallita; si usa 1920x1080");

        return new Nativo.RECT { left = 0, top = 0, right = 1920, bottom = 1080 };
    }

    /// <summary>
    /// Colloca un rettangolo di quella dimensione vicino al puntatore
    /// senza che esca dal monitor. Restituisce pixel fisici, che è ciò
    /// che vuole AppWindow.MoveAndResize.
    /// </summary>
    public static (int X, int Y) VicinoAlPuntatore(int larghezza, int altezza)
    {
        var (mouse, y) = Puntatore();
        var area = AreaUtile(mouse, y);

        int x = mouse + 12;
        int destinazioneY = y + 12;

        if (x + larghezza > area.right) x = mouse - larghezza - 12;
        if (destinazioneY + altezza > area.bottom) destinazioneY = y - altezza - 12;

        // Se non ci sta nemmeno dall'altro lato, si attacca al bordo
        // piuttosto che restare mezzo fuori dallo schermo.
        x = Math.Clamp(x, area.left, Math.Max(area.left, area.right - larghezza));
        destinazioneY = Math.Clamp(destinazioneY, area.top, Math.Max(area.top, area.bottom - altezza));

        return (x, destinazioneY);
    }
}
