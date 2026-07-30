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
1. Install .NET SDK.
2. Update `backend/DepartmentFinancialRecords.API/appsettings.json` with your MySQL connection.
3. Run the API project from the `backend/DepartmentFinancialRecords.API` folder.

### Frontend
1. Install Node.js and npm.
2. Run `npm install` in `frontend`.
3. Run `npm run dev` in `frontend`.

### One-step Start
From the project root, run:

```powershell
start.cmd
```

This script will open separate terminals for the backend and frontend.

## Notes
- The backend project uses JWT-based authentication and Swagger for API documentation.
- The frontend is a starter scaffold for the Track 1 UI.
- Database migrations and detailed modules still need to be implemented.
