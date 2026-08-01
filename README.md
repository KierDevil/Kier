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

## How to Run
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

This script will open separate terminals for the backend and frontend. The backend runs at `http://localhost:5000`, and the frontend runs at `http://localhost:5173`.

## Notes
- The backend project uses JWT-based authentication and Swagger for API documentation.
- The frontend now includes a starter dashboard and checks `/api/health` through the Vite proxy.
- Database migrations and detailed modules still need to be implemented.
