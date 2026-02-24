namespace LawCorp.Mcp.Core.Models;

public class Court
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Jurisdiction { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;

    public ICollection<Case> Cases { get; set; } = new List<Case>();
    public ICollection<Hearing> Hearings { get; set; } = new List<Hearing>();
}
