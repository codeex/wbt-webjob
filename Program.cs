using Hangfire;
using Hangfire.MySql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Pomelo.EntityFrameworkCore.MySql.Internal;
using WbtWebJob;
using WbtWebJob.Data;
using WbtWebJob.Hubs;
using WbtWebJob.Services;

var builder = WebApplication.CreateBuilder(args);

// 添加服务到容器
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 添加Session支持（用于向导模式）
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 配置MySQL数据库连接 - 使用HangfireConnection作为唯一数据库
var hangfireConnectionString = builder.Configuration.GetConnectionString("HangfireConnection");
var serverVersion = new MySqlServerVersion(new Version(8, 0, 21));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(hangfireConnectionString, serverVersion, mySqlOptions =>
    {
       // mySqlOptions.MigrationsAssembly(typeof(Program).Assembly.GetName().Name);

        // 其他 MySQL 特定配置
        mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
});

// 配置Hangfire使用相同的MySQL数据库
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
builder.Services.AddScoped<IHangfireJobService, HangfireJobService>();
builder.Services.AddScoped<DetailedMigrationDiagnosticService>();

var app = builder.Build();

// 配置HTTP请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

// 配置Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=CustomJobs}/{action=Index}/{id?}");

app.MapControllers();

app.MapRazorPages();

// 映射SignalR Hub
app.MapHub<JobProgressHub>("/hubs/job-progress");

// 自动检查并创建数据库表
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    //var test = scope.ServiceProvider.GetRequiredService<DetailedMigrationDiagnosticService>();

    try
    {
        //await test.FullDiagnosisAsync();
        logger.LogInformation("开始检查数据库状态...");       
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();      
        //var list2 = db.Database.GetPendingMigrations();
        db.Database.Migrate();
        logger.LogInformation("升级数据库完成.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "数据库初始化失败");
        throw;
    }
}

app.Run();
