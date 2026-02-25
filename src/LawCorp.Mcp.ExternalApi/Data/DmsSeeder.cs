using LawCorp.Mcp.Core.Models;
using LawCorp.Mcp.ExternalApi.Models;

namespace LawCorp.Mcp.ExternalApi.Data;

/// <summary>
/// Seeds the DMS database with realistic legal document management data.
/// Workspaces map to matter numbers that correspond to the MCP server's CaseNumbers.
/// </summary>
public static class DmsSeeder
{
    public static async Task SeedAsync(DmsDbContext db)
    {
        if (db.Workspaces.Any()) return;

        var workspaces = new[]
        {
            CreateWorkspace("M-2025-001", "TechCorp v. DataStream - IP Litigation", "Intellectual Property",
                isRestricted: false,
                docs: [
                    ("Motion to Compel Discovery", DmsDocumentType.Motion, DmsDocumentStatus.Filed, false),
                    ("Expert Witness Report - Software Architecture", DmsDocumentType.Evidence, DmsDocumentStatus.Final, false),
                    ("Privileged Strategy Memo", DmsDocumentType.Memo, DmsDocumentStatus.Final, true),
                    ("Patent Infringement Brief", DmsDocumentType.Brief, DmsDocumentStatus.UnderReview, false),
                ],
                rules: AllRolesRead()),

            CreateWorkspace("M-2025-002", "GlobalBank Merger with Regional Trust", "Mergers & Acquisitions",
                isRestricted: true,
                docs: [
                    ("Merger Agreement - Draft 3", DmsDocumentType.Contract, DmsDocumentStatus.Draft, false),
                    ("Due Diligence Checklist", DmsDocumentType.Other, DmsDocumentStatus.Final, false),
                    ("Board Resolution Template", DmsDocumentType.Filing, DmsDocumentStatus.Draft, false),
                    ("Confidential Valuation Analysis", DmsDocumentType.Memo, DmsDocumentStatus.Final, true),
                    ("Regulatory Filing - SEC Form S-4", DmsDocumentType.Filing, DmsDocumentStatus.UnderReview, false),
                ],
                rules: RestrictedRoles()),

            CreateWorkspace("M-2025-003", "Estate of Henderson - Probate", "Trusts & Estates",
                isRestricted: false,
                docs: [
                    ("Last Will and Testament", DmsDocumentType.Filing, DmsDocumentStatus.Final, true),
                    ("Asset Inventory", DmsDocumentType.Other, DmsDocumentStatus.Final, false),
                    ("Beneficiary Correspondence", DmsDocumentType.Correspondence, DmsDocumentStatus.Final, false),
                ],
                rules: AllRolesRead()),

            CreateWorkspace("M-2025-004", "ClearWater Environmental Compliance", "Environmental Law",
                isRestricted: false,
                docs: [
                    ("EPA Response Letter", DmsDocumentType.Correspondence, DmsDocumentStatus.Filed, false),
                    ("Environmental Impact Assessment", DmsDocumentType.Evidence, DmsDocumentStatus.Final, false),
                    ("Consent Decree Draft", DmsDocumentType.Contract, DmsDocumentStatus.Draft, false),
                ],
                rules: AllRolesRead()),
        };

        db.Workspaces.AddRange(workspaces);
        await db.SaveChangesAsync();
    }

    private static DmsWorkspace CreateWorkspace(
        string matterNumber, string name, string practiceArea, bool isRestricted,
        (string title, DmsDocumentType type, DmsDocumentStatus status, bool privileged)[] docs,
        List<DmsAccessRule> rules)
    {
        var workspace = new DmsWorkspace
        {
            MatterNumber = matterNumber,
            Name = name,
            PracticeArea = practiceArea,
            IsRestricted = isRestricted,
            CreatedDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(30, 365)),
            AccessRules = rules
        };

        workspace.Documents = docs.Select((d, i) => new DmsDocument
        {
            Title = d.title,
            DocumentType = d.type,
            Status = d.status,
            Content = $"[Content of {d.title} would be here in a production system]",
            AuthorName = "System",
            AuthorEntraObjectId = "",
            IsPrivileged = d.privileged,
            CreatedDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 180)),
            ModifiedDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(0, 30)),
            Workspace = workspace
        }).ToList();

        return workspace;
    }

    private static List<DmsAccessRule> AllRolesRead() =>
        Enum.GetValues<FirmRole>().Select(r => new DmsAccessRule
        {
            Role = r,
            CanRead = true,
            CanWrite = r is FirmRole.Partner or FirmRole.Associate or FirmRole.OfCounsel
        }).ToList();

    private static List<DmsAccessRule> RestrictedRoles() =>
        Enum.GetValues<FirmRole>().Select(r => new DmsAccessRule
        {
            Role = r,
            CanRead = r is FirmRole.Partner or FirmRole.Associate,
            CanWrite = r is FirmRole.Partner
        }).ToList();
}
