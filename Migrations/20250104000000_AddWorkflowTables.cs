using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WbtWebJob.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 创建Workflows表
            migrationBuilder.CreateTable(
                name: "Workflows",
                columns: table => new
                {
                    WorkflowId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    XmlContent = table.Column<string>(type: "longtext", nullable: true),
                    XmlFilePath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CronExpression = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    EnableSchedule = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    NextExecutionTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastExecutionTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflows", x => x.WorkflowId);
                });

            // 创建WorkflowNodes表
            migrationBuilder.CreateTable(
                name: "WorkflowNodes",
                columns: table => new
                {
                    NodeId = table.Column<Guid>(type: "char(36)", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "char(36)", nullable: false),
                    NodeType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    Configuration = table.Column<string>(type: "longtext", nullable: true),
                    PositionX = table.Column<double>(type: "double", nullable: false),
                    PositionY = table.Column<double>(type: "double", nullable: false),
                    StyleConfig = table.Column<string>(type: "longtext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowNodes", x => x.NodeId);
                    table.ForeignKey(
                        name: "FK_WorkflowNodes_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "WorkflowId",
                        onDelete: ReferentialAction.Cascade);
                });

            // 创建WorkflowEdges表
            migrationBuilder.CreateTable(
                name: "WorkflowEdges",
                columns: table => new
                {
                    EdgeId = table.Column<Guid>(type: "char(36)", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SourceNodeId = table.Column<Guid>(type: "char(36)", nullable: false),
                    TargetNodeId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Label = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Condition = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    StyleConfig = table.Column<string>(type: "longtext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowEdges", x => x.EdgeId);
                    table.ForeignKey(
                        name: "FK_WorkflowEdges_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "WorkflowId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowEdges_WorkflowNodes_SourceNodeId",
                        column: x => x.SourceNodeId,
                        principalTable: "WorkflowNodes",
                        principalColumn: "NodeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowEdges_WorkflowNodes_TargetNodeId",
                        column: x => x.TargetNodeId,
                        principalTable: "WorkflowNodes",
                        principalColumn: "NodeId",
                        onDelete: ReferentialAction.Restrict);
                });

            // 创建索引
            migrationBuilder.CreateIndex(
                name: "IX_Workflows_Name",
                table: "Workflows",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_IsActive",
                table: "Workflows",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_CreatedAt",
                table: "Workflows",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowNodes_WorkflowId",
                table: "WorkflowNodes",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowNodes_NodeType",
                table: "WorkflowNodes",
                column: "NodeType");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowEdges_WorkflowId",
                table: "WorkflowEdges",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowEdges_SourceNodeId_TargetNodeId",
                table: "WorkflowEdges",
                columns: new[] { "SourceNodeId", "TargetNodeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "WorkflowEdges");
            migrationBuilder.DropTable(name: "WorkflowNodes");
            migrationBuilder.DropTable(name: "Workflows");
        }
    }
}
