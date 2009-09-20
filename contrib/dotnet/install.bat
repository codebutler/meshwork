@echo off

set OLDDIR=%CD%

mkdir C:\MeshworkDLLs
copy *.dll C:\MeshworkDLLs

if not exist %systemroot%\syswow64\regedit goto 20
	echo Detected 32-bit system
	regedit %CD%\add.reg
:20
	echo Detected 64-bit system, using 32-bit registry...
	%systemroot%\syswow64\regedit %CD%\add.reg
	goto end
:end
