using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using WbtWebJob.Data;

#nullable disable

namespace WbtWebJob.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250102000000_AddCustomJobWizardFields")]
    public partial class AddCustomJobWizardFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 为 CustomJobs 表添加新字段
            migrationBuilder.AddColumn<string>(
                name: "RequestBody",
                table: "CustomJobs",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurlCommand",
                table: "CustomJobs",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssertionExpression",
                table: "CustomJobs",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 删除添加的字段
            migrationBuilder.DropColumn(
                name: "RequestBody",
                table: "CustomJobs");

            migrationBuilder.DropColumn(
                name: "CurlCommand",
                table: "CustomJobs");

            migrationBuilder.DropColumn(
                name: "AssertionExpression",
                table: "CustomJobs");
        }
    }
}
