namespace LawCorp.Mcp.Core.Models;

public class ResearchMemo
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public int AuthorId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Findings { get; set; } = string.Empty;
    public string Jurisdiction { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string Tags { get; set; } = string.Empty;

    public Case Case { get; set; } = null!;
    public Attorney Author { get; set; } = null!;
}
