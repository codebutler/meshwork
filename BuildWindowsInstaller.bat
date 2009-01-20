REM BuildWindowsInstaller.bat - Create the windows installer exe
REM
REM Authors: 
REM 	Eric Butler <eric@filefind.net>
REM
REM

path=%path%;C:\program files\NSIS

del "MeshworkSetup.exe"

makensis meshwork.nsi

pause
