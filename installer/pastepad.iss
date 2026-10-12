; Installer di pastepad. Si compila con ISCC:
;
;   ISCC /DVersion=4.0.0 /DPublish=<cartella di publish> pastepad.iss
;
; Il programma va in %LOCALAPPDATA%\Programs\pastepad e i dati restano
; in %LOCALAPPDATA%\pastepad. Questa separazione non è cosmetica: prima
; il programma si copiava DENTRO la cartella dei dati, e per questo il
; disinstallatore doveva chiedere "elimino anche i tuoi dati?".
; Separati, disinstallare non può toccare la cronologia nemmeno volendo
; —non perché si ricordi di chiedere, ma perché {app} e la cartella dei
; dati non sono più la stessa.

#ifndef Version
  #define Version "0.0.0"
#endif

; La cartella che lascia
;   dotnet publish csharp/Pastepad.App -c Release -p:Platform=x64 \
;       -r win-x64 -p:Version=<versione> -o pubblicato-x64
#ifndef Publish
  #define Publish "..\pubblicato-x64"
#endif

; Dove Inno annota ciò che è installato. È la stessa GUID dell'AppId qui
; sotto con il suffisso che le mette Inno, e va in HKCU e non in HKLM
; perché l'installer è PrivilegesRequired=lowest. Verificato leggendo il
; registro di una macchina con pastepad installato: DisplayVersion=4.3.0
; in HKCU\...\Uninstall\{DF56...}_is1.
;
; Si scrive a parte e non si ricava dall'AppId perché l'AppId ha la
; graffa raddoppiata ({{) per fare l'escape della costante, e in quella
; forma non vale come percorso.
#define ChiaveInstallato \
  "Software\Microsoft\Windows\CurrentVersion\Uninstall\{DF56167A-B0A2-4B5B-AF7F-91DF597A67C9}_is1"

[Setup]
; Fisso e per sempre: è ciò che fa sì che reinstallare sostituisca
; invece di duplicare, e ciò che identifica la voce di Installazione
; applicazioni. Se cambia, Windows si trova due pastepad installati.
AppId={{DF56167A-B0A2-4B5B-AF7F-91DF597A67C9}

AppName=pastepad
AppVersion={#Version}
AppVerName=pastepad {#Version}
VersionInfoVersion={#Version}
; L'utente di GitHub, che è già pubblico, e non il nome reale. Questo
; campo è quello che Windows mostra come «Autore» in Installazione
; applicazioni e nell'avviso di SmartScreen, quindi si vede.
AppPublisher=Josemgu
AppPublisherURL=https://github.com/Josemgu/pastepad
AppSupportURL=https://github.com/Josemgu/pastepad/issues
AppUpdatesURL=https://github.com/Josemgu/pastepad/releases

; Senza amministratore. Con questo {autopf} si risolve in
; %LOCALAPPDATA%\Programs e non in Programmi, che è dove Windows blocca
; la scrittura senza avvisare.
PrivilegesRequired=lowest
DefaultDirName={autopf}\pastepad
DefaultGroupName=pastepad
DisableProgramGroupPage=yes
DisableDirPage=auto

; Il programma è a 64 bit, quindi l'installer si rifiuta di partire su
; un Windows a 32 bit, dove l'eseguibile non girerebbe. x64compatible
; comprende anche ARM64, che esegue x64 in emulazione.
ArchitecturesAllowed=x64compatible

; Senza ArchitecturesInstallIn64BitMode apposta: l'installer continua a
; girare in modalità a 32 bit, che è ciò che fa sì che {autopf} e il
; ramo del registro puntino allo stesso posto di prima. Cambiarlo
; sposterebbe la chiave di disinstallazione e un aggiornamento non
; riconoscerebbe più la copia già installata.

LicenseFile=..\LICENSE
SetupIconFile=..\csharp\Pastepad.App\Assets\AppIcon.ico
UninstallDisplayIcon={app}\pastepad.exe
UninstallDisplayName=pastepad

OutputDir=..\dist
OutputBaseFilename=pastepad-{#Version}-installer

; lzma2/ultra64 con dizionario grande: sono 476 file e quasi 200 MB di
; runtime che si ripetono molto tra loro. Se ISCC resta senza memoria
; —la documentazione chiede circa 742 MB per ultra64— scendere a
; lzma2/max: costa qualche MB di download, non il piano.
Compression=lzma2/ultra64
SolidCompression=yes

WizardStyle=modern

; Chiude pastepad prima di sovrascriverlo. Senza, reinstallare sopra con
; il programma aperto fallisce per file bloccato. Inno usa per questo il
; Restart Manager di Windows.
CloseApplications=yes

; E lo riapre dopo. Questo è rimasto a "no" fino alla 4.1.0 perché a
; "yes" non serviva a niente: la condizione che pone la documentazione
; di Inno è che "the application needs to be using the Windows
; RegisterApplicationRestart API function", e pastepad non la chiamava.
; Misurato con la stessa API del Restart Manager: bRestartable=False.
;
; Dalla 4.2.0 la chiama —Sistema/Chiusura.cs—, quindi aggiornare sopra
; lascia pastepad aperto e con la sua scorciatoia impostata, invece che
; installato e chiuso. Conta di più nell'aggiornamento silenzioso, che è
; quello che nessuno sta guardando quando avviene.
RestartApplications=yes

[Languages]
Name: "it"; MessagesFile: "compiler:Languages\Italian.isl"

[Tasks]
Name: "desktop"; Description: "Crea un collegamento sul desktop"; Flags: unchecked

[Files]
Source: "{#Publish}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\pastepad"; Filename: "{app}\pastepad.exe"; Comment: "Gestore di appunti"
Name: "{autodesktop}\pastepad"; Filename: "{app}\pastepad.exe"; Tasks: desktop

; Senza sezione [Registry], ed è la decisione, non una dimenticanza:
; dell'avvio con Windows si occupa l'applicazione, che lo applica a ogni
; avvio secondo la preferenza "avvio_automatico" di config.json. Con due
; padroni dello stesso valore, uno lo scrive e l'altro lo cancella — e
; disinstallare si portava via una preferenza che l'utente aveva scelto.
;
; Effetto collaterale buono: chi disinstalla e reinstalla conserva la
; sua preferenza in config.json e l'applicazione la riapplica da sola.

[Run]
; Se l'abbiamo chiuso noi per poter aggiornare, si riapre senza chiedere
; e anche in installazione silenziosa: l'utente non l'ha chiuso, e ciò
; che ha chiesto è aggiornare. Questo sostituisce RestartApplications
; per questo caso, perché avendolo chiuso prima il Restart Manager non
; lo vede più in uso e non ha nessuno da riaprire.
Filename: "{app}\pastepad.exe"; Flags: nowait; Check: VaRiaperto

; E quello di sempre —con la sua casella— solo quando NON lo stavamo
; usando: offrire "apri pastepad" a chi l'aveva aperto dieci secondi fa
; non ha senso.
Filename: "{app}\pastepad.exe"; Description: "Apri pastepad"; \
    Flags: nowait postinstall skipifsilent; Check: NonSiEChiusoDaSolo

; Il registro di riserva che l'applicazione scrive accanto al suo
; eseguibile. Non lo installa l'installer —lo crea il programma
; all'avvio—, quindi Inno non lo elimina da solo e la cartella
; sopravviveva alla disinstallazione con un file dentro. Lo trovò il qa.
;
; Quel log esiste perché ci furono avvii che non lasciavano nemmeno una
; riga nella cartella dei dati; scrivere anche qui è ciò che garantisce
; che resti traccia. Disinstallando non serve più.
[UninstallDelete]
Type: files; Name: "{app}\errori.log"

; Niente [UninstallDelete] sulla cartella dei DATI, e non è una
; dimenticanza. %LOCALAPPDATA%\pastepad —cronologia, testi salvati e
; immagini— è dell'utente e non l'ho installata io, quindi non tocca a
; me eliminarla. Disinstallare si porta via {app} e nient'altro: nemmeno
; la voce di avvio, perché la gestisce l'applicazione secondo la
; preferenza dell'utente.

; ---------------------------------------------------------------------
; Riconoscere ciò che è già installato prima di installare qualcosa
; ---------------------------------------------------------------------
;
; Inno sostituiva già l'installazione precedente —per questo c'è l'AppId
; fisso—, ma non lo diceva: l'installer della 4.4.0 sopra una 4.3.0 si
; vedeva esattamente come un'installazione nuova. Chi lo esegue non sa
; se sta aggiornando o duplicando, né se i suoi testi salvati
; sopravvivono, e la risposta alla seconda domanda —che sì— è proprio
; quella da dare prima e non dopo.
;
; **[Code] va per ultima, e non è estetica.** Da qui in poi il file è
; Pascal, dove ";" non apre un commento ma chiude un'istruzione. Un
; blocco di commenti come quelli sopra messo dopo questa riga si compila
; come codice: "'BEGIN' expected. Compile aborted." È successo
; esattamente questo.
[Code]

const
  { Riga vuota dentro l'avviso. #13#10 e non #13 da solo: la finestra
    di messaggio è di Windows. }
  Acapo = #13#10#13#10;

  { La finestra di ascolto della chiusura di pastepad. È di primo livello
    apposta —una finestra solo-messaggi «cannot be enumerated»— e per
    questo la si può trovare da qui. Ricevendo WM_CLOSE riversa i dati e
    termina, che è la stessa porta che usa Windows spegnendosi. }
  ClasseChiusura = 'pastepad_chiusura_3ff1c0de';
  WM_CLOSE = $0010;

var
  Installata: string;
  EUnAggiornamento: Boolean;
  EraInEsecuzione: Boolean;

{ Confronta due versioni. Non si usa il segno restituito da
  ComparePackedVersion perché la sua documentazione dice che restituisce
  un Integer senza dire in che ordine; gli Int64 impacchettati invece si
  possono confrontare direttamente, che è il motivo per cui si
  impacchettano.

  Restituisce False se una delle due non si riesce a leggere, e allora
  chi chiama deve accontentarsi di sapere che c'è qualcosa installato. }
function Confronta(const a, b: string; var uscita: Integer): Boolean;
var
  va, vb: Int64;
begin
  Result := StrToVersion(a, va) and StrToVersion(b, vb);
  if not Result then exit;

  if va = vb then uscita := 0
  else if va < vb then uscita := -1
  else uscita := 1;
end;

function InitializeSetup(): Boolean;
var
  cmp: Integer;
begin
  Result := True;
  EUnAggiornamento := False;

  if not RegQueryStringValue(HKEY_CURRENT_USER, '{#ChiaveInstallato}',
                             'DisplayVersion', Installata) then
    exit;

  EUnAggiornamento := True;

  { In installazione silenziosa non si chiede niente. Un MsgBox qui
    resta ad aspettare qualcuno che non sta guardando, e l'installer non
    restituisce mai il controllo — un errore che questo progetto ha già
    incontrato per un'altra strada. }
  if WizardSilent() then exit;

  { I letterali si uniscono con + e non mettendoli uno dopo l'altro:
    Pascal lo ammette, ma Pascal Script non è Delphi e non serve
    scoprirlo a proprie spese nella compilazione di una release. }
  if not Confronta(Installata, '{#Version}', cmp) then
  begin
    Result := MsgBox(
      'C''è già una versione di pastepad installata.' + Acapo +
      'Verrà sostituita dalla {#Version}. La tua cronologia e i tuoi ' +
      'testi salvati non si toccano: vivono in un''altra cartella.' + Acapo +
      'Continuare?', mbConfirmation, MB_YESNO) = IDYES;
    exit;
  end;

  if cmp = 0 then
  begin
    Result := MsgBox(
      'Hai già pastepad {#Version}, che è proprio quella di questo ' +
      'installer.' + Acapo +
      'Puoi reinstallarla sopra se qualcosa non va. La tua cronologia e ' +
      'i tuoi testi salvati non si toccano.' + Acapo +
      'Reinstallare?', mbConfirmation, MB_YESNO) = IDYES;
    exit;
  end;

  if cmp < 0 then
  begin
    Result := MsgBox(
      'Hai pastepad ' + Installata + ' e questo installer porta la ' +
      '{#Version}.' + Acapo +
      'Verrà AGGIORNATO: si sostituisce il programma e si conserva tutto ' +
      'ciò che è tuo —cronologia, testi salvati, scorciatoia e ' +
      'preferenze—, che vive in un''altra cartella. Se pastepad è ' +
      'aperto, si chiude e si riapre da solo.' + Acapo +
      'Aggiornare adesso?', mbConfirmation, MB_YESNO) = IDYES;
    exit;
  end;

  { Installare una versione più vecchia sopra una più nuova non si
    vieta —a volte è proprio ciò che si vuole quando la nuova esce male—
    ma non può passare senza dirlo: dall'installer le due si vedono
    uguali. }
  Result := MsgBox(
    'Hai pastepad ' + Installata + ', che è PIÙ NUOVA della ' +
    '{#Version} di questo installer.' + Acapo +
    'Se continui, resterai con una versione precedente.' + Acapo +
    'Proseguire comunque?', mbConfirmation, MB_YESNO) = IDYES;
end;

{ E che lo dica anche la pagina di conferma, non solo l'avviso
  dell'inizio: chi arriva fin lì deve continuare a vedere che questo
  sostituisce e non duplica. }
procedure CurPageChanged(IdPagina: Integer);
begin
  if (IdPagina = wpReady) and EUnAggiornamento then
    WizardForm.NextButton.Caption := '&Aggiorna';
end;

{ Chiude la copia in esecuzione, e aspetta che se ne vada davvero.
  Restituisce True solo se era in esecuzione E si è chiusa. }
function ChiudiQuellaInEsecuzione(): Boolean;
var
  v: HWND;
  i: Integer;
  inviato: Boolean;
begin
  Result := False;

  v := FindWindowByClassName(ClasseChiusura);
  if v = 0 then exit;

  { WM_CLOSE e non uccidere il processo: così riversa ciò che non ha
    ancora salvato —la cronologia si scrive ogni pochi secondi, non a
    ogni copia—, rilascia la scorciatoia globale e si toglie dall'area
    di notifica. Ucciderlo si porterebbe via fino a tre secondi di copie
    e lascerebbe l'icona morta accanto all'orologio finché non ci si
    passa sopra il mouse. }
  { Il risultato si raccoglie in una variabile invece di scartarlo:
    Pascal Script non è Delphi e non serve scoprire a proprie spese se
    ammette di chiamare una funzione come se fosse una procedura. }
  inviato := PostMessage(v, WM_CLOSE, 0, 0);
  if not inviato then exit;

  { Fino a 10 s. Si aspetta che la FINESTRA sparisca, che è il segnale
    che ha finito davvero e ha rilasciato i file; proseguendo senza
    aspettare, l'installer troverebbe l'eseguibile in uso e
    ricomparirebbe la pagina che questo vuole evitare. }
  for i := 1 to 100 do
  begin
    Sleep(100);
    if FindWindowByClassName(ClasseChiusura) = 0 then
    begin
      Result := True;
      exit;
    end;
  end;
end;

{ Qui, e non in PrepareToInstall, perché deve succedere PRIMA che Setup
  guardi quali file sono in uso. Se a quel punto pastepad si è già
  chiuso, non c'è niente in uso e la pagina «Preparazione
  dell'installazione» —quella che chiede se può chiudere le
  applicazioni— non arriva a comparire.

  Che comparisse quella pagina non era solo brutto: lasciava in mano
  all'utente qualcosa che ha una sola risposta giusta, e se sceglieva
  «Non chiudere le applicazioni» i file si sostituivano con la copia
  vecchia ancora viva. Installare sopra restava diverso da installare
  pulito, che è ciò che si notò usandolo. }
function NextButtonClick(IdPagina: Integer): Boolean;
begin
  Result := True;

  if IdPagina = wpReady then
    EraInEsecuzione := ChiudiQuellaInEsecuzione();
end;

{ Per il [Run] più sopra: se l'abbiamo chiuso noi, lo riapriamo noi e
  senza chiedere. L'utente non l'ha chiuso, e ciò che ha chiesto è
  aggiornare, non restare senza il programma. }
function VaRiaperto(): Boolean;
begin
  Result := EraInEsecuzione;
end;

function NonSiEChiusoDaSolo(): Boolean;
begin
  Result := not EraInEsecuzione;
end;
