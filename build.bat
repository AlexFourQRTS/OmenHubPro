@echo off
rem Builds OmenHub.exe with the .NET Framework 4 compiler that ships with Windows (no SDK needed).
setlocal
set CSC=%windir%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
cd /d "%~dp0src"
"%CSC%" /nologo /target:winexe /out:..\OmenHub.exe /win32manifest:app.manifest /win32icon:app.ico ^
  /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Management.dll ^
  Core.cs Ui.cs Main.cs
if errorlevel 1 (echo BUILD FAILED & exit /b 1)
echo Built %~dp0OmenHub.exe
