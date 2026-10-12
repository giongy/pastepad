using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Pastepad.App;

/// <summary>
/// Sceglie il modello in base a ciò che c'è nella lista: una riga,
/// l'intestazione di un gruppo richiudibile, o il riquadro di
/// un'annotazione. Stanno nella stessa lista perché devono scorrere
/// insieme; se il gruppo vivesse fuori dal ListView, scendendo in
/// Salvati le intestazioni resterebbero inchiodate.
/// </summary>
public sealed partial class SelettoreDiRiga : DataTemplateSelector
{
    public DataTemplate? DiRiga { get; set; }
    public DataTemplate? DiGruppo { get; set; }
    public DataTemplate? DiAnnotazione { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item) =>
        item is Gruppo ? DiGruppo
        : item is Riga { EUnAnnotazione: true } ? DiAnnotazione
        : DiRiga;

    protected override DataTemplate? SelectTemplateCore(
        object item, DependencyObject contenitore) => SelectTemplateCore(item);
}
