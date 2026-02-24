namespace LawCorp.Mcp.Core.Models;

public class Paralegal
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int PracticeGroupId { get; set; }
    public DateOnly HireDate { get; set; }
    public string? EntraObjectId { get; set; }

    public PracticeGroup PracticeGroup { get; set; } = null!;
    public ICollection<Attorney> AssignedAttorneys { get; set; } = new List<Attorney>();
}
