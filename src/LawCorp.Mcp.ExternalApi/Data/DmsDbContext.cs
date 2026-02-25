using LawCorp.Mcp.ExternalApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LawCorp.Mcp.ExternalApi.Data;

public class DmsDbContext(DbContextOptions<DmsDbContext> options) : DbContext(options)
{
    public DbSet<DmsWorkspace> Workspaces => Set<DmsWorkspace>();
    public DbSet<DmsDocument> Documents => Set<DmsDocument>();
    public DbSet<DmsAccessRule> AccessRules => Set<DmsAccessRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DmsWorkspace>(e =>
        {
            e.HasIndex(w => w.MatterNumber).IsUnique();
        });

        modelBuilder.Entity<DmsDocument>(e =>
        {
            e.HasOne(d => d.Workspace)
                .WithMany(w => w.Documents)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DmsAccessRule>(e =>
        {
            e.HasOne(r => r.Workspace)
                .WithMany(w => w.AccessRules)
                .HasForeignKey(r => r.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            e.Property(r => r.Role).HasConversion<string>();
            e.HasIndex(r => new { r.WorkspaceId, r.Role }).IsUnique();
        });
    }
}
