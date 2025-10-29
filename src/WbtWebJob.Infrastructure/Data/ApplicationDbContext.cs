using Microsoft.EntityFrameworkCore;
using WbtWebJob.Core.Models;

namespace WbtWebJob.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<WebJob> WebJobs { get; set; }
    public DbSet<JobLog> JobLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WebJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.BusinessId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.HangfireJobId).HasMaxLength(50);
            entity.HasIndex(e => e.BusinessId);
            entity.HasIndex(e => e.HangfireJobId);
        });

        modelBuilder.Entity<JobLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.JobId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Level).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.StepName).HasMaxLength(100);
            entity.HasIndex(e => e.JobId);

            entity.HasOne(e => e.Job)
                .WithMany(e => e.Logs)
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
