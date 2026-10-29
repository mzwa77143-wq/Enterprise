using EnterpriseApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;

    public WeatherController(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(WeatherDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWeather([FromQuery] string city, [FromQuery] string units = "metric", CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Missing Parameter",
                Detail = "A valid city query parameter is required. Example: /api/weather?city=London"
            });
        }

        var result = await _weatherService.GetWeatherAsync(city, units, ct);
        return Ok(result);
    }
}
