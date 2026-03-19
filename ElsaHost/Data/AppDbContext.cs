using ElsaHost.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElsaHost.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<ApprovalHistory> ApprovalHistories => Set<ApprovalHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.ToTable("LeaveRequests");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EmployeeName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.Property(x => x.CurrentStep).HasMaxLength(100).IsRequired();
            entity.Property(x => x.WorkflowInstanceId).HasMaxLength(100);
        });

        modelBuilder.Entity<ApprovalHistory>(entity =>
        {
            entity.ToTable("ApprovalHistories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ApproverRole).HasMaxLength(50).IsRequired();
            entity.Property(x => x.StepName).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Comment).HasMaxLength(1000);
        });
    }
}