@echo off
title Compilatore Nativo SpotifyKnob
color 0A
echo ========================================================
echo   Compilazione Nativa Windows per SpotifyKnob.exe
echo ========================================================
echo.

set CSC="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist %CSC% (
    set CSC="C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

if not exist %CSC% (
    color 0C
    echo [ERRORE] Compilatore C# di Windows non trovato in %CSC%
    pause
    exit /b 1
)

echo Uso il compilatore nativo di Windows: %CSC%
echo Compilazione in corso...

if not exist "bin" mkdir "bin"

%CSC% /nologo /target:winexe /platform:anycpu /optimize+ /out:"bin\SpotifyKnob.exe" /r:System.dll,System.Core.dll src_csharp\*.cs

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================================
    echo   [SUCCESSO] SpotifyKnob.exe compilato perfettamente!
    echo   File generato in: bin\SpotifyKnob.exe
    echo ========================================================
    echo.
) else (
    color 0C
    echo.
    echo [ERRORE] Compilazione fallita!
    echo.
)

pause
