using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Pastepad.Nucleo;

namespace Pastepad.App;

/// <summary>Ciò che può comparire nella lista: una riga o un'intestazione.</summary>
public abstract class ItemLista
{
}

/// <summary>
/// L'intestazione che apre e chiude un gruppo dentro Salvati.
///
/// I segnalibri e le note vivono nella stessa scheda ma non si usano
/// allo stesso modo: un segnalibro si apre nel browser e una nota si
/// incolla. Separarli fa trovare ogni cosa senza leggerle tutte.
/// </summary>
public sealed class Gruppo(string chiave, string etichetta, int quanti,
                          bool aperto, string icona, bool inAccento) : ItemLista
{
    public string Chiave { get; } = chiave;
    public string Etichetta { get; } = etichetta;
    public string Quanti { get; } = quanti.ToString();
    public bool Aperto { get; } = aperto;
    public string Icona { get; } = icona;

    public string Chevron =>
        Aperto ? Stile.Icone.SottoV : Stile.Icone.DestraV;

    /// <summary>
    /// Il gruppo dei segnalibri ha l'icona in accento; quello delle note,
    /// in medio. È lo stesso segnale che usa la riga di link.
    /// </summary>
    public Brush ColoreIcona => inAccento
        ? Stile.Pennello(Stile.ColoreAccento.Color)
        : Stile.Pennello(Stile.Attuale.Medio);
}

/// <summary>
/// Una riga della lista, già risolta in testo e colore. Non tiene
/// riferimenti a controlli: la lista si ricostruisce intera a ogni
/// aggiornamento, e ciò che cambia senza ricostruirla (attiva, hover,
/// spuntata) avvisa con PropertyChanged.
/// </summary>
public sealed class Riga : ItemLista, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public Elemento Dato { get; }

    /// <summary>True se viene dalla cronologia; false se è un salvato.</summary>
    public bool EDiCronologia { get; }

    public string Titolo { get; }
    public string Dettaglio { get; }

    /// <summary>Il testo semplice, per incollare, copiare o aprire.</summary>
    public string Testo { get; }

    /// <summary>
    /// L'intero testo è un indirizzo. È un fatto su ciò che è scritto,
    /// non una preferenza: per questo continua a comandare se si offre
    /// «Apri nel browser» e se sotto il titolo va il dominio. Un'email
    /// che l'utente ha archiviato come tale si può aprire lo stesso se
    /// risulta essere una url.
    /// </summary>
    public bool EUnLink { get; }

    /// <summary>
    /// Ha dei [[campi]] dentro, cioè usandola chiede prima di incollare.
    /// Si vede nella riga perché finora non c'era modo di sapere quale
    /// avrebbe chiesto e quale no finché non la si premeva.
    ///
    /// Nemmeno questo dipende dal tipo scelto: chi archivia un modello
    /// come email deve comunque riempirne i campi.
    /// </summary>
    public bool EUnModello { get; }

    /// <summary>
    /// Di che tipo è: ciò che ha scelto l'utente, o ciò che si deduce
    /// dal testo finché non sceglie. Decide il gruppo, l'icona, e —solo
    /// in un salvato— se il clic apre o incolla: un segnalibro salvato
    /// si apre nel browser, tutto il resto si incolla. La cronologia
    /// incolla sempre, anche se ciò che è copiato è un indirizzo.
    /// </summary>
    public string Tipo { get; }

    public bool EUnImmagine { get; }
    public bool Fissata { get; }

    /// <summary>
    /// È un'annotazione del blocco. Non si incolla mai: premerla la apre
    /// per modificarla, e il menu non offre l'incolla.
    ///
    /// L'ha chiesto l'utente così —«non deve avere l'opzione di incollare
    /// perché sono note»— e in più evita il problema che ci sarebbe a
    /// offrirlo: incollare lascia il pannello e restituisce il focus a
    /// un'altra finestra, che è proprio il contrario di ciò che si vuole
    /// scrivendo un'annotazione.
    /// </summary>
    public bool EUnAnnotazione { get; }

    /// <summary>Vuoto quando la riga non ha icona, che è il caso normale.</summary>
    public string Icona { get; }

    public bool Compatta { get; }

    public Riga(Elemento dato, bool compatta)
    {
        Dato = dato;
        Compatta = compatta;

        switch (dato)
        {
            case Voce voce:
                EDiCronologia = true;
                Fissata = voce.Pin;
                EUnImmagine = voce.EUnImmagine;

                if (voce.EUnImmagine)
                {
                    Titolo = "Immagine copiata";
                    Dettaglio = "cattura";
                    Testo = "";
                    Tipo = Tipi.Nota;
                    Icona = Stile.Icone.Immagine;
                    break;
                }

                Testo = voce.Testo ?? "";
                EUnLink = Regole.EUnLink(Testo);
                EUnModello = !EUnLink && Regole.CampiDi(Testo).Count > 0;

                // Ciò che passa dagli appunti non lo archivia nessuno, quindi
                // qui il tipo si può solo dedurre. E si deduce da ciò che è
                // appena stato calcolato: Tipi.Deduci(Testo) ripercorrerebbe
                // l'intero testo cercando la stessa cosa due volte.
                Tipo = Tipi.DeduciDa(EUnLink, EUnModello);

                Titolo = Regole.UnaLinea(Testo, 80);
                if (Titolo.Length == 0) Titolo = "—";

                Dettaglio = EUnLink
                    ? Regole.DominioDi(Testo)
                    : Testi.Componi("%d caratteri", Testo.Length);

                Icona = IconaDi(Tipo);
                break;

            case Snippet snippet:
                Testo = Regole.TestoDi(snippet.Runs);
                EUnLink = Regole.EUnLink(Testo);
                EUnModello = !EUnLink && Regole.CampiDi(Testo).Count > 0;
                // Tipi.Di(snippet) riunirebbe tutti i frammenti con TestoDi
                // —il testo è già qui sopra— e ripeterebbe le due ricerche.
                // Qui si sfrutta tutto ciò che è già fatto.
                Tipo = Tipi.Vale(snippet.Tipo)
                    ? snippet.Tipo!
                    : Tipi.DeduciDa(EUnLink, EUnModello);

                // Il titolo si salva intero e si accorcia qui, come già si
                // faceva con la cronologia: il taglio è di schermo e non
                // deve restare scritto nel file.
                Titolo = Regole.UnaLinea(snippet.Titolo, 80);
                if (Titolo.Length == 0) Titolo = "—";

                Dettaglio = EUnLink ? Regole.DominioDi(Testo) : snippet.Categoria;
                Icona = IconaDi(Tipo);
                break;

            case Nota annotazione:
                EUnAnnotazione = true;
                Testo = Regole.TestoDi(annotazione.Runs);
                Vista = Regole.Anteprima(Testo);

                // Il nome che le ha dato l'utente, se ce l'ha. Senza
                // nome non si mette niente: il riquadro mostra già le
                // prime linee, e ripetere la prima come se fosse un
                // titolo sprecherebbe una riga per dire due volte la stessa cosa.
                Nome = annotazione.Titolo;

                Titolo = Regole.UnaLinea(Testo, 80);
                if (Titolo.Length == 0) Titolo = "—";

                // Né il dominio né la cartella: un'annotazione non ha
                // nessuno dei due. Sotto il titolo è utile quando è
                // stata toccata, come nelle note rapide di Windows.
                Dettaglio = DateLeggibili.Leggibile(annotazione.Modificata, DateTimeOffset.Now);

                Tipo = Tipi.Nota;

                // Senza icona, per la stessa regola delle note salvate:
                // in questa scheda TUTTE le righe sono annotazioni, quindi
                // un'icona in ognuna sarebbe una colonna di rumore che non
                // distingue niente.
                Icona = "";
                break;

            default:
                Titolo = "";
                Dettaglio = "";
                Testo = "";
                Tipo = Tipi.Nota;
                Icona = "";
                break;
        }
    }

    /// <summary>
    /// I quattro tipi con nome hanno l'icona; la nota, nessuna. Una
    /// riga con icona in tutte sarebbe una colonna di rumore, e la nota
    /// è la maggioranza.
    /// </summary>
    static string IconaDi(string tipo) => tipo switch
    {
        Tipi.Segnalibro => Stile.Icone.Link,
        Tipi.Modello => Stile.Icone.Modello,
        Tipi.Email => Stile.Icone.Email,
        Tipi.Prompt => Stile.Icone.Prompt,
        _ => "",
    };

    // -------------------------------------------------------- lo stato

    bool _attiva;
    bool _inPrimoPiano;
    bool _spuntando;
    bool _spuntata;

    public bool Attiva
    {
        get => _attiva;
        set { if (_attiva != value) { _attiva = value; TuttoCambiato(); } }
    }

    public bool InPrimoPiano
    {
        get => _inPrimoPiano;
        set { if (_inPrimoPiano != value) { _inPrimoPiano = value; TuttoCambiato(); } }
    }

    public bool Spuntando
    {
        get => _spuntando;
        set { if (_spuntando != value) { _spuntando = value; TuttoCambiato(); } }
    }

    public bool Spuntata
    {
        get => _spuntata;
        set { if (_spuntata != value) { _spuntata = value; TuttoCambiato(); } }
    }

    // ------------------------------------------------------- l'aspetto

    /// <summary>
    /// In un'annotazione questo è il MINIMO, non l'altezza: il riquadro
    /// cresce con ciò che contiene, come nelle note rapide. Con altezza
    /// fissa, una nota di due linee lasciava mezzo riquadro vuoto.
    /// </summary>
    public double Altezza =>
        EUnAnnotazione ? Stile.AltezzaAnnotazione
        : Compatta ? Stile.AltezzaRigaMini
        : Stile.AltezzaRiga;

    // ---------------------------------- il riquadro dell'annotazione

    /// <summary>
    /// Le prime linee dell'annotazione, con i suoi a capo. Vuoto in
    /// tutto ciò che non è un'annotazione: il modello a riquadro si usa
    /// solo lì.
    /// </summary>
    public string Vista { get; } = "";

    /// <summary>Il nome dell'annotazione, se gliene hanno dato uno.</summary>
    public string Nome { get; } = "";

    public Visibility VediNome =>
        Nome.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// L'accento di sfondo, e lo STESSO in tutte. Si provò a dipingere
    /// l'attiva intera e le altre attenuate, e sembravano due tipi di
    /// nota diversi: nelle note rapide di Windows tutti i riquadri sono
    /// dello stesso colore. Quale è scelta si dice con il bordo, che non
    /// cambia ciò che il riquadro È.
    /// </summary>
    public Brush SfondoAnnotazione => Stile.Pennello(Stile.ColoreAccento.Color);

    /// <summary>
    /// Solo il riquadro attivo ha il bordo. Va del colore del testo e
    /// non dell'accento —su accento, l'accento non si vede—, e attenuato
    /// perché segnali senza gridare.
    /// </summary>
    public Brush BordoAnnotazione => Attiva || InPrimoPiano
        ? Stile.Pennello(Stile.ColoreAccento.Sopra, Attiva ? 0.85 : 0.35)
        : Stile.Pennello(Stile.ColoreAccento.Color);

    /// <summary>
    /// Il colore che la tavolozza ha verificato per scrivere SOPRA
    /// l'accento. Su un riquadro di accento, il colore di testo normale
    /// non ha contrasto garantito; questo sì.
    /// </summary>
    public Brush ColoreAnnotazione => Stile.Pennello(Stile.ColoreAccento.Sopra);

    public Brush ColoreDataAnnotazione =>
        Stile.Pennello(Stile.ColoreAccento.Sopra, 0.75);

    /// <summary>
    /// 16 senza icona e 22 con, sezione 3 delle specifiche. Conta
    /// l'icona di tipo, non la barra di attiva né la spunta.
    /// </summary>
    public Thickness Riempimento =>
        new(Icona.Length > 0 && !Spuntando ? 22 : 16, 4, 8, 4);

    public Brush Sfondo => Attiva
        ? Stile.Pennello(Stile.ColoreAccento.Color)
        : Stile.Pennello(InPrimoPiano ? Stile.Attuale.Hover : Stile.Attuale.Riquadro);

    /// <summary>
    /// In tema chiaro il riquadro è bianco su sfondo quasi bianco: senza
    /// bordo non si stacca. In scuro è di troppo.
    /// </summary>
    public Brush BordoColore => Stile.Pennello(Stile.Attuale.Bordo);

    public Thickness BordoSpessore =>
        Stile.EChiaro && !Attiva ? new Thickness(1) : new Thickness(0);

    public Brush ColoreTitolo => Attiva
        ? Stile.Pennello(Stile.ColoreAccento.Sopra)
        : Stile.Pennello(Stile.Attuale.Testo);

    public Windows.UI.Text.FontWeight PesoTitolo =>
        Attiva ? Microsoft.UI.Text.FontWeights.SemiBold
               : Microsoft.UI.Text.FontWeights.Normal;

    /// <summary>
    /// Il dominio va in accento e non in tenue: distingue a colpo
    /// d'occhio la riga che contiene un indirizzo da quella che ha una
    /// cartella o un conteggio di caratteri.
    /// </summary>
    public Brush ColoreDettaglio => Attiva
        ? Stile.Pennello(Stile.ColoreAccento.Sopra, 0.75)
        : Stile.Pennello(EUnLink ? Stile.ColoreAccento.Color : Stile.Attuale.Tenue);

    /// <summary>
    /// In accento l'icona di tipo —segnalibro, modello, email—, perché
    /// dice qualcosa che va visto. Quella dell'immagine no: lì l'icona è
    /// tutto ciò che c'è nella riga e in accento si mangerebbe il
    /// titolo. Era così prima che esistessero i tipi e lo è ancora.
    /// </summary>
    public Brush ColoreIcona => Attiva
        ? Stile.Pennello(Stile.ColoreAccento.Sopra)
        : Stile.Pennello(EUnImmagine
            ? Stile.Attuale.Tenue
            : Stile.ColoreAccento.Color);

    public Brush ColoreSpunta => Attiva
        ? Stile.Pennello(Stile.ColoreAccento.Sopra)
        : Stile.Pennello(Spuntata ? Stile.ColoreAccento.Color : Stile.Attuale.Tenue);

    public Brush ColorePuntina => Attiva
        ? Stile.Pennello(Stile.ColoreAccento.Sopra)
        : Stile.Pennello(Stile.ColoreAccento.Color);

    public Brush ColoreMenu => Attiva
        ? Stile.Pennello(Stile.ColoreAccento.Sopra)
        : Stile.Pennello(Stile.Attuale.Tenue);

    /// <summary>
    /// La barra bianca di 3 px. Non è un ornamento: è il segnale di
    /// focus che non dipende dal colore, per chi distingue male i
    /// colori.
    /// </summary>
    public Visibility VediBarra =>
        Attiva && !Spuntando ? Visibility.Visible : Visibility.Collapsed;

    public Visibility VediSpunta =>
        Spuntando ? Visibility.Visible : Visibility.Collapsed;

    public Visibility VediIcona =>
        !Spuntando && Icona.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// La puntina resta visibile finché qualcosa è fissato: altrimenti
    /// bisognerebbe passare il mouse su ogni riga per sapere quale lo è.
    /// </summary>
    public Visibility VediPuntina =>
        Fissata && !Spuntando ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Il pulsante con i tre punti compare solo al passaggio del mouse
    /// o nella riga attiva: con lui sempre presente la lista sembra
    /// carica.
    /// </summary>
    public Visibility VediMenu => !Spuntando && (Attiva || InPrimoPiano)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility VediDettaglio =>
        Compatta ? Visibility.Collapsed : Visibility.Visible;

    public string GlifoSpunta =>
        Spuntata ? Stile.Icone.SpuntaAttiva : Stile.Icone.Spunta;

    // Le diciture del menu della riga. Arrivano da qui tramite
    // x:Bind e non come letterali nello XAML: sono rimaste nel
    // codice da quando la lingua si sceglieva a caldo.
    public string TestoFissa =>
        Fissata ? "Togli dall'alto" : "Fissa in alto";

    public string TxtApri => "Apri nel browser";
    public string TxtIncolla => "Incolla";
    public string TxtIncollaSemplice => "Incolla senza formato";
    public string TxtCopia => "Copia";
    public string TxtModifica =>
        EUnAnnotazione ? "Modifica" : "Modifica e salva...";
    public string TxtElimina => "Elimina";

    /// <summary>
    /// Incolla e incolla senza formato. Si nascondono nelle annotazioni,
    /// che è l'unica cosa che separa il blocco dal resto del pannello.
    /// </summary>
    public Visibility VediIncolla =>
        EUnAnnotazione ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// Nemmeno «Apri nel browser»: un'annotazione che menziona un
    /// indirizzo resta un'annotazione, e aprirlo si porta via il focus
    /// come incollare.
    /// </summary>
    public Visibility VediApri =>
        EUnLink && !EUnAnnotazione ? Visibility.Visible : Visibility.Collapsed;

    public Visibility VediFissa =>
        EDiCronologia ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Si può modificare solo ciò che è testo: un'immagine della
    /// cronologia non ha niente da aprire nell'editor.
    /// </summary>
    public Visibility VediModifica =>
        EUnImmagine ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// Rilegge la tavolozza. I pennelli della riga si costruiscono da
    /// codice, quindi un cambio di tema di Windows non li tocca da solo:
    /// bisogna dirglielo.
    /// </summary>
    public void Aggiorna() => TuttoCambiato();

    void TuttoCambiato([CallerMemberName] string? _ = null)
    {
        // Tutto ciò che è visibile dipende da questi quattro flag, quindi
        // si avvisa in un colpo con la stringa vuota: è ciò che XAML
        // intende come "rileggi tutto".
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
    }
}
