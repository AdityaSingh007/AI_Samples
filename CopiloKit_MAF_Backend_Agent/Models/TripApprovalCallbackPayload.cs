using System.Text.Json.Serialization;

public sealed record TripApprovalCallbackPayload(
    [property: JsonConverter(typeof(JsonStringEnumConverter<TripApprovalDecision>))] TripApprovalDecision Decision,
    string Message,
    string[] Highlights);
