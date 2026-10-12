using Pastepad.Nucleo;

namespace Pastepad.Nucleo.Prove;

/// <summary>
/// Le 19 prove della versione in Python, portate una per una. Sono la
/// specifica eseguibile delle regole: se passano, la logica è portata.
///
/// Girano senza aprire nessuna finestra e senza il Windows App SDK. È il
/// vantaggio di avere il nucleo separato dall'interfaccia, ed è ciò che
/// permise di migrare da tkinter a Flet riusando 906 righe.
/// </summary>
public abstract class BaseConCartellaTemporanea
{
    protected string Temporaneo { get; private set; } = "";
    protected Percorsi Percorsi { get; private set; } = null!;

    [TestInitialize]
    public void Prepara()
    {
        Temporaneo = Path.Combine(
            Path.GetTempPath(), "pastepad-prove-" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(Temporaneo);
        Percorsi = Percorsi.InCartella(Temporaneo);
    }

    [TestCleanup]
    public void Pulisci()
    {
        try
        {
            if (Directory.Exists(Temporaneo)) Directory.Delete(Temporaneo, true);
        }
        catch (IOException)
        {
            // Una cartella temporanea che sopravvive non invalida la prova.
        }
    }
}

[TestClass]
public sealed class ProvaRegole : BaseConCartellaTemporanea
{
    Archivio Nuovo() => new(Percorsi);

    [TestMethod]
    public void test_creare_ed_eliminare_cartella()
    {
        var a = Nuovo();

        a.CreaCartella("Lavoro");

        a.AggiungiSnippet(new Snippet
        {
            Titolo = "uno",
            Categoria = "Lavoro",
            Runs = [Regole.CreaFrammento("ciao")],
        });

        a.AggiungiSnippet(new Snippet
        {
            Titolo = "due",
            Categoria = "Lavoro",
            Runs = [Regole.CreaFrammento("addio")],
        });

        Assert.HasCount(2, a.ContenutoDi("Lavoro"));

        int eliminati = a.EliminaCartella("Lavoro");

        Assert.AreEqual(2, eliminati);
        CollectionAssert.DoesNotContain(a.Cartelle, "Lavoro");
        Assert.IsEmpty(a.Snippets);
    }

    [TestMethod]
    public void test_rinominare_trascina_il_contenuto()
    {
        var a = Nuovo();

        a.CreaCartella("Vecchia");

        a.AggiungiSnippet(new Snippet
        {
            Titolo = "x",
            Categoria = "Vecchia",
            Runs = [Regole.CreaFrammento("y")],
        });

        Assert.IsTrue(a.RinominaCartella("Vecchia", "Nuova"));
        Assert.AreEqual("Nuova", a.Snippets[0].Categoria);
    }

    [TestMethod]
    public void test_non_rinominare_a_uno_esistente()
    {
        var a = Nuovo();

        a.CreaCartella("A");
        a.CreaCartella("B");

        Assert.IsFalse(a.RinominaCartella("A", "B"));
    }

    [TestMethod]
    public void test_i_fissati_sopravvivono_al_ritaglio()
    {
        var a = Nuovo();

        var fisso = new Voce
        {
            Tipo = Voce.Testo_,
            Testo = "importante",
            Pin = true,
        };

        a.Cron.Add(fisso);

        for (int i = 0; i < Config.MaxCron + 20; i++)
        {
            a.Annota(new Voce
            {
                Tipo = Voce.Testo_,
                Testo = $"riempitivo {i}",
            });
        }

        CollectionAssert.Contains(a.Cron, fisso);

        int liberi = a.Cron.Count(x => !x.Pin);
        Assert.IsLessThanOrEqualTo(Config.MaxCron, liberi,
            $"sono rimaste {liberi} sciolte, il limite è {Config.MaxCron}");
    }

    [TestMethod]
    public void test_svuotare_rispetta_i_fissati()
    {
        var a = Nuovo();

        a.Cron =
        [
            new Voce { Tipo = Voce.Testo_, Testo = "a", Pin = true },
            new Voce { Tipo = Voce.Testo_, Testo = "b" },
        ];

        a.SvuotaCronologia();

        Assert.HasCount(1, a.Cron);
    }

    [TestMethod]
    public void test_non_ripete_l_appena_copiato()
    {
        var a = Nuovo();

        Assert.IsTrue(a.Annota(
            new Voce { Tipo = Voce.Testo_, Testo = "uguale" }));

        Assert.IsFalse(a.Annota(
            new Voce { Tipo = Voce.Testo_, Testo = "uguale" }));
    }

    [TestMethod]
    public void test_scrittura_atomica()
    {
        var a = Nuovo();
        a.CreaCartella("Persistente");

        var altro = Nuovo();

        CollectionAssert.Contains(altro.Cartelle, "Persistente");
    }
}

[TestClass]
public sealed class ProvaModelli
{
    [TestMethod]
    public void test_campi_in_ordine_senza_ripetere()
    {
        const string t = "Ciao [[nome]], su [[tema]] e di nuovo [[nome]].";

        CollectionAssert.AreEqual(
            new[] { "nome", "tema" }, Regole.CampiDi(t));
    }

    [TestMethod]
    public void test_riempire()
    {
        var f = new[] { Regole.CreaFrammento("Ciao [[nome]]") };

        var r = Regole.Riempi(f, new Dictionary<string, string>
        {
            ["nome"] = "Ana",
        });

        Assert.AreEqual("Ciao Ana", Regole.TestoDi(r));
    }

    /// <summary>
    /// Con la barra di formato, un salvato ha più frammenti e il taglio
    /// cade dove l'utente ha messo il grassetto. Un [[campo]] diviso in
    /// tre non lo vedeva la sostituzione di prima e si incollava con le
    /// parentesi quadre dentro.
    /// </summary>
    [TestMethod]
    public void test_riempire_un_campo_diviso_tra_frammenti()
    {
        var f = new[]
        {
            Regole.CreaFrammento("Ciao [["),
            Regole.CreaFrammento("nome", grassetto: 1),
            Regole.CreaFrammento("]], come va"),
        };

        var r = Regole.Riempi(f, new Dictionary<string, string> { ["nome"] = "Ana" });

        Assert.AreEqual("Ciao Ana, come va", Regole.TestoDi(r));
        Assert.DoesNotContain("[[", Regole.TestoDi(r));
    }

    /// <summary>
    /// E il resto del formato non si muove: ciò che era in grassetto
    /// resta in grassetto dopo aver riempito.
    /// </summary>
    [TestMethod]
    public void test_riempire_conserva_il_formato_intorno()
    {
        var f = new[]
        {
            Regole.CreaFrammento("Gentile "),
            Regole.CreaFrammento("[[cliente]]", grassetto: 1, colore: "#C00000"),
            Regole.CreaFrammento(": allego il rapporto di [[mese]]."),
        };

        var r = Regole.Riempi(f, new Dictionary<string, string>
        {
            ["cliente"] = "Ana Perez",
            ["mese"] = "luglio",
        });

        Assert.AreEqual(
            "Gentile Ana Perez: allego il rapporto di luglio.", Regole.TestoDi(r));

        var inGrassetto = r.Single(x => x.B == 1);
        Assert.AreEqual("Ana Perez", inGrassetto.T);
        Assert.AreEqual("#C00000", inGrassetto.C);
    }

    /// <summary>
    /// Riempi non può moltiplicare i frammenti: senza unire i pezzetti
    /// consecutivi con lo stesso formato, ogni incolla ne lasciava uno
    /// per pezzo.
    /// </summary>
    [TestMethod]
    public void test_riempire_non_moltiplica_i_frammenti()
    {
        var f = new[] { Regole.CreaFrammento("a [[x]] b [[x]] c") };

        var r = Regole.Riempi(f, new Dictionary<string, string> { ["x"] = "1" });

        Assert.AreEqual("a 1 b 1 c", Regole.TestoDi(r));
        Assert.HasCount(1, r);
    }

    [TestMethod]
    public void test_una_linea_taglia_bene()
    {
        string lungo = string.Concat(Enumerable.Repeat("parola ", 5000));

        Assert.IsLessThanOrEqualTo(43, Regole.UnaLinea(lungo, 40).Length);
    }
}

[TestClass]
public sealed class ProvaRicerca : BaseConCartellaTemporanea
{
    [TestMethod]
    public void test_ignora_gli_accenti()
    {
        Assert.AreEqual("citta", Ricerca.Normalizza("città"));
    }

    [TestMethod]
    public void test_parole_in_qualsiasi_ordine()
    {
        var p = Ricerca.DaiPunteggio(["rap", "men"], "rapporto mensile", "");

        Assert.IsNotNull(p);
    }

    [TestMethod]
    public void test_il_titolo_pesa_piu_del_corpo()
    {
        var nelTitolo = Ricerca.DaiPunteggio(["pagamento"], "pagamento in sospeso", "");
        var nelCorpo = Ricerca.DaiPunteggio(["pagamento"], "un'altra cosa", "c'è un pagamento qui");

        Assert.IsNotNull(nelTitolo);
        Assert.IsNotNull(nelCorpo);
        Assert.IsGreaterThan(nelCorpo.Value, nelTitolo.Value,
            $"titolo {nelTitolo} non supera il corpo {nelCorpo}");
    }

    [TestMethod]
    public void test_non_coincide_restituisce_none()
    {
        Assert.IsNull(Ricerca.DaiPunteggio(["zzz"], "ciao", "mondo"));
    }

    [TestMethod]
    public void test_l_indice_si_rifa_invalidando()
    {
        var a = new Archivio(Percorsi);
        var idx = new Indice(a);

        Assert.AreEqual(0, idx.Quante());

        a.AggiungiSnippet(new Snippet
        {
            Titolo = "Rapporto mensile",
            Categoria = "W",
            Runs = [Regole.CreaFrammento("cifre")],
        });

        idx.Invalida();

        Assert.HasCount(1, idx.Cerca("rap men"));
        Assert.IsEmpty(idx.Cerca("inesistente"));
    }
}

/// <summary>
/// Queste non vengono dalla versione in Python. Custodiscono la
/// promessa che nel JSON non finiscano chiavi in più: una volta fu
/// scritta una proprietà calcolata —false in ogni voce— che si era
/// infilata nel file, e il controllo di andata e ritorno non la vide,
/// perché confrontava i campi già interpretati e non le chiavi crude.
/// </summary>
[TestClass]
public sealed class ProvaFormatoDelFile : BaseConCartellaTemporanea
{
    static HashSet<string> ChiaviDi(System.Text.Json.JsonElement e) =>
        [.. e.EnumerateObject().Select(p => p.Name)];

    [TestMethod]
    public void la_cronologia_porta_solo_le_chiavi_di_sempre()
    {
        var a = new Archivio(Percorsi);

        a.Annota(new Voce { Tipo = Voce.Testo_, Testo = "sciolta" });
        a.Annota(new Voce { Tipo = Voce.Testo_, Testo = "fissata" });
        a.Fissa(a.Cron[0]);
        a.Riversa(forza: true);

        using var doc = System.Text.Json.JsonDocument.Parse(
            File.ReadAllText(Percorsi.Cronologia));

        var voci = doc.RootElement.EnumerateArray().ToList();
        Assert.HasCount(2, voci);

        var fissata = voci.First(e => ChiaviDi(e).Contains("pin"));
        var sciolta = voci.First(e => !ChiaviDi(e).Contains("pin"));

        CollectionAssert.AreEquivalent(
            new[] { "tipo", "testo", "pin" }, ChiaviDi(fissata).ToArray());

        // Senza pin quando è false, come faceva il JSON di Python,
        // dove la chiave semplicemente non c'era.
        CollectionAssert.AreEquivalent(
            new[] { "tipo", "testo" }, ChiaviDi(sciolta).ToArray());
    }

    [TestMethod]
    public void un_immagine_porta_percorso_e_non_testo()
    {
        var a = new Archivio(Percorsi);

        a.Annota(new Voce { Tipo = Voce.Immagine, Percorso = @"C:\x\img.bmp" });
        a.Riversa(forza: true);

        using var doc = System.Text.Json.JsonDocument.Parse(
            File.ReadAllText(Percorsi.Cronologia));

        CollectionAssert.AreEquivalent(
            new[] { "tipo", "percorso" },
            ChiaviDi(doc.RootElement.EnumerateArray().First()).ToArray());
    }

    [TestMethod]
    public void uno_snippet_porta_titolo_categoria_e_runs()
    {
        var a = new Archivio(Percorsi);

        a.AggiungiSnippet(new Snippet
        {
            Titolo = "Modello",
            Categoria = "Lavoro",
            Runs = [Regole.CreaFrammento("Ciao")],
        });

        using var doc = System.Text.Json.JsonDocument.Parse(
            File.ReadAllText(Percorsi.Dati));

        CollectionAssert.AreEquivalent(
            new[] { "categorie", "snippets" },
            ChiaviDi(doc.RootElement).ToArray());

        var snippet = doc.RootElement.GetProperty("snippets")
                                     .EnumerateArray().First();

        CollectionAssert.AreEquivalent(
            new[] { "titolo", "categoria", "runs" }, ChiaviDi(snippet).ToArray());

        // Le chiavi di una lettera sono ciò che fa sì che il file non
        // ingrassi, e quelle che scriveva già la versione in Python.
        CollectionAssert.AreEquivalent(
            new[] { "t", "f", "s", "b", "i", "u", "c" },
            ChiaviDi(snippet.GetProperty("runs").EnumerateArray().First()).ToArray());
    }
}

/// <summary>
/// Un file che esiste e non si può leggere NON si sovrascrive.
///
/// Viene da un errore reale: un'istanza partì senza poter accedere
/// alla sua cartella, mostrò una cronologia vuota e chiudendosi salvò
/// quel vuoto sopra le voci vere. Senza un solo messaggio, perché il
/// registro degli errori viveva nella stessa cartella rotta.
/// </summary>
[TestClass]
public sealed class ProvaNonSovrascrivereLIlleggibile : BaseConCartellaTemporanea
{
    const string Corrotto = "{ questo non è JSON valido";

    [TestMethod]
    public void una_cronologia_illeggibile_non_si_sovrascrive()
    {
        File.WriteAllText(Percorsi.Cronologia, Corrotto);

        var a = new Archivio(Percorsi);

        Assert.IsTrue(a.LetturaIncompleta);
        Assert.IsNotNull(a.Problema);

        a.Annota(new Voce { Tipo = Voce.Testo_, Testo = "nuova" });
        a.Riversa(forza: true);

        Assert.AreEqual(Corrotto, File.ReadAllText(Percorsi.Cronologia),
            "è stato sovrascritto un file che non si poteva leggere");
    }

    [TestMethod]
    public void degli_snippet_illeggibili_non_si_sovrascrivono()
    {
        File.WriteAllText(Percorsi.Dati, Corrotto);

        var a = new Archivio(Percorsi);
        a.CreaCartella("Lavoro");

        Assert.AreEqual(Corrotto, File.ReadAllText(Percorsi.Dati));
    }

    [TestMethod]
    public void il_file_sano_si_scrive_anche_se_un_altro_e_rotto()
    {
        File.WriteAllText(Percorsi.Cronologia, Corrotto);

        var a = new Archivio(Percorsi);
        a.CreaCartella("Lavoro");

        // Si protegge solo ciò che non si è potuto leggere. Bloccare
        // tutto trasformerebbe un file rotto in un programma inutile.
        CollectionAssert.Contains(new Archivio(Percorsi).Cartelle, "Lavoro");
    }

    [TestMethod]
    public void senza_file_non_ce_problema_da_segnalare()
    {
        var a = new Archivio(Percorsi);

        // Primo avvio: che non esistano è normale, non un errore.
        Assert.IsFalse(a.LetturaIncompleta);
        Assert.IsNull(a.Problema);
    }

    /// <summary>
    /// Il buco che lasciava File.Exists: il riferimento di .NET avverte
    /// che restituisce false quando non c'è il permesso di lettura,
    /// invece di lanciare. Con la domanda davanti, un file negato
    /// passava per "primo avvio" e non veniva segnato: il programma
    /// caricava vuoto e da lì scriveva sopra quello buono, in silenzio.
    ///
    /// Qui il diniego si costruisce con una cartella che si chiama come
    /// il file. Non è un giro capriccioso: misurato, File.Exists
    /// restituisce **false** su di essa e aprirla dà
    /// UnauthorizedAccessException con HResult 0x80070005, esattamente
    /// come un vero permesso negato. Ed esce uguale su qualsiasi
    /// macchina, senza toccare ACL né chiedere elevazione.
    /// </summary>
    [TestMethod]
    public void una_cronologia_che_esiste_e_non_si_lascia_leggere_si_segna()
    {
        var anomalie = new List<string>();

        Directory.CreateDirectory(Percorsi.Cronologia);

        var a = new Archivio(Percorsi, (dove, _) => anomalie.Add(dove));

        Assert.IsTrue(a.LetturaIncompleta,
            "File.Exists ha mentito e lo si è preso per un primo avvio");

        Assert.IsNotNull(a.Problema);
        Assert.IsNotEmpty(anomalie, "e per di più senza lasciare una riga");

        // E con il segno impostato, non ci si scrive sopra.
        anomalie.Clear();

        a.Annota(new Voce { Tipo = Voce.Testo_, Testo = "questa sessione" });
        a.Riversa(forza: true);

        Assert.IsTrue(
            anomalie.Any(x => x.Contains("non si scrive")),
            "si è scritto sopra ciò che non si poteva leggere");

        Assert.IsFalse(File.Exists(Percorsi.Cronologia),
            "è stata creata una cronologia nuova sopra quella che c'era");
    }

    /// <summary>
    /// L'altra strada: il file è aperto da un altro e non si può
    /// condividere. Copre il nuovo tentativo —tre passate da 100 ms, nel
    /// caso sia l'istanza precedente che finisce di morire— e il fatto
    /// che, se nemmeno così entra, le cose dell'utente restano com'erano.
    /// </summary>
    [TestMethod]
    public void una_cronologia_che_non_si_puo_aprire_non_si_sovrascrive()
    {
        const string buono = """[{"tipo":"testo","testo":"quello dell'utente"}]""";

        File.WriteAllText(Percorsi.Cronologia, buono);

        var anomalie = new List<string>();

        using (new FileStream(Percorsi.Cronologia, FileMode.Open,
                              FileAccess.Read, FileShare.None))
        {
            var a = new Archivio(Percorsi, (dove, _) => anomalie.Add(dove));

            Assert.IsTrue(a.LetturaIncompleta,
                "un file bloccato va segnato come illeggibile");

            Assert.IsNotNull(a.Problema);
            Assert.IsEmpty(a.Cron);

            a.Annota(new Voce { Tipo = Voce.Testo_, Testo = "questa sessione" });
            a.Riversa(forza: true);
        }

        Assert.AreEqual(buono, File.ReadAllText(Percorsi.Cronologia),
            "la cronologia dell'utente è stata sovrascritta con quella vuota di questa sessione");

        // E non in silenzio: il nuovo tentativo e la rinuncia lasciano una riga.
        Assert.IsNotEmpty(anomalie);
    }

    /// <summary>
    /// L'altro lato della stessa cosa: che non esista resta primo avvio
    /// e si scrive con tutta normalità. Se la correzione qui sopra
    /// esagerasse in prudenza, il programma non salverebbe mai niente.
    /// </summary>
    [TestMethod]
    public void un_file_che_non_esiste_resta_primo_avvio()
    {
        var anomalie = new List<string>();

        var a = new Archivio(Percorsi, (dove, _) => anomalie.Add(dove));

        Assert.IsFalse(a.LetturaIncompleta);
        Assert.IsEmpty(anomalie, "un primo avvio non è un'anomalia");

        a.Annota(new Voce { Tipo = Voce.Testo_, Testo = "la prima" });
        a.Riversa(forza: true);

        CollectionAssert.Contains(
            new Archivio(Percorsi).Cron.Select(x => x.Testo).ToList(), "la prima");
    }
}

[TestClass]
public sealed class ProvaTitoloSalvato : BaseConCartellaTemporanea
{
    const string Lungo =
        "Ciao [[nome]], il tuo appuntamento è il [[giorno]] alle [[ora]]. "
        + "Grazie per la fiducia e a presto.";

    /// <summary>
    /// Ciò che si salva deve essere contenuto in ciò che l'utente ha
    /// scritto. Si salvava il riassunto di schermo, e i puntini di
    /// sospensione finivano dentro snippets.json: "Ciao [[nome]], il tuo
    /// appuntamento è il [[giorno]] alle [[ora]]. Graz...". Un titolo
    /// che l'utente non ha mai scritto, nel suo file.
    /// </summary>
    [TestMethod]
    public void test_il_titolo_salvato_non_ha_puntini_di_sospensione()
    {
        var s = Regole.CreaSnippet(Lungo, "Lavoro");

        Assert.DoesNotContain("...", s.Titolo);

        Assert.Contains(s.Titolo, Lungo,
            "il titolo salvato non è un pezzo letterale del testo");
    }

    /// <summary>E sopravvive intatto al viaggio sul disco.</summary>
    [TestMethod]
    public void test_il_titolo_sopravvive_a_salvataggio_e_rilettura()
    {
        var a = new Archivio(Percorsi);
        a.AggiungiSnippet(Regole.CreaSnippet(Lungo, "Lavoro"));

        var letto = new Archivio(Percorsi).Snippets.Single();

        Assert.AreEqual(Lungo, letto.Titolo);
        Assert.AreEqual(Lungo, Regole.TestoDi(letto.Runs));

        Assert.DoesNotContain("...", File.ReadAllText(Percorsi.Dati));
    }

    /// <summary>
    /// Solo la prima linea, e senza a capo dentro: un titolo di più
    /// linee rompe la riga.
    /// </summary>
    [TestMethod]
    public void test_solo_la_prima_linea_con_qualcosa_di_scritto()
    {
        Assert.AreEqual("la prima",
            Regole.PrimaLinea("\n  \n  la   prima  \nla seconda\n"));

        Assert.AreEqual("", Regole.PrimaLinea(""));
        Assert.AreEqual("", Regole.PrimaLinea("   \n\t\n"));
    }

    /// <summary>
    /// Il TextBox di WinUI separa le linee con \r e basta. Dividendo
    /// solo per \n, il titolo di un testo di tre linee salvato dal
    /// dialogo usciva "Prima linea Seconda linea Terza linea": le tre
    /// attaccate. Verificato leggendo lo snippets.json scritto dal
    /// programma.
    /// </summary>
    [TestMethod]
    public void test_il_titolo_con_acapo_di_windows_e_di_textbox()
    {
        Assert.AreEqual("Prima linea",
            Regole.PrimaLinea("Prima linea\rSeconda linea\rTerza linea"));

        Assert.AreEqual("Prima linea",
            Regole.PrimaLinea("Prima linea\r\nSeconda linea"));

        Assert.AreEqual("Prima linea",
            Regole.CreaSnippet("Prima linea\rSeconda linea", "Lavoro").Titolo);
    }

    /// <summary>
    /// Dalla 4.3.0 il nome si chiede per qualsiasi salvato e non solo
    /// per i link, quindi questa ripartizione passa dal valere per i
    /// segnalibri al valere per tutto ciò che si salva.
    ///
    /// Lasciarlo in bianco deve continuare a dare la prima linea: è ciò
    /// che fa sì che chi salva due frasi non debba riempire un campo in
    /// più.
    /// </summary>
    [TestMethod]
    public void test_il_nome_in_bianco_lascia_la_prima_linea()
    {
        const string corpo = "Hi team,\rI am practicing Git.";

        foreach (string? vuoto in new[] { null, "", "   ", "\r\n" })
        {
            Assert.AreEqual("Hi team,",
                Regole.CreaSnippet(corpo, "Corpi", vuoto).Titolo,
                $"con il nome in '{vuoto ?? "null"}'");
        }

        // E con un nome vince il nome, che è il caso chiesto
        // dall'utente: cinque corpi che iniziano uguale e che vanno
        // distinti nella riga.
        Assert.AreEqual("Esercizi di Git",
            Regole.CreaSnippet(corpo, "Corpi", "  Esercizi di Git  ").Titolo);
    }

    /// <summary>
    /// E le linee si contano uguale comunque arrivino: il dialogo per
    /// aggiungere un elenco contava "1 nota" con sessanta linee
    /// incollate.
    /// </summary>
    [TestMethod]
    public void test_le_linee_si_dividono_comunque_arrivi_l_acapo()
    {
        Assert.HasCount(3, Regole.LineeDi("una\rdue\rtre"));
        Assert.HasCount(3, Regole.LineeDi("una\r\ndue\r\ntre"));
        Assert.HasCount(3, Regole.LineeDi("una\ndue\ntre"));

        // Quelle vuote sono separazione, non contenuto.
        Assert.HasCount(2, Regole.LineeDi("una\r\n\r\n  \r\ndue"));
        Assert.IsEmpty(Regole.LineeDi(""));
    }

    /// <summary>
    /// Aprire un salvato nell'editor e salvarlo **senza toccare niente**
    /// deve lasciarlo uguale. Questa è la prova dell'errore che si
    /// infilò nella 4.0.1: una nota di cento linee restava alla prima
    /// —da 7990 caratteri a 77— e il titolo proprio si perdeva. Non si
    /// vede guardando lo schermo, bisogna guardare il file.
    ///
    /// Riproduce l'intero viaggio: leggere dal disco, estrarre il testo
    /// come lo estrae il dialogo, ricomporlo come lo ricompone salvando,
    /// e rileggere.
    /// </summary>
    [TestMethod]
    public void test_aprire_e_salvare_senza_modifiche_non_tocca_il_salvato()
    {
        string lungo = string.Join(
            "\r\n",
            Enumerable.Range(1, 100).Select(n => $"Linea {n} della nota lunga"));

        var a = new Archivio(Percorsi);

        a.AggiungiSnippet(new Snippet
        {
            Titolo = "TITOLO DIVERSO DAL TESTO",
            Categoria = "Lavoro",
            Runs = [Regole.CreaFrammento(lungo)],
        });

        var prima = new Archivio(Percorsi).Snippets.Single();

        // Ciò che il dialogo carica nella casella e restituisce salvando.
        string caricato = Regole.TestoDi(prima.Runs);

        Assert.AreEqual(lungo.Length, caricato.Length,
            "l'editor non carica il testo intero");

        var altro = new Archivio(Percorsi);
        var vecchio = altro.Snippets.Single();

        altro.SostituisciSnippet(
            vecchio, Regole.CreaSnippet(caricato, vecchio.Categoria, vecchio.Titolo));

        var dopo = new Archivio(Percorsi).Snippets.Single();

        Assert.AreEqual(lungo, Regole.TestoDi(dopo.Runs),
            "salvare senza modifiche ha cambiato il testo");

        Assert.AreEqual("TITOLO DIVERSO DAL TESTO", dopo.Titolo,
            "salvare senza modifiche si è portato via il titolo");
    }

    /// <summary>
    /// Un segnalibro può avere un nome proprio. Senza, il titolo è
    /// l'indirizzo, e cercare per titolo in una lista di indirizzi non
    /// serve a niente.
    /// </summary>
    [TestMethod]
    public void test_il_segnalibro_puo_avere_un_nome_proprio()
    {
        const string url = "https://esempio.com/molto/lunga/e/brutta?x=1";

        var conNome = Regole.CreaSnippet(url, "Lavoro", "Pannello della SIE");
        Assert.AreEqual("Pannello della SIE", conNome.Titolo);
        Assert.AreEqual(url, Regole.TestoDi(conNome.Runs));

        // Senza nome, o con uno in bianco, continua a valere la prima
        // linea: un salvato non può restare senza titolo.
        Assert.AreEqual(url, Regole.CreaSnippet(url, "Lavoro").Titolo);
        Assert.AreEqual(url, Regole.CreaSnippet(url, "Lavoro", "   ").Titolo);
    }

    /// <summary>
    /// E un testo di più linee non è un link anche se arriva diviso con
    /// \r e basta, che è come lo restituisce il TextBox.
    /// </summary>
    [TestMethod]
    public void test_un_testo_di_piu_linee_non_e_un_link()
    {
        Assert.IsTrue(Regole.EUnLink("https://esempio.com"));
        Assert.IsFalse(Regole.EUnLink("https://esempio.com\rseconda linea"));
        Assert.IsFalse(Regole.EUnLink("https://esempio.com\r\nseconda linea"));
    }

    /// <summary>
    /// Ciò che si salva porta gli a capo di Windows. Con \r e basta, un
    /// testo di tre linee incollato nel Blocco note usciva su una.
    /// </summary>
    [TestMethod]
    public void test_il_salvato_ha_gli_acapo_di_windows()
    {
        var s = Regole.CreaSnippet("una\rdue\r\ntre\nquattro", "Lavoro");

        Assert.AreEqual("una\r\ndue\r\ntre\r\nquattro", Regole.TestoDi(s.Runs));

        // E non li raddoppia passando due volte.
        Assert.AreEqual("una\r\ndue",
            Regole.NormalizzaAcapi(Regole.NormalizzaAcapi("una\r\ndue")));
    }

    /// <summary>
    /// E il riassunto di schermo continua a segnare il taglio: togliere
    /// i puntini di sospensione dal salvato non può toglierli dalla riga.
    /// </summary>
    [TestMethod]
    public void test_il_riassunto_a_schermo_segna_il_taglio()
    {
        Assert.EndsWith("...", Regole.UnaLinea(Lungo, 40));
    }
}

/// <summary>
/// L'editor dell'intera cartella: una casella con una nota per linea.
///
/// Ciò che è in gioco qui è lo stesso di sempre —che salvare non
/// distrugga ciò che l'utente non ha chiesto di distruggere—, e con
/// il volume che l'utente ha dato come esempio: 3000 note di cui se
/// ne eliminano cento.
/// </summary>
[TestClass]
public sealed class ProvaModificaCartella : BaseConCartellaTemporanea
{
    static Snippet Nota(
        string testo, string? titolo = null, string cartella = "Lavoro") =>
        Regole.CreaSnippet(testo, cartella, titolo);

    [TestMethod]
    public void test_le_linee_intatte_conservano_la_stessa_nota()
    {
        var uno = Nota("https://esempio.com/x", "Pannello della SIE");
        var due = Nota("seconda");

        // Una con formato messo a mano, che è ciò che si perderebbe se la
        // fusione rifacesse le note invece di riusarle.
        var tre = new Snippet
        {
            Titolo = "terza",
            Categoria = "Lavoro",
            Runs = [Regole.CreaFrammento("terza", "Arial", 20, grassetto: 1)],
        };

        var prima = Regole.DividiCartella([uno, due, tre]);

        var f = Regole.FondiCartella(
            prima, "https://esempio.com/x\rseconda\rterza", "Lavoro");

        Assert.AreEqual(3, f.Conservate);
        Assert.AreEqual(0, f.Nuove);
        Assert.IsEmpty(f.Tolte);

        // La stessa nota, non una copia uguale.
        Assert.AreSame(uno, f.Risultato[0]);
        Assert.AreSame(tre, f.Risultato[2]);

        Assert.AreEqual("Pannello della SIE", f.Risultato[0].Titolo,
            "il nome proprio del segnalibro è stato rifatto");

        Assert.AreEqual("Arial", f.Risultato[2].Runs[0].F);
        Assert.AreEqual(20, f.Risultato[2].Runs[0].S);
        Assert.AreEqual(1, f.Risultato[2].Runs[0].B);
    }

    /// <summary>
    /// L'esempio dell'utente, con il suo volume: 3000 note linea per
    /// linea e vuole toglierne cento. Le 2900 che restano non si toccano.
    /// </summary>
    [TestMethod]
    public void test_togliere_cento_da_tremila_non_tocca_le_altre()
    {
        var cartella = Enumerable.Range(1, 3000)
            .Select(n => Nota($"nota numero {n}"))
            .ToList();

        var prima = Regole.DividiCartella(cartella);

        string modificato = string.Join("\r",
            Enumerable.Range(101, 2900).Select(n => $"nota numero {n}"));

        var f = Regole.FondiCartella(prima, modificato, "Lavoro");

        Assert.HasCount(2900, f.Risultato);
        Assert.HasCount(100, f.Tolte);
        Assert.AreEqual(2900, f.Conservate);
        Assert.AreEqual(0, f.Nuove, "è stata rifatta qualche nota che non era cambiata");

        Assert.AreSame(cartella[100], f.Risultato[0]);
        Assert.AreSame(cartella[2999], f.Risultato[2899]);

        // E quelle che vanno via sono le prime cento, non altre cento.
        CollectionAssert.AreEqual(
            cartella.Take(100).ToList(), f.Tolte);
    }

    /// <summary>
    /// L'intero viaggio sul disco, che è dove si videro i due errori che
    /// distruggevano testo in silenzio: si salva, si rilegge, e si conta
    /// ciò che c'è in snippets.json.
    /// </summary>
    [TestMethod]
    public void test_salvare_la_cartella_modificata_sopravvive_al_disco()
    {
        var a = new Archivio(Percorsi);

        foreach (var n in Enumerable.Range(1, 3000))
            a.Snippets.Add(Nota($"nota numero {n}"));

        a.CreaCartella("Lavoro");
        a.AggiungiSnippet(Nota("di un'altra cartella", cartella: "Altra"));

        var altro = new Archivio(Percorsi);
        var prima = Regole.DividiCartella(altro.ContenutoDi("Lavoro"));

        Assert.HasCount(3000, prima.DiUnaLinea);

        string modificato = string.Join("\r",
            Enumerable.Range(101, 2900).Select(n => $"nota numero {n}"));

        var f = Regole.FondiCartella(prima, modificato, "Lavoro");
        altro.SostituisciContenuto("Lavoro", f.Risultato);

        var letto = new Archivio(Percorsi);

        Assert.HasCount(2900, letto.ContenutoDi("Lavoro"));
        Assert.HasCount(1, letto.ContenutoDi("Altra"),
            "modificare una cartella ne ha travolta un'altra");

        Assert.AreEqual("nota numero 101",
            Regole.TestoDi(letto.ContenutoDi("Lavoro")[0].Runs));

        Assert.AreEqual("nota numero 3000",
            Regole.TestoDi(letto.ContenutoDi("Lavoro")[2899].Runs));
    }

    /// <summary>
    /// Una nota di più linee non sta in "una nota per linea": non si
    /// mostra divisa e non si perde salvando.
    /// </summary>
    [TestMethod]
    public void test_una_nota_di_piu_linee_resta_com_era()
    {
        var lunga = Nota(string.Join("\r\n",
            Enumerable.Range(1, 60).Select(n => $"linea {n}")));

        var prima = Regole.DividiCartella([Nota("corta"), lunga]);

        Assert.HasCount(1, prima.DiUnaLinea);
        Assert.HasCount(1, prima.DiPiuLinee);
        Assert.AreEqual("corta", prima.Testo,
            "la nota lunga è finita nella casella divisa in 60 linee");

        // L'utente elimina "corta" e salva con la casella vuota.
        var f = Regole.FondiCartella(prima, "", "Lavoro");

        Assert.HasCount(1, f.Tolte);
        Assert.HasCount(1, f.Risultato);
        Assert.AreSame(lunga, f.Risultato[0]);

        Assert.HasCount(60, Regole.LineeDi(Regole.TestoDi(f.Risultato[0].Runs)),
            "la nota di 60 linee è uscita divisa o tagliata");
    }

    /// <summary>
    /// Due note con lo stesso testo sono due note. Abbinate una a una,
    /// lasciare una sola linea ripetuta ne elimina una e conserva
    /// l'altra.
    /// </summary>
    [TestMethod]
    public void test_due_note_uguali_si_abbinano_una_a_una()
    {
        var uno = Nota("ripetuta");
        var due = Nota("ripetuta");

        var prima = Regole.DividiCartella([uno, due]);

        var dueVolte = Regole.FondiCartella(prima, "ripetuta\rripetuta", "Lavoro");
        Assert.AreEqual(2, dueVolte.Conservate);
        Assert.IsEmpty(dueVolte.Tolte);

        var unaVolta = Regole.FondiCartella(prima, "ripetuta", "Lavoro");
        Assert.HasCount(1, unaVolta.Risultato);
        Assert.HasCount(1, unaVolta.Tolte);
        Assert.AreSame(uno, unaVolta.Risultato[0]);
    }

    /// <summary>
    /// E ciò che è cambiato si salva come nota nuova, con il titolo
    /// preso dalla prima linea, come qualsiasi altro salvato.
    /// </summary>
    [TestMethod]
    public void test_una_linea_cambiata_e_una_nota_nuova()
    {
        var prima = Regole.DividiCartella([Nota("com'era")]);

        var f = Regole.FondiCartella(prima, "com'è rimasta", "Lavoro");

        Assert.AreEqual(0, f.Conservate);
        Assert.AreEqual(1, f.Nuove);
        Assert.HasCount(1, f.Tolte);

        Assert.AreEqual("com'è rimasta", f.Risultato[0].Titolo);
        Assert.AreEqual("Lavoro", f.Risultato[0].Categoria);
    }

    /// <summary>
    /// Sostituire il contenuto non può mandare la cartella in fondo al
    /// file: la scheda si ordina secondo come stanno in snippets.json.
    /// </summary>
    [TestMethod]
    public void test_la_cartella_modificata_non_va_in_fondo()
    {
        var a = new Archivio(Percorsi);

        a.AggiungiSnippet(Nota("prima di lavoro"));
        a.AggiungiSnippet(Nota("di un'altra", cartella: "Altra"));

        a.SostituisciContenuto("Lavoro", [Nota("resta la prima")]);

        Assert.AreEqual("Lavoro", new Archivio(Percorsi).Snippets[0].Categoria);
    }
}

/// <summary>
/// La barra di formato. Ciò che si salva deve restare testo semplice
/// corretto e continuare ad avere i suoi [[campi]].
/// </summary>
[TestClass]
public sealed class ProvaFormato
{
    [TestMethod]
    public void test_il_testo_semplice_non_dipende_dal_formato()
    {
        var runs = new[]
        {
            Regole.CreaFrammento("Gentile ", "Calibri", 11),
            Regole.CreaFrammento("Ana", "Arial", 14, grassetto: 1, colore: "#C00000"),
            Regole.CreaFrammento(":\r\nallego quanto richiesto.", "Calibri", 11),
        };

        var s = Regole.CreaSnippet(runs, "Email");

        Assert.AreEqual("Gentile Ana:\r\nallego quanto richiesto.",
            Regole.TestoDi(s.Runs));

        // Il titolo esce dal testo intero, non dal primo frammento.
        Assert.AreEqual("Gentile Ana:", s.Titolo);

        // E i campi si vedono ancora anche se sono distribuiti.
        CollectionAssert.AreEqual(
            new[] { "cliente" },
            Regole.CampiDi(Regole.TestoDi(new[]
            {
                Regole.CreaFrammento("Ciao [["),
                Regole.CreaFrammento("cliente]]"),
            })));
    }

    [TestMethod]
    public void test_i_frammenti_consecutivi_con_lo_stesso_formato_si_uniscono()
    {
        // Ciò che restituisce l'editor scrivendo lettera per lettera.
        var runs = "ciao".Select(c => Regole.CreaFrammento(c.ToString())).ToList();

        var s = Regole.CreaSnippet(runs, "Email");

        Assert.HasCount(1, s.Runs);
        Assert.AreEqual("ciao", s.Runs[0].T);
    }

    [TestMethod]
    public void test_gli_acapo_dell_editor_si_normalizzano_salvando()
    {
        var s = Regole.CreaSnippet(
            [Regole.CreaFrammento("una\rdue"), Regole.CreaFrammento("\rtre", grassetto: 1)],
            "Email");

        Assert.AreEqual("una\r\ndue\r\ntre", Regole.TestoDi(s.Runs));
    }

    [TestMethod]
    public void test_i_punti_elenco_vanno_e_vengono()
    {
        const string blocco = "uno\rdue\rtre";

        string con = Regole.AlternaPuntiElenco(blocco);
        Assert.AreEqual("• uno\r• due\r• tre", con);

        // E lo stesso pulsante li toglie.
        Assert.AreEqual(blocco, Regole.AlternaPuntiElenco(con));
    }

    [TestMethod]
    public void test_i_numeri_vanno_e_vengono_e_sostituiscono_il_punto_elenco()
    {
        string num = Regole.AlternaNumeri("uno\rdue\rtre");
        Assert.AreEqual("1. uno\r2. due\r3. tre", num);

        Assert.AreEqual("uno\rdue\rtre", Regole.AlternaNumeri(num));

        // Una linea già numerata non si numera due volte passando a punto elenco.
        Assert.AreEqual("• uno\r• due\r• tre", Regole.AlternaPuntiElenco(num));
    }

    [TestMethod]
    public void test_le_linee_vuote_restano_come_sono()
    {
        Assert.AreEqual("• uno\r\r• due", Regole.AlternaPuntiElenco("uno\r\rdue"));
        Assert.AreEqual("1. uno\r\r2. due", Regole.AlternaNumeri("uno\r\rdue"));
    }

    [TestMethod]
    public void test_il_rientro_si_mette_e_si_toglie()
    {
        string piu = Regole.Rientra("uno\rdue", true);
        Assert.AreEqual("\tuno\r\tdue", piu);

        Assert.AreEqual("uno\rdue", Regole.Rientra(piu, false));

        // Senza rientro, toglierlo non intacca il testo.
        Assert.AreEqual("uno\rdue", Regole.Rientra("uno\rdue", false));
    }
}

[TestClass]
public sealed class ProvaLink
{
    [TestMethod]
    public void test_riconosce_indirizzi()
    {
        foreach (var t in new[]
        {
            "https://github.com/x",
            "http://localhost:8000",
            "www.google.com",
            "  https://x.com  ",
        })
        {
            Assert.IsTrue(Regole.EUnLink(t), t);
        }
    }

    [TestMethod]
    public void test_ignora_testo_con_link_dentro()
    {
        // Un paragrafo che menziona una url non deve aprirsi facendo clic.
        Assert.IsFalse(Regole.EUnLink("guarda qui https://x.com"));
        Assert.IsFalse(Regole.EUnLink("testo normale"));
        Assert.IsFalse(Regole.EUnLink(""));
    }

    [TestMethod]
    public void test_completa_lo_schema()
    {
        Assert.AreEqual("https://www.google.com", Regole.UrlDi("www.google.com"));
        Assert.AreEqual("https://x.com", Regole.UrlDi("https://x.com"));
    }

    [TestMethod]
    public void test_dominio_pulito()
    {
        Assert.AreEqual("github.com",
            Regole.DominioDi("https://www.github.com/a/b"));

        Assert.AreEqual("localhost:8000",
            Regole.DominioDi("http://localhost:8000/x"));
    }
}

/// <summary>
/// La sessione congelata: quando ciò che si legge non è dell'utente,
/// non si scrive NIENTE.
///
/// Non è teoria. Il 14 agosto 2026 pastepad si aprì dentro il
/// contenitore di un'altra applicazione impacchettata, Windows gli
/// reindirizzò i file, e l'utente trovò il programma vuoto. I suoi
/// dati erano intatti sul disco; a fallire era il posto. Se quella
/// sessione avesse scritto, se li sarebbe portati via davvero.
/// </summary>
[TestClass]
public sealed class ProvaCongelato : BaseConCartellaTemporanea
{
    [TestMethod]
    public void test_congelato_non_scrive_nemmeno_una_preferenza()
    {
        var a = new Archivio(Percorsi);
        a.MettiPref("scorciatoia", "ctrl+q");

        // E adesso si congela, con il file già su disco.
        a.Congela("di prova");
        a.MettiPref("scorciatoia", "ctrl+j");

        // Ciò che c'era prima resta; ciò che viene dopo non è arrivato.
        Assert.AreEqual("ctrl+q", new Archivio(Percorsi).Pref<string>("scorciatoia"));
    }

    [TestMethod]
    public void test_congelato_non_scrive_cio_che_si_salva_all_istante()
    {
        // Fissare, eliminare e svuotare si scrivono subito e non con il
        // riversamento rimandato. Se la garanzia vivesse in Riversa,
        // questo sfuggirebbe — ed è proprio ciò che l'utente fa apposta.
        var a = new Archivio(Percorsi);
        a.AggiungiSnippet(Regole.CreaSnippet("uno", "I miei testi"));

        a.Congela("di prova");
        a.AggiungiSnippet(Regole.CreaSnippet("due", "I miei testi"));

        Assert.HasCount(1, new Archivio(Percorsi).Snippets);
    }

    [TestMethod]
    public void test_congelato_non_riversa_la_cronologia()
    {
        var a = new Archivio(Percorsi);
        a.Annota(new Voce { Tipo = Voce.Testo_, Testo = "prima" });
        a.Riversa(forza: true);

        a.Congela("di prova");
        a.Annota(new Voce { Tipo = Voce.Testo_, Testo = "dopo" });
        a.Riversa(forza: true);

        var letto = new Archivio(Percorsi);
        Assert.HasCount(1, letto.Cron);
        Assert.AreEqual("prima", letto.Cron[0].Testo);
    }

    [TestMethod]
    public void test_l_utente_lo_viene_a_sapere()
    {
        var a = new Archivio(Percorsi);
        Assert.IsNull(a.Problema);
        Assert.IsFalse(a.Congelato);

        a.Congela("mi hanno aperto nel posto sbagliato");

        Assert.IsTrue(a.Congelato);
        Assert.AreEqual("mi hanno aperto nel posto sbagliato", a.Problema);
    }

    [TestMethod]
    public void test_non_si_scongela()
    {
        // Una sessione che si scongela da sola è quella che finirebbe
        // per scrivere dove non deve. Comanda il primo motivo.
        var a = new Archivio(Percorsi);
        a.Congela("il primo");
        a.Congela("il secondo");

        Assert.AreEqual("il primo", a.Problema);

        a.MettiPref("x", "y");
        Assert.IsNull(new Archivio(Percorsi).Pref<string>("x"));
    }
}

/// <summary>
/// Di che tipo è ogni salvato. Ciò che si prova qui non è la proposta
/// —che è la classificazione di sempre— ma le due regole nuove: che
/// ciò che sceglie l'utente vince, e che ciò che non sceglie non si
/// scrive.
/// </summary>
[TestClass]
public sealed class ProvaTipi
{
    [TestMethod]
    public void test_propone_cio_che_si_deduceva_prima()
    {
        Assert.AreEqual(Tipi.Segnalibro, Tipi.Deduci("https://x.com"));
        Assert.AreEqual(Tipi.Modello, Tipi.Deduci("Ciao [[nome]]"));
        Assert.AreEqual(Tipi.Nota, Tipi.Deduci("una cosa qualsiasi"));
        Assert.AreEqual(Tipi.Nota, Tipi.Deduci(""));
        Assert.AreEqual(Tipi.Nota, Tipi.Deduci(null));
    }

    [TestMethod]
    public void test_un_link_con_campi_e_segnalibro()
    {
        // Tra i due vince quello che si riconosce con più certezza.
        Assert.AreEqual(Tipi.Segnalibro, Tipi.Deduci("https://x.com/[[id]]"));
    }

    [TestMethod]
    public void test_email_non_si_deduce_mai()
    {
        // Nel corpo di un'email non c'è niente che lo separi da una nota.
        // Dedurlo da una chiocciola trasformerebbe in email qualsiasi
        // testo che menzioni un indirizzo.
        Assert.AreEqual(Tipi.Nota, Tipi.Deduci("scrivi a ciao@x.com"));
        Assert.AreEqual(Tipi.Nota, Tipi.Deduci("Gentile cliente:"));
    }

    [TestMethod]
    public void test_nemmeno_il_prompt_si_deduce()
    {
        // E con il prompt è peggio che con l'email: lì almeno c'è una
        // chiocciola a cui aggrapparsi. Qui è testo e basta.
        Assert.AreEqual(Tipi.Nota,
            Tipi.Deduci("Agisci come un revisore di codice e spiega..."));

        Assert.AreEqual(Tipi.Nota, Tipi.Deduci("Sei un assistente utile"));
    }

    [TestMethod]
    public void test_un_prompt_con_campi_si_propone_modello()
    {
        // E va bene così: ciò che decide se chiede prima di incollare è
        // il testo, non il tipo. Chi lo vuole tra i suoi prompt lo cambia
        // a mano e continua a chiedergli i campi lo stesso.
        Assert.AreEqual(Tipi.Modello,
            Tipi.Deduci("Riassumi questo in [[parole]] parole"));

        Assert.AreEqual(Tipi.Prompt, Tipi.Di(
            Tipi.Prompt, "Riassumi questo in [[parole]] parole"));
    }

    [TestMethod]
    public void test_il_prompt_scelto_si_salva()
    {
        var s = Regole.CreaSnippet(
            "Agisci come un editor esigente", "Lavoro", null, Tipi.Prompt);

        Assert.AreEqual(Tipi.Prompt, s.Tipo);
        Assert.AreEqual(Tipi.Prompt, Tipi.Di(s));
    }

    [TestMethod]
    public void test_i_cinque_tipi_e_senza_ripetere()
    {
        // Tutti alimenta la tendina e i gruppi del pannello: uno
        // ripetuto uscirebbe due volte nella lista, e uno che manca
        // lascia i suoi salvati senza gruppo.
        Assert.HasCount(5, Tipi.Tutti);
        Assert.HasCount(5, Tipi.Tutti.Distinct().ToList());

        foreach (var t in Tipi.Tutti) Assert.IsTrue(Tipi.Vale(t), t);

        // La nota va per ultima: è il cassetto di tutto il resto.
        Assert.AreEqual(Tipi.Nota, Tipi.Tutti[^1]);
    }

    [TestMethod]
    public void test_cio_che_sceglie_l_utente_comanda()
    {
        // Il caso che ha portato a tutto questo: il corpo di un'email è
        // testo normale, e finora non c'era modo di dire che lo era.
        Assert.AreEqual(Tipi.Email, Tipi.Di(Tipi.Email, "Ciao squadra,"));

        // E al contrario: un indirizzo che l'utente vuole trattare come
        // nota smette di comportarsi da segnalibro.
        Assert.AreEqual(Tipi.Nota, Tipi.Di(Tipi.Nota, "https://x.com"));
    }

    [TestMethod]
    public void test_senza_scegliere_vale_il_dedotto()
    {
        Assert.AreEqual(Tipi.Segnalibro, Tipi.Di(null, "https://x.com"));
        Assert.AreEqual(Tipi.Nota, Tipi.Di("", "testo"));
    }

    [TestMethod]
    public void test_un_tipo_sconosciuto_non_lascia_fuori_il_salvato()
    {
        // snippets.json si modifica a mano e arriva da versioni che non
        // esistono ancora. Un tipo che non conosciamo non può lasciare la
        // riga senza gruppo, che in pratica è sparire dalla lista.
        Assert.AreEqual(Tipi.Nota, Tipi.Di("fattura", "testo"));
        Assert.IsFalse(Tipi.Vale("fattura"));
        Assert.IsFalse(Tipi.Vale(null));
    }

    [TestMethod]
    public void test_non_si_scrive_cio_che_gia_si_deduceva()
    {
        // Aprire un salvato e premere Salva senza toccare niente non può
        // aggiungergli una chiave che prima non aveva.
        Assert.IsNull(Tipi.PerSalvare(Tipi.Segnalibro, "https://x.com"));
        Assert.IsNull(Tipi.PerSalvare(Tipi.Nota, "testo normale"));
        Assert.IsNull(Tipi.PerSalvare(null, "testo normale"));
        Assert.IsNull(Tipi.PerSalvare("fattura", "testo normale"));
    }

    [TestMethod]
    public void test_si_scrive_cio_che_contraddice_il_testo()
    {
        Assert.AreEqual(Tipi.Email, Tipi.PerSalvare(Tipi.Email, "Ciao,"));
        Assert.AreEqual(Tipi.Nota, Tipi.PerSalvare(Tipi.Nota, "https://x.com"));
    }

    [TestMethod]
    public void test_il_salvato_nuovo_lo_porta()
    {
        var s = Regole.CreaSnippet("Ciao squadra,", "Lavoro", null, Tipi.Email);

        Assert.AreEqual(Tipi.Email, s.Tipo);
        Assert.AreEqual(Tipi.Email, Tipi.Di(s));
    }

    [TestMethod]
    public void test_il_salvato_di_sempre_non_guadagna_una_chiave()
    {
        var s = Regole.CreaSnippet("https://x.com", "Lavoro");

        Assert.IsNull(s.Tipo);
        Assert.AreEqual(Tipi.Segnalibro, Tipi.Di(s));
    }

    [TestMethod]
    public void test_anche_con_formato()
    {
        var runs = new List<Frammento>
        {
            Regole.CreaFrammento("Gentile "),
            Regole.CreaFrammento("cliente", grassetto: 1),
        };

        var s = Regole.CreaSnippet(runs, "Lavoro", "Benvenuta", Tipi.Email);

        Assert.AreEqual(Tipi.Email, s.Tipo);
        Assert.AreEqual("Benvenuta", s.Titolo);
        Assert.HasCount(2, s.Runs);
    }
}

/// <summary>
/// La riga con cui Windows ci riapre dopo un aggiornamento.
/// Sbagliare qui non rompe niente a vista: pastepad si riapre
/// tranquillo su un'altra cartella di dati.
/// </summary>
[TestClass]
public sealed class ProvaArgomenti
{
    [TestMethod]
    public void test_senza_argomenti_non_registra_riga()
    {
        // null e stringa vuota significano la stessa cosa per la API di
        // Windows: elimina ciò che è registrato. Restituire null lo rende
        // chiaro.
        Assert.IsNull(Argomenti.Componi([]));
    }

    [TestMethod]
    public void test_argomento_semplice_non_va_tra_virgolette()
    {
        Assert.AreEqual("--dati C:\\temp\\prova",
            Argomenti.Componi(["--dati", "C:\\temp\\prova"]));
    }

    /// <summary>
    /// Quella che conta davvero. Una cartella con spazi nel nome, senza
    /// virgolette, Windows la dividerebbe in più argomenti: pastepad
    /// tornerebbe senza una cartella valida e scriverebbe dove non deve.
    ///
    /// Tutte le prove manuali furono fatte sul percorso corto in formato
    /// 8.3, che non ha spazi, quindi questo ramo non fu eseguito nemmeno
    /// una volta.
    /// </summary>
    [TestMethod]
    public void test_percorso_con_spazi_va_tra_virgolette()
    {
        Assert.AreEqual(
            "--dati \"C:\\Users\\Ana Maria Perez\\La mia cartella\"",
            Argomenti.Componi(
                ["--dati", "C:\\Users\\Ana Maria Perez\\La mia cartella"]));
    }

    [TestMethod]
    public void test_non_mette_virgolette_due_volte()
    {
        Assert.AreEqual("--dati \"C:\\con spazi\"",
            Argomenti.Componi(["--dati", "\"C:\\con spazi\""]));
    }
}

[TestClass]
public sealed class ProvaTesti
{
    /// <summary>
    /// I buchi si numerano nell'ordine in cui compaiono. Prima erano
    /// tutti {0} e questa frase usciva come "la cartella 3 e i suoi 3
    /// testi".
    /// </summary>
    [TestMethod]
    public void test_due_buchi_ricevono_valori_diversi()
    {
        Assert.AreEqual(
            "Eliminare la cartella Lavoro e i suoi 3 testi? "
            + "Non si può annullare.",
            Testi.Componi("Eliminare la cartella %s e i suoi %d testi? "
                          + "Non si può annullare.", "Lavoro", 3));
    }

    /// <summary>
    /// Una graffa nel testo è letterale. Le cartelle le nomina l'utente
    /// e niente gli impedisce di chiamarne una "{modelli}".
    /// </summary>
    [TestMethod]
    public void test_una_graffa_nel_testo_non_esplode()
    {
        Assert.AreEqual("Eliminare la cartella {strano}?",
            Testi.Componi("Eliminare la cartella %s?", "{strano}"));
    }
}

[TestClass]
public sealed class ProvaAvvioAutomatico : BaseConCartellaTemporanea
{
    /// <summary>
    /// I tre casi della preferenza. Si provano qui, sulla decisione
    /// pura, e non sul registro di Windows: HKCU è globale ed è
    /// dell'utente, non della prova — una prova che ci scrive cambia
    /// l'avvio di sessione a chi la esegue.
    /// </summary>
    [TestMethod]
    public void test_senza_preferenza_si_avvia_con_windows()
    {
        // Primo avvio: non c'è ancora config.json.
        var a = new Archivio(Percorsi);

        Assert.IsTrue(AvvioAutomatico.Vuole(
            a.Pref(AvvioAutomatico.Chiave, AvvioAutomatico.ComePredefinito)));
    }

    [TestMethod]
    public void test_con_si_si_avvia_con_windows()
    {
        var a = new Archivio(Percorsi);
        a.MettiPref(AvvioAutomatico.Chiave, "si");

        Assert.IsTrue(AvvioAutomatico.Vuole(
            new Archivio(Percorsi).Pref(AvvioAutomatico.Chiave, AvvioAutomatico.ComePredefinito)));
    }

    [TestMethod]
    public void test_con_no_non_si_avvia_con_windows()
    {
        var a = new Archivio(Percorsi);
        a.MettiPref(AvvioAutomatico.Chiave, "no");

        Assert.IsFalse(AvvioAutomatico.Vuole(
            new Archivio(Percorsi).Pref(AvvioAutomatico.Chiave, AvvioAutomatico.ComePredefinito)));
    }

    /// <summary>
    /// Un config.json si modifica a mano. "Si " con la maiuscola o con
    /// uno spazio di troppo resta un sì.
    /// </summary>
    [TestMethod]
    public void test_il_si_si_legge_di_manica_larga()
    {
        Assert.IsTrue(AvvioAutomatico.Vuole("Si"));
        Assert.IsTrue(AvvioAutomatico.Vuole(" si "));
        Assert.IsTrue(AvvioAutomatico.Vuole("SI"));
    }

    /// <summary>
    /// E ciò che non è un si, non lo è — lo stesso criterio della
    /// versione precedente, che confrontava con "si" tale e quale.
    /// </summary>
    [TestMethod]
    public void test_cio_che_non_e_si_non_avvia()
    {
        Assert.IsFalse(AvvioAutomatico.Vuole("no"));
        Assert.IsFalse(AvvioAutomatico.Vuole(""));
        Assert.IsFalse(AvvioAutomatico.Vuole("qualsiasi cosa"));
    }
}

[TestClass]
public sealed class ProvaVersioni
{
    /// <summary>
    /// La trappola del confronto di versioni: come testo, "4.0.10" esce
    /// MINORE di "4.0.9" perché confronta l'"1" con il "9". Con dieci
    /// correzioni pubblicate l'avviso smetterebbe di comparire, e in
    /// silenzio.
    /// </summary>
    [TestMethod]
    public void test_la_decima_e_maggiore_della_nona()
    {
        Assert.IsTrue(Versioni.CeNovita("4.0.9", "4.0.10"),
            "4.0.10 deve essere più nuova di 4.0.9");

        Assert.IsFalse(Versioni.CeNovita("4.0.10", "4.0.9"));

        // E che non sia un caso di quei due numeri.
        Assert.IsTrue(Versioni.CeNovita("4.9.0", "4.10.0"));
        Assert.IsTrue(Versioni.CeNovita("9.0.0", "10.0.0"));
    }

    [TestMethod]
    public void test_la_stessa_versione_non_e_una_novita()
    {
        Assert.IsFalse(Versioni.CeNovita("4.0.1", "4.0.1"));

        // Nemmeno con un numero diverso di parti: System.Version tratta
        // quelle che mancano come -1, e "4.0.1" gli uscirebbe minore di
        // "4.0.1.0" se non si normalizzassero.
        Assert.IsFalse(Versioni.CeNovita("4.0.1", "4.0.1.0"));
        Assert.IsFalse(Versioni.CeNovita("4.0.1.0", "4.0.1"));
    }

    [TestMethod]
    public void test_la_v_del_tag_si_toglie()
    {
        Assert.AreEqual("4.0.1", Versioni.SenzaLaV("v4.0.1"));
        Assert.AreEqual("4.0.1", Versioni.SenzaLaV("4.0.1"));
        Assert.AreEqual("", Versioni.SenzaLaV(null));

        // E anche il tag intero vale per il confronto.
        Assert.IsTrue(Versioni.CeNovita("4.0.1", "v4.1.0"));
    }

    /// <summary>
    /// Ciò che non si capisce non avvisa. Meglio tacere che mostrare
    /// all'utente una fascia con dentro qualsiasi cosa.
    /// </summary>
    [TestMethod]
    public void test_cio_che_non_si_capisce_non_avvisa()
    {
        Assert.IsFalse(Versioni.CeNovita("4.0.1", "quattro"));
        Assert.IsFalse(Versioni.CeNovita("4.0.1", ""));
        Assert.IsFalse(Versioni.CeNovita("4.0.1", null));
        Assert.IsFalse(Versioni.CeNovita(null, "4.1.0"));

        // Nemmeno un tag di prova: non si interpreta e non si avvisa.
        Assert.IsFalse(Versioni.CeNovita("4.0.1", "v4.1.0-beta1"));
    }

    [TestMethod]
    public void test_si_controlla_una_volta_al_giorno()
    {
        var adesso = new DateTimeOffset(2026, 8, 13, 10, 0, 0, TimeSpan.Zero);

        // Senza data salvata: primo avvio, tocca.
        Assert.IsTrue(Versioni.ToccaControllare(null, adesso));

        // Si è già guardato oggi, anche se il programma si riapre dieci volte.
        Assert.IsFalse(Versioni.ToccaControllare("2026-08-13", adesso));

        Assert.IsTrue(Versioni.ToccaControllare("2026-08-12", adesso));

        // Una data che non si capisce non può lasciare l'avviso spento
        // per sempre.
        Assert.IsTrue(Versioni.ToccaControllare("ieri", adesso));

        Assert.AreEqual("2026-08-13", Versioni.Oggi(adesso));
    }

    /// <summary>
    /// Di ogni versione si avvisa una sola volta. Una fascia che esce
    /// tutti i giorni si smette di leggerla.
    /// </summary>
    [TestMethod]
    public void test_non_si_ripete_l_avviso_della_stessa_versione()
    {
        Assert.IsTrue(Versioni.ToccaAvvisare("4.0.1", "4.1.0", null));
        Assert.IsFalse(Versioni.ToccaAvvisare("4.0.1", "4.1.0", "4.1.0"));

        // Ma della successiva sì.
        Assert.IsTrue(Versioni.ToccaAvvisare("4.0.1", "4.2.0", "4.1.0"));

        // E se non c'è novità, non si avvisa anche se non risulta avvisata.
        Assert.IsFalse(Versioni.ToccaAvvisare("4.1.0", "4.1.0", null));
    }
}

[TestClass]
public sealed class ProvaMisureDelPannello
{
    /// <summary>
    /// La dimensione del primo avvio deve stare tra i limiti del
    /// ridimensionamento. Altrimenti il pannello nasce fuori dai propri
    /// limiti e il presentatore lo rimpicciolisce appena aperto — un
    /// avvio che si vede male solo la prima volta è di quelli che
    /// nessuno riproduce.
    /// </summary>
    [TestMethod]
    public void test_dimensione_iniziale_entro_i_limiti()
    {
        Assert.IsGreaterThanOrEqualTo(Config.MinLarghezza, Config.LarghezzaDef);
        Assert.IsLessThanOrEqualTo(Config.MaxLarghezza, Config.LarghezzaDef);

        Assert.IsGreaterThanOrEqualTo(Config.MinAltezza, Config.AltezzaDef);
        Assert.IsLessThanOrEqualTo(Config.MaxAltezza, Config.AltezzaDef);
    }
}

/// <summary>
/// Il blocco delle annotazioni. Vive in un file suo e non si incolla
/// mai, quindi ciò che va fissato qui è che non si mescoli con il
/// resto e che l'ordine regga.
/// </summary>
[TestClass]
public sealed class ProvaNote : BaseConCartellaTemporanea
{
    static DateTimeOffset El(int giorno) =>
        new(2026, 8, giorno, 10, 0, 0, TimeSpan.FromHours(-4));

    /// <summary>
    /// Un file delle note scritto prima della 4.15.0 porta "testo" e
    /// non "runs". Deve aprirsi lo stesso.
    ///
    /// È la prova che non si può saltare: se fallisce, l'utente aggiorna
    /// e trova il blocco vuoto. E non fallirebbe gridando —il file si
    /// legge, la lista si dipinge, le annotazioni escono senza niente
    /// dentro— che è il modo più costoso di fallire.
    /// </summary>
    [TestMethod]
    public void test_legge_le_annotazioni_di_prima_del_formato()
    {
        File.WriteAllText(Percorsi.Note, """
            [
             {
              "titolo": "Server",
              "testo": "Security details\\r\\nIAM role",
              "modificata": "2026-08-01T10:00:00.0000000-04:00"
             }
            ]
            """);

        var a = new Archivio(Percorsi);

        Assert.HasCount(1, a.Note);
        Assert.AreEqual("Server", a.Note[0].Titolo);
        Assert.AreEqual("Security details\\r\\nIAM role", Regole.TestoDi(a.Note[0].Runs));

        // E riscrivendolo va già nel formato nuovo, senza la chiave
        // vecchia: lasciarla sarebbe avere lo stesso contenuto in due
        // posti che possono discordare.
        a.SalvaNote();

        string crudo = File.ReadAllText(Percorsi.Note);
        StringAssert.Contains(crudo, "runs");
        Assert.DoesNotContain("\"testo\"", crudo);

        Assert.AreEqual(
            "Security details\\r\\nIAM role",
            Regole.TestoDi(new Archivio(Percorsi).Note[0].Runs));
    }

    /// <summary>Il formato viaggia fino al file e torna.</summary>
    [TestMethod]
    public void test_il_formato_dell_annotazione_sopravvive()
    {
        var a = new Archivio(Percorsi);

        a.AggiungiNota(Regole.CreaNota(
            [
                new Frammento { T = "in grassetto", B = 1 },
                new Frammento { T = " e normale" },
            ],
            El(1)));

        var letti = new Archivio(Percorsi).Note[0].Runs;

        Assert.HasCount(2, letti);
        Assert.AreEqual(1, letti[0].B);
        Assert.AreEqual(0, letti[1].B);
        Assert.AreEqual("in grassetto e normale", Regole.TestoDi(letti));
    }

    [TestMethod]
    public void test_la_nota_si_salva_nel_suo_file()
    {
        var a = new Archivio(Percorsi);
        a.AggiungiNota(Regole.CreaNota("comprare il pane", El(1)));

        Assert.IsTrue(File.Exists(Percorsi.Note), "non è stato scritto note.json");

        // E soprattutto: non si infila in ciò che si incolla.
        Assert.IsFalse(File.Exists(Percorsi.Dati),
            "una nota non può finire in snippets.json");

        var altra = new Archivio(Percorsi);
        Assert.HasCount(1, altra.Note);
        Assert.AreEqual("comprare il pane", Regole.TestoDi(altra.Note[0].Runs));
    }

    /// <summary>
    /// L'ultima toccata, in alto — come le note rapide di Windows. Si
    /// controlla dopo aver riletto il file, non solo in memoria:
    /// l'ordine deve sopravvivere al disco.
    /// </summary>
    [TestMethod]
    public void test_l_ultimo_toccato_resta_in_alto()
    {
        var a = new Archivio(Percorsi);
        a.AggiungiNota(Regole.CreaNota("la vecchia", El(1)));
        a.AggiungiNota(Regole.CreaNota("la nuova", El(3)));
        a.AggiungiNota(Regole.CreaNota("quella di mezzo", El(2)));

        CollectionAssert.AreEqual(
            new[] { "la nuova", "quella di mezzo", "la vecchia" },
            new Archivio(Percorsi).Note.Select(n => Regole.TestoDi(n.Runs)).ToArray());
    }

    [TestMethod]
    public void test_modificare_una_nota_la_porta_in_cima()
    {
        var a = new Archivio(Percorsi);
        a.AggiungiNota(Regole.CreaNota("la vecchia", El(1)));
        a.AggiungiNota(Regole.CreaNota("la nuova", El(3)));

        var vecchia = a.Note.Single(n => Regole.TestoDi(n.Runs) == "la vecchia");
        Assert.IsTrue(a.CambiaNota(
            vecchia, [Regole.CreaFrammento("la vecchia, ritoccata")],
            El(5).ToString("O")));

        CollectionAssert.AreEqual(
            new[] { "la vecchia, ritoccata", "la nuova" },
            new Archivio(Percorsi).Note.Select(n => Regole.TestoDi(n.Runs)).ToArray());
    }

    /// <summary>
    /// Si cerca per riferimento e non per indice apposta: la lista si
    /// riordina a ogni salvataggio, quindi un indice preso prima punta a
    /// un'altra nota. Questa prova fissa proprio questo — quella che si
    /// elimina è quella chiesta, non quella rimasta in quella posizione.
    /// </summary>
    [TestMethod]
    public void test_si_elimina_quella_richiesta_anche_se_la_lista_si_riordina()
    {
        var a = new Archivio(Percorsi);
        a.AggiungiNota(Regole.CreaNota("prima", El(1)));
        a.AggiungiNota(Regole.CreaNota("seconda", El(2)));
        a.AggiungiNota(Regole.CreaNota("terza", El(3)));

        var seconda = a.Note.Single(n => Regole.TestoDi(n.Runs) == "seconda");
        Assert.IsTrue(a.EliminaNota(seconda));

        CollectionAssert.AreEqual(
            new[] { "terza", "prima" },
            new Archivio(Percorsi).Note.Select(n => Regole.TestoDi(n.Runs)).ToArray());
    }

    [TestMethod]
    public void test_eliminare_qualcosa_che_non_ce_piu_non_esplode()
    {
        var a = new Archivio(Percorsi);
        var sciolta = Regole.CreaNota("mai aggiunta", El(1));

        Assert.IsFalse(a.EliminaNota(sciolta));
        Assert.IsFalse(a.CambiaNota(
            sciolta, [Regole.CreaFrammento("qualcosa")], El(2).ToString("O")));
    }

    /// <summary>
    /// Un file scritto a mano può non avere la data. Questo non può far
    /// cadere l'avvio: la nota senza data va in fondo e le altre
    /// conservano il loro ordine.
    /// </summary>
    [TestMethod]
    public void test_una_nota_senza_data_non_rompe_l_ordine()
    {
        File.WriteAllText(Percorsi.Note,
            """
            [
              { "testo": "senza data" },
              { "testo": "con data", "modificata": "2026-08-01T10:00:00.0000000-04:00" }
            ]
            """);

        CollectionAssert.AreEqual(
            new[] { "con data", "senza data" },
            new Archivio(Percorsi).Note.Select(n => Regole.TestoDi(n.Runs)).ToArray());
    }

    /// <summary>
    /// Gli a capo si normalizzano all'ingresso, come in tutto ciò che
    /// arriva dall'utente: una casella di WinUI restituisce \r e basta,
    /// e salvarlo così lascia un'annotazione di tre linee trasformata in
    /// una.
    /// </summary>
    [TestMethod]
    public void test_gli_acapo_si_normalizzano()
    {
        var a = new Archivio(Percorsi);
        a.AggiungiNota(Regole.CreaNota("una\rdue\ntre", El(1)));

        Assert.AreEqual(
            "una\r\ndue\r\ntre",
            Regole.TestoDi(new Archivio(Percorsi).Note[0].Runs));

        var n = a.Note[0];
        a.CambiaNota(
            n, [Regole.CreaFrammento("quattro\rcinque")],
            El(2).ToString("O"));

        Assert.AreEqual(
            "quattro\r\ncinque",
            Regole.TestoDi(new Archivio(Percorsi).Note[0].Runs));
    }

    /// <summary>
    /// La data si scrive in ISO-8601 e non nel formato della regione.
    /// Con «15/08/2026», un computer configurato in inglese legge lo
    /// stesso file come l'8 marzo, e l'ordine della lista cambia da solo.
    /// </summary>
    [TestMethod]
    public void test_la_data_va_in_iso_8601()
    {
        var nota = Regole.CreaNota("x", El(9));

        StringAssert.StartsWith(nota.Modificata, "2026-08-09T10:00:00");
        StringAssert.Contains(nota.Modificata, "-04:00");
    }
}

/// <summary>
/// Ciò che si vede nel riquadro di un'annotazione senza aprirla. La
/// regola è che deve distinguere due annotazioni che iniziano
/// uguale, che è proprio ciò che una sola linea non fa.
/// </summary>
[TestClass]
public sealed class ProvaAnteprima
{
    /// <summary>Gli a capo si conservano; non è una riga sola.</summary>
    [TestMethod]
    public void test_conserva_gli_acapo()
    {
        var v = Regole.Anteprima("Security details\r\nIAM role\r\nOwner ID");

        Assert.AreEqual(3, v.Split('\n').Length);
        Assert.AreEqual("Security details", v.Split('\n')[0]);
    }

    /// <summary>
    /// Il caso che ha motivato tutto questo: due annotazioni con la
    /// stessa prima linea. Riassunte in una riga uscivano identiche
    /// nella lista.
    /// </summary>
    [TestMethod]
    public void test_distingue_due_che_iniziano_uguale()
    {
        var a = Regole.Anteprima("Security details\r\nIAM role\r\nOwner 111");
        var b = Regole.Anteprima("Security details\r\nIAM role\r\nOwner 222");

        Assert.AreNotEqual(a, b);
        StringAssert.Contains(a, "111");
    }

    [TestMethod]
    public void test_salta_i_buchi_sopra_e_sotto()
    {
        Assert.AreEqual(
            "La prima cosa vera",
            Regole.Anteprima("\r\n   \r\nLa prima cosa vera\r\n\r\n"));
    }

    [TestMethod]
    public void test_taglia_per_linee()
    {
        var molte = string.Join(
            "\r\n", Enumerable.Range(1, 20).Select(i => "linea " + i));

        Assert.AreEqual(6, Regole.Anteprima(molte).Split('\n').Length);
    }

    /// <summary>
    /// La seconda rete: nemmeno sei linee da mille caratteri ci stanno.
    /// </summary>
    [TestMethod]
    public void test_taglia_per_caratteri()
    {
        var v = Regole.Anteprima(new string('x', 500));

        Assert.IsTrue(v.Length <= 241, "sono uscite " + v.Length);
        StringAssert.EndsWith(v, "…");
    }

    [TestMethod]
    public void test_vuoto_non_esplode()
    {
        Assert.AreEqual("", Regole.Anteprima(""));
        Assert.AreEqual("", Regole.Anteprima("   \r\n\r\n  "));
    }
}

/// <summary>
/// Chiamare un'annotazione per nome. È ciò che trasforma il blocco
/// in qualcosa che si usa: senza nome bisogna riconoscere la nota da
/// come inizia, e con venti annotazioni smette di funzionare.
/// </summary>
[TestClass]
public sealed class ProvaNomeDelleAnnotazioni : BaseConCartellaTemporanea
{
    static Nota Annotazione(string titolo, string testo) => new()
    {
        Titolo = titolo,
        Runs = [Regole.CreaFrammento(testo)],
        Modificata = "2026-09-10T10:00:00.0000000-04:00",
    };

    [TestMethod]
    public void test_senza_nome_vale_la_prima_linea()
    {
        Assert.AreEqual(
            "Security details",
            Regole.NomeDi(Annotazione("", "Security details\r\nIAM role")));
    }

    [TestMethod]
    public void test_con_nome_comanda_il_nome()
    {
        Assert.AreEqual(
            "Server AWS",
            Regole.NomeDi(Annotazione("Server AWS", "Security details\r\nIAM role")));
    }

    /// <summary>
    /// Il nome pesa più del corpo. Altrimenti cercare «server» metterebbe
    /// per prima qualsiasi annotazione che menziona la parola
    /// all'interno, e quella che si chiama così resterebbe sepolta.
    /// </summary>
    [TestMethod]
    public void test_quello_che_si_chiama_cosi_va_per_primo()
    {
        var archivio = new Archivio(Percorsi);

        archivio.AggiungiNota(Annotazione("Altra cosa", "qui parlo del server e del server"));
        archivio.AggiungiNota(Annotazione("Server AWS", "IAM role"));

        var escono = archivio.CercaNote("server");

        Assert.AreEqual(2, escono.Count);
        Assert.AreEqual("Server AWS", escono[0].Titolo);
    }

    [TestMethod]
    public void test_trova_anche_dal_corpo()
    {
        var archivio = new Archivio(Percorsi);
        archivio.AggiungiNota(Annotazione("Server AWS", "Owner ID 334716554232"));

        Assert.AreEqual(1, archivio.CercaNote("334716554232").Count);
        Assert.AreEqual(0, archivio.CercaNote("niente di questo").Count);
    }

    /// <summary>Senza scrivere niente escono tutte, senza filtro.</summary>
    [TestMethod]
    public void test_query_vuota_li_restituisce_tutti()
    {
        var archivio = new Archivio(Percorsi);
        archivio.AggiungiNota(Annotazione("Uno", "a"));
        archivio.AggiungiNota(Annotazione("Due", "b"));

        Assert.AreEqual(2, archivio.CercaNote("").Count);
        Assert.AreEqual(2, archivio.CercaNote("   ").Count);
    }

    /// <summary>
    /// Gli accenti non contano, come nella ricerca di sempre.
    /// </summary>
    [TestMethod]
    public void test_gli_accenti_non_contano()
    {
        var archivio = new Archivio(Percorsi);
        archivio.AggiungiNota(Annotazione("Attività di squadra", "note"));

        Assert.AreEqual(1, archivio.CercaNote("attivita").Count);
    }

    /// <summary>
    /// Il nome viaggia fino al file e torna. Una chiave nuova che non
    /// si rilegge lascia l'utente a rinominare la stessa cosa ogni volta.
    /// </summary>
    [TestMethod]
    public void test_il_nome_sopravvive_al_file()
    {
        var primo = new Archivio(Percorsi);
        primo.AggiungiNota(Regole.CreaNota(
            "qualcosa", DateTimeOffset.Now, "La mia lavagna"));

        var secondo = new Archivio(Percorsi);

        Assert.AreEqual(1, secondo.Note.Count);
        Assert.AreEqual("La mia lavagna", secondo.Note[0].Titolo);
    }
}
