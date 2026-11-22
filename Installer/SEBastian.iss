; SEBастьян — установщик Safe Exam Browser с панелью преподавателя (Windows 10/11, x64).
; Собирается в GitHub Actions: iscc /DAppVersion=... /DBinRoot=... Installer\SEBastian.iss

#ifndef AppVersion
  #define AppVersion "3.10.3"
#endif
#ifndef BinRoot
  #define BinRoot ".."
#endif
#ifndef Config
  #define Config "x64\Release"
#endif

#define AppDir      BinRoot + "\SafeExamBrowser.Runtime\bin\" + Config
#define ConfigDir   BinRoot + "\SebWindowsConfig\bin\" + Config
#define ResetDir    BinRoot + "\SafeExamBrowser.ResetUtility\bin\" + Config
#define ServiceDir  BinRoot + "\SafeExamBrowser.Service\bin\" + Config

[Setup]
AppId={{6E3B1C8A-5E2D-4C7B-9A31-5EBA57A11A11}
AppName=SEBастьян — Safe Exam Browser
AppVersion={#AppVersion}
AppVerName=SEBастьян {#AppVersion} (бета)
AppPublisher=SEBастьян
AppPublisherURL=https://seb.korova.pro
AppSupportURL=https://seb.korova.pro
DefaultDirName={autopf}\SafeExamBrowser
DisableDirPage=yes
DefaultGroupName=SEBастьян
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17134
PrivilegesRequired=admin
WizardStyle=modern
WizardSizePercent=110
WizardImageFile=wizard.bmp,wizard-125.bmp,wizard-150.bmp,wizard-200.bmp
WizardSmallImageFile=small.bmp,small-125.bmp,small-150.bmp,small-200.bmp
WizardImageStretch=no
SetupIconFile=..\Setup\Resources\Application.ico
UninstallDisplayIcon={app}\Application\SafeExamBrowser.exe
UninstallDisplayName=SEBастьян — Safe Exam Browser
OutputDir=Output
OutputBaseFilename=SEBastian-win-setup
Compression=lzma2/ultra64
SolidCompression=yes
CloseApplications=force
RestartApplications=no
ChangesAssociations=yes
VersionInfoVersion={#AppVersion}
VersionInfoDescription=SEBастьян — установщик Safe Exam Browser

[Languages]
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"

[Messages]
ru.WelcomeLabel1=Привет! Это SEBастьян
ru.WelcomeLabel2=Сейчас поставим Safe Exam Browser с панелью преподавателя.%n%nУстановщик сам поставит всё нужное (библиотеки Visual C++ и службу SEB). Если на компьютере уже есть Safe Exam Browser — он будет заменён.%n%n⚠ Это бета-версия: используйте на свой страх и риск.%n%nЗакройте браузеры и нажмите «Далее».
ru.FinishedHeadingLabel=Готово!
ru.FinishedLabel=Safe Exam Browser установлен. Ярлык — на рабочем столе.%n%nЕсли SEB попросит ключ — получите его в Telegram-боте SEBастьян и вводите с включённым VPN (после входа VPN можно выключить). Что-то не так — https://seb.korova.pro

[Tasks]
Name: "desktopicon"; Description: "Ярлык на рабочем столе"; GroupDescription: "Дополнительно:"

[Files]
Source: "{#AppDir}\*";     DestDir: "{app}\Application";   Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"
Source: "{#ConfigDir}\*";  DestDir: "{app}\Configuration"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"
Source: "{#ResetDir}\*";   DestDir: "{app}\Reset";         Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"
Source: "{#ServiceDir}\*"; DestDir: "{app}\Service";       Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"
Source: "..\Setup\Resources\Application.ico";       DestDir: "{app}\Application"
Source: "..\Setup\Resources\ConfigurationFile.ico"; DestDir: "{app}\Application"
Source: "vc_redist.x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: NeedsVCRedist

[Icons]
Name: "{autoprograms}\SEBастьян\Safe Exam Browser"; Filename: "{app}\Application\SafeExamBrowser.exe"; WorkingDir: "{app}\Application"; IconFilename: "{app}\Application\Application.ico"
Name: "{autoprograms}\SEBастьян\Сброс SEB (если что-то сломалось)"; Filename: "{app}\Reset\SafeExamBrowser.ResetUtility.exe"; WorkingDir: "{app}\Reset"
Name: "{autoprograms}\SEBастьян\Сайт и помощь"; Filename: "https://seb.korova.pro"
Name: "{autodesktop}\Safe Exam Browser"; Filename: "{app}\Application\SafeExamBrowser.exe"; WorkingDir: "{app}\Application"; IconFilename: "{app}\Application\Application.ico"; Tasks: desktopicon

[Registry]
; .seb-файлы и ссылки seb:// / sebs:// открываются в SEB (как в официальном установщике)
Root: HKA; Subkey: "Software\Classes\.seb"; ValueType: string; ValueName: ""; ValueData: "SafeExamBrowser.ConfigurationFile"; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\.seb"; ValueType: string; ValueName: "Content Type"; ValueData: "application/seb"; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\SafeExamBrowser.ConfigurationFile"; ValueType: string; ValueName: ""; ValueData: "SEB Configuration File"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SafeExamBrowser.ConfigurationFile\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\Application\ConfigurationFile.ico"
Root: HKA; Subkey: "Software\Classes\SafeExamBrowser.ConfigurationFile\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Application\SafeExamBrowser.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\seb"; ValueType: string; ValueName: ""; ValueData: "URL:Safe Exam Browser Protocol"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\seb"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\seb\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\Application\Application.ico"
Root: HKA; Subkey: "Software\Classes\seb\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Application\SafeExamBrowser.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\sebs"; ValueType: string; ValueName: ""; ValueData: "URL:Safe Exam Browser Secure Protocol"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\sebs"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\sebs\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\Application\Application.ico"
Root: HKA; Subkey: "Software\Classes\sebs\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Application\SafeExamBrowser.exe"" ""%1"""

[Run]
Filename: "{tmp}\vc_redist.x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Ставлю библиотеки Visual C++…"; Flags: waituntilterminated; Check: NeedsVCRedist
Filename: "{sys}\sc.exe"; Parameters: "create SafeExamBrowser binPath= ""\""{app}\Service\SafeExamBrowser.Service.exe\"""" start= auto DisplayName= ""Safe Exam Browser Service"""; Flags: runhidden waituntilterminated; StatusMsg: "Регистрирую службу SEB…"
Filename: "{sys}\sc.exe"; Parameters: "config SafeExamBrowser binPath= ""\""{app}\Service\SafeExamBrowser.Service.exe\"""" start= auto"; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "description SafeExamBrowser ""Performs operations which require elevated privileges."""; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "failure SafeExamBrowser reset= 86400 actions= restart/5000/restart/5000/restart/5000"; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "start SafeExamBrowser"; Flags: runhidden waituntilterminated; StatusMsg: "Запускаю службу SEB…"
Filename: "{app}\Application\SafeExamBrowser.exe"; Description: "Запустить Safe Exam Browser"; WorkingDir: "{app}\Application"; Flags: postinstall nowait skipifsilent unchecked

[UninstallRun]
Filename: "{sys}\sc.exe"; Parameters: "stop SafeExamBrowser"; Flags: runhidden waituntilterminated; RunOnceId: "StopService"
Filename: "{sys}\timeout.exe"; Parameters: "/t 3 /nobreak"; Flags: runhidden waituntilterminated; RunOnceId: "WaitService"
Filename: "{sys}\sc.exe"; Parameters: "delete SafeExamBrowser"; Flags: runhidden waituntilterminated; RunOnceId: "DeleteService"

[UninstallDelete]
Type: filesandordirs; Name: "{app}\Application"
Type: filesandordirs; Name: "{app}\Service"

[Code]
const
  NET48_RELEASE = 528040;

function NeedsVCRedist: Boolean;
var
  Installed, Major, Minor: Cardinal;
begin
  Result := True;
  if RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Installed', Installed) and (Installed = 1) then
    if RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Major', Major) and
       RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Minor', Minor) then
      Result := not ((Major > 14) or ((Major = 14) and (Minor >= 30)));
end;

function InitializeSetup: Boolean;
var
  Release: Cardinal;
  ErrorCode: Integer;
begin
  Result := True;
  if not (RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= NET48_RELEASE)) then
  begin
    if MsgBox('Для SEB нужен .NET Framework 4.8 (обычно он уже есть в Windows 10/11).' + #13#10#13#10 +
              'Открыть страницу загрузки .NET Framework 4.8? После его установки запустите этот установщик снова.',
              mbConfirmation, MB_YESNO) = IDYES then
      ShellExecAsOriginalUser('open', 'https://dotnet.microsoft.com/download/dotnet-framework/net48', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    Result := False;
  end;
end;

procedure RunHidden(const FileName, Params: String);
var
  Code: Integer;
begin
  Exec(FileName, Params, '', SW_HIDE, ewWaitUntilTerminated, Code);
end;

{ Удаляет ранее установленный официальный Safe Exam Browser (MSI), чтобы не было двух копий и двух служб. }
procedure RemoveOfficialSEB;
var
  Keys: TArrayOfString;
  I: Integer;
  Base, Name, Publisher, Uninstall, Guid: String;
begin
  Base := 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall';
  if not RegGetSubkeyNames(HKLM64, Base, Keys) then
    Exit;
  for I := 0 to GetArrayLength(Keys) - 1 do
  begin
    Name := '';
    Publisher := '';
    Uninstall := '';
    RegQueryStringValue(HKLM64, Base + '\' + Keys[I], 'DisplayName', Name);
    RegQueryStringValue(HKLM64, Base + '\' + Keys[I], 'Publisher', Publisher);
    RegQueryStringValue(HKLM64, Base + '\' + Keys[I], 'UninstallString', Uninstall);
    if (Pos('Safe Exam Browser', Name) = 1) and (Pos('ETH', Publisher) > 0) and (Pos('MsiExec', Uninstall) > 0) and (Pos('{', Keys[I]) = 1) then
    begin
      Guid := Keys[I];
      Log('Removing official SEB: ' + Name + ' ' + Guid);
      WizardForm.StatusLabel.Caption := 'Удаляю прежний Safe Exam Browser…';
      RunHidden(ExpandConstant('{sys}\msiexec.exe'), '/x ' + Guid + ' /qn /norestart');
    end;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  { Закрываем SEB и останавливаем службу, чтобы файлы можно было заменить }
  RunHidden(ExpandConstant('{sys}\taskkill.exe'), '/F /IM SafeExamBrowser.Client.exe');
  RunHidden(ExpandConstant('{sys}\taskkill.exe'), '/F /IM SafeExamBrowser.exe');
  RunHidden(ExpandConstant('{sys}\sc.exe'), 'stop SafeExamBrowser');
  Sleep(1500);
  RemoveOfficialSEB;
  RunHidden(ExpandConstant('{sys}\sc.exe'), 'stop SafeExamBrowser');
  RunHidden(ExpandConstant('{sys}\taskkill.exe'), '/F /IM SafeExamBrowser.Service.exe');
  Sleep(500);
end;
