using Microsoft.AspNetCore.Mvc;

namespace Enterprise_Api_Sample.Weather
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType<WeatherForecastDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<WeatherForecastDto>> Get([FromQuery] int statusCode = 200)
        {
            switch (statusCode)
            {
                case StatusCodes.Status200OK:
                    var result = new WeatherForecastDto("Seattle", DateOnly.FromDateTime(DateTime.UtcNow), 18, "Partly cloudy");
                    return Ok(result);
                case StatusCodes.Status400BadRequest:
                case StatusCodes.Status401Unauthorized:
                case StatusCodes.Status403Forbidden:
                case StatusCodes.Status404NotFound:
                case StatusCodes.Status503ServiceUnavailable:
                    return Problem(statusCode: statusCode, title: $"Simulated {statusCode} response");
                case StatusCodes.Status500InternalServerError:
                    throw new InvalidOperationException("Simulated 500 exception");
                default:
                    return Problem(statusCode: StatusCodes.Status400BadRequest, title: $"Unsupported statusCode '{statusCode}'. Use 200, 400, 401, 403, 404, 500 or 503.");
            }
        }
    }
}
