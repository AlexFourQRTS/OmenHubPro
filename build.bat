@echo off
rem Builds OmenHub.exe with the .NET Framework 4 compiler that ships with Windows (no SDK needed).
setlocal
set FW=%windir%\Microsoft.NET\Framework64\v4.0.30319
set WINMD=%windir%\System32\WinMetadata
cd /d "%~dp0src"
"%FW%\csc.exe" /nologo /target:winexe /out:..\OmenHub.exe /win32manifest:App\app.manifest /win32icon:App\app.ico /recurse:*.cs ^
  /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Management.dll ^
  /r:"%WINMD%\Windows.Devices.winmd" /r:"%WINMD%\Windows.Foundation.winmd" /r:"%WINMD%\Windows.UI.winmd" ^
  /r:"%FW%\System.Runtime.WindowsRuntime.dll" /r:"%FW%\System.Numerics.Vectors.dll" /r:"%FW%\System.Numerics.dll" /r:System.Runtime.dll
if errorlevel 1 (echo BUILD FAILED & exit /b 1)
echo Built %~dp0OmenHub.exe
