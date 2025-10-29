using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using WbtWebJob.Data;

#nullable disable

namespace WbtWebJob.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250103000000_AddCronExpressionToCustomJob")]
    public partial class AddCronExpressionToCustomJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 为 CustomJobs 表添加 Cron 表达式相关字段
            migrationBuilder.AddColumn<string>(
                name: "CronExpression",
                table: "CustomJobs",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableSchedule",
                table: "CustomJobs",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextExecutionTime",
                table: "CustomJobs",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastExecutionTime",
                table: "CustomJobs",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 删除添加的字段
            migrationBuilder.DropColumn(
                name: "CronExpression",
                table: "CustomJobs");

            migrationBuilder.DropColumn(
                name: "EnableSchedule",
                table: "CustomJobs");

            migrationBuilder.DropColumn(
                name: "NextExecutionTime",
                table: "CustomJobs");

            migrationBuilder.DropColumn(
                name: "LastExecutionTime",
                table: "CustomJobs");
        }
    }
}
