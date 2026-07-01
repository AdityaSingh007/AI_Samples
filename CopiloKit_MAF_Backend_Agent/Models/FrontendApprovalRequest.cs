namespace CopiloKit_MAF_Backend_Agent.Models
{
    public sealed record FrontendApprovalRequest(
        string ToolName,
        ConfirmActionInput Arguments);
}
