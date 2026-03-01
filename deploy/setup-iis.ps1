# IIS 部署脚本 - IIS Site Manager
# 需要以管理员身份运行

$ErrorActionPreference = "Stop"
$deployRoot = $PSScriptRoot

# 配置
$siteName = "IIS-Site-Manager"
$appPoolName = "IIS-Site-Manager-Pool"
$webPath = Join-Path $deployRoot "web"
$apiPath = Join-Path $deployRoot "api"
$port = 8081   # 前端端口
$apiPort = 8082  # 后端 API 独立站点端口（与前端同源部署时使用 /api 应用）

Write-Host "=== IIS Site Manager 部署 ===" -ForegroundColor Cyan
Write-Host "部署路径: $deployRoot" -ForegroundColor Gray
Write-Host ""

# 检查 ASP.NET Core Hosting Bundle
$hostingBundle = Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\ASP.NET Core\Shared Framework\*" -ErrorAction SilentlyContinue
if (-not $hostingBundle) {
    Write-Host "警告: 未检测到 ASP.NET Core Hosting Bundle。请先安装 .NET 10 运行时。" -ForegroundColor Yellow
    Write-Host "下载: https://dotnet.microsoft.com/download/dotnet/10.0" -ForegroundColor Gray
}

# 导入 WebAdministration 模块
Import-Module WebAdministration -ErrorAction SilentlyContinue
if (-not (Get-Module WebAdministration)) {
    Write-Host "错误: 无法加载 WebAdministration 模块，请确保 IIS 已安装。" -ForegroundColor Red
    exit 1
}

# 1. 创建或更新应用程序池（No Managed Code - 用于 ASP.NET Core）
if (Test-Path "IIS:\AppPools\$appPoolName") {
    Write-Host "应用程序池 '$appPoolName' 已存在，跳过创建。" -ForegroundColor Yellow
} else {
    New-WebAppPool -Name $appPoolName -Force
    Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value ""
    Write-Host "已创建应用程序池: $appPoolName" -ForegroundColor Green
}

# 2. 创建或更新站点
if (Test-Path "IIS:\Sites\$siteName") {
    Write-Host "站点 '$siteName' 已存在，将更新绑定和路径。" -ForegroundColor Yellow
    Set-ItemProperty "IIS:\Sites\$siteName" -Name physicalPath -Value $webPath
    Set-ItemProperty "IIS:\Sites\$siteName" -Name applicationPool -Value $appPoolName
} else {
    New-Website -Name $siteName -PhysicalPath $webPath -ApplicationPool $appPoolName -Port $port -Force
    Write-Host "已创建站点: $siteName (端口 $port)" -ForegroundColor Green
}

# 3. 添加 API 应用程序
if (-not (Test-Path $webPath)) {
    Write-Host "错误: 前端路径不存在: $webPath" -ForegroundColor Red
    Write-Host "请先运行 build.ps1 生成部署文件。" -ForegroundColor Gray
    exit 1
}

if (-not (Test-Path $apiPath)) {
    Write-Host "错误: 后端路径不存在: $apiPath" -ForegroundColor Red
    Write-Host "请先运行 build.ps1 生成部署文件。" -ForegroundColor Gray
    exit 1
}

# 添加 /api 应用程序 或 创建独立 API 站点
$useSeparateApiSite = $false  # true=独立站点(8082), false=同站点的/api应用
if ($useSeparateApiSite) {
    $apiSiteName = "IIS-Site-Manager-API"
    if (Test-Path "IIS:\Sites\$apiSiteName") {
        Set-ItemProperty "IIS:\Sites\$apiSiteName" -Name physicalPath -Value $apiPath
        Write-Host "API 站点已存在，已更新路径。" -ForegroundColor Yellow
    } else {
        New-Website -Name $apiSiteName -PhysicalPath $apiPath -ApplicationPool $appPoolName -Port $apiPort -Force
        Write-Host "已创建 API 站点: $apiSiteName (端口 $apiPort)" -ForegroundColor Green
    }
} else {
    $apiAppPath = "IIS:\Sites\$siteName\api"
    if (Test-Path $apiAppPath) {
        Set-ItemProperty $apiAppPath -Name physicalPath -Value $apiPath
        Write-Host "API 应用程序已存在，更新路径。" -ForegroundColor Yellow
    } else {
        New-WebApplication -Site $siteName -Name "api" -PhysicalPath $apiPath -ApplicationPool $appPoolName
        Write-Host "已添加 API 应用程序: /api" -ForegroundColor Green
    }
}

# 4. 确保 API 应用池支持 .NET Core
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value ""

# 5. 授予 IIS 读取权限
$acl = Get-Acl $deployRoot
$identity = "IIS_IUSRS"
$fileSystemRights = [System.Security.AccessControl.FileSystemRights]::ReadAndExecute
$type = [System.Security.AccessControl.AccessControlType]::Allow
$inheritance = [System.Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [System.Security.AccessControl.InheritanceFlags]::ObjectInherit
$perm = New-Object System.Security.AccessControl.FileSystemAccessRule($identity, $fileSystemRights, $inheritance, "None", $type)
$acl.SetAccessRule($perm)
Set-Acl -Path $deployRoot -AclObject $acl -ErrorAction SilentlyContinue
Write-Host "已设置 IIS_IUSRS 读取权限." -ForegroundColor Green

Write-Host ""
Write-Host "=== 部署完成 ===" -ForegroundColor Green
Write-Host "前端地址: http://localhost:$port" -ForegroundColor Cyan
if ($useSeparateApiSite) {
    Write-Host "API 地址:  http://localhost:$apiPort (需修改 frontend .env 并重新 build)" -ForegroundColor Cyan
} else {
    Write-Host "API 地址:  http://localhost:$port/api" -ForegroundColor Cyan
}
Write-Host ""
Write-Host "注意: 创建 IIS 站点功能需要应用程序池以高权限运行（管理员）。" -ForegroundColor Yellow
