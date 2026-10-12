using System.Diagnostics;
using System.Text;
using Microsoft.Win32;

namespace Pastepad.App.Sistema;

/// <summary>
/// Avviarsi con Windows, per due strade insieme.
///
/// La prima è una voce di registro in HKCU, che continua a funzionare
/// in un'applicazione non impacchettata: la documentazione sulla
/// distribuzione senza identità di pacchetto lo dice espressamente.
///
/// La seconda è un'attività pianificata all'accesso, ed esiste perché
/// la prima fallì senza lasciare traccia. Verificato sulla macchina
/// dell'utente dopo un avvio di Windows: la voce di registro era
/// impostata, puntava a un eseguibile che esiste, non era disattivata
/// in Gestione attività, e Windows elaborò quella chiave in
/// quell'avvio —OneDrive, che è un'altra voce della stessa chiave,
/// partì 15 secondi dopo explorer—. pastepad non lasciò nemmeno una
/// riga, né nella cartella dati né nel log di riserva accanto
/// all'eseguibile, e non ci fu rapporto di errore. O il processo non
/// arrivò a crearsi, o morì prima della sua prima istruzione, che è
/// dove l'host di .NET fallisce in silenzio.
///
/// Non si sostituisce una cosa con l'altra: si mettono tutte e due.
/// Se funzionano tutte e due, la seconda istanza cede alla prima e se
/// ne va —questo era già risolto e lascia la sua riga nel log—. Se
/// fallisce il registro, parte l'attività. L'attività aspetta mezzo
/// minuto apposta, perché nel caso normale vinca sempre il registro e
/// il log dica quale dei due ci ha portato.
/// </summary>
internal static class Avvio
{
    const string Chiave = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string Nome = "pastepad";

    /// <summary>
    /// Come si chiama l'attività. Senza barre: si crea nella radice, che
    /// è dove un utente senza privilegi può crearla.
    /// </summary>
    const string AttivitaPianificata = "pastepad";

    /// <summary>
    /// Il percorso da cui è stato lanciato. Se si sposta la cartella
    /// bisogna aprirlo una volta dal posto nuovo.
    /// </summary>
    static string PercorsoProprio() =>
        Environment.ProcessPath ?? AppContext.BaseDirectory;

    public static bool Attivo()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(Chiave);
            return k?.GetValue(Nome) is not null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException
                                    or System.Security.SecurityException)
        {
            Registro.Errore("leggere l'avvio automatico", e);
            return false;
        }
    }

    /// <summary>
    /// Ciò che c'è oggi nella chiave, per poter dire cosa è cambiato e
    /// da cosa a cosa. Restituisce il valore così com'è salvato —
    /// virgolette comprese—, non il percorso pulito: ciò che interessa
    /// annotare è esattamente ciò che c'era.
    /// </summary>
    public static string? ValoreAttuale()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(Chiave);
            return k?.GetValue(Nome) as string;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException
                                    or System.Security.SecurityException)
        {
            Registro.Errore("leggere il valore dell'avvio automatico", e);
            return null;
        }
    }

    public static void Metti(bool attivare)
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(Chiave, writable: true);

            if (k is null)
            {
                Registro.Annota($"impossibile aprire HKCU\\{Chiave}");
                return;
            }

            if (attivare)
                k.SetValue(Nome, $"\"{PercorsoProprio()}\"", RegistryValueKind.String);
            else if (k.GetValue(Nome) is not null)
                k.DeleteValue(Nome);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException
                                    or System.Security.SecurityException)
        {
            Registro.Errore("scrivere l'avvio automatico", e);
        }
    }

    // --------------------------------------------- l'attività pianificata

    /// <summary>
    /// Lascia l'attività come deve essere, e restituisce se è servito
    /// toccarla.
    ///
    /// **Non si chiama dal thread dell'interfaccia.** Lancia schtasks
    /// una o due volte, e il requisito numero due del progetto è che
    /// aprire il pannello sia istantaneo: l'avvio è misurato in ~420 ms
    /// e non se ne spende nemmeno uno per questo. Si controlla a ogni
    /// avvio invece di annotare in config.json che è già stato fatto,
    /// perché un'attività che l'utente cancella a mano deve tornare da
    /// sola; in un thread in sottofondo, chiedere non costa niente.
    /// </summary>
    public static void AssicuraAttivitaPianificata(bool attivare)
    {
        try
        {
            bool esiste = AttivitaPianificataPresente();

            if (!attivare)
            {
                if (!esiste) return;

                Esegui("/Delete", "/TN", AttivitaPianificata, "/F");
                Registro.Annota("attività di accesso: tolta");
                return;
            }

            // Si riscrive che esista o no, per lo stesso motivo della
            // voce di registro: è il modo di correggere un percorso
            // vecchio dopo aver reinstallato in un'altra cartella.
            string xml = Path.Combine(Path.GetTempPath(), "pastepad-attivita.xml");

            // UTF-16, che è ciò che schtasks legge. In UTF-8 risponde che
            // l'XML non è valido senza dire perché.
            File.WriteAllText(xml, DefinizioneXml(), Encoding.Unicode);

            try
            {
                var (codice, uscita) = Esegui(
                    "/Create", "/TN", AttivitaPianificata, "/XML", xml, "/F");

                Registro.Annota(codice == 0
                    ? $"attività di accesso: {(esiste ? "aggiornata" : "creata")} "
                      + $"per {PercorsoProprio()}"
                    : $"attività di accesso: schtasks ha restituito {codice}: {uscita}");
            }
            finally
            {
                // Contiene il nome utente. Non si lascia in giro nella
                // cartella temporanea.
                try { File.Delete(xml); }
                catch (Exception e) when (e is IOException
                                            or UnauthorizedAccessException)
                {
                    Registro.Errore("eliminare l'XML dell'attività", e);
                }
            }
        }
        catch (Exception e)
        {
            // Largo apposta: questo gira in un thread in sottofondo, e
            // un'eccezione che scappa da qui si porta via l'intero processo.
            // L'avvio automatico da registro resta impostato.
            Registro.Errore("assicurare l'attività di accesso", e);
        }
    }

    static bool AttivitaPianificataPresente() => Esegui("/Query", "/TN", AttivitaPianificata).Codice == 0;

    /// <summary>
    /// L'attività, così come la vuole schtasks.
    ///
    /// Due decisioni che non sono di ornamento:
    ///
    /// <c>InteractiveToken</c> con livello <c>LeastPrivilege</c> è ciò
    /// che permette di registrarla senza essere amministratore e senza
    /// chiedere la password —«you do not need to specify a password when
    /// registering the task if you register the task to run under the
    /// security context of your account and you use the S4U or
    /// interactive logon type»—. Con <c>Password</c> o <c>S4U</c>
    /// servirebbe il privilegio di accesso come processo batch, che un
    /// utente normale non ha.
    ///
    /// E senza nessuna delle condizioni di fabbrica: l'attività porta di
    /// serie «non avviare se va a batteria» e «fermare se passa a
    /// batteria», che su un portatile scollegato è esattamente l'avvio
    /// che non avviene.
    /// </summary>
    static string DefinizioneXml()
    {
        string utente = System.Security.Principal.WindowsIdentity
            .GetCurrent().Name;

        return $"""
        <?xml version="1.0" encoding="UTF-16"?>
        <Task version="1.4"
              xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
          <RegistrationInfo>
            <Description>Apre pastepad all'accesso. La crea il programma
        stesso; la preferenza "avvio_automatico" di config.json la toglie.</Description>
            <URI>\{AttivitaPianificata}</URI>
          </RegistrationInfo>
          <Triggers>
            <LogonTrigger>
              <Enabled>true</Enabled>
              <UserId>{Proteggi(utente)}</UserId>
              <Delay>PT30S</Delay>
            </LogonTrigger>
          </Triggers>
          <Principals>
            <Principal id="Author">
              <UserId>{Proteggi(utente)}</UserId>
              <LogonType>InteractiveToken</LogonType>
              <RunLevel>LeastPrivilege</RunLevel>
            </Principal>
          </Principals>
          <Settings>
            <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
            <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
            <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
            <AllowHardTerminate>false</AllowHardTerminate>
            <StartWhenAvailable>false</StartWhenAvailable>
            <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
            <IdleSettings>
              <StopOnIdleEnd>false</StopOnIdleEnd>
              <RestartOnIdle>false</RestartOnIdle>
            </IdleSettings>
            <AllowStartOnDemand>true</AllowStartOnDemand>
            <Enabled>true</Enabled>
            <Hidden>false</Hidden>
            <RunOnlyIfIdle>false</RunOnlyIfIdle>
            <DisallowStartOnRemoteAppSession>false</DisallowStartOnRemoteAppSession>
            <UseUnifiedSchedulingEngine>true</UseUnifiedSchedulingEngine>
            <WakeToRun>false</WakeToRun>
            <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
            <Priority>7</Priority>
          </Settings>
          <Actions Context="Author">
            <Exec>
              <Command>{Proteggi(PercorsoProprio())}</Command>
            </Exec>
          </Actions>
        </Task>
        """;
    }

    /// <summary>
    /// Un nome utente o un percorso possono contenere &amp; o virgolette
    /// —«Jose &amp; Co», una cartella con un apostrofo—, e dentro l'XML
    /// questo lo rende non valido. schtasks direbbe solo che non lo
    /// capisce.
    /// </summary>
    static string Proteggi(string testo) => testo
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;")
        .Replace("'", "&apos;");

    /// <summary>
    /// schtasks, senza finestra e raccogliendo ciò che dice. Gli
    /// argomenti vanno in <c>ArgumentList</c> e non in una stringa: il
    /// percorso dell'eseguibile contiene spazi appena l'utente ha un
    /// nome con uno spazio.
    /// </summary>
    static (int Codice, string Uscita) Esegui(params string[] argomenti)
    {
        var avvio = new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var a in argomenti) avvio.ArgumentList.Add(a);

        using var processo = Process.Start(avvio);

        if (processo is null) return (-1, "impossibile avviare schtasks");

        string uscita = processo.StandardOutput.ReadToEnd()
                      + processo.StandardError.ReadToEnd();

        // Con limite: se schtasks restasse appeso, questo thread resta
        // con lui, e con il processo non si può chiudere pastepad.
        if (!processo.WaitForExit(15_000))
        {
            Registro.Annota("schtasks non è terminato in 15 s; lo si lascia");
            return (-1, "nessuna risposta");
        }

        return (processo.ExitCode, uscita.Trim().Replace("\r\n", " "));
    }
}
