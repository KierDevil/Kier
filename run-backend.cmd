@echo off
cd /d "%~dp0backend\DepartmentFinancialRecords.API"

set "DOTNET_CLI_HOME=%~dp0.dotnet-home"
set "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "NUGET_PACKAGES=%~dp0packages"
set "APPDATA=%~dp0.dotnet-home\AppData\Roaming"
set "LOCALAPPDATA=%~dp0.dotnet-home\AppData\Local"

set "LOCAL_DOTNET=%~dp0.dotnet\dotnet.exe"
if not exist "%LOCAL_DOTNET%" set "LOCAL_DOTNET=%~dp0..\.dotnet\dotnet.exe"

if exist "%LOCAL_DOTNET%" (
    "%LOCAL_DOTNET%" restore
    "%LOCAL_DOTNET%" run --urls http://0.0.0.0:5000
) else (
    dotnet restore
    dotnet run --urls http://0.0.0.0:5000
)
