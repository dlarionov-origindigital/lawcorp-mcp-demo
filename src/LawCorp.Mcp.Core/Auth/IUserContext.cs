using LawCorp.Mcp.Core.Models;

namespace LawCorp.Mcp.Core.Auth;

/// <summary>
/// Represents the identity and role of the current caller.
/// Injected by the auth layer. Use <see cref="AnonymousUserContext"/> (in Server project)
/// when UseAuth=false for local dev and demo mode.
/// When UseAuth=true, wire in an Entra ID implementation (Epic 1.2).
/// </summary>
public interface IUserContext
{
    /// <summary>Numeric attorney ID — used for row-level filtering and audit log writes.</summary>
    int UserId { get; }

    /// <summary>Display name for audit log and event attribution.</summary>
    string DisplayName { get; }

    /// <summary>Role determines row-level access and operation permissions.</summary>
    AttorneyRole Role { get; }

    /// <summary>True for Partner role only — grants unrestricted case access and admin actions.</summary>
    bool IsPartner { get; }

    /// <summary>True for Partner, Associate, and OfCounsel — can perform attorney-only actions.</summary>
    bool IsAttorney { get; }
}
