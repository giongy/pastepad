namespace Pastepad.App.Sistema;

/// <summary>
/// Traduce "ctrl+shift+v" in ciò che capisce RegisterHotKey.
/// </summary>
internal static class Combinazione
{
    static readonly Dictionary<string, uint> _modificatori = new()
    {
        ["ctrl"] = Nativo.MOD_CONTROL,
        ["control"] = Nativo.MOD_CONTROL,
        ["alt"] = Nativo.MOD_ALT,
        ["shift"] = Nativo.MOD_SHIFT,
        ["win"] = Nativo.MOD_WIN,
    };

    static readonly Dictionary<string, uint> _tasti = new()
    {
        ["space"] = 0x20,
        ["spazio"] = 0x20,
        ["enter"] = 0x0D,
        ["return"] = 0x0D,
        ["tab"] = 0x09,
        ["esc"] = 0x1B,
        ["escape"] = 0x1B,
        ["insert"] = 0x2D,
    };

    /// <summary>
    /// "ctrl+shift+v" -> (MOD_CONTROL|MOD_SHIFT, 0x56). null se non è
    /// valida.
    ///
    /// Esige almeno un modificatore: Windows rifiuta di registrare un
    /// tasto da solo, e in più lo toglierebbe al resto del sistema.
    /// </summary>
    public static (uint Mods, uint Tasto)? Scomponi(string testo)
    {
        uint mods = 0;
        uint? tasto = null;

        foreach (var parte in testo.ToLowerInvariant().Replace(" ", "").Split('+'))
        {
            if (parte.Length == 0) continue;

            if (_modificatori.TryGetValue(parte, out uint m))
                mods |= m;
            else if (_tasti.TryGetValue(parte, out uint t))
                tasto = t;
            else if (parte.Length == 1)
                tasto = char.ToUpperInvariant(parte[0]);
            else
                return null;
        }

        return tasto is not null && mods != 0 ? (mods, tasto.Value) : null;
    }
}
