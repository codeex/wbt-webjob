# wbt-webjob

构造一个开放的.NET技术栈的WebJob，集成了Hangfire框架，使用MySQL数据库。

## Features

1. **通过API创建WebJob**：job创建时根据jobtype创建job id，jobtype可以指定内置job任务，也可以是定制化job；创建的job包含业务id，由调用方传入，可根据业务id查到job id。根据jobid可以查到job的信息。

2. **完整的日志记录**：job执行后产生唯一日志id，根据job id或业务id可以查看业务执行的步骤和详情。

3. **自定义Job配置**：可通过Web API进行构建，支持配置HTTP授权方法以及HTTP执行方法。

4. **WebSocket实时通讯**：支持WebSocket接口通讯（基于SignalR），可以实时跟踪任务的进度、完成等事件。

5. **MySQL数据库**：使用MySQL作为主数据库和Hangfire存储，支持高性能和可扩展性。

## 技术栈

- **.NET 8.0**
- **Hangfire** - 后台任务调度框架
- **MySQL** - 数据库
- **Entity Framework Core** - ORM框架
- **Pomelo.EntityFrameworkCore.MySql** - MySQL驱动
- **SignalR** - WebSocket通讯
- **Swagger** - API文档

## 快速开始

### 1. 启动MySQL数据库

使用Docker Compose快速启动MySQL：

```bash
docker-compose up -d
```

详细的数据库配置请参考 [MYSQL_SETUP.md](MYSQL_SETUP.md)

### 2. 配置连接字符串

复制环境变量示例文件：

```bash
cp .env.example .env
```

或直接编辑 `appsettings.json` 更新MySQL连接字符串。

### 3. 运行应用程序

```bash
dotnet restore
dotnet run
```

应用程序会自动应用数据库迁移。

### 4. 访问应用

- **Swagger API文档**: http://localhost:5000/swagger
- **Hangfire Dashboard**: http://localhost:5000/hangfire
- **SignalR Hub**: ws://localhost:5000/hubs/job-progress

## API端点

### Job管理

- `POST /api/jobs` - 创建新Job
- `GET /api/jobs/{jobId}` - 根据Job ID获取Job信息
- `GET /api/jobs/business/{businessId}` - 根据业务ID获取Job信息
- `GET /api/jobs/{jobId}/logs` - 获取Job执行日志
- `GET /api/jobs/business/{businessId}/logs` - 根据业务ID获取日志
- `GET /api/jobs/type/{jobType}` - 获取指定类型的所有Jobs

### 自定义Job管理

- `POST /api/customjobs` - 创建自定义Job配置
- `GET /api/customjobs` - 获取所有自定义Job
- `GET /api/customjobs/{jobType}` - 获取指定类型的自定义Job
- `PUT /api/customjobs/{customJobId}` - 更新自定义Job配置
- `DELETE /api/customjobs/{customJobId}` - 删除自定义Job配置

## 使用示例

### 创建Job

```bash
curl -X POST http://localhost:5000/api/jobs \
  -H "Content-Type: application/json" \
  -d '{
    "jobType": "data-export",
    "businessId": "ORDER-12345",
    "description": "Export order data",
    "parameters": {
      "startDate": "2024-01-01",
      "endDate": "2024-01-31"
    }
  }'
```

### 查询Job状态

```bash
curl http://localhost:5000/api/jobs/business/ORDER-12345
```

### WebSocket订阅（使用SignalR客户端）

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5000/hubs/job-progress")
    .build();

connection.on("JobProgress", (data) => {
    console.log("Job progress:", data);
});

await connection.start();
await connection.invoke("SubscribeToJob", "ORDER-12345");
```

### 创建自定义Job

```bash
curl -X POST http://localhost:5000/api/customjobs \
  -H "Content-Type: application/json" \
  -d '{
    "jobType": "webhook-notification",
    "name": "Webhook Notification Job",
    "description": "Send webhook notification",
    "httpUrl": "https://api.example.com/webhook",
    "httpMethod": "POST",
    "authType": "Bearer",
    "authConfig": "{\"token\":\"your-token\"}",
    "headers": "{\"Content-Type\":\"application/json\"}"
  }'
```

## 数据库结构

项目使用MySQL数据库，包含以下主要表：

- **WebJobs** - 存储所有Job信息
- **JobLogs** - 存储Job执行日志
- **CustomJobs** - 存储自定义Job配置
- **Hangfire_*** - Hangfire框架表

详细的数据库架构和迁移说明请参考 [MYSQL_SETUP.md](MYSQL_SETUP.md)

## 开发

### 创建数据库迁移

```bash
dotnet ef migrations add MigrationName
```

### 应用迁移

```bash
dotnet ef database update
```

## License

本项目采用MIT许可证。
