using Hangfire;
using Hangfire.MySql;
using Microsoft.EntityFrameworkCore;
using WbtWebJob.Data;
using WbtWebJob.Hubs;
using WbtWebJob.Services;

var builder = WebApplication.CreateBuilder(args);

// 添加服务到容器
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 配置MySQL数据库连接
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var serverVersion = new MySqlServerVersion(new Version(8, 0, 21));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

// 配置Hangfire使用MySQL存储
var hangfireConnectionString = builder.Configuration.GetConnectionString("HangfireConnection");
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseStorage(new MySqlStorage(
        hangfireConnectionString,
        new MySqlStorageOptions
        {
            QueuePollInterval = TimeSpan.FromSeconds(15),
            JobExpirationCheckInterval = TimeSpan.FromHours(1),
            CountersAggregateInterval = TimeSpan.FromMinutes(5),
            PrepareSchemaIfNecessary = true,
            DashboardJobListLimit = 50000,
            TransactionTimeout = TimeSpan.FromMinutes(1),
            TablesPrefix = "Hangfire"
        }
    )));

// 添加Hangfire服务器
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = builder.Configuration.GetValue<int>("Hangfire:WorkerCount", 5);
    options.Queues = builder.Configuration.GetSection("Hangfire:Queues").Get<string[]>()
        ?? new[] { "default", "critical", "normal" };
});

// 添加SignalR用于WebSocket通信
builder.Services.AddSignalR();

// 添加CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 添加HttpClient支持
builder.Services.AddHttpClient();

// 注册服务
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<ICustomJobService, CustomJobService>();
builder.Services.AddScoped<IJobExecutor, JobExecutor>();

var app = builder.Build();

// 配置HTTP请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

// 配置Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.MapControllers();

// 映射SignalR Hub
app.MapHub<JobProgressHub>("/hubs/job-progress");

// 自动应用数据库迁移
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
