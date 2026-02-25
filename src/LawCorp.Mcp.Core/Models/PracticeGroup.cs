namespace LawCorp.Mcp.Core.Models;

public class PracticeGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Case> Cases { get; set; } = new List<Case>();
    public ICollection<IntakeRequest> IntakeRequests { get; set; } = new List<IntakeRequest>();
}
