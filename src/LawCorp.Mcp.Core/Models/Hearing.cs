namespace LawCorp.Mcp.Core.Models;

public class Hearing
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public int CourtId { get; set; }
    public int? JudgeId { get; set; }
    public DateTime DateTime { get; set; }
    public HearingType Type { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public Case Case { get; set; } = null!;
    public Court Court { get; set; } = null!;
    public Contact? Judge { get; set; }
}
