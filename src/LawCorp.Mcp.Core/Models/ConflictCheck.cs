namespace LawCorp.Mcp.Core.Models;

public class ConflictCheck
{
    public int Id { get; set; }
    public int IntakeRequestId { get; set; }
    public int CheckedById { get; set; }
    public DateTime CheckDate { get; set; }
    public ConflictCheckStatus Status { get; set; }
    public string Notes { get; set; } = string.Empty;

    public IntakeRequest IntakeRequest { get; set; } = null!;
    public User CheckedBy { get; set; } = null!;
}
