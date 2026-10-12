using System.Runtime.InteropServices;
using System.Text;
using Pastepad.Nucleo;

namespace Pastepad.App.Sistema;

public enum TipoContenuto
{
    Niente,

    Testo,

    Immagine,

    /// <summary>Chi ha copiato ha chiesto espressamente di non salvare.</summary>
    Privato,
}

public readonly record struct Contenuto(
    TipoContenuto Tipo, string? Testo, byte[]? Immagine);

/// <summary>
/// Lettura e scrittura degli appunti con la API di Win32.
///
/// Non si usa la classe Clipboard di WinRT apposta: la sua
/// documentazione dice che agli appunti si può accedere solo con
/// l'applicazione in primo piano, e un gestore di appunti vive in
/// secondo piano per definizione.
/// </summary>
internal static class Appunti
{
    // Formati con cui un programma dice "non salvare questo". Li usano
    // KeePass, Bitwarden, Gestione credenziali di Windows e la modalità
    // in incognito di Chrome.
    // learn.microsoft.com/windows/win32/dataxchg/clipboard-formats
    static readonly string[] _privatiPerPresenza =
    [
        "Clipboard Viewer Ignore",
        "ExcludeClipboardContentFromMonitorProcessing",
    ];

    static readonly string[] _privatiPerZero =
    [
        "CanIncludeInClipboardHistory",
        "CanUploadToCloudClipboard",
    ];

    /// <summary>
    /// Contatore che Windows incrementa a ogni copia. Leggerlo costa una
    /// chiamata; aprire gli appunti costa moltissimo di più.
    ///
    /// Serve anche a scartare gli avvisi ripetuti: WM_CLIPBOARDUPDATE
    /// non arriva una volta per copia, ma una per ogni sessione aperta
    /// dal programma che copia. Misurato: PowerShell ne fa partire tre
    /// per copia.
    /// </summary>
    public static uint Sequenza() => Nativo.GetClipboardSequenceNumber();

    /// <summary>
    /// Apre gli appunti, fa qualcosa e li chiude qualunque cosa succeda.
    /// Windows li lascia aprire a un solo programma alla volta, quindi
    /// bisogna ritentare.
    /// </summary>
    static T? Con<T>(Func<T> azione, int tentativi = 4)
    {
        for (int i = 0; i < tentativi; i++)
        {
            if (Nativo.OpenClipboard(0))
            {
                try
                {
                    return azione();
                }
                catch (Exception e)
                {
                    Registro.Errore("Appunti", e);
                    return default;
                }
                finally
                {
                    Nativo.CloseClipboard();
                }
            }

            Thread.Sleep(40);
        }

        Registro.Annota(
            $"impossibile aprire gli appunti in {tentativi} tentativi");

        return default;
    }

    /// <summary>
    /// True se chi ha copiato ha chiesto di non salvare. Con gli appunti
    /// già aperti.
    /// </summary>
    static bool EPrivato()
    {
        foreach (var nome in _privatiPerPresenza)
        {
            uint f = Nativo.RegisterClipboardFormatW(nome);
            if (f != 0 && Nativo.IsClipboardFormatAvailable(f)) return true;
        }

        foreach (var nome in _privatiPerZero)
        {
            uint f = Nativo.RegisterClipboardFormatW(nome);
            if (f == 0 || !Nativo.IsClipboardFormatAvailable(f)) continue;

            // Il formato esiste: vale zero, o non si può leggere, e nei
            // due casi significa "no".
            nint mano = Nativo.GetClipboardData(f);
            if (mano == 0) return true;

            nint dati = Nativo.GlobalLock(mano);
            if (dati == 0) return true;

            try
            {
                if ((nuint)Nativo.GlobalSize(mano) >= 4 &&
                    Marshal.ReadInt32(dati) == 0)
                    return true;
            }
            finally
            {
                Nativo.GlobalUnlock(mano);
            }
        }

        return false;
    }

    public static Contenuto Leggi() => Con(() =>
    {
        if (EPrivato())
            return new Contenuto(TipoContenuto.Privato, null, null);

        if (Nativo.IsClipboardFormatAvailable(Nativo.CF_UNICODETEXT))
        {
            nint mano = Nativo.GetClipboardData(Nativo.CF_UNICODETEXT);
            if (mano != 0)
            {
                nint dati = Nativo.GlobalLock(mano);
                try
                {
                    if (dati != 0)
                    {
                        string? testo = Marshal.PtrToStringUni(dati);

                        if (!string.IsNullOrEmpty(testo))
                        {
                            if (testo.Length > Config.MaxCaratteri)
                                testo = testo[..Config.MaxCaratteri];

                            return new Contenuto(TipoContenuto.Testo, testo, null);
                        }
                    }
                }
                finally
                {
                    Nativo.GlobalUnlock(mano);
                }
            }
        }

        if (Nativo.IsClipboardFormatAvailable(Nativo.CF_DIB))
        {
            nint mano = Nativo.GetClipboardData(Nativo.CF_DIB);
            if (mano != 0)
            {
                nint dati = Nativo.GlobalLock(mano);
                try
                {
                    if (dati != 0)
                    {
                        int dim = (int)Nativo.GlobalSize(mano);
                        var dib = new byte[dim];
                        Marshal.Copy(dati, dib, 0, dim);
                        return new Contenuto(TipoContenuto.Immagine, null, dib);
                    }
                }
                finally
                {
                    Nativo.GlobalUnlock(mano);
                }
            }
        }

        return new Contenuto(TipoContenuto.Niente, null, null);
    }, 2);

    /// <summary>
    /// Lascia il testo negli appunti in due versioni insieme: con
    /// formato per Word e Outlook, semplice per tutto il resto.
    /// </summary>
    public static bool Copia(
        IReadOnlyList<Frammento> frammenti, bool senzaFormato = false)
    {
        string semplice = Regole.TestoDi(frammenti);

        return Con(() =>
        {
            if (!Nativo.EmptyClipboard())
            {
                Registro.Annota("EmptyClipboard fallita con errore " +
                                Marshal.GetLastWin32Error());
                return false;
            }

            if (!MettiTesto(Nativo.CF_UNICODETEXT, semplice)) return false;

            if (!senzaFormato)
            {
                uint rtfId = Nativo.RegisterClipboardFormatW("Rich Text Format");

                if (rtfId != 0)
                {
                    // L'RTF va in ASCII: i caratteri fuori da quell'intervallo
                    // viaggiano già con escape come \uNNNN?.
                    byte[] rtf = Encoding.ASCII.GetBytes(ARtf(frammenti) + "\0");
                    MettiBytes(rtfId, rtf);
                }
            }

            return true;
        });
    }

    public static bool CopiaImmagine(string percorso)
    {
        byte[] file;

        try
        {
            file = File.ReadAllBytes(percorso);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Registro.Errore($"leggere l'immagine {percorso}", e);
            return false;
        }

        if (file.Length <= 14) return false;

        // Gli si toglie l'intestazione di file BMP: negli appunti va il
        // DIB crudo, senza quei 14 byte.
        byte[] dib = file[14..];

        return Con(() =>
        {
            if (!Nativo.EmptyClipboard()) return false;
            return MettiBytes(Nativo.CF_DIB, dib);
        });
    }

    /// <summary>
    /// Mette l'intestazione di file ai byte crudi che dà Windows.
    /// </summary>
    public static byte[] DibABmp(byte[] dib)
    {
        var uscita = new byte[14 + dib.Length];

        uscita[0] = (byte)'B';
        uscita[1] = (byte)'M';

        BitConverter.GetBytes(14 + dib.Length).CopyTo(uscita, 2);
        BitConverter.GetBytes(54).CopyTo(uscita, 10);

        dib.CopyTo(uscita, 14);
        return uscita;
    }

    static bool MettiTesto(uint formato, string testo) =>
        MettiBytes(formato, Encoding.Unicode.GetBytes(testo + "\0"));

    /// <summary>
    /// Copia i byte in memoria globale e la consegna a Windows.
    /// Attenzione: se SetClipboardData funziona, il blocco diventa del
    /// sistema e non va liberato; se fallisce, sì.
    /// </summary>
    static bool MettiBytes(uint formato, byte[] dati)
    {
        nint mano = Nativo.GlobalAlloc(Nativo.GMEM_MOVEABLE, (nuint)dati.Length);
        if (mano == 0) return false;

        nint destinazione = Nativo.GlobalLock(mano);

        if (destinazione == 0)
        {
            Nativo.GlobalFree(mano);
            return false;
        }

        try
        {
            Marshal.Copy(dati, 0, destinazione, dati.Length);
        }
        finally
        {
            Nativo.GlobalUnlock(mano);
        }

        if (Nativo.SetClipboardData(formato, mano) == 0)
        {
            Registro.Annota($"SetClipboardData({formato}) fallita con errore " +
                            Marshal.GetLastWin32Error());
            Nativo.GlobalFree(mano);
            return false;
        }

        return true;
    }

    // ------------------------------------------------------------ RTF

    static string ProteggiRtf(string s)
    {
        var uscita = new StringBuilder(s.Length);

        foreach (char ch in s)
        {
            if (ch is '\\' or '{' or '}') uscita.Append('\\').Append(ch);
            else if (ch == '\n') uscita.Append("\\par\n");
            else if (ch == '\t') uscita.Append("\\tab ");
            else if (ch < 128) uscita.Append(ch);
            else uscita.Append("\\u").Append((int)ch).Append('?');
        }

        return uscita.ToString();
    }

    /// <summary>
    /// Converte i frammenti in RTF, che è ciò che capiscono Word e
    /// Outlook.
    /// </summary>
    public static string ARtf(IReadOnlyList<Frammento> frammenti)
    {
        var caratteri = new List<string>();
        var colori = new List<string>();

        foreach (var f in frammenti)
        {
            if (!caratteri.Contains(f.F)) caratteri.Add(f.F);

            string c = f.C.ToUpperInvariant();
            if (!colori.Contains(c)) colori.Add(c);
        }

        var tabellaF = new StringBuilder();

        for (int i = 0; i < caratteri.Count; i++)
            tabellaF.Append($"{{\\f{i}\\fnil {caratteri[i]};}}");

        var tabellaC = new StringBuilder();

        for (int i = 0; i < colori.Count; i++)
        {
            if (i > 0) tabellaC.Append(';');

            string c = colori[i];

            tabellaC.Append($"\\red{Convert.ToInt32(c.Substring(1, 2), 16)}")
                  .Append($"\\green{Convert.ToInt32(c.Substring(3, 2), 16)}")
                  .Append($"\\blue{Convert.ToInt32(c.Substring(5, 2), 16)}");
        }

        var corpo = new StringBuilder("\\pard\\plain ");

        foreach (var f in frammenti)
        {
            corpo.Append($"\\f{caratteri.IndexOf(f.F)}")
                  .Append($"\\fs{f.S * 2}")
                  .Append($"\\cf{colori.IndexOf(f.C.ToUpperInvariant()) + 1}");

            if (f.B != 0) corpo.Append("\\b");
            if (f.I != 0) corpo.Append("\\i");
            if (f.U != 0) corpo.Append("\\ul");

            corpo.Append(' ').Append(ProteggiRtf(f.T));

            if (f.U != 0) corpo.Append("\\ulnone");
            if (f.I != 0) corpo.Append("\\i0");
            if (f.B != 0) corpo.Append("\\b0");
        }

        return $"{{\\rtf1\\ansi\\deff0{{\\fonttbl{tabellaF}}}" +
               $"{{\\colortbl;{tabellaC};}}{corpo}}}";
    }
}
