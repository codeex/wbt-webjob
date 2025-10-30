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
    public DbSet<WorkflowNode> WorkflowNodes { get; set; }
    public DbSet<WorkflowConnection> WorkflowConnections { get; set; }

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

        // 配置WorkflowNode
        modelBuilder.Entity<WorkflowNode>(entity =>
        {
            entity.HasIndex(e => e.CustomJobId);
            entity.HasIndex(e => e.NodeType);

            entity.HasOne(e => e.CustomJob)
                .WithMany()
                .HasForeignKey(e => e.CustomJobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置WorkflowConnection
        modelBuilder.Entity<WorkflowConnection>(entity =>
        {
            entity.HasIndex(e => e.CustomJobId);
            entity.HasIndex(e => e.SourceNodeId);
            entity.HasIndex(e => e.TargetNodeId);

            entity.HasOne(e => e.CustomJob)
                .WithMany()
                .HasForeignKey(e => e.CustomJobId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SourceNode)
                .WithMany()
                .HasForeignKey(e => e.SourceNodeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TargetNode)
                .WithMany()
                .HasForeignKey(e => e.TargetNodeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
