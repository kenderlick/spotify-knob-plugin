@echo off
set CSC="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist %CSC% set CSC="C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "..\bin" mkdir "..\bin"
%CSC% /nologo /target:winexe /platform:anycpu /optimize+ /out:"..\bin\SpotifyKnob.exe" /r:System.dll,System.Core.dll *.cs
if %ERRORLEVEL% EQU 0 (
    echo [SUCCESSO] SpotifyKnob.exe compilato in ..\bin\SpotifyKnob.exe
) else (
    echo [ERRORE] Compilazione fallita!
)
pause
