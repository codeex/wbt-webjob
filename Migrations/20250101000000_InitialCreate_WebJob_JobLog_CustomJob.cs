using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WbtWebJob.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_WebJob_JobLog_CustomJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 创建 WebJobs 表
            migrationBuilder.CreateTable(
                name: "WebJobs",
                columns: table => new
                {
                    JobId = table.Column<Guid>(type: "char(36)", nullable: false),
                    JobType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    BusinessId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    JobParameters = table.Column<string>(type: "longtext", nullable: true),
                    HangfireJobId = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebJobs", x => x.JobId);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            // 为 WebJobs 创建索引
            migrationBuilder.CreateIndex(
                name: "IX_WebJobs_BusinessId",
                table: "WebJobs",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_WebJobs_JobType",
                table: "WebJobs",
                column: "JobType");

            migrationBuilder.CreateIndex(
                name: "IX_WebJobs_Status",
                table: "WebJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WebJobs_CreatedAt",
                table: "WebJobs",
                column: "CreatedAt");

            // 创建 JobLogs 表
            migrationBuilder.CreateTable(
                name: "JobLogs",
                columns: table => new
                {
                    LogId = table.Column<Guid>(type: "char(36)", nullable: false),
                    JobId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Step = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Level = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "longtext", nullable: true),
                    Details = table.Column<string>(type: "longtext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobLogs", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_JobLogs_WebJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "WebJobs",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            // 为 JobLogs 创建索引
            migrationBuilder.CreateIndex(
                name: "IX_JobLogs_JobId",
                table: "JobLogs",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobLogs_CreatedAt",
                table: "JobLogs",
                column: "CreatedAt");

            // 创建 CustomJobs 表
            migrationBuilder.CreateTable(
                name: "CustomJobs",
                columns: table => new
                {
                    CustomJobId = table.Column<Guid>(type: "char(36)", nullable: false),
                    JobType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    HttpUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    HttpMethod = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    AuthType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    AuthConfig = table.Column<string>(type: "longtext", nullable: true),
                    Headers = table.Column<string>(type: "longtext", nullable: true),
                    DefaultParameters = table.Column<string>(type: "longtext", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomJobs", x => x.CustomJobId);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            // 为 CustomJobs 创建索引
            migrationBuilder.CreateIndex(
                name: "IX_CustomJobs_JobType",
                table: "CustomJobs",
                column: "JobType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomJobs_IsActive",
                table: "CustomJobs",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 删除 JobLogs 表（先删除有外键的表）
            migrationBuilder.DropTable(
                name: "JobLogs");

            // 删除 CustomJobs 表
            migrationBuilder.DropTable(
                name: "CustomJobs");

            // 删除 WebJobs 表
            migrationBuilder.DropTable(
                name: "WebJobs");
        }
    }
}
