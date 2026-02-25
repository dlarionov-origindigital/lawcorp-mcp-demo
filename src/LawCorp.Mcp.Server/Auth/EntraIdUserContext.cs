using LawCorp.Mcp.Core.Auth;
using LawCorp.Mcp.Core.Models;

namespace LawCorp.Mcp.Server.Auth;

public sealed class EntraIdUserContext : IUserContext
{
    public required int UserId { get; init; }
    public required string DisplayName { get; init; }
    public required FirmRole Role { get; init; }
    public bool IsPartner => Role == FirmRole.Partner;
    public bool IsAttorney => Role is FirmRole.Partner or FirmRole.Associate or FirmRole.OfCounsel;
}
