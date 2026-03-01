# 构建部署包
$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptRoot

Write-Host "=== 构建 IIS Site Manager 部署包 ===" -ForegroundColor Cyan

# 1. 发布后端
Write-Host "`n[1/2] 发布后端..." -ForegroundColor Yellow
Set-Location (Join-Path $projectRoot "backend")
dotnet publish -c Release -o (Join-Path $scriptRoot "api")
if ($LASTEXITCODE -ne 0) { exit 1 }
Write-Host "后端发布完成." -ForegroundColor Green

# 2. 构建前端（静态导出）
Write-Host "`n[2/2] 构建前端..." -ForegroundColor Yellow
Set-Location (Join-Path $projectRoot "frontend")
$env:NEXT_PUBLIC_API_URL = ""  # 同源，使用相对路径 /api
npm run build 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    npm run build
    exit 1
}

# 复制到 deploy/web
$webPath = Join-Path $scriptRoot "web"
if (Test-Path $webPath) { Remove-Item $webPath -Recurse -Force }
New-Item -ItemType Directory -Path $webPath -Force | Out-Null
Copy-Item (Join-Path $projectRoot "frontend\out\*") -Destination $webPath -Recurse -Force

# 写入 web.config（用于 SPA 路由和 MIME 类型）
$webConfigPath = Join-Path $webPath "web.config"
$webConfigContent = @'
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
  <system.webServer>
    <staticContent>
      <mimeMap fileExtension=".json" mimeType="application/json" />
    </staticContent>
    <defaultDocument>
      <files>
        <clear />
        <add value="index.html" />
      </files>
    </defaultDocument>
  </system.webServer>
</configuration>
'@
Set-Content -Path $webConfigPath -Value $webConfigContent -Encoding UTF8

Write-Host "前端构建完成." -ForegroundColor Green

Write-Host "`n=== 构建完成 ===" -ForegroundColor Green
Write-Host "输出目录: $scriptRoot" -ForegroundColor Gray
Write-Host "  - api\  (后端)" -ForegroundColor Gray
Write-Host "  - web\  (前端)" -ForegroundColor Gray
Write-Host "`n运行 setup-iis.ps1 (管理员) 部署到 IIS." -ForegroundColor Cyan
