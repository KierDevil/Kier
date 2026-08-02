@echo off
cd /d "%~dp0frontend"

set "CODEX_NODE=%USERPROFILE%\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin"
set "CODEX_PNPM=%USERPROFILE%\.cache\codex-runtimes\codex-primary-runtime\dependencies\bin\fallback\pnpm.cmd"

where npm >nul 2>&1
if not errorlevel 1 (
    if not exist "node_modules" npm install
    npm run dev -- --host 0.0.0.0 --port 5173
) else (
    if exist "%CODEX_PNPM%" (
        set "PATH=%CODEX_NODE%;%PATH%"
        if not exist "node_modules" call "%CODEX_PNPM%" install
        call "%CODEX_PNPM%" run dev -- --host 0.0.0.0 --port 5173
    ) else (
        echo npm is not installed. Install Node.js to run the frontend.
        pause
    )
)
