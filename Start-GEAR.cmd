@echo off
cd /d "%~dp0"
dotnet run --project src\GearPdf.App\GearPdf.App.csproj
if errorlevel 1 pause
