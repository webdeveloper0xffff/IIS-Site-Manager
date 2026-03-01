# IIS 部署指南

## 前置要求

1. **IIS** - 已安装 Internet Information Services
2. **.NET 10 运行时** - 或 ASP.NET Core Hosting Bundle
   - 下载: https://dotnet.microsoft.com/download/dotnet/10.0
   - 安装后需重启 IIS：`iisreset`
3. **URL Rewrite 模块**（可选，当前前端已避免依赖）

## 部署步骤

### 1. 构建部署包

在项目目录运行：

```powershell
cd c:\Users\Administrator\Desktop\hosting_web\IIS-Site-Manager\deploy
.\build.ps1
```

生成内容：
- `api\` - 后端 .NET 应用
- `web\` - 前端静态文件（Next.js 导出）

### 2. 配置到 IIS

**以管理员身份**运行 PowerShell，执行：

```powershell
cd c:\Users\Administrator\Desktop\hosting_web\IIS-Site-Manager\deploy
.\setup-iis.ps1
```

脚本会：
1. 创建应用程序池 `IIS-Site-Manager-Pool`（No Managed Code）
2. 创建站点 `IIS-Site-Manager`，根路径指向 `web\`
3. 添加应用程序 `api`，路径 `/api`，物理路径 `api\`
4. 默认监听端口 `8080`

### 3. 访问

- 前端: http://localhost:8080
- API:  http://localhost:8080/api/metrics
- API:  http://localhost:8080/api/sites

### 4. 修改端口

编辑 `setup-iis.ps1`，修改：

```powershell
$port = 8080  # 改为需要的端口，如 80
```

### 5. 创建 IIS 站点功能

创建新 IIS 站点需要应用程序池以**管理员权限**运行。可在 IIS 管理器中：

1. 选择 `IIS-Site-Manager-Pool`
2. 高级设置 → 标识 → 选择 `LocalSystem` 或配置自定义管理员账户

## 目录结构

```
deploy/
├── api/           # 后端（ASP.NET Core）
│   ├── IIS-Site-Manager.API.dll
│   └── web.config
├── web/           # 前端（静态）
│   ├── index.html
│   ├── _next/
│   └── web.config
├── build.ps1      # 构建脚本
├── setup-iis.ps1  # IIS 配置脚本
└── DEPLOY.md      # 本说明
```

## 推荐部署路径

建议将 `deploy` 目录复制到 IIS 常用路径，如 `C:\inetpub\IIS-Site-Manager`，可避免 Desktop 等目录的权限问题：

```powershell
Copy-Item -Path "C:\...\IIS-Site-Manager\deploy" -Destination "C:\inetpub\IIS-Site-Manager" -Recurse
# 然后修改 setup-iis.ps1 中的 $deployRoot 或直接在该路径运行
```

## 备选方案：后端独立运行

若 IIS 托管 ASP.NET Core 出现 500.19，可让后端独立运行：

```powershell
cd c:\Users\Administrator\Desktop\hosting_web\IIS-Site-Manager\deploy\api
dotnet IIS-Site-Manager.API.dll --urls "http://localhost:5032"
```

然后重新构建前端并设置 `NEXT_PUBLIC_API_URL=http://localhost:5032`，仅将前端部署到 IIS。

## 故障排查

1. **500.19 / 0x8007000d**
   - 确认已安装 ASP.NET Core Hosting Bundle（含 AspNetCoreModuleV2）
   - 或采用上方“备选方案”让后端独立运行

2. **500.19 权限错误**
   - 对 `deploy` 文件夹授予 `IIS_IUSRS` 读取权限
   - 或将部署目录移到 `C:\inetpub\` 下

2. **500 错误 / 模块错误**
   - 确认已安装 ASP.NET Core Hosting Bundle
   - 重启 IIS: `iisreset`

2. **API 返回 404**
   - 检查 `api` 应用程序是否正确添加到站点
   - 确认 `deploy\api` 下有 web.config 和 DLL

3. **前端空白或 404**
   - 若未安装 URL Rewrite，可直接访问 `http://localhost:8080/index.html`
   - 或安装 URL Rewrite 模块
