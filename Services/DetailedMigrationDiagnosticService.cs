using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Data.Common;
using System.Reflection;
using WbtWebJob.Data;

namespace WbtWebJob.Services
{
    public class DetailedMigrationDiagnosticService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DetailedMigrationDiagnosticService> _logger;

        public DetailedMigrationDiagnosticService(ApplicationDbContext context, ILogger<DetailedMigrationDiagnosticService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task FullDiagnosisAsync()
        {
            _logger.LogInformation("=== 详细迁移诊断开始 ===");

            // 1. 检查程序集中的迁移
            await CheckMigrationsInAssembly();

            // 2. 检查 IMigrationsAssembly 服务
            await CheckMigrationsAssemblyService();

            // 3. 检查数据库迁移历史
            await CheckDatabaseMigrationHistory();

            // 4. 检查迁移模型快照
            CheckMigrationModelSnapshot();

            _logger.LogInformation("=== 详细迁移诊断结束 ===");
        }

        private async Task CheckMigrationsInAssembly()
        {
            _logger.LogInformation("1. 检查程序集中的迁移...");

            var currentAssembly = typeof(Program).Assembly;
            var migrationTypes = currentAssembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(Migration)) && !t.IsAbstract)
                .ToList();

            _logger.LogInformation("找到 {Count} 个迁移类:", migrationTypes.Count);

            foreach (var migrationType in migrationTypes)
            {
                _logger.LogInformation("  - {Name} (Full: {FullName})",
                    migrationType.Name, migrationType.FullName);

                // 检查迁移特性
                var migrationAttribute = migrationType.GetCustomAttribute<MigrationAttribute>();
                if (migrationAttribute != null)
                {
                    _logger.LogInformation("    迁移ID: {Id}", migrationAttribute.Id);
                }

                // 检查是否有对应的 Designer 类
                var designerType = currentAssembly.GetTypes()
                    .FirstOrDefault(t => t.Name == migrationType.Name + "ModelSnapshot");
                _logger.LogInformation("    模型快照: {Exists}", designerType != null ? "存在" : "缺失");
            }
        }

        private async Task CheckMigrationsAssemblyService()
        {
            _logger.LogInformation("2. 检查 IMigrationsAssembly 服务...");

            try
            {
                // 获取 IMigrationsAssembly 服务
                var serviceProvider = _context.GetService<IServiceProvider>();
                var migrationsAssembly = serviceProvider.GetService<IMigrationsAssembly>();

                if (migrationsAssembly == null)
                {
                    _logger.LogError("IMigrationsAssembly 服务未注册");
                    return;
                }

                _logger.LogInformation("IMigrationsAssembly 类型: {Type}", migrationsAssembly.GetType().FullName);
                _logger.LogInformation("迁移程序集: {Assembly}", migrationsAssembly.Assembly.FullName);

                // 尝试通过服务获取迁移
                var migrations = migrationsAssembly.Migrations;
                _logger.LogInformation("通过 IMigrationsAssembly 找到 {Count} 个迁移", migrations.Count);

                foreach (var migration in migrations)
                {
                    _logger.LogInformation("  - {Key} -> {Type}", migration.Key, migration.Value.Name);
                }

                // 检查迁移ID
                var migrationIds = migrationsAssembly.Migrations.Keys.OrderBy(x => x).ToList();
                _logger.LogInformation("迁移ID列表: {Ids}", string.Join(", ", migrationIds));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查 IMigrationsAssembly 服务时发生错误");
            }
        }

        private async Task CheckDatabaseMigrationHistory()
        {
            _logger.LogInformation("3. 检查数据库迁移历史...");

            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                _logger.LogInformation("数据库连接: {Status}", canConnect ? "成功" : "失败");

                if (canConnect)
                {
                    var connection = _context.Database.GetDbConnection();
                    await connection.OpenAsync();

                    try
                    {
                        // 检查迁移历史表
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = @"
                            SELECT COUNT(*) 
                            FROM information_schema.tables 
                            WHERE table_schema = DATABASE() 
                            AND table_name = '__EFMigrationsHistory'";

                            var tableExists = (long)await command.ExecuteScalarAsync() > 0;
                            _logger.LogInformation("迁移历史表存在: {Exists}", tableExists);

                            if (tableExists)
                            {
                                command.CommandText = "SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId";
                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    var records = new List<string>();
                                    while (await reader.ReadAsync())
                                    {
                                        records.Add($"{reader["MigrationId"]} (EF Core {reader["ProductVersion"]})");
                                    }
                                    _logger.LogInformation("迁移历史记录 ({Count}):", records.Count);
                                    foreach (var record in records)
                                    {
                                        _logger.LogInformation("  - {Record}", record);
                                    }
                                }
                            }
                            else
                            {
                                _logger.LogWarning("迁移历史表不存在，将尝试创建...");
                                await CreateMigrationHistoryTable(connection);
                            }
                        }
                    }
                    finally
                    {
                        await connection.CloseAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查数据库迁移历史时发生错误");
            }
        }

        private async Task CreateMigrationHistoryTable(DbConnection connection)
        {
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
                        `MigrationId` varchar(150) NOT NULL,
                        `ProductVersion` varchar(32) NOT NULL,
                        PRIMARY KEY (`MigrationId`)
                    )";

                    await command.ExecuteNonQueryAsync();
                    _logger.LogInformation("迁移历史表创建成功");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建迁移历史表失败");
            }
        }

        private void CheckMigrationModelSnapshot()
        {
            _logger.LogInformation("4. 检查迁移模型快照...");

            try
            {
                var currentAssembly = typeof(Program).Assembly;
                var snapshotTypes = currentAssembly.GetTypes()
                    .Where(t => t.Name.EndsWith("ModelSnapshot") && !t.IsAbstract)
                    .ToList();

                _logger.LogInformation("找到 {Count} 个模型快照:", snapshotTypes.Count);
                foreach (var snapshotType in snapshotTypes)
                {
                    _logger.LogInformation("  - {Name}", snapshotType.Name);
                }

                // 检查 DbContext 模型差异
                var model = _context.Model;
                _logger.LogInformation("当前 DbContext 模型: {ModelHash}", model.GetHashCode());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查迁移模型快照时发生错误");
            }
        }
    }
}
