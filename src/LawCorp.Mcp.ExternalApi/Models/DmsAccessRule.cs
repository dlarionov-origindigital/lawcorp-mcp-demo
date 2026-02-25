using LawCorp.Mcp.Core.Models;

namespace LawCorp.Mcp.ExternalApi.Models;

/// <summary>
/// Maps a FirmRole to workspace access. The external DMS reads the user's role
/// from the OBO token claims and checks these rules independently.
/// </summary>
public class DmsAccessRule
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public FirmRole Role { get; set; }
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }

    public DmsWorkspace Workspace { get; set; } = null!;
}
