# Meshwork for Windows Installer
# Cobbled together by Eric Butler <eric@extremeboredom.net>

!include 'MUI2.nsh'
!include 'Sections.nsh'
!include 'LogicLib.nsh'
!include 'WordFunc.nsh'

Var STARTMENU_FOLDER

!define PRODUCT "Meshwork"

!define DOTNET_URL "http://www.microsoft.com/downloads/info.aspx?na=90&p=&SrcDisplayLang=en&SrcCategoryId=&SrcFamilyId=333325fd-ae52-4e35-b531-508d977d32a6&u=http%3a%2f%2fdownload.microsoft.com%2fdownload%2f7%2f0%2f3%2f703455ee-a747-4cc8-bd3e-98a615c3aedb%2fdotNetFx35setup.exe"
!define DOTNET_FILE "dotNetFx35setup.exe"
!define DOTNET_VERSION "3.5.30729.4926"
!define GTKSHARP_URL "http://ftp.novell.com/pub/mono/gtk-sharp/gtk-sharp-2.12.9-2.win32.msi"
!define GTKSHARP_FILE "gtk-sharp-2.12.9-2.win32.msi"
!define GTKSHARP_VERSION "2.12.9"

!define MUI_WELCOMEFINISHPAGE_BITMAP  "Resources\Images\install_side.bmp"
!define MUI_WELCOMEFINISHPAGE_BITMAP_NOSTRETCH
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_BITMAP "Resources\Images\install_header.bmp"
!define MUI_HEADERIMAGE_BITMAP_NOSTRETCH
!define MUI_HEADERIMAGE_RIGHT

SetCompressor LZMA
Name "${PRODUCT}"
OutFile "MeshworkSetup.exe"
InstallDir "$PROGRAMFILES\${PRODUCT}"
InstallDirRegKey HKCU "Software\${PRODUCT}" ""
LicenseData "COPYING"
BrandingText "Copyright (C) 2003-2009 Eric Butler"

!define MUI_ABORTWARNING
!define MUI_COMPONENTSPAGE_NODESC
!define MUI_STARTMENUPAGE_REGISTRY_ROOT "HKCU"
!define MUI_STARTMENUPAGE_REGISTRY_KEY "Software\${PRODUCT}"
!define MUI_STARTMENUPAGE_REGISTRY_VALUENAME "Start Menu Folder"

# FIXME: Temporarily disabled. Environment PATH needs to be updated to include new GTK# directory or Meshwork won't start. Apparently the way this gets executed, this doesn't happen.
# !define MUI_FINISHPAGE_RUN "$PROGRAMFILES\${PRODUCT}\FileFind.Meshwork.GtkClient.exe"

# !define MUI_FINISHPAGE_SHOWREADME "$PROGRAMFILES\${PRODUCT}\Changelog.txt"
# !define MUI_FINISHPAGE_SHOWREADME_TEXT "View release notes"

!define MUI_FINISHPAGE_NOAUTOCLOSE

!define MUI_FINISHPAGE_TEXT_LARGE
!define MUI_FINISHPAGE_TEXT "Meshwork has been installed on your computer.$\r$\n$\r$\nClick Finish to close this wizard."

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "COPYING"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_STARTMENU Application $STARTMENU_FOLDER
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_LANGUAGE "English"

Section "!.NET Framework v3.5 (Required)" SecFramework
	SectionIn 1 RO
	Call ConnectInternet
	StrCpy $2 "$TEMP\${DOTNET_FILE}"
	NSISdl::download ${DOTNET_URL} ${DOTNET_FILE}
	Pop $0
	StrCmp $0 success success
	SetDetailsView show
	DetailPrint ".NET download failed: $0"
	MessageBox MB_OK "The Microsoft .NET Framework failed to download: $\n $0 $\n Please check your internet connection and/or install the .NET Framework manually, then run Meshwork setup again."
	Quit
	success:
		DetailPrint "Installing the Microsoft .NET Framework..."
		ExecWait '"$2" /Q'
		IfErrors die
		DetailPrint "Microsoft .NET Framework install complete."
		Goto continue
	die:
		MessageBox MB_OK|MB_ICONEXCLAMATION "The Microsoft .NET Framework failed to install. Try downloading/installing .NET manually, then run Meshwork setup again."
		SetDetailsView show
		Quit
	continue:
SectionEnd

Section "!GTK# for .NET v${GTKSHARP_VERSION} (Required)" SecGtkSharp
	SectionIn 1 RO
	Call ConnectInternet
	StrCpy $2 "$TEMP\${GTKSHARP_FILE}"
	NSISdl::download ${GTKSHARP_URL} $2
	Pop $0
	Strcmp $0 success success
	SetDetailsView show
	DetailPrint "GTK# download failed: $0"
	MessageBox MB_OK|MB_ICONEXCLAMATION "GTK# failed to download: $\n $0 $\n Please check your internet connection and/or install GTK# manually, then run Meshwork setup again."
	Quit
	success:
		ClearErrors
		DetailPrint "Installing GTK#..."
		ExecWait 'msiexec /i "$2" /quiet'
    		IfErrors die
		DetailPrint "GTK# Install Complete"
		Goto continue
	die:
		SetDetailsView show
		MessageBox MB_OK|MB_ICONEXCLAMATION "GTK# failed to install. Try downloading/installing GTK# manually, then run Meshwork setup again."
		Quit
	continue:
SectionEnd

Section "!Meshwork Program Files" secProgFiles
	SectionIn 1 RO
	SetCompress Auto
	SetOverwrite IfNewer
	SetOutPath "$INSTDIR"

	File "Build\FileFind.Meshwork.GtkClient.exe"
	File "Build\Filefind.Meshwork.GtkClient.pdb"
	File "Build\FileFind.Meshwork.dll"
	File "Build\FileFind.Meshwork.pdb"
	File "Build\DiffieHellman.dll"
	File "Build\gtkspell-sharp.dll"
	File "Build\Mono.Data.dll"
	File "Build\Mono.Data.Sqlite.dll"
	File "Build\Mono.Data.SqliteClient.dll"
	File "Build\Mono.GetOptions.dll"
	File "Build\MonoTorrent.dll"
	File "Build\sqlite3.dll"
	File "Build\ige-mac-integration-sharp.dll"

	WriteRegStr HKCU "Software\${PRODUCT}" "" $INSTDIR
	WriteUninstaller "$INSTDIR\Uninstall.exe"

	!insertmacro MUI_STARTMENU_WRITE_BEGIN Application
	CreateDirectory "$SMPROGRAMS\${PRODUCT}"
	CreateShortCut "$SMPROGRAMS\${PRODUCT}\Meshwork.lnk" "$INSTDIR\FileFind.Meshwork.GtkClient.exe" "" "$INSTDIR\FileFind.Meshwork.GtkClient.exe" 0
	CreateShortCut "$SMPROGRAMS\${PRODUCT}\Uninstall.lnk" "$INSTDIR\Uninstall.exe" "" "$INSTDIR\Uninstall.exe" 0
;	CreateShortCut "$SMPROGRAMS\${PRODUCT}\Update Meshwork.lnk" "$INSTDIR\AutoUpdate.exe" "" "$INSTDIR\AutoUpdate.exe" 0
	!insertmacro MUI_STARTMENU_WRITE_END
SectionEnd


;SubSection /e "Plugins"
;	Section "I2P Plugin"
;		SetOverwrite on
;		SetOutPath "$INSTDIR\Plugins"
;		File "I2P.dll"
;	SectionEnd
;SubSectionEnd

Section "Desktop Icon" SecDesktopIcon
	CreateShortCut "$DESKTOP\Meshwork.lnk" "$INSTDIR\FileFind.Meshwork.GtkClient.exe" "" "$INSTDIR\FileFind.Meshwork.GtkClient.exe" 0
SectionEnd

Section "Uninstall"
	Delete "$INSTDIR\FileFind.Meshwork.GtkClient.exe"
	Delete "$INSTDIR\Filefind.Meshwork.GtkClient.pdb"
	Delete "$INSTDIR\FileFind.Meshwork.dll"
	Delete "$INSTDIR\FileFind.Meshwork.pdb"
	Delete "$INSTDIR\DiffieHellman.dll"
	Delete "$INSTDIR\FileFind.Stun.dll"
	Delete "$INSTDIR\gtkspell-sharp.dll"
	Delete "$INSTDIR\Mono.Data.dll"
	Delete "$INSTDIR\Mono.Data.Sqlite.dll"
	Delete "$INSTDIR\Mono.Data.SqliteClient.dll"
	Delete "$INSTDIR\Mono.GetOptions.dll"
	Delete "$INSTDIR\MonoTorrent.dll"
	Delete "$INSTDIR\sqlite3.dll"
	Delete "$INSTDIR\ige-mac-integration-sharp.dll"
	RMDir "$INSTDIR"

	Delete $SMPROGRAMS\${PRODUCT}\Meshwork.lnk
	Delete $SMPROGRAMS\${PRODUCT}\Uninstall.lnk
	RMDir $SMPROGRAMS\${PRODUCT}\

	Delete $DESKTOP\Meshwork.lnk

	DeleteRegKey /ifempty HKCU "Software\${PRODUCT}"
SectionEnd

Function .onInit
	Call IsDotNET3Point5Installed
	Pop $R3
	StrCmp $R3 0 NoDotNet
	StrCmp $R3 1 GoodDotNet
	StrCmp $R3 2 OldDotNet
	GoodDotNet:
	SectionSetFlags ${SecFramework} 16
	SectionSetText ${SecFramework} ".NET Framework v3.5 (Already Installed)"
	Goto Continue
	NoDotNet:
	Goto Continue
	OldDotNet:
	SectionSetText ${SecFramework} ".NET Framework (Needs Upgrade)"
	Continue:

	Call IsGtkSharpInstalled
	Pop $R4
	StrCmp $R4 0 NoGtkSharp
	SectionSetFlags ${SecGtkSharp} 16
	SectionSetText ${SecGtkSharp} "GTK# for .NET v${GTKSHARP_VERSION} (Already Installed)"
	NoGtkSharp:
FunctionEnd

Function .onInstSuccess
	Delete "$TEMP\${DOTNET_FILE}"
	Delete "$TEMP\${GTKSHARP_FILE}"
FunctionEnd

Function un.onInit
FunctionEnd

;--------------------------------------
;--------------------------------------

; 0 = .NET 3.5 not found
; 1 = .NET 3.5 found
; 2 = .NET 3.5 outdated
Function IsDotNET3Point5Installed
	Push $0
	Push $1
	Push $2

	ClearErrors
	ReadRegStr $1 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5" "Version"
	${IF} ${ERRORS}
		StrCpy $0 0
	${ELSE}
		${VersionCompare} $1 ${DOTNET_VERSION} $2
		${IF} $2 == 2
			StrCpy $0 2
		${ELSE}
			StrCpy $0 1
		${ENDIF}
	${ENDIF}

	Pop $2
	Pop $1
	Exch $0
FunctionEnd

; 0 = GTK# not found or outdated
; 1 = GTK# found
Function IsGtkSharpInstalled
	Push $0
	Push $1
	Push $2

	ClearErrors
	ReadRegStr $1 HKLM "Software\Novell\GtkSharp\Version" ""
	${IF} ${ERRORS}
		StrCpy $0 0
	${ELSE}
		${VersionCompare} $1 ${GTKSHARP_VERSION} $2
		${IF} $2 == 2
			StrCpy $0 0
		${ELSE}
			StrCpy $0 1
		${ENDIF}
	${ENDIF}

	Pop $2
	Pop $1
	Exch $0
FunctionEnd

Function ConnectInternet
  Push $R0
    ClearErrors
    Dialer::AttemptConnect
    IfErrors noie3
    Pop $R0
    StrCmp $R0 "online" connected
      MessageBox MB_OK|MB_ICONSTOP "Cannot connect to the internet."
      Quit
    noie3:
    ; IE3 not installed
    MessageBox MB_OK|MB_ICONINFORMATION "Please connect to the internet now."
    connected:
  Pop $R0
FunctionEnd
