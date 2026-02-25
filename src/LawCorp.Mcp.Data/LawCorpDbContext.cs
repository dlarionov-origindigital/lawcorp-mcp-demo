using LawCorp.Mcp.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LawCorp.Mcp.Data;

public class LawCorpDbContext : DbContext
{
    public LawCorpDbContext(DbContextOptions<LawCorpDbContext> options) : base(options) { }

    // People & Organization
    public DbSet<User> Users => Set<User>();
    public DbSet<AttorneyDetails> AttorneyDetails => Set<AttorneyDetails>();
    public DbSet<InternDetails> InternDetails => Set<InternDetails>();
    public DbSet<PracticeGroup> PracticeGroups => Set<PracticeGroup>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Contact> Contacts => Set<Contact>();

    // Case Management
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<CaseAssignment> CaseAssignments => Set<CaseAssignment>();
    public DbSet<CaseParty> CaseParties => Set<CaseParty>();
    public DbSet<CaseEvent> CaseEvents => Set<CaseEvent>();

    // Documents
    public DbSet<Document> Documents => Set<Document>();

    // Billing
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();

    // Calendar & Deadlines
    public DbSet<Hearing> Hearings => Set<Hearing>();
    public DbSet<Deadline> Deadlines => Set<Deadline>();

    // Research, Intake & Audit
    public DbSet<ResearchMemo> ResearchMemos => Set<ResearchMemo>();
    public DbSet<IntakeRequest> IntakeRequests => Set<IntakeRequest>();
    public DbSet<ConflictCheck> ConflictChecks => Set<ConflictCheck>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LawCorpDbContext).Assembly);

        modelBuilder.Entity<AttorneyDetails>(e =>
        {
            e.HasKey(a => a.UserId);
            e.HasOne(a => a.User)
                .WithOne(u => u.AttorneyDetails)
                .HasForeignKey<AttorneyDetails>(a => a.UserId);
        });

        modelBuilder.Entity<InternDetails>(e =>
        {
            e.HasKey(i => i.UserId);
            e.HasOne(i => i.User)
                .WithOne(u => u.InternDetails)
                .HasForeignKey<InternDetails>(i => i.UserId);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasOne(u => u.Supervisor)
                .WithMany()
                .HasForeignKey(u => u.SupervisorId)
                .IsRequired(false);
        });

        modelBuilder.Entity<Document>(e =>
        {
            e.HasOne(d => d.Author)
                .WithMany(u => u.AuthoredDocuments)
                .HasForeignKey(d => d.AuthorId);
        });

        // Prevent SQL Server "multiple cascade paths" errors
        foreach (var fk in modelBuilder.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
