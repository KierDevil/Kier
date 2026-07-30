@echo off
setlocal
cd /d "%~dp0"
echo Starting Kier Project...
echo.

rem Backend
if exist "backend\DepartmentFinancialRecords.API\DepartmentFinancialRecords.API.csproj" (
    where dotnet >nul 2>&1
    if %ERRORLEVEL%==0 (
        start "Kier Backend" cmd /k "cd /d "%~dp0backend\DepartmentFinancialRecords.API" && dotnet restore && dotnet run"
    ) else (
        echo WARNING: dotnet is not installed or not on PATH. Install the .NET SDK to run the backend.
    )
) else (
    echo Backend project not found.
)

echo.

rem Frontend
if exist "frontend\package.json" (
    where npm >nul 2>&1
    if %ERRORLEVEL%==0 (
        if not exist "frontend\node_modules" (
            echo Installing frontend dependencies...
            start "Kier Frontend" cmd /k "cd /d "%~dp0frontend" && npm install && npm run dev -- --host 0.0.0.0 --port 5173"
        ) else (
            start "Kier Frontend" cmd /k "cd /d "%~dp0frontend" && npm run dev -- --host 0.0.0.0 --port 5173"
        )
    ) else (
        echo WARNING: npm is not installed or not on PATH. Install Node.js to run the frontend.
    )
) else (
    echo Frontend project not found.
)

echo.
echo Start script finished. Close this window when finished.
pause
