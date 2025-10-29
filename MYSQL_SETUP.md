# MySQL数据库配置指南

本项目使用MySQL作为数据库，支持通过Docker快速部署或连接到现有的MySQL实例。

## 快速开始 - 使用Docker

### 1. 启动MySQL容器

```bash
docker-compose up -d
```

这将启动一个MySQL 8.0容器，并自动创建以下数据库：
- `wbt_webjob` - 应用程序主数据库
- `wbt_webjob_hangfire` - Hangfire任务调度数据库

默认配置：
- 端口: 3306
- Root密码: root_password
- 用户: wbt_user
- 密码: wbt_password

### 2. 更新连接字符串

编辑 `appsettings.json` 或创建 `.env` 文件：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=wbt_webjob;User=wbt_user;Password=wbt_password;",
    "HangfireConnection": "Server=localhost;Port=3306;Database=wbt_webjob_hangfire;User=wbt_user;Password=wbt_password;"
  }
}
```

### 3. 运行数据库迁移

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

或者直接运行应用程序，它会自动应用迁移：

```bash
dotnet run
```

## 使用现有MySQL实例

### 1. 创建数据库

```sql
CREATE DATABASE wbt_webjob CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE wbt_webjob_hangfire CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

### 2. 创建用户并授权（可选）

```sql
CREATE USER 'wbt_user'@'%' IDENTIFIED BY 'your_password';
GRANT ALL PRIVILEGES ON wbt_webjob.* TO 'wbt_user'@'%';
GRANT ALL PRIVILEGES ON wbt_webjob_hangfire.* TO 'wbt_user'@'%';
FLUSH PRIVILEGES;
```

### 3. 配置连接字符串

更新 `appsettings.json` 中的连接字符串，替换为你的MySQL服务器信息。

## 数据库架构

### WebJobs 表
存储所有WebJob的信息：
- JobId (Guid) - 主键
- JobType (string) - Job类型
- BusinessId (string) - 业务ID
- Status (string) - 状态 (Pending, Running, Completed, Failed)
- CreatedAt, StartedAt, CompletedAt (DateTime)

### JobLogs 表
存储Job执行日志：
- LogId (Guid) - 主键
- JobId (Guid) - 外键关联WebJobs
- Step (string) - 步骤名称
- Level (string) - 日志级别
- Message (string) - 消息内容
- Details (JSON) - 详细信息

### CustomJobs 表
存储自定义Job配置：
- CustomJobId (Guid) - 主键
- JobType (string) - Job类型（唯一）
- HttpUrl (string) - HTTP执行地址
- HttpMethod (string) - HTTP方法
- AuthType (string) - 认证类型
- AuthConfig (JSON) - 认证配置

### Hangfire表
Hangfire会自动创建所需的表，前缀为 `Hangfire`

## Entity Framework Core 命令

### 创建新迁移

```bash
dotnet ef migrations add MigrationName
```

### 应用迁移

```bash
dotnet ef database update
```

### 回滚到指定迁移

```bash
dotnet ef database update MigrationName
```

### 删除最后一次迁移

```bash
dotnet ef migrations remove
```

### 查看所有迁移

```bash
dotnet ef migrations list
```

## 连接字符串格式

标准格式：
```
Server=<host>;Port=3306;Database=<database>;User=<username>;Password=<password>;
```

支持的其他参数：
- `SslMode=None` - 禁用SSL
- `AllowPublicKeyRetrieval=True` - 允许检索公钥
- `CharSet=utf8mb4` - 字符集
- `ConnectionTimeout=30` - 连接超时（秒）

示例：
```
Server=localhost;Port=3306;Database=wbt_webjob;User=root;Password=password;SslMode=None;
```

## 故障排查

### 连接被拒绝
确保MySQL服务正在运行：
```bash
docker ps
```

### 认证失败
检查用户名和密码是否正确，或尝试使用root用户。

### 表不存在
运行数据库迁移：
```bash
dotnet ef database update
```

### Hangfire表创建失败
确保 `PrepareSchemaIfNecessary = true` 在Hangfire配置中。
