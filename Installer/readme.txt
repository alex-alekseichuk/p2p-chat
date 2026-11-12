How to build 2 isntalltion (Full with .NET included and Setup version without included .NET but available to download)

- install nsis system http://nsis.sourceforge.net/Download
- set PATH env. variable to the dir with makensis.exe tool
- copy all files to Files subfolder of installer directory. there should be Sexternet.exe at least
- copy .net framework 3.0 isntallation .exe module to installer directory (not to Files) as filename: dotnetfx.exe
	rename to dotnetfx.exe if it needs
- correct paramteres in Sexternet.nsi file if it needs
	; Define your application name
	!define APPNAME "Sexternet"
	!define APPNAMEANDVERSION "Sexternet 1.0"

	!define FEEDBACK_URL "http://sexclient.ru/installstat.php?"

	!define DOTNET_VERSION "3.0"
	!define DOT_MAJOR "3"
	!define DOT_MINOR "0"

	; url to download .net 3.0 from MS site
	!define DOTNET_URL "http://go.microsoft.com/fwlink/?LinkId=70848"
- run build.bat script in the installer dir.
- there should be 2 files created: SexternetFull.exe and SexternetSetup.exe
