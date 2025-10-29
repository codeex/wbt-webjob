# WbtWebJob

一个开放的.NET技术栈的WebJob框架，集成了Hangfire后台任务处理框架和SignalR实时通信。

## 项目简介

WbtWebJob 是一个功能强大、可扩展的后台任务处理系统，基于 .NET 8 和 Hangfire 构建。它提供了完整的任务生命周期管理、实时进度跟踪和可视化监控界面。

## 主要特性

1. **灵活的Job类型支持**
   - 通过API创建WebJob
   - Job创建时根据JobType生成唯一Job ID
   - 支持三种Job类型：
     - **内置Job (BuiltIn)**: 预定义的任务类型
     - **定制化Job (Custom)**: 自定义业务逻辑
     - **HTTP Job**: 可配置HTTP请求的任务
   - 每个Job包含业务ID，可通过业务ID或Job ID查询

2. **完整的日志系统**
   - Job执行后生成唯一日志ID
   - 根据Job ID或业务ID查看执行步骤和详情
   - 支持多级别日志记录（Info, Warning, Error等）
   - 按步骤组织的详细执行记录

3. **HTTP Job自定义配置**
   - 通过Web界面或API配置HTTP请求
   - 支持多种HTTP方法（GET, POST, PUT, DELETE等）
   - 可配置授权方式（None, Basic, Bearer等）
   - 自定义请求头和请求体
   - 可配置超时时间

4. **实时通信支持**
   - 使用SignalR实现WebSocket通信
   - 实时跟踪任务进度
   - 任务完成/失败事件推送
   - 支持订阅特定Job的状态更新

5. **可视化管理**
   - Hangfire Dashboard (/hangfire)
   - Swagger API文档 (/swagger)
   - 任务队列监控
   - 执行历史记录

## 技术栈

- **.NET 8**: 最新的.NET平台
- **ASP.NET Core**: Web API框架
- **Hangfire**: 后台任务处理
- **Entity Framework Core**: ORM框架
- **SQL Server**: 数据存储
- **SignalR**: 实时通信
- **Swagger**: API文档

## 项目结构

```
wbt-webjob/
├── src/
│   ├── WbtWebJob.Api/          # API层 - Web API和SignalR Hubs
│   ├── WbtWebJob.Core/         # 核心层 - 模型、接口、DTOs
│   └── WbtWebJob.Infrastructure/ # 基础设施层 - 数据访问、服务实现
├── docs/
│   ├── API.md                  # API文档
│   └── SETUP.md                # 部署指南
├── Dockerfile                  # Docker镜像构建文件
├── docker-compose.yml          # Docker Compose配置
└── WbtWebJob.sln              # 解决方案文件
```

## 快速开始

### 使用 Docker Compose (推荐)

```bash
# 启动所有服务（包括SQL Server）
docker-compose up -d

# 访问应用
# API: http://localhost:5000
# Swagger: http://localhost:5000/swagger
# Hangfire: http://localhost:5000/hangfire
```

### 本地开发

```bash
# 还原依赖
dotnet restore

# 运行数据库迁移
cd src/WbtWebJob.Api
dotnet ef database update

# 启动应用
dotnet run
```

详细的部署说明请参考 [SETUP.md](docs/SETUP.md)

## API使用示例

### 创建Job

```bash
curl -X POST http://localhost:5000/api/jobs \
  -H "Content-Type: application/json" \
  -d '{
    "businessId": "ORDER_12345",
    "jobType": 1,
    "name": "订单处理任务",
    "description": "处理订单号12345的相关业务"
  }'
```

### 查询Job状态

```bash
# 根据Job ID查询
curl http://localhost:5000/api/jobs/{jobId}

# 根据业务ID查询
curl http://localhost:5000/api/jobs/business/{businessId}
```

### 查看Job日志

```bash
curl http://localhost:5000/api/jobs/{jobId}/logs
```

### SignalR客户端示例 (JavaScript)

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5000/hubs/job-progress")
    .build();

// 连接到Hub
await connection.start();

// 订阅Job进度
await connection.invoke("SubscribeToJob", jobId);

// 监听进度更新
connection.on("JobProgress", (data) => {
    console.log("Progress:", data);
});

// 监听完成事件
connection.on("JobCompleted", (data) => {
    console.log("Completed:", data);
});
```

完整的API文档请参考 [API.md](docs/API.md)

## 许可证

本项目采用 MIT 许可证。详见 [LICENSE](LICENSE) 文件。

## 贡献

欢迎提交 Issue 和 Pull Request！

## 支持

如有问题或建议，请通过 GitHub Issues 联系我们。
