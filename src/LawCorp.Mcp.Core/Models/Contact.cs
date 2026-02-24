namespace LawCorp.Mcp.Core.Models;

public class Contact
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public ContactType Type { get; set; }
    public string Organization { get; set; } = string.Empty;
    public string ContactInfo { get; set; } = string.Empty;
    public string Jurisdiction { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public ICollection<CaseParty> CaseParties { get; set; } = new List<CaseParty>();
}
