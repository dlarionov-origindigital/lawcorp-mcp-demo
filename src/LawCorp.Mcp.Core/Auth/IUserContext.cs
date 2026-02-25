using LawCorp.Mcp.Core.Models;

namespace LawCorp.Mcp.Core.Auth;

/// <summary>
/// Represents the identity and role of the current caller.
/// Injected by the auth layer. Use <see cref="AnonymousUserContext"/> (in Server project)
/// when UseAuth=false for local dev and demo mode.
/// </summary>
public interface IUserContext
{
    int UserId { get; }
    string DisplayName { get; }
    FirmRole Role { get; }
    bool IsPartner { get; }
    bool IsAttorney { get; }
}
