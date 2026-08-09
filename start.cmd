@echo off
setlocal
cd /d "%~dp0"
echo Starting Kier Project...
echo.
echo Project folder: %CD%
echo.

set "CODEX_NODE=%USERPROFILE%\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin"
set "CODEX_PNPM=%USERPROFILE%\.cache\codex-runtimes\codex-primary-runtime\dependencies\bin\fallback\pnpm.cmd"
set "LOCAL_DOTNET=%~dp0.dotnet\dotnet.exe"
if not exist "%LOCAL_DOTNET%" set "LOCAL_DOTNET=%~dp0..\.dotnet\dotnet.exe"
set "LOCAL_MYSQL=%~dp0mysql-8.4.10-winx64\bin\mysqld.exe"
set "LOCAL_MYSQL_CONFIG=%~dp0mysql-local.ini"
set "LOCAL_MYSQL_START=%~dp0run-mysql.cmd"
set "LOCAL_BACKEND_START=%~dp0run-backend.cmd"
set "LOCAL_FRONTEND_START=%~dp0run-frontend.cmd"

rem Kill duplicate app instances to keep one local setup only.
for %%P in (5000 5173 5174 3307) do (
    for /f "skip=5 tokens=5" %%A in ('netstat -ano ^| findstr ":%%P " 2^>nul') do (
        taskkill /f /pid %%A >nul 2>&1
    )
)

echo.

rem Database
if exist "%LOCAL_MYSQL%" (
    netstat -ano | find ":3307" >nul
    if errorlevel 1 (
        echo Starting local MySQL on port 3307...
        start "Kier MySQL" "%LOCAL_MYSQL_START%"
        timeout /t 5 /nobreak >nul
    ) else (
        echo MySQL already appears to be running.
    )
) else (
    echo WARNING: local MySQL was not found. Start MySQL manually before the backend.
)

echo.

rem Backend
if exist "backend\DepartmentFinancialRecords.API\DepartmentFinancialRecords.API.csproj" (
    echo Starting backend on http://localhost:5000
    start "Kier Backend" "%LOCAL_BACKEND_START%"
) else (
    echo Backend project not found.
)

echo.

rem Frontend
if exist "frontend\package.json" (
    echo Starting frontend on http://localhost:5173
    start "Kier Frontend" "%LOCAL_FRONTEND_START%"
) else (
    echo Frontend project not found.
)

echo.
echo Start script finished. Close this window when finished.
pause
