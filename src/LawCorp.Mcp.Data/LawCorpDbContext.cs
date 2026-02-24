using LawCorp.Mcp.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LawCorp.Mcp.Data;

public class LawCorpDbContext : DbContext
{
    public LawCorpDbContext(DbContextOptions<LawCorpDbContext> options) : base(options) { }

    // People & Organization
    public DbSet<PracticeGroup> PracticeGroups => Set<PracticeGroup>();
    public DbSet<Attorney> Attorneys => Set<Attorney>();
    public DbSet<Paralegal> Paralegals => Set<Paralegal>();
    public DbSet<LegalAssistant> LegalAssistants => Set<LegalAssistant>();
    public DbSet<Intern> Interns => Set<Intern>();
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

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LawCorpDbContext).Assembly);
    }
}
