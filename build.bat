@echo off
rem Builds OmenHub.exe with the .NET Framework 4 compiler that ships with Windows (no SDK needed).
setlocal
set FW=%windir%\Microsoft.NET\Framework64\v4.0.30319
cd /d "%~dp0src"
"%FW%\csc.exe" /nologo /target:winexe /out:..\OmenHub.exe /win32manifest:App\app.manifest /win32icon:App\app.ico /recurse:*.cs ^
  /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Management.dll
if errorlevel 1 (echo BUILD FAILED & exit /b 1)
echo Built %~dp0OmenHub.exe
