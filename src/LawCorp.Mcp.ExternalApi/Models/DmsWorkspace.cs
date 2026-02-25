namespace LawCorp.Mcp.ExternalApi.Models;

/// <summary>
/// A workspace in the document management system, mapped to a legal matter/case.
/// </summary>
public class DmsWorkspace
{
    public int Id { get; set; }
    public string MatterNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PracticeArea { get; set; } = string.Empty;
    public bool IsRestricted { get; set; }
    public DateTime CreatedDate { get; set; }

    public ICollection<DmsDocument> Documents { get; set; } = new List<DmsDocument>();
    public ICollection<DmsAccessRule> AccessRules { get; set; } = new List<DmsAccessRule>();
}
