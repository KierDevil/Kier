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

rem Backend
if exist "backend\DepartmentFinancialRecords.API\DepartmentFinancialRecords.API.csproj" (
    where dotnet >nul 2>&1
    if not errorlevel 1 (
        start "Kier Backend" cmd /k "cd /d ""%~dp0backend\DepartmentFinancialRecords.API"" && dotnet restore && dotnet run --urls http://localhost:5000"
    ) else (
        if exist "%LOCAL_DOTNET%" (
            echo dotnet is not installed on PATH. Starting the backend with the local .NET SDK.
            start "Kier Backend" cmd /k "cd /d ""%~dp0backend\DepartmentFinancialRecords.API"" && ""%LOCAL_DOTNET%"" restore && ""%LOCAL_DOTNET%"" run --urls http://localhost:5000"
        ) else (
            echo WARNING: dotnet is not installed or not on PATH. Install the .NET SDK to run the backend.
        )
    )
) else (
    echo Backend project not found.
)

echo.

rem Frontend
if exist "frontend\package.json" (
    where npm >nul 2>&1
    if not errorlevel 1 (
        if not exist "frontend\node_modules" (
            echo Installing frontend dependencies...
            start "Kier Frontend" cmd /k "cd /d ""%~dp0frontend"" && npm install && npm run dev -- --host 0.0.0.0 --port 5173"
        ) else (
            start "Kier Frontend" cmd /k "cd /d ""%~dp0frontend"" && npm run dev -- --host 0.0.0.0 --port 5173"
        )
    ) else (
        if exist "%CODEX_PNPM%" (
            echo npm is not installed. Starting the frontend with the bundled Codex Node runtime.
            if exist "%CODEX_NODE%" set "PATH=%CODEX_NODE%;%PATH%"
            if not exist "frontend\node_modules" (
                start "Kier Frontend" cmd /k "set ""PATH=%CODEX_NODE%;%PATH%"" && cd /d ""%~dp0frontend"" && ""%CODEX_PNPM%"" install && ""%CODEX_PNPM%"" run dev -- --host 0.0.0.0 --port 5173"
            ) else (
                start "Kier Frontend" cmd /k "set ""PATH=%CODEX_NODE%;%PATH%"" && cd /d ""%~dp0frontend"" && ""%CODEX_PNPM%"" run dev -- --host 0.0.0.0 --port 5173"
            )
        ) else (
            echo WARNING: npm is not installed or not on PATH. Install Node.js to run the frontend.
        )
    )
) else (
    echo Frontend project not found.
)

echo.
echo Start script finished. Close this window when finished.
pause
