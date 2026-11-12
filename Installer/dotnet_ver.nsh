
!ifndef DOT_MAJOR
	!define DOT_MAJOR "3"
!endif
!ifndef DOT_MINOR
	!define DOT_MINOR "0"
!endif


; just checking installed .net framework and its version


; Usage
; Define in your script two constants:
;   DOT_MAJOR "(Major framework version)"
;   DOT_MINOR "{Minor frameword version)"
; 
Function IsDotNetInstalled
 
  StrCpy $0 "0"
  ;StrCpy $1 "SOFTWARE\Microsoft\.NETFramework\policy" ;registry entry to look in.
	StrCpy $1 "SOFTWARE\Microsoft\NET Framework Setup\NDP" ;registry entry to look in.
  StrCpy $2 0
 
  StartEnum:
    ;Enumerate the versions installed.
    EnumRegKey $3 HKLM "$1" $2
    
    ;If we don't find any versions installed, it's not here.
    StrCmp $3 "" noDotNet notEmpty
    
    ;We found something.
    notEmpty:
      ;Find out if the RegKey starts with 'v'.  
      ;If it doesn't, goto the next key.
      StrCpy $4 $3 1 0
      StrCmp $4 "v" +1 goNext
      StrCpy $4 $3 1 1
      
      ;It starts with 'v'.  Now check to see how the installed major version
      ;relates to our required major version.
      ;If it's equal check the minor version, if it's greater, 
      ;we found a good RegKey.
      IntCmp $4 ${DOT_MAJOR} +1 goNext yesDotNetReg
      ;Check the minor version.  If it's equal or greater to our requested 
      ;version then we're good.
      StrCpy $4 $3 1 3
      IntCmp $4 ${DOT_MINOR} yesDotNetReg goNext yesDotNetReg
 
    goNext:
      ;Go to the next RegKey.
      IntOp $2 $2 + 1
      goto StartEnum
 
  yesDotNetReg:
		Push $3
		Return
  noDotNet:
    ;Nope, something went wrong along the way.  Looks like the 
    ;proper .NETFramework isn't installed.  
    ;MessageBox MB_OK "You must have v${DOT_MAJOR}.${DOT_MINOR} or greater of the .NETFramework installed.  Aborting!"
    ;Abort
		Push ""
 
;  yesDotNet:
;    ;Everything checks out.  Go on with the rest of the installation.
    
FunctionEnd
Function GetDotNetVersion
 
  StrCpy $0 "0"
  ;StrCpy $1 "SOFTWARE\Microsoft\.NETFramework\policy" ;registry entry to look in.
	StrCpy $1 "SOFTWARE\Microsoft\NET Framework Setup\NDP" ;registry entry to look in.
  StrCpy $2 0
  StrCpy $6 0
	StrCpy $7 0
  StrCpy $8 ""
 
  StartEnum:
    ;Enumerate the versions installed.
    EnumRegKey $3 HKLM "$1" $2
    
    ;If we don't find any versions installed, it's not here.
    StrCmp $3 "" noDotNet notEmpty
    
    ;We found something.
    notEmpty:
      ;Find out if the RegKey starts with 'v'.  
      ;If it doesn't, goto the next key.
      StrCpy $4 $3 1 0
      StrCmp $4 "v" +1 goNext
      StrCpy $4 $3 1 1
      
      ;It starts with 'v'.  Now check to see how the installed major version
      ;relates to our required major version.
      ;If it's equal check the minor version, if it's greater, 
      ;we found a good RegKey.
      IntCmp $4 ${DOT_MAJOR} +1 goNext yesDotNetReg
      ;Check the minor version.  If it's equal or greater to our requested 
      ;version then we're good.
      StrCpy $5 $3 1 3
      IntCmp $5 ${DOT_MINOR} yesDotNetReg goNext yesDotNetReg
 
    goNext:
			IntCmp $6 $4 CompMinor CopyVer SkipCopy
			CompMinor:
				IntCmp $7 $5 SkipCopy CopyVer SkipCopy
			CopyVer:
				StrCpy $6 $4
				StrCpy $7 $5
				StrCpy $8 $3
			SkipCopy:
      ;Go to the next RegKey.
      IntOp $2 $2 + 1
      goto StartEnum
 
  yesDotNetReg:
		StrCpy $8 $3
  noDotNet:
    ;Nope, something went wrong along the way.  Looks like the 
    ;proper .NETFramework isn't installed.  
    ;MessageBox MB_OK "You must have v${DOT_MAJOR}.${DOT_MINOR} or greater of the .NETFramework installed.  Aborting!"
    ;Abort
		Push $8
 
;  yesDotNet:
;    ;Everything checks out.  Go on with the rest of the installation.
    
FunctionEnd
