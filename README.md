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
- `mobile/Kier.Mobile` - .NET MAUI Android/iOS mobile wrapper for the web app

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
.\start.cmd
```

This script will open separate terminals for the backend and frontend. The backend runs at `http://localhost:5000`, and the frontend runs at `http://localhost:5173` by default.

## QR and RFID Attendance
- QR payload format: `KIER:2026-001`
- RFID maps through the `RfidUid` column in the `students` table.
- Attendance scans are saved through the backend API to MySQL.
- Attendance supports open time, late time, close time, late fine per minute, max fine, and excused status.
- Excused attendance is neutral and does not count as absent.

## Phone Access
Start the frontend with `--host 0.0.0.0`, then open this on a phone connected to the same Wi-Fi:

```text
http://YOUR-PC-IP:5173
```

Example:

```text
http://192.168.10.205:5173
```

If you enable local HTTPS manually, the phone may show a certificate warning for development use.

## Mobile App
The repository now includes a .NET MAUI Android/iOS app in:

```text
mobile/Kier.Mobile
```

It opens the same Kier web app inside a phone app, so it still uses the same backend API and MySQL database.

Before building the Android app on Windows, install the MAUI Android workload:

```powershell
dotnet workload restore mobile/Kier.Mobile/Kier.Mobile.csproj
```

Then build it:

```powershell
dotnet build mobile/Kier.Mobile/Kier.Mobile.csproj -f net8.0-android
```

When the app opens, enter the running web app URL, for example:

```text
http://192.168.10.205:5173
```

Allow camera permission so the QR scanner can work.

iOS support is included, but building/running the iPhone app requires a Mac with Xcode and Apple signing.

## Free Cloud Hosting Option
Use this when you do not want your laptop to act as the server.

### 1. Aiven MySQL Free
Create a free MySQL service on Aiven, then copy the host, port, user, password, and database name.

Your backend connection string should look like:

```text
server=AIVEN_HOST;port=AIVEN_PORT;database=defaultdb;user=AIVEN_USER;password=AIVEN_PASSWORD;SslMode=Required;AllowPublicKeyRetrieval=True;
```

### 2. Koyeb Backend API
Create a Koyeb web service from this GitHub repo.

Use:
- Root / Dockerfile path: `backend/DepartmentFinancialRecords.API`
- Port: `8080`

Set these environment variables in Koyeb:

```text
ConnectionStrings__DefaultConnection=server=AIVEN_HOST;port=AIVEN_PORT;database=defaultdb;user=AIVEN_USER;password=AIVEN_PASSWORD;SslMode=Required;AllowPublicKeyRetrieval=True;
AllowedCorsOrigins=https://YOUR_FRONTEND_DOMAIN
Jwt__Key=replace-with-a-long-random-secret
ASPNETCORE_ENVIRONMENT=Production
```

After deploy, test:

```text
https://YOUR_KOYEB_APP/api/health
```

### 3. Frontend Hosting
Deploy the `frontend` folder to a static host such as Vercel, Netlify, or Render Static Site.

Set this frontend environment variable:

```text
VITE_API_BASE_URL=https://YOUR_KOYEB_APP
```

Then rebuild/redeploy the frontend.

## Notes
- The backend project uses JWT-based authentication and Swagger for API documentation.
- The frontend now includes a starter dashboard and checks `/api/health` through the Vite proxy.
- Students and attendance have backend APIs. Collections, fines, expenses, and reports still need full backend API wiring.
- Do not commit real database passwords. Keep local passwords in your own `appsettings.json`.
