namespace LawCorp.Mcp.Core.Models;

public class IntakeRequest
{
    public int Id { get; set; }
    public string ProspectName { get; set; } = string.Empty;
    public string ContactInfo { get; set; } = string.Empty;
    public string MatterDescription { get; set; } = string.Empty;
    public int PracticeGroupId { get; set; }
    public string ReferralSource { get; set; } = string.Empty;
    public IntakeStatus Status { get; set; } = IntakeStatus.Pending;
    public DateTime CreatedDate { get; set; }
    public int? ReviewedById { get; set; }

    public PracticeGroup PracticeGroup { get; set; } = null!;
    public Attorney? ReviewedBy { get; set; }
    public ICollection<ConflictCheck> ConflictChecks { get; set; } = new List<ConflictCheck>();
}
