@echo off
rem Usa o SDK local (%USERPROFILE%\.dotnet) se existir; senao usa o dotnet do PATH.
if exist "%USERPROFILE%\.dotnet\dotnet.exe" (set "DOTNET=%USERPROFILE%\.dotnet\dotnet.exe") else (set "DOTNET=dotnet")
set ASPNETCORE_ENVIRONMENT=Development
cd /d "%~dp0src\AgroCerebro.Server"
"%DOTNET%" run --urls http://localhost:5015
