; Instalador de Windows de "Guardianes del Bosque" (Inno Setup 6).
; 1. Compila el juego en Unity: Guardianes del Bosque > Compilar para Windows  (-> ..\Compilacion\Windows)
; 2. Compila este script con Inno Setup (ISCC.exe GuardianesDelBosque.iss)        (-> ..\Instalador)
; Se instala sin pedir permisos de administrador (en la carpeta de programas del usuario),
; con accesos directos en el menú Inicio y, si se quiere, en el escritorio, y con desinstalador.

#define Nombre "Guardianes del Bosque"
#define Version "1.0"
#define Empresa "UNAD"
#define Exe "GuardianesDelBosque.exe"
#define Juego "..\Compilacion\Windows"

[Setup]
AppId={{6F1C2B7E-3A4D-4E8B-9C21-5D7A0B3E9F14}
AppName={#Nombre}
AppVersion={#Version}
AppVerName={#Nombre} {#Version}
AppPublisher={#Empresa}
DefaultDirName={autopf}\{#Nombre}
DefaultGroupName={#Nombre}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\Instalador
OutputBaseFilename=Instalar_GuardianesDelBosque_{#Version}
SetupIconFile=icono.ico
UninstallDisplayIcon={app}\{#Exe}
UninstallDisplayName={#Nombre}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "escritorio"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; todo el juego compilado, menos la carpeta de depuración que Unity pide no distribuir
Source: "{#Juego}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; \
  Excludes: "\GuardianesDelBosque_BackUpThisFolder_ButDontShipItWithYourGame,\GuardianesDelBosque_BurstDebugInformation_DoNotShip"

[Icons]
Name: "{group}\{#Nombre}"; Filename: "{app}\{#Exe}"
Name: "{group}\{cm:UninstallProgram,{#Nombre}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#Nombre}"; Filename: "{app}\{#Exe}"; Tasks: escritorio

[Run]
Filename: "{app}\{#Exe}"; Description: "{cm:LaunchProgram,{#Nombre}}"; Flags: nowait postinstall skipifsilent
