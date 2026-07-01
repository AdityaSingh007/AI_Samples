public sealed record TripApprovalCallbackResponse(
    string Decision,
    string Message,
    string[] Highlights,
    DateTimeOffset ProcessedAtUtc);
