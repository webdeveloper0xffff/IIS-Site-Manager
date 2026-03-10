# Publish To GitHub

## Before Pushing

1. Confirm `backend/appsettings*.json` still contains placeholders, not real secrets.
2. Set runtime secrets through environment variables or deployment-only config.
3. Run:

```powershell
dotnet restore backend/IIS-Site-Manager.API.csproj --configfile NuGet.Config
dotnet build backend/IIS-Site-Manager.API.csproj --no-restore
dotnet run --project backend -- --run-security-smoke-tests
```

4. Review `git status` and confirm local caches plus `memory-bank/` are excluded.

## Push Commands

```powershell
cd C:\Users\Administrator\Desktop\hosting_web\IIS-Site-Manager
git branch -M main
git push -u origin main
```

Or:

```powershell
.\push-to-github.ps1 -RepoUrl "https://github.com/YOUR_USERNAME/IIS-Site-Manager.git"
```

## Authentication

- HTTPS: use a GitHub Personal Access Token when prompted.
- SSH: switch `origin` to an SSH URL first if you already have keys configured.
