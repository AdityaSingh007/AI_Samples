namespace CopiloKit_MAF_Backend_Agent.Models
{
    public sealed record TripApprovalRequestResult(
        string ApprovalId,
        string Status,
        string ApprovalType,
        string Title,
        string Summary,
        string? Details,
        string[] AllowedDecisions,
        DateTimeOffset RequestedAtUtc,
        bool RequiresFrontendApproval,
        TripPlan TripPlan,
        FrontendApprovalRequest FrontendApproval);
}
