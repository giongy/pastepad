using System.Net;
using System.Net.Http;
using System.Text.Json;
using Pastepad.Nucleo;

namespace Pastepad.App.Sistema;

/// <summary>
/// Chiedere a GitHub se c'è una versione più nuova.
///
/// Chiede soltanto e restituisce il dato: chi decide se merita un avviso
/// è <see cref="Versioni"/>, nel nucleo, dove si può provare senza rete.
/// Qui sta solo ciò che non si può provare senza uscire su internet.
///
/// Non scarica niente. Il pulsante dell'avviso apre la pagina della
/// release nel browser, e questo ha un vantaggio non piccolo: scaricando
/// l'installer con il browser torna il contrassegno del web, così
/// SmartScreen continua a fare da rete di sicurezza finché il binario
/// non è firmato.
/// </summary>
internal static class Aggiornamento
{
    const string Url =
        "https://api.github.com/repos/Josemgu/pastepad/releases/latest";

    /// <summary>
    /// Corto apposta. Succede in sottofondo e non interessa a nessuno: se
    /// la rete va male, tace e riprova domani.
    /// </summary>
    static readonly TimeSpan Attesa = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Uno solo per tutto il processo, che è come si usa HttpClient: uno
    /// per chiamata esaurisce le porte del sistema.
    /// </summary>
    static readonly HttpClient _client = Crea();

    static HttpClient Crea()
    {
        var c = new HttpClient { Timeout = Attesa };

        // Lo User-Agent NON è cortesia: senza, GitHub risponde 403
        // "Request forbidden by administrative rules" e lo dice nel
        // corpo. Misurato sulla API reale prima di scrivere questo.
        c.DefaultRequestHeaders.Add("User-Agent", $"pastepad/{Config.Version}");

        // La versione della API, perché un cambio di quella predefinita
        // non ci cambi a sorpresa il formato della risposta.
        c.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        c.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

        return c;
    }

    /// <summary>Ciò che è pubblicato, o null se non si è potuto sapere.</summary>
    internal sealed record Pubblicata(string Version, string Pagina);

    /// <summary>
    /// Chiede e restituisce ciò che è pubblicato. Non lancia mai: un
    /// errore di rete non può far cadere l'applicazione, tanto meno
    /// quello di un controllo che l'utente non ha chiesto.
    /// </summary>
    public static async Task<Pubblicata?> Consulta()
    {
        try
        {
            using var risposta = await _client.GetAsync(Url);

            if (!risposta.IsSuccessStatusCode)
            {
                // 403 e 429 sono il limite di richieste di GitHub. Non è
                // un errore nostro e non c'è niente da fare se non aspettare
                // domani, ma si annota: se un giorno l'avviso smette di
                // funzionare, questa riga è quella che lo spiega.
                Registro.Annota(
                    $"aggiornamenti: GitHub ha risposto {(int)risposta.StatusCode} "
                    + $"{risposta.StatusCode}"
                    + (risposta.StatusCode is HttpStatusCode.Forbidden
                                             or HttpStatusCode.TooManyRequests
                        ? " (limite di richieste)"
                        : ""));

                return null;
            }

            using var flusso = await risposta.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(flusso);

            if (!json.RootElement.TryGetProperty("tag_name", out var tag)
                || tag.GetString() is not { Length: > 0 } etichetta)
            {
                Registro.Annota("aggiornamenti: la risposta non conteneva tag_name");
                return null;
            }

            string pagina =
                json.RootElement.TryGetProperty("html_url", out var url)
                && url.GetString() is { Length: > 0 } indirizzo
                    ? indirizzo
                    : "https://github.com/Josemgu/pastepad/releases/latest";

            return new Pubblicata(Versioni.SenzaLaV(etichetta), pagina);
        }
        catch (Exception e) when (e is HttpRequestException
                                    or TaskCanceledException
                                    or OperationCanceledException
                                    or JsonException
                                    or UriFormatException)
        {
            // Senza rete, con il DNS giù o con la risposta rotta: una
            // riga e si passa oltre. All'utente non si mostra niente.
            Registro.Annota(
                $"aggiornamenti: impossibile consultare ({e.GetType().Name}: "
                + $"{e.Message})");

            return null;
        }
    }
}
