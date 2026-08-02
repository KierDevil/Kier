# Department Financial Records, Collections, Attendance, and Reporting Information System

## Technology Track
Track 1: Microsoft Enterprise Stack

## Technology Used
- ASP.NET Core
- Entity Framework Core
- MySQL
- Pomelo.EntityFrameworkCore.MySql
- JWT Authentication
- Swagger / OpenAPI
- Vue.js
- Vuetify
- Vite
- Axios
- .NET MAUI (for mobile support)
- Git

## Project Structure
- `backend/DepartmentFinancialRecords.API` - ASP.NET Core Web API project
- `frontend` - Vue.js + Vuetify frontend scaffold

## Clone on Another PC
```powershell
cd C:\appdev
git clone https://github.com/KierDevil/Kier.git
cd Kier
git checkout agent/interactive-qr-rfid-attendance
```

Install these on the PC:
- .NET SDK 8
- Node.js and npm
- MySQL Server

Then update `backend/DepartmentFinancialRecords.API/appsettings.json` with that PC's MySQL connection string and password.

## How to Run
Run these in separate terminals.

### Database
Use MySQL. Create a database named `DepartmentFinancialRecords`, then make sure the backend connection string points to it.

If using the local portable MySQL setup from this workspace, run:

```powershell
mysql-8.4.10-winx64\bin\mysqld.exe --defaults-file=mysql-local.ini
```

### Backend
1. Install .NET SDK 8.
2. Update `backend/DepartmentFinancialRecords.API/appsettings.json` with your MySQL connection.
3. Run the API project from the `backend/DepartmentFinancialRecords.API` folder:

```powershell
dotnet restore
dotnet run --urls http://localhost:5000
```

Inside this workspace, a project-local SDK can also be installed under `.dotnet`; `start.cmd` will use it automatically when system `dotnet` is not on PATH.

Swagger will be available from the backend URL in development. The health check is available at `http://localhost:5000/api/health`.

### Frontend
1. Install Node.js and npm, or use the bundled Codex pnpm runtime when running inside Codex.
2. Run the frontend from the `frontend` folder:

```powershell
npm install
npm run dev -- --host 0.0.0.0 --port 5173
```

If you use pnpm instead:

```powershell
pnpm install
pnpm run dev -- --host 0.0.0.0 --port 5173
```

### One-step Start
From the project root, run:

```powershell
start.cmd
```

This script will open separate terminals for the backend and frontend. The backend runs at `http://localhost:5000`, and the frontend runs at `https://localhost:5173` when local HTTPS certificates are present.

## QR and RFID Attendance
- QR payload format: `KIER:2026-001`
- RFID maps through the `RfidUid` column in the `students` table.
- Attendance scans are saved through the backend API to MySQL.
- Attendance supports open time, late time, close time, late fine per minute, max fine, and excused status.
- Excused attendance is neutral and does not count as absent.

## Phone Access
Start the frontend with `--host 0.0.0.0`, then open this on a phone connected to the same Wi-Fi:

```text
https://YOUR-PC-IP:5173
```

Example:

```text
https://192.168.10.205:5173
```

The phone may show a certificate warning for local HTTPS. Continue through the warning for development use.

## Notes
- The backend project uses JWT-based authentication and Swagger for API documentation.
- The frontend now includes a starter dashboard and checks `/api/health` through the Vite proxy.
- Students and attendance have backend APIs. Collections, fines, expenses, and reports still need full backend API wiring.
- Do not commit real database passwords. Keep local passwords in your own `appsettings.json`.
