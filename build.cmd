@echo off
rem Short — NativeAOT build. Potrebno: .NET 10 SDK + Visual Studio workload "Desktop development with C++".
setlocal
cd /d "%~dp0"
dotnet publish src\Short\Short.csproj -c Release -r win-x64 -o publish
if errorlevel 1 (
  echo.
  echo BUILD NIJE USPEO.
  exit /b 1
)
echo.
echo Gotovo: %~dp0publish\Short.exe
endlocal
