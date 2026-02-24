namespace LawCorp.Mcp.Core.Models;

public class CaseParty
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public PartyType PartyType { get; set; }
    public int? ContactId { get; set; }
    public string Representation { get; set; } = string.Empty;

    public Case Case { get; set; } = null!;
    public Contact? Contact { get; set; }
}
