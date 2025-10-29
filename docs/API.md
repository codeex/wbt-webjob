# WbtWebJob API 文档

## 概述

WbtWebJob 是一个基于 .NET 8 和 Hangfire 的开放式 WebJob 框架，支持创建、执行和监控各种类型的后台任务。

## 主要特性

1. **多种Job类型支持**
   - 内置Job (BuiltIn)
   - 定制化Job (Custom)
   - HTTP Job (Http)

2. **完整的Job生命周期管理**
   - 创建Job
   - 执行Job
   - 监控Job状态
   - 查看执行日志
   - 取消Job

3. **WebSocket实时通信**
   - 使用SignalR实现实时进度推送
   - 支持订阅特定Job的状态更新

4. **Hangfire集成**
   - 可视化管理面板 (/hangfire)
   - 自动重试机制
   - 持久化任务队列

## API端点

### 1. 创建Job

**端点:** `POST /api/jobs`

**请求体:**
```json
{
  "businessId": "ORDER_12345",
  "jobType": 1,
  "name": "订单处理任务",
  "description": "处理订单号12345的相关业务",
  "configuration": "{\"key\":\"value\"}"
}
```

**JobType枚举:**
- 1: BuiltIn (内置Job)
- 2: Custom (定制化Job)
- 3: Http (HTTP Job)

**响应:**
```json
{
  "id": "BI_20231029120000_abc123",
  "businessId": "ORDER_12345",
  "jobType": 1,
  "name": "订单处理任务",
  "description": "处理订单号12345的相关业务",
  "status": 1,
  "configuration": "{\"key\":\"value\"}",
  "createdAt": "2023-10-29T12:00:00Z",
  "updatedAt": null,
  "startedAt": null,
  "completedAt": null
}
```

### 2. 根据JobId查询Job信息

**端点:** `GET /api/jobs/{jobId}`

**响应:**
```json
{
  "id": "BI_20231029120000_abc123",
  "businessId": "ORDER_12345",
  "jobType": 1,
  "name": "订单处理任务",
  "status": 3,
  "createdAt": "2023-10-29T12:00:00Z",
  "startedAt": "2023-10-29T12:00:05Z",
  "completedAt": "2023-10-29T12:00:10Z"
}
```

### 3. 根据业务ID查询Job信息

**端点:** `GET /api/jobs/business/{businessId}`

**响应:** 同上

### 4. 查询Job执行日志

**端点:** `GET /api/jobs/{jobId}/logs`

**响应:**
```json
[
  {
    "id": "log_001",
    "jobId": "BI_20231029120000_abc123",
    "level": "Info",
    "message": "Job started",
    "details": "开始执行任务",
    "stepName": null,
    "createdAt": "2023-10-29T12:00:05Z"
  },
  {
    "id": "log_002",
    "jobId": "BI_20231029120000_abc123",
    "level": "Info",
    "message": "Processing step 1",
    "details": "处理第一步",
    "stepName": "Step1",
    "createdAt": "2023-10-29T12:00:07Z"
  }
]
```

### 5. 取消Job

**端点:** `POST /api/jobs/{jobId}/cancel`

**响应:**
```json
{
  "message": "Job cancelled successfully",
  "jobId": "BI_20231029120000_abc123"
}
```

## JobStatus枚举

- 1: Pending (待执行)
- 2: Running (执行中)
- 3: Completed (已完成)
- 4: Failed (失败)
- 5: Cancelled (已取消)

## SignalR WebSocket 连接

**端点:** `/hubs/job-progress`

### 客户端事件

**订阅Job进度:**
```javascript
connection.invoke("SubscribeToJob", jobId);
```

**取消订阅:**
```javascript
connection.invoke("UnsubscribeFromJob", jobId);
```

### 服务端推送事件

**JobProgress** - Job进度更新
```javascript
connection.on("JobProgress", (data) => {
  console.log("Job progress:", data);
});
```

**JobCompleted** - Job完成
```javascript
connection.on("JobCompleted", (data) => {
  console.log("Job completed:", data);
});
```

**JobFailed** - Job失败
```javascript
connection.on("JobFailed", (data) => {
  console.log("Job failed:", data);
});
```

## HTTP Job 配置示例

```json
{
  "businessId": "HTTP_TASK_001",
  "jobType": 3,
  "name": "HTTP请求任务",
  "configuration": "{\"url\":\"https://api.example.com/data\",\"method\":\"POST\",\"headers\":{\"Authorization\":\"Bearer token\"},\"body\":\"{}\",\"authType\":\"Bearer\",\"timeoutSeconds\":30}"
}
```

## 错误处理

所有API端点在发生错误时返回以下格式:

```json
{
  "error": "错误描述",
  "message": "详细错误信息"
}
```

HTTP状态码:
- 200: 成功
- 201: 创建成功
- 404: 资源未找到
- 500: 服务器内部错误
