using Microsoft.AspNetCore.Http.HttpResults;

namespace Enterprise_Api_Sample.Weather
{
    public static class WeatherMinimalEndpoints
    {
        public static IEndpointRouteBuilder MapWeatherSimulation(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/weather/simulate/{statusCode:int}", Simulate)
               .WithName("SimulateWeatherResponse");

            return app;
        }

        private static Results<Ok<WeatherForecastDto>, BadRequest, UnauthorizedHttpResult, NotFound, StatusCodeHttpResult> Simulate(int statusCode)
        {
            return statusCode switch
            {
                StatusCodes.Status200OK => TypedResults.Ok(new WeatherForecastDto("Seattle", DateOnly.FromDateTime(DateTime.UtcNow), 18, "Partly cloudy")),
                StatusCodes.Status400BadRequest => TypedResults.BadRequest(),
                StatusCodes.Status401Unauthorized => TypedResults.Unauthorized(),
                StatusCodes.Status403Forbidden => TypedResults.StatusCode(StatusCodes.Status403Forbidden),
                StatusCodes.Status404NotFound => TypedResults.NotFound(),
                StatusCodes.Status503ServiceUnavailable => TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable),
                StatusCodes.Status500InternalServerError => throw new InvalidOperationException("Simulated 500 exception"),
                _ => TypedResults.BadRequest()
            };
        }
    }
}
