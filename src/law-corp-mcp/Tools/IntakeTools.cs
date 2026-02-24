using ModelContextProtocol.Server;
using System.ComponentModel;

namespace LawCorp.Mcp.Server.Tools;

[McpServerToolType]
public static class IntakeTools
{
    [McpServerTool, Description("Create a new client intake request for a potential new client or matter.")]
    public static string IntakeCreateRequest(
        [Description("Prospective client or company name")] string clientName,
        [Description("Contact information (JSON with email, phone, address)")] string contactInfo,
        [Description("Description of the legal matter")] string matterDescription,
        [Description("Name of the relevant practice group")] string practiceGroup,
        [Description("How the prospect was referred (optional)")] string? referralSource = null)
        => throw new NotImplementedException("intake_create_request is not yet implemented.");

    [McpServerTool, Description("Retrieve the details and current status of an intake request.")]
    public static string IntakeGetRequest(
        [Description("The unique intake request ID")] int requestId)
        => throw new NotImplementedException("intake_get_request is not yet implemented.");

    [McpServerTool, Description("Run conflict-of-interest checks for an intake request against existing firm relationships.")]
    public static string IntakeRunConflictCheck(
        [Description("The unique intake request ID")] int requestId)
        => throw new NotImplementedException("intake_run_conflict_check is not yet implemented.");

    [McpServerTool, Description("Approve or reject an intake request and optionally assign a partner. Partner-only action.")]
    public static string IntakeApprove(
        [Description("The unique intake request ID")] int requestId,
        [Description("The partner ID to assign to the new matter")] int assignedPartnerId,
        [Description("Optional notes on the approval or rejection decision")] string? notes = null)
        => throw new NotImplementedException("intake_approve is not yet implemented.");

    [McpServerTool, Description("Generate an engagement letter document from an approved intake request.")]
    public static string IntakeGenerateEngagementLetter(
        [Description("The unique intake request ID")] int requestId,
        [Description("Fee structure description (e.g., Hourly, Retainer, Contingency)")] string feeStructure,
        [Description("Scope of representation description")] string scope)
        => throw new NotImplementedException("intake_generate_engagement_letter is not yet implemented.");
}
