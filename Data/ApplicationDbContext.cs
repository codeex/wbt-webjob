using Microsoft.EntityFrameworkCore;
using WbtWebJob.Models;

namespace WbtWebJob.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<WebJob> WebJobs { get; set; }
    public DbSet<JobLog> JobLogs { get; set; }
    public DbSet<CustomJob> CustomJobs { get; set; }

    // DAG工作流相关表
    public DbSet<Workflow> Workflows { get; set; }
    public DbSet<WorkflowNode> WorkflowNodes { get; set; }
    public DbSet<WorkflowEdge> WorkflowEdges { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置WebJob
        modelBuilder.Entity<WebJob>(entity =>
        {
            entity.HasIndex(e => e.BusinessId);
            entity.HasIndex(e => e.JobType);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        // 配置JobLog
        modelBuilder.Entity<JobLog>(entity =>
        {
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.WebJob)
                .WithMany(w => w.JobLogs)
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置CustomJob
        modelBuilder.Entity<CustomJob>(entity =>
        {
            entity.HasIndex(e => e.JobType).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        // 配置Workflow
        modelBuilder.Entity<Workflow>(entity =>
        {
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasMany(e => e.Nodes)
                .WithOne(n => n.Workflow)
                .HasForeignKey(n => n.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Edges)
                .WithOne(e => e.Workflow)
                .HasForeignKey(e => e.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置WorkflowNode
        modelBuilder.Entity<WorkflowNode>(entity =>
        {
            entity.HasIndex(e => e.WorkflowId);
            entity.HasIndex(e => e.NodeType);
        });

        // 配置WorkflowEdge
        modelBuilder.Entity<WorkflowEdge>(entity =>
        {
            entity.HasIndex(e => e.WorkflowId);
            entity.HasIndex(e => new { e.SourceNodeId, e.TargetNodeId });

            // 配置源节点关系（不使用级联删除，避免多路径问题）
            entity.HasOne(e => e.SourceNode)
                .WithMany()
                .HasForeignKey(e => e.SourceNodeId)
                .OnDelete(DeleteBehavior.Restrict);

            // 配置目标节点关系
            entity.HasOne(e => e.TargetNode)
                .WithMany()
                .HasForeignKey(e => e.TargetNodeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
