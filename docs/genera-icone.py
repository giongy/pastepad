# -*- coding: utf-8 -*-
"""Genera i PNG e gli .ico a partire dal logo.

Si genera invece di esportare a mano perché bisogna produrre nove
dimensioni diverse e due varianti del disegno, e rifarlo a occhio ogni
volta che cambia il logo è il modo in cui si infilano le versioni
sballate.

La geometria è quella di docs/logo.svg. Sulla macchina non c'è un
rasterizzatore di SVG, quindi le figure si disegnano con Pillow: sono
cinque rettangoli arrotondati, due rotazioni e un'ombra.

    python docs/genera-icone.py
"""

import os

from PIL import Image, ImageDraw, ImageFilter

QUI = os.path.dirname(os.path.abspath(__file__))
RADICE = os.path.dirname(QUI)

# Campionamento: si disegna a questo fattore e si riduce con LANCZOS. I
# bordi arrotondati e le rotazioni di 2 e 4 gradi escono seghettati
# senza.
CAMPIONAMENTO = 8

TELA = 256.0

MENTA_SOPRA = (0x3B, 0xE0, 0xB4)
MENTA_SOTTO = (0x1F, 0xA6, 0x80)
BORDO_MENTA = (0x0F, 0x3D, 0x30)
INCHIOSTRO = (0x05, 0x2E, 0x23)


def sfumatura(dim, sopra, sotto):
    """Una sfumatura verticale dell'altezza chiesta."""
    larghezza, altezza = dim
    striscia = Image.new("RGB", (1, altezza))
    penna = ImageDraw.Draw(striscia)

    for y in range(altezza):
        t = y / max(1, altezza - 1)
        penna.point((0, y), fill=(
            round(sopra[0] + (sotto[0] - sopra[0]) * t),
            round(sopra[1] + (sotto[1] - sopra[1]) * t),
            round(sopra[2] + (sotto[2] - sopra[2]) * t),
        ))

    return striscia.resize((larghezza, altezza), Image.NEAREST)


def scheda(tela, riquadro, raggio, sopra, sotto, bordo, spessore, rotazione=0.0):
    """Una scheda: rettangolo arrotondato con sfumatura, bordo e rotazione.

    Si disegna in un suo livello e si ruota lì; ruotare sulla tela
    comune trascinerebbe ciò che è già dipinto.
    """
    x, y, larghezza, altezza = riquadro

    livello = Image.new("RGBA", tela.size, (0, 0, 0, 0))
    maschera = Image.new("L", tela.size, 0)
    ImageDraw.Draw(maschera).rounded_rectangle(
        (x, y, x + larghezza, y + altezza), radius=raggio, fill=255)

    riempimento = Image.new("RGBA", tela.size, (0, 0, 0, 0))
    riempimento.paste(sfumatura(tela.size, sopra, sotto), (0, 0))
    livello.paste(riempimento, (0, 0), maschera)

    ImageDraw.Draw(livello).rounded_rectangle(
        (x, y, x + larghezza, y + altezza), radius=raggio,
        outline=bordo + (255,), width=spessore)

    if rotazione:
        livello = livello.rotate(
            rotazione, resample=Image.BICUBIC,
            center=(x + larghezza / 2, y + altezza / 2))

    tela.alpha_composite(livello)


def barra(tela, riquadro, raggio, colore, opacita=255):
    x, y, larghezza, altezza = riquadro
    livello = Image.new("RGBA", tela.size, (0, 0, 0, 0))
    ImageDraw.Draw(livello).rounded_rectangle(
        (x, y, x + larghezza, y + altezza), radius=raggio,
        fill=colore + (opacita,))
    tela.alpha_composite(livello)


def completo(lato, inquadrare=False):
    """Le tre schede. Il disegno di docs/logo.svg.

    Con inquadrare=False esce così come lo disegna l'SVG, con la sua
    aria. È ciò che va nei PNG della documentazione: lì il logo si vede
    in una pagina e il margine fa parte del disegno.

    Con inquadrare=True la composizione si scala fino a occupare la
    tela. È ciò che va nell'.ico: un'icona da desktop che lascia un
    terzo del riquadro vuoto si vede più piccola di quelle accanto. È
    inquadratura, non ridisegno: non cambia nessuna proporzione.
    """
    n = lato * CAMPIONAMENTO
    img = Image.new("RGBA", (n, n), (0, 0, 0, 0))

    if inquadrare:
        # Riquadro del contenuto nell'SVG, con i suoi bordi e le sue
        # rotazioni.
        cx0, cy0, cx1, cy1 = 44.0, 60.0, 212.0, 188.0
        scala = (n * 0.92) / max(cx1 - cx0, cy1 - cy0)
        dx = (n - (cx1 - cx0) * scala) / 2 - cx0 * scala
        dy = (n - (cy1 - cy0) * scala) / 2 - cy0 * scala
    else:
        scala = n / TELA
        dx = dy = 0.0

    def e(v):
        return v * scala

    def p(x, y):
        return (x * scala + dx, y * scala + dy)

    def riquadro(x, y, larghezza, altezza):
        px, py = p(x, y)
        return (px, py, e(larghezza), e(altezza))

    def spessore(v):
        return max(1, round(e(v)))

    # --- ombra: la sagoma delle tre schede, sfocata
    sagoma = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    for x, y, la, al, r, ro in (
        (52, 122, 152, 60, 12, -4),
        (52, 96, 152, 60, 12, 2),
        (48, 64, 160, 66, 13, 0),
    ):
        scheda(sagoma, riquadro(x, y, la, al), e(r),
               (0, 0, 0), (0, 0, 0), (0, 0, 0), spessore(4), ro)

    ombra = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    ombra.putalpha(sagoma.getchannel("A").point(lambda a: int(a * 0.28)))
    ombra = ombra.filter(ImageFilter.GaussianBlur(e(8)))
    img.alpha_composite(ombra, (0, max(0, round(e(6)))))

    # --- le tre schede, da dietro in avanti
    scheda(img, riquadro(52, 122, 152, 60), e(12),
           (0xF5, 0xF5, 0xF3), (0xDA, 0xDA, 0xD6),
           (0xC6, 0xC6, 0xC2), spessore(4), -4)

    scheda(img, riquadro(52, 96, 152, 60), e(12),
           (0xFF, 0xFF, 0xFF), (0xED, 0xED, 0xEA),
           (0xD4, 0xD4, 0xD0), spessore(4.5), 2)

    scheda(img, riquadro(48, 64, 160, 66), e(13),
           MENTA_SOPRA, MENTA_SOTTO, BORDO_MENTA, spessore(5))

    barra(img, riquadro(68, 82, 90, 9), e(4.5), (0xFF, 0xFF, 0xFF))
    barra(img, riquadro(68, 100, 60, 7), e(3.5), INCHIOSTRO, 153)

    return img.resize((lato, lato), Image.LANCZOS)


def semplice(lato):
    """Variante per 48 px e sotto.

    A quelle dimensioni le tre schede smettono di leggersi: quella di
    mezzo resta alta tre pixel, i bordi di 4 e 5 px dell'SVG scendono
    sotto il mezzo pixel e l'insieme è una macchia verde e grigia.
    Guardato a dimensione reale, non ingrandito.

    Resta la scheda menta —che è quella con il colore del marchio e il
    bordo scuro che la stacca da qualsiasi sfondo— più grande e con una
    sola riga di testo. Due righe si uniscono sotto i 24 px.
    """
    n = lato * CAMPIONAMENTO
    img = Image.new("RGBA", (n, n), (0, 0, 0, 0))

    margine = n * 0.05
    larghezza = n - margine * 2
    altezza = larghezza * 0.74
    y0 = (n - altezza) / 2

    scheda(img, (margine, y0, larghezza, altezza), larghezza * 0.19,
           MENTA_SOPRA, MENTA_SOTTO, BORDO_MENTA,
           max(1, round(n * 0.038)))

    bw = larghezza * 0.54
    bh = altezza * 0.16
    barra(img, (margine + (larghezza - bw) / 2, y0 + altezza * 0.42, bw, bh),
          bh / 2, (0xFF, 0xFF, 0xFF))

    return img.resize((lato, lato), Image.LANCZOS)


def disegna(lato):
    """Quale variante tocca a ogni dimensione dell'.ico."""
    return semplice(lato) if lato <= 48 else completo(lato, inquadrare=True)


def scrivi(img, percorso):
    img.save(percorso)
    print("  %-46s %5d byte" % (
        os.path.relpath(percorso, RADICE).replace("\\", "/"),
        os.path.getsize(percorso)))


def main():
    print("PNG del logo:")
    for lato in (128, 256, 512):
        scrivi(completo(lato), os.path.join(QUI, "logo-%d.png" % lato))

    # Le dimensioni che Windows chiede a un .ico: 16 e 32 per l'area di
    # notifica e la barra delle applicazioni, il resto per Esplora file
    # e l'installer.
    lati = (16, 20, 24, 32, 48, 64, 128, 256)
    fotogrammi = [disegna(l) for l in lati]

    print("icone:")
    for destinazione in (
        os.path.join(QUI, "pastepad.ico"),
        os.path.join(RADICE, "csharp", "Pastepad.App", "Assets", "AppIcon.ico"),
    ):
        fotogrammi[-1].save(destinazione, format="ICO",
                            sizes=[(l, l) for l in lati],
                            append_images=fotogrammi[:-1])
        print("  %-46s %5d byte" % (
            os.path.relpath(destinazione, RADICE).replace("\\", "/"),
            os.path.getsize(destinazione)))

    # Foglio di contatto per guardare le dimensioni piccole come si
    # vedono davvero, senza ingrandire, su sfondo chiaro e su sfondo
    # scuro.
    prova(lati)


def prova(lati):
    spazio = 12
    larghezza = sum(lati) + spazio * (len(lati) + 1)
    altezza = max(lati) + spazio * 2

    foglio = Image.new("RGB", (larghezza, altezza * 2), (0xF6, 0xF6, 0xF4))
    ImageDraw.Draw(foglio).rectangle(
        (0, altezza, larghezza, altezza * 2), fill=(0x1B, 0x1B, 0x1F))

    x = spazio
    for lato in lati:
        icona = disegna(lato)
        foglio.paste(icona, (x, spazio + (max(lati) - lato)), icona)
        foglio.paste(icona, (x, altezza + spazio + (max(lati) - lato)), icona)
        x += lato + spazio

    percorso = os.path.join(QUI, "icone-prova.png")
    foglio.save(percorso)
    print("foglio di contatto: %s" % os.path.relpath(percorso, RADICE))


if __name__ == "__main__":
    main()
