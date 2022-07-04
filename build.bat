@echo off

ECHO Dotnet Core SDK required for building. Get it at https://dotnet.microsoft.com/download/dotnet-core/3.1
ECHO Choose target platform:
ECHO 1. win-x64
ECHO 2. linux-x64
ECHO 0. Custom

CHOICE /C 012 /M "Your choice:"

IF ERRORLEVEL 3 GOTO Lin
IF ERRORLEVEL 2 GOTO Win
IF ERRORLEVEL 1 GOTO Cust


:Lin
ECHO.
del /q "bld"
mkdir "bld"
dotnet publish -c Release -r linux-x64 -o "bld"
GOTO End

:Win
ECHO.
del /q "bld"
mkdir "bld"
dotnet publish -c Release -r win-x64 -o "bld"
GOTO End

:Cust
ECHO.
SET /p rtime="Enter custom RID (https://docs.microsoft.com/ru-ru/dotnet/core/rid-catalog): "
del /q "bld"
mkdir "bld"
dotnet publish -c Release -r %rtime% -o "bld"
GOTO End

:End

pause