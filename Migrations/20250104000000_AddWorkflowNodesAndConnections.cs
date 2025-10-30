using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using WbtWebJob.Data;

#nullable disable

namespace WbtWebJob.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250104000000_AddWorkflowNodesAndConnections")]
    public partial class AddWorkflowNodesAndConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 创建 WorkflowNodes 表
            migrationBuilder.CreateTable(
                name: "WorkflowNodes",
                columns: table => new
                {
                    NodeId = table.Column<Guid>(type: "char(36)", nullable: false),
                    CustomJobId = table.Column<Guid>(type: "char(36)", nullable: false),
                    NodeType = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    PositionX = table.Column<double>(type: "double", nullable: false),
                    PositionY = table.Column<double>(type: "double", nullable: false),
                    Configuration = table.Column<string>(type: "longtext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowNodes", x => x.NodeId);
                    table.ForeignKey(
                        name: "FK_WorkflowNodes_CustomJobs_CustomJobId",
                        column: x => x.CustomJobId,
                        principalTable: "CustomJobs",
                        principalColumn: "CustomJobId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            // 创建 WorkflowConnections 表
            migrationBuilder.CreateTable(
                name: "WorkflowConnections",
                columns: table => new
                {
                    ConnectionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    CustomJobId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SourceNodeId = table.Column<Guid>(type: "char(36)", nullable: false),
                    TargetNodeId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SourcePort = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    TargetPort = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowConnections", x => x.ConnectionId);
                    table.ForeignKey(
                        name: "FK_WorkflowConnections_CustomJobs_CustomJobId",
                        column: x => x.CustomJobId,
                        principalTable: "CustomJobs",
                        principalColumn: "CustomJobId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowConnections_WorkflowNodes_SourceNodeId",
                        column: x => x.SourceNodeId,
                        principalTable: "WorkflowNodes",
                        principalColumn: "NodeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowConnections_WorkflowNodes_TargetNodeId",
                        column: x => x.TargetNodeId,
                        principalTable: "WorkflowNodes",
                        principalColumn: "NodeId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            // 创建索引
            migrationBuilder.CreateIndex(
                name: "IX_WorkflowNodes_CustomJobId",
                table: "WorkflowNodes",
                column: "CustomJobId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowNodes_NodeType",
                table: "WorkflowNodes",
                column: "NodeType");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowConnections_CustomJobId",
                table: "WorkflowConnections",
                column: "CustomJobId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowConnections_SourceNodeId",
                table: "WorkflowConnections",
                column: "SourceNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowConnections_TargetNodeId",
                table: "WorkflowConnections",
                column: "TargetNodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 删除表
            migrationBuilder.DropTable(name: "WorkflowConnections");
            migrationBuilder.DropTable(name: "WorkflowNodes");
        }
    }
}
