# IIS Site Manager

Admin-first shared hosting MVP for IIS.

## Components

- `backend/`: .NET 10 control-plane API
- `frontend/`: Next.js admin panel
- `agent/`: remote Windows agent for IIS provisioning
- `deploy/`: build and IIS deployment scripts

## Current Flow

1. Customer registers.
2. Admin approves the customer.
3. Customer is assigned to a node.
4. Admin queues site provisioning.
5. Agent polls the job and creates the IIS site.
6. Control-plane tracks jobs, sites, and node health.

## Required Backend Configuration

Set secure backend values through environment variables or deployment-time config. The backend now fails fast if required values are missing or still set to placeholders.

```json
{
  "Admin": {
    "Username": "<set-admin-username>",
    "PasswordHash": "<set-admin-password-hash>",
    "JwtKey": "<set-admin-jwt-key>",
    "JwtIssuer": "IIS-Site-Manager",
    "JwtAudience": "IIS-Site-Manager-Admin",
    "JwtExpiresMinutes": 720
  },
  "ConnectionStrings": {
    "Default": "<set-sqlserver-connection-string>"
  }
}
```

Environment variable names:

- `Admin__Username`
- `Admin__PasswordHash`
- `Admin__JwtKey`
- `Admin__JwtIssuer`
- `Admin__JwtAudience`
- `Admin__JwtExpiresMinutes`
- `ConnectionStrings__Default`

## Generate Admin Password Hash

```powershell
dotnet run --project backend -- --hash-password "YourStrongAdminPassword!"
```

Copy the output into `Admin__PasswordHash`.

## Local Development

Backend:

```powershell
dotnet restore backend/IIS-Site-Manager.API.csproj --configfile NuGet.Config
dotnet run --project backend
```

Frontend:

```powershell
cd frontend
npm install
npm run dev
```

Default backend URL is `http://localhost:5032`.

## Security Notes

- Admin login uses hashed password verification.
- `Admin:JwtKey` is required.
- `X-Admin-Key` bypass auth is removed.
- Customer passwords are stored as hashes.
- Legacy customer plaintext passwords migrate to a hash on successful login.

## Verification

```powershell
$env:SECURITY_TEST_SQL_CONNECTION="Server=localhost\\SQLEXPRESS;User Id=<user>;Password=<password>;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False"
dotnet run --project backend -- --run-security-smoke-tests
```

If your local SQL Server supports integrated authentication, the environment variable is optional.

## Deployment Notes

- Control-plane runtime currently serves on `:5032`.
- Frontend runtime currently serves on `:8082`.
- Remote agent currently runs via scheduled task, not Windows Service.
- Remote Windows Service startup is still a known issue.
