@if not defined _echo @echo off
setlocal

if [%1]==[]                GOTO NETFX
if [%1]==[netfx]           GOTO NETFX
if [%1]==[netstandard-win] GOTO VNEXT
if [%1]==[netstandard]     GOTO VNEXT
if [%1]==[all]             GOTO ALL

:NETFX
cmd /c "%~dp0src\Test.cmd"
GOTO END

:VNEXT
cmd /c "%~dp0vNext\src\Test.cmd"
GOTO END

:ALL
cmd /c "%~dp0src\Test.cmd"
cmd /c "%~dp0vNext\src\Test.cmd"

:END
