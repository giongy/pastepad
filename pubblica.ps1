# Compila pastepad in Release e lo lascia pronto all'uso in una cartella.
#
#   .\pubblica.ps1                 chiede: 1 normale, 2 zip
#   .\pubblica.ps1 -Normale        solo la cartella, senza chiedere
#   .\pubblica.ps1 -Zip            cartella e zip, senza chiedere
#   .\pubblica.ps1 -Destinazione "D:\programmi\pastepad"
#   .\pubblica.ps1 -Versione 4.15.0
#
# Di fabbrica la cartella e' pubblicato-x64, dentro il repo: e' il
# programma da usare, ed e' anche cio' che impacchetta l'installer. Non
# ci sono copie intermedie.
#
# Con lo zip lascia anche dist\pastepad-<versione>-x64.zip, da portare
# su un altro computer: dentro c'e' una cartella pastepad con tutto.
#
# Niente lettere accentate in questo file: Windows PowerShell 5.1 legge
# gli script senza BOM con la codifica di sistema e le rovinerebbe.

param(
    [string]$Destinazione = (Join-Path $PSScriptRoot "pubblicato-x64"),
    [string]$Versione,
    [switch]$Normale,
    [switch]$Zip
)

$ErrorActionPreference = "Stop"

# Senza -Normale ne' -Zip si chiede. I due interruttori restano per chi
# lancia lo script da un altro script, dove non c'e' nessuno a rispondere.
if (-not $Normale -and -not $Zip) {
    Write-Host ""
    Write-Host "Come pubblico pastepad?"
    Write-Host "  1  normale: solo la cartella"
    Write-Host "  2  zip: la cartella e anche lo zip in dist"

    do { $scelta = (Read-Host "Scelta (1 o 2)").Trim() } until ($scelta -in "1", "2")

    $Zip = [switch]($scelta -eq "2")
}

$radice = $PSScriptRoot
$Destinazione = [System.IO.Path]::GetFullPath($Destinazione)

# La versione esce dall'ultimo tag, come nel CI. Senza, l'eseguibile dice
# 1.0.0 e l'avviso di versione nuova scatta tutti i giorni.
if (-not $Versione) {
    # Solo i tag di versione (v4.15.0): quelli delle release di questa
    # copia in italiano hanno un altro nome e non sono un numero.
    $tag = git -C $radice describe --tags --abbrev=0 --match "v*"
    if ($LASTEXITCODE -ne 0 -or -not $tag) {
        throw "Non c'e' nessun tag da cui ricavare la versione. Passala con -Versione."
    }
    $Versione = $tag -replace '^v', ''
}

# La destinazione viene sostituita per intero, quindi si accetta solo
# una cartella che non esiste, che e' vuota o che e' gia' un pastepad.
# Con qualsiasi altra, un -Destinazione scritto male cancellerebbe cio'
# che c'e' dentro.
if (Test-Path $Destinazione) {
    $ceQualcosa = @(Get-ChildItem $Destinazione -Force).Count -gt 0
    $ePastepad = Test-Path (Join-Path $Destinazione "pastepad.exe")

    if ($ceQualcosa -and -not $ePastepad) {
        throw "$Destinazione contiene dei file e non e' una cartella di pastepad. Non si tocca."
    }
}

# Con il programma aperto i file sono bloccati e la pubblicazione resta a
# meta', che e' peggio di non cominciare.
$aperto = Get-Process pastepad -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -and $_.Path.StartsWith($Destinazione, [StringComparison]::OrdinalIgnoreCase) }

if ($aperto) {
    throw "pastepad e' aperto da $Destinazione. Chiudilo dall'area di notifica e rilancia questo script."
}

# Si pubblica in una cartella accanto e si scambia solo se e' andata
# bene. Pubblicando direttamente nella destinazione bisognava svuotarla
# prima, e una compilazione fallita lasciava senza programma: e'
# successo, per un file bloccato da un altro processo.
#
# La cartella nuova parte vuota, che serve anche per altro: dotnet
# publish non svuota la sua destinazione, e cio' che resta da prima
# finirebbe nel programma e dentro l'installer.
$nuova = "$Destinazione.nuova"

try {
    if (Test-Path $nuova) { Remove-Item $nuova -Recurse -Force }

    Write-Host "Pubblico pastepad $Versione..."

    dotnet publish (Join-Path $radice "csharp\Pastepad.App") `
        -c Release -p:Platform=x64 -r win-x64 `
        "-p:Version=$Versione" -o $nuova -nologo -v:minimal

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish e' fallito con $LASTEXITCODE. $Destinazione e' rimasta com'era."
    }

    if (-not (Test-Path (Join-Path $nuova "pastepad.exe"))) {
        throw "pastepad.exe non e' stato generato. $Destinazione e' rimasta com'era."
    }

    if (Test-Path $Destinazione) { Remove-Item $Destinazione -Recurse -Force }
    Move-Item $nuova $Destinazione
}
finally {
    if (Test-Path $nuova) { Remove-Item $nuova -Recurse -Force }

    # Il compilatore lascia in bin e obj un'altra copia intera dell'app
    # in Release, oltre 200 MB che non servono piu' a nessuno. Si tolgono
    # anche se la pubblicazione e' fallita. Le build di Debug non si
    # toccano: sono quelle di chi sviluppa.
    foreach ($progetto in "Pastepad.App", "Pastepad.Nucleo") {
        foreach ($cartella in "bin", "obj") {
            $residuo = Join-Path $radice "csharp\$progetto\$cartella\x64\Release"
            if (Test-Path $residuo) { Remove-Item $residuo -Recurse -Force -ErrorAction SilentlyContinue }
        }
    }
}

$file = @(Get-ChildItem $Destinazione -Recurse -File)
$mb = [math]::Round(($file | Measure-Object Length -Sum).Sum / 1MB, 1)

Write-Host ""
Write-Host "Fatto: $($file.Count) file, $mb MB"
Write-Host (Join-Path $Destinazione "pastepad.exe")

if ($Zip) {
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $dist = Join-Path $radice "dist"
    $archivio = Join-Path $dist "pastepad-$Versione-x64.zip"

    if (-not (Test-Path $dist)) { New-Item -ItemType Directory -Path $dist | Out-Null }
    if (Test-Path $archivio) { Remove-Item $archivio -Force }

    Write-Host ""
    Write-Host "Comprimo..."

    # Dentro lo zip tutto sta sotto una cartella "pastepad", e non sotto
    # il nome della destinazione ne' sciolto: chi lo estrae con "Estrai
    # qui" non si ritrova 500 file sparsi, e la cartella ha gia' il nome
    # giusto.
    $base = $Destinazione.TrimEnd('\') + '\'
    $scrittore = [System.IO.Compression.ZipFile]::Open($archivio, 'Create')
    try {
        foreach ($f in $file) {
            $voce = "pastepad/" + $f.FullName.Substring($base.Length).Replace('\', '/')
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $scrittore, $f.FullName, $voce, 'Optimal') | Out-Null
        }
    }
    finally {
        $scrittore.Dispose()
    }

    $zmb = [math]::Round((Get-Item $archivio).Length / 1MB, 1)
    Write-Host "Zip: $zmb MB"
    Write-Host $archivio
}
