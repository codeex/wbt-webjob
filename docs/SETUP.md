# WbtWebJob 部署指南

## 环境要求

- .NET 8.0 SDK
- SQL Server 2019+ 或 Azure SQL Database
- Docker (可选)

## 本地开发环境搭建

### 1. 安装依赖

确保已安装 .NET 8.0 SDK:
```bash
dotnet --version
```

### 2. 配置数据库连接

编辑 `src/WbtWebJob.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=WbtWebJobDb;Trusted_Connection=True;TrustServerCertificate=True;",
    "HangfireConnection": "Server=localhost;Database=WbtWebJobDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 3. 创建数据库

使用 Entity Framework Core 迁移创建数据库:

```bash
cd src/WbtWebJob.Api

# 添加迁移
dotnet ef migrations add InitialCreate

# 更新数据库
dotnet ef database update
```

### 4. 运行应用

```bash
cd src/WbtWebJob.Api
dotnet run
```

应用将在以下地址启动:
- API: https://localhost:5001
- Swagger UI: https://localhost:5001/swagger
- Hangfire Dashboard: https://localhost:5001/hangfire

## 使用 Docker Compose 部署

### 1. 构建并启动服务

```bash
docker-compose up -d
```

这将启动:
- SQL Server 容器
- WbtWebJob API 容器

### 2. 访问服务

- API: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger
- Hangfire Dashboard: http://localhost:5000/hangfire

### 3. 停止服务

```bash
docker-compose down
```

### 4. 清理数据

```bash
docker-compose down -v
```

## 使用 Docker 单独部署

### 1. 构建镜像

```bash
docker build -t wbt-webjob:latest .
```

### 2. 运行容器

```bash
docker run -d \
  -p 5000:80 \
  -e ConnectionStrings__DefaultConnection="Server=your-db-server;Database=WbtWebJobDb;User Id=sa;Password=YourPassword;" \
  -e ConnectionStrings__HangfireConnection="Server=your-db-server;Database=WbtWebJobDb;User Id=sa;Password=YourPassword;" \
  --name wbt-webjob \
  wbt-webjob:latest
```

## 生产环境配置建议

### 1. 安全配置

- 启用HTTPS
- 配置Hangfire Dashboard身份验证
- 使用强密码和安全的连接字符串
- 配置CORS策略限制允许的来源

### 2. 性能优化

- 配置数据库连接池
- 启用Hangfire的高级配置选项
- 使用Redis作为SignalR的背板 (Scale-out场景)

### 3. 监控和日志

- 集成Application Insights或其他APM工具
- 配置结构化日志 (如Serilog)
- 设置健康检查端点

### 4. 数据库迁移

生产环境建议使用脚本方式执行迁移:

```bash
dotnet ef migrations script -o migration.sql
```

然后由DBA审核并执行SQL脚本。

## 故障排查

### 问题: 数据库连接失败

检查连接字符串是否正确，确保SQL Server正在运行并且防火墙允许连接。

### 问题: Hangfire Dashboard无法访问

确保路由配置正确，检查 Program.cs 中的 `UseHangfireDashboard` 配置。

### 问题: SignalR连接失败

检查CORS配置，确保客户端域名在允许列表中。

## 开发工具推荐

- Visual Studio 2022 / Visual Studio Code
- SQL Server Management Studio (SSMS)
- Postman (API测试)
- Azure Data Studio
