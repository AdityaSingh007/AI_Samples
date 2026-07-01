using CopiloKit_MAF_Backend_Agent.Models;

namespace CopiloKit_MAF_Backend_Agent.AgentUtils
{
    public static class AgentUtilityFunction
    {
        public static string BuildTripDetails(TripPlan tripPlan)
        {
            var details = new List<string>
            {
                $"Destination: {tripPlan.Destination}"
            };

            if (tripPlan.Highlights.Length > 0)
            {
                details.Add($"Highlights: {string.Join(", ", tripPlan.Highlights)}");
            }

            return string.Join(Environment.NewLine, details);
        }
    }
}
