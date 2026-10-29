using EnterpriseApp.Domain.Entities;

namespace EnterpriseApp.Application.Interfaces.Services;

public class WeatherDto
{
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double TemperatureCelsius { get; set; }
    public double FeelsLikeCelsius { get; set; }
    public double TemperatureFahrenheit { get; set; }
    public double FeelsLikeFahrenheit { get; set; }
    public int HumidityPercent { get; set; }
    public double WindSpeedKph { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public DateTime ObservationTimeUtc { get; set; }
    public string DataSource { get; set; } = string.Empty;
}

public interface IWeatherService
{
    Task<WeatherDto> GetWeatherAsync(string city, string units = "metric", CancellationToken ct = default);
}
