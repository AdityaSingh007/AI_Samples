using CopiloKit_MAF_Backend_Agent.AgentUtils;
using CopiloKit_MAF_Backend_Agent.Models;
using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace CopiloKit_MAF_Backend_Agent.AgentTools
{

    public class AgentCustomTools
    {
        [Description("Plans a trip and creates a human approval request that the CopilotKit Angular frontend can present before the trip is confirmed.")]
        public async Task<TripApprovalRequestResult> PlanTripWithApprovalRequest(
       [Description("The primary trip destination.")] string destination,
       [Description("Recommended highlights, activities, or stops to include in the trip plan. Break it down day wise")] string[]? highlights = null)
        {
            await Task.Yield();

            var itineraryHighlights = highlights is { Length: > 0 }
                ? highlights
                : ["Arrival", "Local sightseeing", "Return travel"];

            var tripPlan = new TripPlan(
                Destination: destination,
                Highlights: itineraryHighlights);

            var title = $"Approve trip plan for {destination}";
            var summary = $"A trip itinerary to {destination} has been prepared and needs user confirmation before it is finalized.";
            var details = AgentUtilityFunction.BuildTripDetails(tripPlan);
            var confirmationMessage = $"Do you approve this trip plan for {destination}?";

            return new TripApprovalRequestResult(
                ApprovalId: $"APR-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                Status: "pending_approval",
                ApprovalType: "trip_plan",
                Title: title,
                Summary: summary,
                Details: details,
                AllowedDecisions: ["approved", "rejected"],
                RequestedAtUtc: DateTimeOffset.UtcNow,
                RequiresFrontendApproval: true,
                TripPlan: tripPlan,
                FrontendApproval: new FrontendApprovalRequest(
                    ToolName: "confirmAction",
                    Arguments: new ConfirmActionInput(confirmationMessage, tripPlan.Highlights)));
        }
    }
}
