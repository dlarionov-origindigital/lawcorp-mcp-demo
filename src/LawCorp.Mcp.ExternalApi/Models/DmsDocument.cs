namespace LawCorp.Mcp.ExternalApi.Models;

public enum DmsDocumentType { Motion, Brief, Contract, Correspondence, Evidence, Memo, Filing, Other }
public enum DmsDocumentStatus { Draft, UnderReview, Final, Filed, Archived }

/// <summary>
/// A document stored in the external document management system.
/// </summary>
public class DmsDocument
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DmsDocumentType DocumentType { get; set; }
    public DmsDocumentStatus Status { get; set; } = DmsDocumentStatus.Draft;
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorEntraObjectId { get; set; } = string.Empty;
    public bool IsPrivileged { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    public DmsWorkspace Workspace { get; set; } = null!;
}
