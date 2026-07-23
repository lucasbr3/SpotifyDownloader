; Inno Setup Script for Spotify Downloader
; Requires Inno Setup 6+ (https://jrsoftware.org/isdl.php)
; Compile: ISCC.exe installer.iss

#define MyAppName "Spotify Downloader"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Spotify Downloader"
#define MyAppURL "https://github.com/lucasbr3/SpotifyDownloader"
#define MyAppExeName "SpotifyDownloader.exe"

[Setup]
AppId={{7A8B9C0D-E1F2-3456-7890-ABCDEF123456}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=no
OutputDir=Installer
OutputBaseFilename=SpotifyDownloader_Setup_v{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=assets\icon.ico
UninstallDisplayIcon={app}\Assets\icon.ico
DisableProgramGroupPage=yes
CloseApplications=yes
RestartApplications=no
ShowLanguageDialog=yes
LanguageDetectionMethod=uilanguage
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: checkedonce

[Files]
Source: "Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: CheckDir
Source: "assets\icon.ico"; DestDir: "{app}\Assets"; Flags: ignoreversion
Source: "assets\icon.png"; DestDir: "{app}\Assets"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\Assets\icon.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; WorkingDir: "{app}"; IconFilename: "{app}\Assets\icon.ico"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: postinstall nowait skipifsilent

[UninstallRun]
Filename: "{cmd}"; Parameters: "/c taskkill /f /im SpotifyDownloader.exe 2>nul"; Flags: runhidden

[Code]
var
  DownloadPage: TDownloadWizardPage;
  FFmpegPath: string;

function CheckDir: Boolean;
begin
  Result := True;
end;

function GetFFmpegURL: string;
begin
  Result := 'https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip';
end;

procedure InitializeWizard;
begin
  DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), nil);
end;

function PrepareFFmpeg(Progress: string): Boolean;
var
  ZipPath: string;
  ExtractPath: string;
  FFmpegExe: string;
  FindRec: TFindRec;
begin
  Result := True;

  FFmpegPath := ExpandConstant('{app}\ffmpeg.exe');
  if FileExists(FFmpegPath) then
    Exit;

  WizardForm.CancelButton.Enabled := False;

  try
    ZipPath := ExpandConstant('{tmp}\ffmpeg.zip');
    ExtractPath := ExpandConstant('{tmp}\ffmpeg_extract');

    if not DownloadPage.Download(GetFFmpegURL, ZipPath, nil) then
    begin
      SuppressibleMsgBox('Failed to download FFmpeg. Audio conversion will not work.'#13#13 +
        'You can manually place ffmpeg.exe in the application folder.', mbError, MB_OK, IDOK);
      Exit;
    end;

    if not CreateDir(ExtractPath) then
    begin
      Exit;
    end;

    if not UnzipFiles(ZipPath, ExtractPath) then
    begin
      Exit;
    end;

    if FindFirst(ExtractPath + '\*', FindRec) then
    begin
      repeat
        if (FindRec.Name <> '.') and (FindRec.Name <> '..') and FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY > 0 then
        begin
          FFmpegExe := ExtractPath + '\' + FindRec.Name + '\bin\ffmpeg.exe';
          if FileExists(FFmpegExe) then
          begin
            FileCopy(FFmpegExe, FFmpegPath, False);
            Break;
          end;
        end;
      until not FindNext(FindRec);
      FindClose(FindRec);
    end;

    DeleteFile(ZipPath);
    DelTree(ExtractPath, True, True, True);
  except
    SuppressibleMsgBox('Error setting up FFmpeg. You can manually place ffmpeg.exe in the application folder.',
      mbError, MB_OK, IDOK);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    PrepareFFmpeg('Installing FFmpeg...');
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = wpReady then
  begin
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    DeleteFile(ExpandConstant('{app}\ffmpeg.exe'));
  end;
end;
