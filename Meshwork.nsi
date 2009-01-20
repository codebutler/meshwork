!include "MUI.nsh"
!include Sections.nsh

SetCompressor LZMA

Var STARTMENU_FOLDER

!define PRODUCT "Meshwork"

Name "Meshwork"
OutFile "MeshworkSetup.exe"
InstallDir "$PROGRAMFILES\${PRODUCT}"
InstallDirRegKey HKCU "Software\${PRODUCT}" ""
LicenseData "COPYING"

!define MUI_WELCOMEFINISHPAGE_BITMAP  "Resources\Images\install_side.bmp"
!define MUI_WELCOMEFINISHPAGE_BITMAP_NOSTRETCH
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_BITMAP "Resources\Images\install_header.bmp"
!define MUI_HEADERIMAGE_BITMAP_NOSTRETCH
!define MUI_HEADERIMAGE_RIGHT

BrandingText "Copyright (C) 2003-2008 FileFind.net"
!define MUI_ABORTWARNING
;!define MUI_COMPONENTSPAGE_SMALLDESC
;!define MUI_LICENSEPAGE_CHECKBOX
!define MUI_COMPONENTSPAGE_NODESC
!define MUI_STARTMENUPAGE_REGISTRY_ROOT "HKCU"
!define MUI_STARTMENUPAGE_REGISTRY_KEY "Software\${PRODUCT}"
!define MUI_STARTMENUPAGE_REGISTRY_VALUENAME "Start Menu Folder"

; XXX: Temporarily disabled. Environment PATH needs to be updated to include new GTK# directory or Meshwork won't start. Apparently the way this gets executed, this doesn't happen.
; !define MUI_FINISHPAGE_RUN "$PROGRAMFILES\${PRODUCT}\FileFind.Meshwork.GtkClient.exe"

;!define MUI_FINISHPAGE_SHOWREADME "$PROGRAMFILES\${PRODUCT}\Changelog.txt"
;!define MUI_FINISHPAGE_SHOWREADME_TEXT "View release notes (Required)"

!define MUI_FINISHPAGE_NOAUTOCLOSE
!define MUI_PAGE_CUSTOMFUNCTION_LEAVE  componentsLeave

!define MUI_FINISHPAGE_TEXT_LARGE
!define MUI_FINISHPAGE_TEXT "Meshwork has been installed on your computer.\r\n\r\nClick Finish to close this wizard."


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

Section /o ".NET Framework (Already Installed)" SecFrameworkInstalled
	SectionIn 1 RO
	; Microsoft .NET Framework already installed - do nothing.
SectionEnd

Section "!.NET Framework (Required)" SecFramework
	SectionIn 1 RO
	Call ConnectInternet
	StrCpy $2 "$TEMP\dotnetfx.exe"
	NSISdl::download http://download.microsoft.com/download/5/6/7/567758a3-759e-473e-bf8f-52154438565a/dotnetfx.exe $2
	Pop $0
	StrCmp $0 success success
	SetDetailsView show
	DetailPrint "download failed: $0"
	MessageBox MB_OK "The Microsoft .NET Frameworked failed to download: $\n $0 $\n Please check your internet connection and/or install the .NET Framework manually, then run this setup again"
	Quit
	success:
		DetailPrint "Installing the Microsoft .NET Framework..."
		ExecWait '"$2" /Q'
		DetailPrint "Microsoft .NET Framework install complete."
		Pop $0
		StrCmp $0 "" skip
	skip:
SectionEnd

Section /o "GTK# Runtime v2.10.3 (Already Installed)" SecGtkSharpInstalled
	SectionIn 1 RO
	; GTK# already installed - do nothing.
SectionEnd

Section "!GTK# Runtime v2.10.3 (Required)" SecGtkSharp
	SectionIn 1 RO
	Call ConnectInternet
	StrCpy $2 "$TEMP\gtksharp-runtime-2.10.3.exe"
	NSISdl::download http://internap.dl.sourceforge.net/sourceforge/openvista/gtksharp-runtime-2.10.3.exe $2
	Pop $0
	Strcmp $0 success success
	SetDetailsView show
	DetailPrint "download failed: $0"
	MessageBox MB_OK "The GTK# v2.10.3 Runtime failed to download: $\n $0 $\n Please check your internet connection and/or download/install GTK# manually, then run Meshwork setup again"
	Quit
	success:
		ClearErrors
		DetailPrint "Installing GTK# v2.10.3..."
		ExecWait '"$2" /VERYQUIET'
    		IfErrors die
		DetailPrint "GTK# Install Complete"
		Goto continue
	die:
		MessageBox MB_OK "The GTK# v2.10.3 Runtime failed to install. Try downloading/installing GTK# manually, then run Meshwork setup again."
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
	File "Build\FileFind.Stun.dll"
	File "Build\gtkspell-sharp.dll"
	File "Build\Mono.Data.dll"
	File "Build\Mono.Data.Sqlite.dll"
	File "Build\Mono.Data.SqliteClient.dll"
	File "Build\Mono.GetOptions.dll"
	File "Build\Mono.Posix.dll"
	File "Build\MonoPosixHelper.dll"
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

Section "Desktop Icon" secDesktopIcon
	CreateShortCut "$DESKTOP\Meshwork.lnk" "$INSTDIR\FileFind.Meshwork.GtkClient.exe" "" "$INSTDIR\FileFind.Meshwork.GtkClient.exe" 0
SectionEnd

LangString DESC_secProgFiles ${LANG_ENGLISH} "The Meshwork executable."
LangString DESC_secDesktopIcon ${LANG_ENGLISH} "Create a desktop icon for quick access."
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
	!insertmacro MUI_DESCRIPTION_TEXT ${secProgFiles} $(DESC_secProgFiles)
	!insertmacro MUI_DESCRIPTION_TEXT ${secDesktopIcon} $(DESC_secDesktopIcon)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

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
	Delete "$INSTDIR\Mono.Posix.dll"
	Delete "$INSTDIR\MonoPosixHelper.dll"
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
	Call IsDotNETInstalled
	Pop $R3
	StrCmp $R3 0 NoDotNet
	StrCmp $R3 1 GoodDotNet
	StrCmp $R3 2 OldDotNet
	GoodDotNet:
	SectionSetText ${SecFramework} ""
	SectionSetFlags ${SecFramework} 0
	Goto Continue
	NoDotNet:
	SectionSetText ${SecFrameworkInstalled} ""
	goto Continue
	OldDotNet:
	SectionSetText ${SecFramework} ".NET Framework (Needs Upgrade)"
	SectionSetText ${SecFrameworkInstalled} ""
	Continue:

	Call IsGtkSharpInstalled
	Pop $R4
	StrCmp $R4 0 NoGtkSharp
	StrCmp $R4 1 GoodGtkSharp
	GoodGtkSharp:
	SectionSetText ${SecGtkSharp} ""
	SectionSetFlags ${SecGtkSharp} 0
	Goto end
	NoGtkSharp:
	SectionSetText ${SecGtkSharpInstalled} ""
	Goto end
	end:

FunctionEnd

Function .onInstSuccess
	Delete "$TEMP\dotnetfx.exe"
FunctionEnd

Function un.onInit
	IfSilent issilent notsilent
	issilent:
		SetSilent silent
	notsilent:

FunctionEnd

;--------------------------------------
;--------------------------------------

Function IsDotNETInstalled
  Push $0
  Push $1
  Push $2
  Push $3
  Push $4
  ReadRegStr $4 HKEY_LOCAL_MACHINE \
    "Software\Microsoft\.NETFramework" "InstallRoot"
  # remove trailing back slash
  Push $4
  Exch $EXEDIR
  Exch $EXEDIR
  Pop $4
  # if the root directory doesn't exist .NET is not installed
  IfFileExists $4 0 noDotNET
goto foundDotNet
  noDotNET:
    StrCpy $0 0
    Goto done
  foundDotNET:
    ClearErrors
    ReadRegStr $R0 HKLM "SOFTWARE\Microsoft\.NETFramework\policy\v2.0\" "50727"
    IfErrors die
    StrCpy $0 1
    goto done
    die:
    StrCpy $0 2
  done:
    Pop $4
    Pop $3
    Pop $2
    Pop $1
    Exch $0
FunctionEnd

Function IsGtkSharpInstalled
	Push $0
	Push $1

	StrCpy $0 0

	ReadRegStr $1 HKEY_LOCAL_MACHINE "Software\Medsphere\Gtk-Sharp\Runtime" "Version"
	StrCmp $1 "2.10.3" 0 +2
		StrCpy $0 1
	Pop $1
	Exch $0
FunctionEnd

Function componentsLeave
	SectionGetText ${SecFramework} $1
	StrCmp $1 '' moo
	MessageBox MB_YESNO "The Microsoft .NET Framework (Required to run ${PRODUCT}) will be automatically downloaded and installed. $\nIt is 23.1 MB and may take a while to download depending on the speed of your connection. $\n $\nDo you want to continue?" IDYES noabort
	Quit
	noabort:
	moo:
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
