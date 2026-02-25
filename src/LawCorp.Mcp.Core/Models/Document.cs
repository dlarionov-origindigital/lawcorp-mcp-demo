namespace LawCorp.Mcp.Core.Models;

public class Document
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public string Content { get; set; } = string.Empty;
    public int AuthorId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public bool IsPrivileged { get; set; }
    public bool IsRedacted { get; set; }

    public Case Case { get; set; } = null!;
    public User Author { get; set; } = null!;
}
