namespace LawCorp.Mcp.Core.Models;

public class PracticeGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ICollection<Attorney> Attorneys { get; set; } = new List<Attorney>();
    public ICollection<Paralegal> Paralegals { get; set; } = new List<Paralegal>();
    public ICollection<Intern> Interns { get; set; } = new List<Intern>();
    public ICollection<Case> Cases { get; set; } = new List<Case>();
    public ICollection<IntakeRequest> IntakeRequests { get; set; } = new List<IntakeRequest>();
}
