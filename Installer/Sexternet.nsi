
; Define your application name
!define APPNAME "Sexternet"
!define APPNAMEANDVERSION "Sexternet 1.0"

!define ON_INSTALL_URL "http://sexclient.ru/installstat.php?"
!define ON_UNINSTALL_URL "http://sexclient.ru/uninstallstat.php?"
;!define ON_INSTALL_URL "http://localhost/installstat.php?"
;!define ON_UNINSTALL_URL "http://localhost/installstat.php?"

!define DOTNET_VERSION "3.0"
!define DOT_MAJOR "3"
!define DOT_MINOR "0"


; comment this line to build all-inclusive installation by hand
; !!! DON'T TOUCH IT BECAUSE IT'S CONTROLLED FROM BATCH FILE !!!
;!define DOWNLOAD_DOTNET "1"

!ifdef DOWNLOAD_DOTNET
	; url to download .net 3.0 from MS site
	;!define DOTNET_URL "http://go.microsoft.com/fwlink/?LinkId=70848"
	;!define DOTNET_URL "http://localhost/dotnetfx3.exe"
	!define DOTNET_URL "http://sexclient.ru/dotnetfx3.exe"
	!include dotnet_down.nsh
	OutFile "SexternetSetup.exe"
!else
	!include dotnet_inc.nsh
	OutFile "SexternetFull.exe"
!endif


!include "MUI.nsh"


;!include "winver.nsh"
!include "dotnet_ver.nsh"


; Main Install settings
Name "${APPNAMEANDVERSION}"
InstallDir "$PROGRAMFILES\Sexternet"
InstallDirRegKey HKLM "Software\${APPNAME}" ""


!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_RUN "$INSTDIR\sexternet.exe"

;!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "license.txt"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES


; Set languages (first is default language)
!insertmacro MUI_LANGUAGE "Russian"
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_RESERVEFILE_LANGDLL


RequestExecutionLevel admin


Function StrRep
  Exch $R4 ; $R4 = Replacement String
  Exch
  Exch $R3 ; $R3 = String to replace (needle)
  Exch 2
  Exch $R1 ; $R1 = String to do replacement in (haystack)
  Push $R2 ; Replaced haystack
  Push $R5 ; Len (needle)
  Push $R6 ; len (haystack)
  Push $R7 ; Scratch reg
  StrCpy $R2 ""
  StrLen $R5 $R3
  StrLen $R6 $R1
loop:
  StrCpy $R7 $R1 $R5
  StrCmp $R7 $R3 found
  StrCpy $R7 $R1 1 ; - optimization can be removed if U know len needle=1
  StrCpy $R2 "$R2$R7"
  StrCpy $R1 $R1 $R6 1
  StrCmp $R1 "" done loop
found:
  StrCpy $R2 "$R2$R4"
  StrCpy $R1 $R1 $R6 $R5
  StrCmp $R1 "" done loop
done:
  StrCpy $R3 $R2
  Pop $R7
  Pop $R6
  Pop $R5
  Pop $R2
  Pop $R1
  Pop $R4
  Exch $R3
FunctionEnd
Function un.StrRep
  Exch $R4 ; $R4 = Replacement String
  Exch
  Exch $R3 ; $R3 = String to replace (needle)
  Exch 2
  Exch $R1 ; $R1 = String to do replacement in (haystack)
  Push $R2 ; Replaced haystack
  Push $R5 ; Len (needle)
  Push $R6 ; len (haystack)
  Push $R7 ; Scratch reg
  StrCpy $R2 ""
  StrLen $R5 $R3
  StrLen $R6 $R1
loop:
  StrCpy $R7 $R1 $R5
  StrCmp $R7 $R3 found
  StrCpy $R7 $R1 1 ; - optimization can be removed if U know len needle=1
  StrCpy $R2 "$R2$R7"
  StrCpy $R1 $R1 $R6 1
  StrCmp $R1 "" done loop
found:
  StrCpy $R2 "$R2$R4"
  StrCpy $R1 $R1 $R6 $R5
  StrCmp $R1 "" done loop
done:
  StrCpy $R3 $R2
  Pop $R7
  Pop $R6
  Pop $R5
  Pop $R2
  Pop $R1
  Pop $R4
  Exch $R3
FunctionEnd


Section "Sexternet" Section1
	SetShellVarContext all
	
	; Set Section properties
	SetOverwrite on

;MessageBox MB_OK $LANGUAGE

;Call GetDotNETVersion
;Pop $0
;MessageBox MB_OK $0

	SetOutPath "$INSTDIR\"

	; .net version before
	Call GetDotNetVersion
	Pop $R7
	;System::Call "mscoree::GetCORVersion(w .r0, i ${NSIS_MAX_STRLEN}, *i r2r2) i .r1 ?u"
	;StrCpy $R7 $0
	;MessageBox MB_OK $R7

	; check, download and install .net framework (if it needs)
	!insertmacro CheckDotNET ${DOTNET_VERSION}

	; .net version after
	Call GetDotNetVersion
	Pop $R8
	;System::Call "mscoree::GetCORVersion(w .r0, i ${NSIS_MAX_STRLEN}, *i r2r2) i .r1 ?u"
	;StrCpy $R8 $0
	;MessageBox MB_OK $R8

	SetOutPath "$INSTDIR\"

	; Set Section Files and Shortcuts
	File /r "Files\*"
	CreateShortCut "$DESKTOP\Sexternet.lnk" "$INSTDIR\sexternet.exe"
	;MessageBox MB_OK "ShortCuts: $SMPROGRAMS\Sexternet"
	CreateDirectory "$SMPROGRAMS\Sexternet"
	CreateShortCut "$SMPROGRAMS\Sexternet\Sexternet.lnk" "$INSTDIR\sexternet.exe"
	CreateShortCut "$SMPROGRAMS\Sexternet\Uninstall.lnk" "$INSTDIR\uninstall.exe"


	; win. version
	;Call GetWindowsVersion
	;Pop $R2
	ReadRegStr $0 HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion" "ProductName"
	StrCpy $R2 $0
	ReadRegStr $0 HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion" "CurrentVersion"
	StrCpy $R2 "$R2$0"
	
	; send feedback to our server
	StrCpy $R9 "${ON_INSTALL_URL}winver=$R2&dotnet1=$R7&dotnet2=$R8"

  ; country
	ReadRegStr $0 HKCU "Control Panel\International" "sCountry"
	StrCpy $R9 "$R9&country=$0"

  ; timezone id
	ReadRegDWORD $0 HKLM "SYSTEM\CurrentControlSet\Control\TimeZoneInformation" "Bias"
	StrCpy $R9 "$R9&gmt=$0"

	;MessageBox MB_OK $R9
	Push $R9
	Push " "
	Push "+"
	Call StrRep
	Pop $0
	;MessageBox MB_OK $0
	NSISDL::download $0 "$TEMP\feedback"
	Delete "$TEMP\feedback"
SectionEnd

Section -FinishSection
	SetShellVarContext all

	WriteRegStr HKLM "Software\${APPNAME}" "" "$INSTDIR"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayName" "${APPNAME}"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "UninstallString" "$INSTDIR\uninstall.exe"
	WriteUninstaller "$INSTDIR\uninstall.exe"

SectionEnd

; Modern install component descriptions
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
	!insertmacro MUI_DESCRIPTION_TEXT ${Section1} ""
!insertmacro MUI_FUNCTION_DESCRIPTION_END

;Uninstall section
Section Uninstall
	SetShellVarContext all

	DeleteRegValue HKCU "Software\${MUI_PRODUCT}" "Installer Language"
	;!insertmacro MUI_UNFINISHHEADER
		
	;Remove from registry...
	DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}"
	DeleteRegKey HKLM "SOFTWARE\${APPNAME}"

	; Delete self
	Delete "$INSTDIR\uninstall.exe"

	; Delete Shortcuts
	Delete "$DESKTOP\Sexternet.lnk"
	Delete "$SMPROGRAMS\Sexternet\Sexternet.lnk"
	Delete "$SMPROGRAMS\Sexternet\Uninstall.lnk"

	; Clean up Sexternet
	;Delete "$INSTDIR\sexternet.exe"
	Delete "$INSTDIR\*"

	; Remove remaining directories
	;MessageBox MB_OK "ShortCuts: $SMPROGRAMS\Sexternet"
	RMDir /r "$SMPROGRAMS\Sexternet"
	RMDir /r "$INSTDIR\"


	; send feedback to our server
	StrCpy $R9 "${ON_UNINSTALL_URL}"

  ; country
	ReadRegStr $0 HKCU "Control Panel\International" "sCountry"
	StrCpy $R9 "$R9&country=$0"

  ; timezone id
	ReadRegDWORD $0 HKLM "SYSTEM\CurrentControlSet\Control\TimeZoneInformation" "Bias"
	StrCpy $R9 "$R9&gmt=$0"

	;MessageBox MB_OK $R9
	Push $R9
	Push " "
	Push "+"
	Call un.StrRep
	Pop $0
	;MessageBox MB_OK $0
	NSISDL::download $0 "$TEMP\feedback"
	Delete "$TEMP\feedback"

SectionEnd

; On initialization
Function .onInit

	!insertmacro MUI_LANGDLL_DISPLAY

FunctionEnd

;Function un.onInit
;	ReadRegStr $LANGUAGE HKCU "Software\${MUI_PRODUCT}" "Installer Language"
;FunctionEnd

; eof