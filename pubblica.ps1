# Compila pastepad in Release e lascia la cartella pronta all'uso in una
# destinazione.
#
#   .\pubblica.ps1
#   .\pubblica.ps1 -Destinazione "D:\programmi\pastepad"
#   .\pubblica.ps1 -Versione 4.15.0
#
# La destinazione di fabbrica e' lo stesso posto dove installa
# l'installer, e non e' un caso: cosi' l'avvio automatico punta a una
# cartella stabile e non a una build dentro il repo.
#
# Niente lettere accentate in questo file: Windows PowerShell 5.1 legge
# gli script senza BOM con la codifica di sistema e le rovinerebbe.

param(
    [string]$Destinazione = (Join-Path $env:LOCALAPPDATA "Programs\pastepad"),
    [string]$Versione
)

$ErrorActionPreference = "Stop"

$radice = $PSScriptRoot
$pubblicato = Join-Path $radice "pubblicato-x86"
$Destinazione = [System.IO.Path]::GetFullPath($Destinazione)

# La versione esce dall'ultimo tag, come nel CI. Senza, l'eseguibile dice
# 1.0.0 e l'avviso di versione nuova scatta tutti i giorni.
if (-not $Versione) {
    $tag = git -C $radice describe --tags --abbrev=0
    if ($LASTEXITCODE -ne 0 -or -not $tag) {
        throw "Non c'e' nessun tag da cui ricavare la versione. Passala con -Versione."
    }
    $Versione = $tag -replace '^v', ''
}

# La destinazione si svuota prima di copiare, quindi si accetta solo una
# cartella che non esiste, che e' vuota o che e' gia' un pastepad. Con
# qualsiasi altra, un -Destinazione scritto male cancellerebbe cio' che
# c'e' dentro.
if (Test-Path $Destinazione) {
    $ceQualcosa = @(Get-ChildItem $Destinazione -Force).Count -gt 0
    $ePastepad = Test-Path (Join-Path $Destinazione "pastepad.exe")

    if ($ceQualcosa -and -not $ePastepad) {
        throw "$Destinazione contiene dei file e non e' una cartella di pastepad. Non si tocca."
    }
}

# Con il programma aperto i file sono bloccati e la copia resta a meta',
# che e' peggio di non cominciare.
$aperto = Get-Process pastepad -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -and $_.Path.StartsWith($Destinazione, [StringComparison]::OrdinalIgnoreCase) }

if ($aperto) {
    throw "pastepad e' aperto da $Destinazione. Chiudilo dall'area di notifica e rilancia questo script."
}

# dotnet publish non svuota la cartella: cio' che resta da prima
# viaggerebbe fino alla destinazione.
if (Test-Path $pubblicato) {
    Remove-Item $pubblicato -Recurse -Force
}

Write-Host "Pubblico pastepad $Versione..."

dotnet publish (Join-Path $radice "csharp\Pastepad.App") `
    -c Release -p:Platform=x86 -r win-x86 `
    "-p:Version=$Versione" -o $pubblicato -nologo -v:minimal

if ($LASTEXITCODE -ne 0) { throw "dotnet publish e' fallito con $LASTEXITCODE" }

if (-not (Test-Path (Join-Path $pubblicato "pastepad.exe"))) {
    throw "pastepad.exe non e' stato generato in $pubblicato"
}

if (Test-Path $Destinazione) {
    Get-ChildItem $Destinazione -Force | Remove-Item -Recurse -Force
} else {
    New-Item -ItemType Directory -Path $Destinazione | Out-Null
}

Copy-Item (Join-Path $pubblicato "*") $Destinazione -Recurse -Force

$file = @(Get-ChildItem $Destinazione -Recurse -File)
$mb = [math]::Round(($file | Measure-Object Length -Sum).Sum / 1MB, 1)

Write-Host ""
Write-Host "Fatto: $($file.Count) file, $mb MB"
Write-Host (Join-Path $Destinazione "pastepad.exe")
