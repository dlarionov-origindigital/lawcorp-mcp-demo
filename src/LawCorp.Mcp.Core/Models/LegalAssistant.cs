namespace LawCorp.Mcp.Core.Models;

public class LegalAssistant
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int AssignedAttorneyId { get; set; }
    public DateOnly HireDate { get; set; }
    public string? EntraObjectId { get; set; }

    public Attorney AssignedAttorney { get; set; } = null!;
}
