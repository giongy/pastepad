using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Pastepad.Nucleo;

/// <summary>
/// Tutto lo stato dell'applicazione in un solo posto. Prima ogni parte
/// dell'interfaccia leggeva e scriveva i file per conto suo; adesso
/// passano da qui, che è l'unico a toccare il disco.
/// </summary>
public sealed class Archivio
{
    /// <summary>Ogni quanto si riversa la cronologia in sospeso.</summary>
    public static readonly TimeSpan IntervalloRiversamento = TimeSpan.FromSeconds(3);

    static readonly JsonSerializerOptions _formato = new()
    {
        WriteIndented = true,
        IndentSize = 1,
        // Equivale all'ensure_ascii=False di Python: gli accenti si
        // scrivono tali e quali invece che come \uXXXX.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Qualsiasi errore che l'archivio ingoia per poter proseguire.
    /// L'applicazione lo aggancia al suo registro: nessun catch muto,
    /// che fu il motivo per cui l'errore della scorciatoia passò tre
    /// tentativi senza diagnosi.
    /// </summary>
    public event Action<string, Exception>? Anomalia;

    /// <summary>
    /// Ciò che merita una riga nel registro senza essere un errore:
    /// soprattutto, che una lettura fallita sia riuscita al nuovo
    /// tentativo. È il dato che chiude la diagnosi del perché a volte
    /// non si riusciva a leggere la cronologia.
    /// </summary>
    public event Action<string>? Avviso;

    /// <summary>
    /// Quante volte si ritenta una lettura che dà <see
    /// cref="IOException"/>, e quanto si aspetta tra l'una e l'altra.
    ///
    /// L'ipotesi dell'errore aperto è una violazione di condivisione con
    /// l'istanza precedente che sta ancora morendo: il file si libera in
    /// millisecondi. Se è quello, il secondo tentativo entra e il
    /// registro lo dice; se non lo è, tre tentativi costano 200 ms una
    /// volta all'avvio e non si perde niente.
    /// </summary>
    const int Tentativi = 3;

    static readonly TimeSpan AttesaTraTentativi = TimeSpan.FromMilliseconds(100);

    public Percorsi Percorsi { get; }

    public List<string> Cartelle { get; private set; } = [];
    public List<Snippet> Snippets { get; private set; } = [];
    public List<Voce> Cron { get; set; } = [];

    /// <summary>
    /// Le annotazioni del blocco, l'ultima toccata per prima. Non si
    /// incollano mai; vedi <see cref="Nota"/>.
    /// </summary>
    public List<Nota> Note { get; private set; } = [];

    JsonObject _prefs = [];

    bool _cronSporca;
    long _ultimoRiversamento;

    /// <summary>
    /// File che esistono ma che non si è riusciti a leggere. Su questi
    /// NON si scrive: farlo cambierebbe "non sono riuscito a leggere i
    /// tuoi dati" in "li ho appena cancellati".
    ///
    /// È successo davvero: un'istanza partì senza poter accedere alla
    /// sua cartella, mostrò una cronologia vuota, e chiudendosi salvò
    /// quel vuoto sopra le voci reali. Nel silenzio assoluto, perché il
    /// registro degli errori viveva nella stessa cartella che non
    /// funzionava.
    /// </summary>
    readonly HashSet<string> _illeggibili = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// True se qualche file esisteva e non si è potuto leggere. Quando
    /// succede, ciò che è caricato NON è lo stato reale dell'utente.
    /// </summary>
    public bool LetturaIncompleta => _illeggibili.Count > 0;

    /// <summary>
    /// Perché questa sessione non deve scrivere NIENTE, se è il caso.
    ///
    /// È più forte di <see cref="_illeggibili"/>: quello protegge i file
    /// che non si sono potuti leggere, uno per uno, e questo ferma
    /// l'intera sessione. Si usa quando a fallire non è un file ma il
    /// posto: se ciò che si legge non è ciò che l'utente ha, nemmeno ciò
    /// che si scrive andrà dove l'utente crede.
    /// </summary>
    string? _congelato;

    /// <summary>
    /// Lascia la sessione in sola lettura, per sempre. Non si torna
    /// indietro apposta: chi decide questo lo fa all'avvio e con un
    /// motivo, e una sessione che si scongela da sola è proprio quella
    /// che finirebbe per scrivere dove non deve.
    /// </summary>
    public void Congela(string motivo)
    {
        _congelato ??= motivo;
        Avviso?.Invoke("archivio congelato: " + motivo);
    }

    public bool Congelato => _congelato is not null;

    /// <summary>
    /// Cosa raccontare all'utente, se c'è qualcosa da raccontare.
    /// L'interfaccia deve mostrarlo: un errore sui dati che non si vede
    /// è peggio di uno che si vede.
    ///
    /// Il congelamento va per primo perché spiega l'altro: quando i file
    /// che si vedono non sono quelli dell'utente, «non sono riuscito a
    /// leggere i tuoi dati» descrive il sintomo e non la causa.
    /// </summary>
    public string? Problema => _congelato ?? (LetturaIncompleta
        ? "Non sono riuscito a leggere i tuoi dati salvati ("
          + string.Join(", ", _illeggibili.Select(Path.GetFileName))
          + "). Per non perderli, non ci scriverò sopra: ciò che fai in "
          + "questa sessione non verrà salvato. Chiudi il programma e "
          + "riaprilo."
        : null);

    /// <summary>
    /// I due segnalatori si ricevono qui e non si agganciano dopo
    /// apposta: tutte le letture avvengono in questo costruttore.
    /// Iscritto da fuori, il registro si perdeva intero proprio il
    /// giorno in cui serviva —l'avvio in cui non si è potuta leggere la
    /// cronologia è l'unico che ha qualcosa da raccontare— e restava un
    /// Problema a schermo senza una sola riga in errori.log che lo
    /// spiegasse.
    /// </summary>
    public Archivio(
        Percorsi? percorsi = null,
        Action<string, Exception>? anomalia = null,
        Action<string>? avviso = null)
    {
        if (anomalia is not null) Anomalia += anomalia;
        if (avviso is not null) Avviso += avviso;

        Percorsi = percorsi ?? Percorsi.Predefinite();

        _prefs = Leggi<JsonObject>(Percorsi.Preferenze) ?? [];
        Cron = Leggi<List<Voce>>(Percorsi.Cronologia) ?? [];

        Note = Leggi<List<Nota>>(Percorsi.Note) ?? [];
        OrdinaNote();

        // Le annotazioni di prima della 4.15.0 portano "testo" e non "runs".
        foreach (var n in Note)
        {
            if (n.Runs.Count == 0)
                n.Runs = [Regole.CreaFrammento(n.Testo ?? "")];

            n.Testo = null;
        }

        var raccolta = Leggi<Raccolta>(Percorsi.Dati) ?? new Raccolta();
        Cartelle = raccolta.Categorie;
        Snippets = raccolta.Snippets;

        // Gli snippet di versioni vecchie portano "testo" e non "runs".
        foreach (var s in Snippets)
        {
            if (s.Runs.Count == 0)
                s.Runs = [Regole.CreaFrammento(s.Testo ?? "")];
        }
    }

    // ----------------------------------------------------------- file

    /// <summary>
    /// Che il file non esista è normale: primo avvio. Che esista e non
    /// si possa leggere è tutta un'altra cosa, e bisogna distinguerle —
    /// da questo dipende se ci si scrive sopra o no.
    ///
    /// Si apre una volta sola invece di chiedere con File.Exists e
    /// leggere dopo. Il riferimento di .NET lo dice alla lettera:
    /// File.Exists restituisce false se non c'è il permesso di lettura,
    /// e non lancia. Con la domanda davanti, un file a cui ci viene
    /// negato l'accesso passava per "primo avvio": il programma
    /// caricava una cronologia vuota, non la segnava come illeggibile, e
    /// chiudendosi la scriveva sopra quella buona. Aprendolo, "non
    /// esiste" e "non posso" arrivano come due eccezioni diverse e non
    /// c'è modo di confonderle.
    /// </summary>
    T? Leggi<T>(string percorso) where T : class
    {
        for (int tentativo = 1; ; tentativo++)
        {
            try
            {
                // FileShare.ReadWrite: si legge soltanto, e bloccare chi sta
                // scrivendo non aiuterebbe nessuno.
                using var flusso = new FileStream(
                    percorso, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                var dato = JsonSerializer.Deserialize<T>(flusso);

                if (tentativo > 1)
                {
                    Avviso?.Invoke($"{percorso} letto correttamente al tentativo {tentativo} "
                                + $"di {Tentativi}");
                }

                return dato;
            }
            catch (Exception e) when (e is FileNotFoundException
                                        or DirectoryNotFoundException)
            {
                // L'unico caso benigno, e l'unico che non si segna.
                return null;
            }
            catch (IOException e) when (tentativo < Tentativi)
            {
                Anomalia?.Invoke(
                    $"impossibile leggere {percorso} (tentativo {tentativo} di {Tentativi}); "
                    + $"si riprova tra {AttesaTraTentativi.TotalMilliseconds:F0} ms",
                    e);

                Thread.Sleep(AttesaTraTentativi);
            }
            catch (Exception e)
            {
                // Qualsiasi altra cosa è illeggibile, senza una lista di tipi
                // da indovinare: il programma parte lo stesso, ma da qui in
                // poi questo file non si tocca. Sbagliare da questo lato
                // costa una sessione; dall'altro, l'intera cronologia.
                Anomalia?.Invoke($"impossibile leggere {percorso}; non ci si scriverà "
                                 + "sopra per tutta la sessione", e);
                _illeggibili.Add(percorso);
                return null;
            }
        }
    }

    /// <summary>
    /// Scrive prima in un file a parte e poi lo sposta. Se il programma
    /// si chiude a metà della scrittura, il file buono resta intatto
    /// invece di restare troncato.
    /// </summary>
    bool Scrivi<T>(string percorso, T dati)
    {
        // Sessione congelata: non si scrive niente, in nessun file.
        //
        // Questo è l'unico posto da cui passano TUTTE le scritture,
        // quindi è l'unico dove la garanzia si può dare davvero.
        // Metterlo in Riversa lascerebbe fuori ciò che si salva
        // all'istante —fissare, eliminare, svuotare—, che è proprio ciò
        // che l'utente fa apposta e ciò che fa più male perdere.
        if (_congelato is not null)
        {
            Anomalia?.Invoke(
                $"non si scrive {percorso}: {_congelato}",
                new InvalidOperationException("archivio congelato"));

            return false;
        }

        // Ciò che non si è potuto leggere non si sovrascrive. Preferiamo
        // perdere questa sessione che perdere tutte le precedenti.
        if (_illeggibili.Contains(percorso))
        {
            Anomalia?.Invoke(
                $"non si scrive {percorso}: non si è potuto leggere all'avvio",
                new InvalidOperationException("file segnato come illeggibile"));

            return false;
        }

        string temporaneo = percorso + ".tmp";

        try
        {
            string cartella = Path.GetDirectoryName(percorso) ?? "";
            if (cartella.Length > 0) Directory.CreateDirectory(cartella);

            File.WriteAllText(temporaneo,
                JsonSerializer.Serialize(dati, _formato));

            File.Move(temporaneo, percorso, overwrite: true);
            return true;
        }
        catch (Exception e) when (e is IOException
                                    or UnauthorizedAccessException
                                    or NotSupportedException)
        {
            Anomalia?.Invoke($"impossibile scrivere {percorso}", e);

            try
            {
                if (File.Exists(temporaneo)) File.Delete(temporaneo);
            }
            catch (IOException eliminato)
            {
                Anomalia?.Invoke($"impossibile ripulire {temporaneo}", eliminato);
            }

            return false;
        }
    }

    // ----------------------------------------------------- preferenze

    public T? Pref<T>(string chiave, T? predefinito = default)
    {
        if (!_prefs.TryGetPropertyValue(chiave, out var valore) || valore is null)
            return predefinito;

        try
        {
            return valore.GetValue<T>();
        }
        catch (Exception e) when (e is InvalidOperationException
                                    or FormatException)
        {
            Anomalia?.Invoke($"preferenza '{chiave}' con un tipo strano", e);
            return predefinito;
        }
    }

    public void MettiPref<T>(string chiave, T valore)
    {
        _prefs[chiave] = JsonValue.Create(valore);
        Scrivi(Percorsi.Preferenze, _prefs);
    }

    // ---------------------------------------------------------- salva

    public void SalvaDati() => Scrivi(Percorsi.Dati, new Raccolta
    {
        Categorie = Cartelle,
        Snippets = Snippets,
    });

    /// <summary>
    /// Segna la cronologia come in sospeso; il disco può aspettare.
    /// Prima ogni copia riscriveva l'intero JSON: con 80 voci da 20 KB
    /// sono 7,8 ms e 1,2 MB a ogni Ctrl+C.
    ///
    /// Con <paramref name="gia"/> si scrive subito: ciò che l'utente ha
    /// fatto apposta (fissare, eliminare, svuotare) deve sopravvivere
    /// anche se il programma muore un secondo dopo. Si rimanda solo la
    /// cattura automatica degli appunti.
    /// </summary>
    public void SalvaCron(bool gia = false)
    {
        _cronSporca = true;
        if (gia) Riversa(true);
    }

    /// <summary>
    /// Scrive la cronologia in sospeso. True se ha toccato il disco.
    /// </summary>
    public bool Riversa(bool forza = false)
    {
        if (!_cronSporca) return false;

        long adesso = Stopwatch.GetTimestamp();

        if (!forza &&
            Stopwatch.GetElapsedTime(_ultimoRiversamento, adesso) < IntervalloRiversamento)
            return false;

        Scrivi(Percorsi.Cronologia, Cron);
        _cronSporca = false;
        _ultimoRiversamento = adesso;
        return true;
    }

    // ------------------------------------------------------- cartelle

    public bool CreaCartella(string nome)
    {
        if (string.IsNullOrEmpty(nome) || Cartelle.Contains(nome))
            return false;

        Cartelle.Add(nome);
        SalvaDati();
        return true;
    }

    public bool RinominaCartella(string vecchio, string nuovo)
    {
        if (string.IsNullOrEmpty(nuovo) ||
            Cartelle.Contains(nuovo) ||
            !Cartelle.Contains(vecchio))
            return false;

        Cartelle[Cartelle.IndexOf(vecchio)] = nuovo;

        foreach (var s in Snippets)
        {
            if (s.Categoria == vecchio) s.Categoria = nuovo;
        }

        SalvaDati();
        return true;
    }

    public List<Snippet> ContenutoDi(string cartella) =>
        Snippets.Where(s => s.Categoria == cartella).ToList();

    /// <summary>Si porta via la cartella e tutto ciò che contiene.</summary>
    public int EliminaCartella(string cartella)
    {
        var dentro = ContenutoDi(cartella);

        foreach (var s in dentro) Snippets.Remove(s);

        Cartelle.Remove(cartella);
        SalvaDati();
        return dentro.Count;
    }

    // ----------------------------------------------------------- note

    /// <summary>
    /// L'ultima toccata, in alto. Si ordina per la stringa ISO-8601 tale
    /// e quale: quel formato è fatto perché l'ordine alfabetico e quello
    /// cronologico coincidano, e così non serve interpretare date —che è
    /// dove si infilano i problemi di regione— solo per ordinare. Una
    /// nota senza data, di un file scritto a mano, finisce in fondo
    /// invece di rompere qualcosa.
    /// </summary>
    void OrdinaNote() =>
        Note = Note.OrderByDescending(n => n.Modificata, StringComparer.Ordinal)
                     .ToList();

    public void SalvaNote()
    {
        OrdinaNote();
        Scrivi(Percorsi.Note, Note);
    }

    public void AggiungiNota(Nota nota)
    {
        Note.Add(nota);
        SalvaNote();
    }

    /// <summary>
    /// Cambia il testo di un'annotazione che esiste già e la porta in
    /// cima. Si cerca per riferimento e non per indice: la lista si
    /// riordina a ogni salvataggio, e un indice preso prima di riordinare
    /// punta a un'altra nota.
    /// </summary>
    public bool CambiaNota(
        Nota nota, IReadOnlyList<Frammento> runs, string quando,
        string? titolo = null)
    {
        if (!Note.Contains(nota)) return false;

        nota.Runs = Regole.PulisciRuns(runs);
        if (titolo is not null) nota.Titolo = titolo.Trim();
        nota.Modificata = quando;

        SalvaNote();
        return true;
    }

    /// <summary>
    /// Le annotazioni che corrispondono a ciò che è scritto, le più
    /// simili per prime. Si chiama solo dalla scheda delle note: nella
    /// ricerca generale non escono, perché lì tutto ciò che compare si
    /// può incollare e un'annotazione no.
    ///
    /// Il nome pesa più del corpo —lo fa DaiPunteggio—, ed è ciò che
    /// mette l'annotazione per prima quando la chiami per nome, anche se
    /// quelle stesse lettere compaiono dentro altre dieci.
    /// </summary>
    public List<Nota> CercaNote(string query)
    {
        var parole = Ricerca.Parole(query);
        if (parole.Length == 0) return Note;

        return Note
            .Select(n => (Nota: n, Punteggio: Ricerca.DaiPunteggio(
                parole,
                Ricerca.Normalizza(Regole.NomeDi(n)),
                Ricerca.Normalizza(Regole.TestoDi(n.Runs)))))
            .Where(x => x.Punteggio is not null)
            .OrderByDescending(x => x.Punteggio!.Value)
            .Select(x => x.Nota)
            .ToList();
    }

    public bool EliminaNota(Nota nota)
    {
        if (!Note.Remove(nota)) return false;

        SalvaNote();
        return true;
    }

    // -------------------------------------------------------- snippet

    public void AggiungiSnippet(Snippet snippet)
    {
        CreaCartella(snippet.Categoria);
        Snippets.Add(snippet);
        SalvaDati();
    }

    /// <summary>
    /// Lascia la cartella con esattamente questi salvati, in
    /// quest'ordine. Ciò che sta fuori dalla cartella non si tocca e
    /// conserva il suo posto.
    ///
    /// Con una sola scrittura: salvare 3000 note togliendone cento non
    /// può essere tremila eliminazioni e tremila inserimenti, ognuno
    /// serializzando l'intero file.
    /// </summary>
    public void SostituisciContenuto(string cartella, IReadOnlyList<Snippet> nuovi)
    {
        int dove = Snippets.FindIndex(s => s.Categoria == cartella);

        var fuori = Snippets.Where(s => s.Categoria != cartella).ToList();

        // La cartella torna dove cominciava: senza questo, modificarla la
        // mandava in fondo a snippets.json e l'ordine della scheda
        // cambiava solo per aver aperto l'editor.
        fuori.InsertRange(dove < 0 ? fuori.Count : Math.Min(dove, fuori.Count),
                          nuovi);

        Snippets = fuori;

        if (!Cartelle.Contains(cartella)) Cartelle.Add(cartella);

        SalvaDati();
    }

    public bool SostituisciSnippet(Snippet vecchio, Snippet nuovo)
    {
        int i = Snippets.IndexOf(vecchio);
        if (i < 0) return false;

        Snippets[i] = nuovo;
        CreaCartella(nuovo.Categoria);
        SalvaDati();
        return true;
    }

    // ----------------------------------------------------- cronologia

    /// <summary>
    /// Mette qualcosa di copiato all'inizio, sotto i fissati.
    /// </summary>
    public bool Annota(Voce voce)
    {
        // Guarda solo le prime quattro: ripetere qualcosa copiato venti
        // voci fa è legittimo, ripetere quello di un momento fa è quasi
        // sempre lo stesso Ctrl+C contato due volte.
        foreach (var x in Cron.Take(4))
        {
            if (voce.Tipo == Voce.Testo_ && x.Testo == voce.Testo)
                return false;
        }

        int fissati = Cron.Count(x => x.Pin);
        Cron.Insert(fissati, voce);

        Ritaglia();
        SalvaCron();
        return true;
    }

    /// <summary>
    /// Lascia solo le ultime MaxCron sciolte. Le fissate non contano.
    /// </summary>
    void Ritaglia()
    {
        var liberi = Cron.Where(x => !x.Pin).ToList();

        foreach (var vecchio in liberi.Skip(Config.MaxCron))
        {
            EliminaImmagine(vecchio);
            Cron.Remove(vecchio);
        }
    }

    void EliminaImmagine(Voce voce)
    {
        if (!voce.EUnImmagine || string.IsNullOrEmpty(voce.Percorso)) return;

        try
        {
            File.Delete(voce.Percorso);
        }
        catch (Exception e) when (e is IOException
                                    or UnauthorizedAccessException)
        {
            // Che resti un bmp orfano non giustifica perdere la voce.
            Anomalia?.Invoke($"impossibile eliminare {voce.Percorso}", e);
        }
    }

    public void Fissa(Voce voce)
    {
        voce.Pin = !voce.Pin;
        SalvaCron(true);
    }

    public bool Elimina(Elemento elemento)
    {
        switch (elemento)
        {
            case Voce voce:
                EliminaImmagine(voce);
                if (!Cron.Remove(voce)) return false;
                SalvaCron(true);
                return true;

            case Snippet snippet:
                if (!Snippets.Remove(snippet)) return false;
                SalvaDati();
                return true;

            // Anche da qui, e non solo da EliminaNota: il menu della riga
            // e l'eliminazione in blocco passano da questo metodo. Senza
            // questo caso, l'annotazione spariva dallo schermo
            // all'aggiornamento e ricompariva riaprendo il pannello,
            // perché il file non veniva mai toccato.
            case Nota nota:
                return EliminaNota(nota);

            default:
                return false;
        }
    }

    public int EliminaVari(IEnumerable<Elemento> elementi) =>
        elementi.Count(Elimina);

    /// <summary>I fissati sopravvivono: esistono per questo.</summary>
    public void SvuotaCronologia()
    {
        foreach (var x in Cron)
        {
            if (!x.Pin) EliminaImmagine(x);
        }

        Cron = Cron.Where(x => x.Pin).ToList();
        SalvaCron(true);
    }

    public List<Voce> CronOrdinata() =>
        [.. Cron.Where(x => x.Pin), .. Cron.Where(x => !x.Pin)];

    /// <summary>
    /// Salva una bitmap e la annota nella cronologia. Riceve i byte già
    /// convertiti: convertire immagini è compito dello strato grafico, e
    /// questo assembly non lo conosce.
    /// </summary>
    public bool SalvaImmagine(byte[] bmp)
    {
        try
        {
            Directory.CreateDirectory(Percorsi.Immagini);

            string percorso = Path.Combine(
                Percorsi.Immagini,
                $"img_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.bmp");

            File.WriteAllBytes(percorso, bmp);

            return Annota(new Voce { Tipo = Voce.Immagine, Percorso = percorso });
        }
        catch (Exception e) when (e is IOException
                                    or UnauthorizedAccessException)
        {
            Anomalia?.Invoke("impossibile salvare l'immagine", e);
            return false;
        }
    }
}
