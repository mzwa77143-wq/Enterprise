using System.Net.Http.Json;
using System.Text.Json.Serialization;
using EnterpriseApp.Application.Interfaces.Services;
using EnterpriseApp.Domain.Entities;
using EnterpriseApp.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnterpriseApp.Infrastructure.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<WeatherService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<WeatherDto> GetWeatherAsync(string city, string units = "metric", CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(city))
            throw new DomainException("City name is required.");

        var trimmedCity = city.Trim();
        var apiKey = _configuration["WeatherApi:ApiKey"];

        // 1. Try OpenWeatherMap if API Key is configured
        if (!string.IsNullOrWhiteSpace(apiKey) && !apiKey.Contains("YOUR_OPENWEATHERMAP_API_KEY", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"https://api.openweathermap.org/data/2.5/weather?q={Uri.EscapeDataString(trimmedCity)}&appid={apiKey}&units=metric",
                    ct);

                if (response.IsSuccessStatusCode)
                {
                    var owm = await response.Content.ReadFromJsonAsync<OpenWeatherMapResponse>(ct);
                    if (owm != null)
                    {
                        var report = new WeatherReport(
                            owm.Name ?? trimmedCity,
                            owm.Sys?.Country ?? "Unknown",
                            owm.Main?.Temp ?? 0,
                            owm.Main?.FeelsLike ?? 0,
                            owm.Main?.Humidity ?? 0,
                            (owm.Wind?.Speed ?? 0) * 3.6, // m/s to kph
                            owm.Weather?.FirstOrDefault()?.Main ?? "Clear",
                            owm.Weather?.FirstOrDefault()?.Icon ?? "01d",
                            DateTime.UtcNow);

                        return MapToDto(report, "OpenWeatherMap API");
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new EntityNotFoundException("City", trimmedCity);
                }
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenWeatherMap call failed, falling back to Open-Meteo geocoding weather API.");
            }
        }

        // 2. Fallback to Open-Meteo API (High accuracy, global coverage, zero key required)
        try
        {
            var geoUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(trimmedCity)}&count=1&language=en&format=json";
            var geoResult = await _httpClient.GetFromJsonAsync<OpenMeteoGeoResponse>(geoUrl, ct);

            if (geoResult?.Results == null || geoResult.Results.Count == 0)
            {
                throw new EntityNotFoundException("City", trimmedCity);
            }

            var loc = geoResult.Results[0];
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={loc.Latitude}&longitude={loc.Longitude}&current=temperature_2m,relative_humidity_2m,apparent_temperature,wind_speed_10m,weather_code";
            var weatherData = await _httpClient.GetFromJsonAsync<OpenMeteoCurrentResponse>(weatherUrl, ct);

            if (weatherData?.Current == null)
            {
                throw new DomainException($"Unable to retrieve weather data for {trimmedCity}.");
            }

            var condition = InterpretWmoWeatherCode(weatherData.Current.WeatherCode);
            var report = new WeatherReport(
                loc.Name ?? trimmedCity,
                loc.Country ?? "Global",
                weatherData.Current.Temperature2m,
                weatherData.Current.ApparentTemperature,
                weatherData.Current.RelativeHumidity2m,
                weatherData.Current.WindSpeed10m,
                condition.Description,
                condition.Icon,
                DateTime.UtcNow);

            return MapToDto(report, "Open-Meteo Global Weather Service");
        }
        catch (EntityNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch weather for {City}", trimmedCity);
            throw new DomainException($"Error retrieving weather for '{trimmedCity}': {ex.Message}");
        }
    }

    private static WeatherDto MapToDto(WeatherReport report, string source) => new()
    {
        City = report.City,
        Country = report.Country,
        TemperatureCelsius = report.TemperatureCelsius,
        FeelsLikeCelsius = report.FeelsLikeCelsius,
        TemperatureFahrenheit = report.TemperatureFahrenheit,
        FeelsLikeFahrenheit = report.FeelsLikeFahrenheit,
        HumidityPercent = report.HumidityPercent,
        WindSpeedKph = report.WindSpeedKph,
        Condition = report.Condition,
        Icon = report.Icon,
        ObservationTimeUtc = report.ObservationTimeUtc,
        DataSource = source
    };

    private static (string Description, string Icon) InterpretWmoWeatherCode(int code) => code switch
    {
        0 => ("Clear Sky", "01d"),
        1 => ("Mainly Clear", "02d"),
        2 => ("Partly Cloudy", "03d"),
        3 => ("Overcast", "04d"),
        45 or 48 => ("Foggy", "50d"),
        51 or 53 or 55 => ("Drizzle", "09d"),
        61 or 63 or 65 => ("Rain", "10d"),
        71 or 73 or 75 => ("Snow", "13d"),
        80 or 81 or 82 => ("Rain Showers", "09d"),
        95 or 96 or 99 => ("Thunderstorm", "11d"),
        _ => ("Partly Cloudy", "02d")
    };

    private class OpenWeatherMapResponse
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("main")] public OwmMain? Main { get; set; }
        [JsonPropertyName("weather")] public List<OwmWeather>? Weather { get; set; }
        [JsonPropertyName("wind")] public OwmWind? Wind { get; set; }
        [JsonPropertyName("sys")] public OwmSys? Sys { get; set; }
    }

    private class OwmMain
    {
        [JsonPropertyName("temp")] public double Temp { get; set; }
        [JsonPropertyName("feels_like")] public double FeelsLike { get; set; }
        [JsonPropertyName("humidity")] public int Humidity { get; set; }
    }

    private class OwmWeather
    {
        [JsonPropertyName("main")] public string? Main { get; set; }
        [JsonPropertyName("icon")] public string? Icon { get; set; }
    }

    private class OwmWind
    {
        [JsonPropertyName("speed")] public double Speed { get; set; }
    }

    private class OwmSys
    {
        [JsonPropertyName("country")] public string? Country { get; set; }
    }

    private class OpenMeteoGeoResponse
    {
        [JsonPropertyName("results")] public List<OpenMeteoGeoItem>? Results { get; set; }
    }

    private class OpenMeteoGeoItem
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("country")] public string? Country { get; set; }
        [JsonPropertyName("latitude")] public double Latitude { get; set; }
        [JsonPropertyName("longitude")] public double Longitude { get; set; }
    }

    private class OpenMeteoCurrentResponse
    {
        [JsonPropertyName("current")] public OpenMeteoCurrentData? Current { get; set; }
    }

    private class OpenMeteoCurrentData
    {
        [JsonPropertyName("temperature_2m")] public double Temperature2m { get; set; }
        [JsonPropertyName("apparent_temperature")] public double ApparentTemperature { get; set; }
        [JsonPropertyName("relative_humidity_2m")] public int RelativeHumidity2m { get; set; }
        [JsonPropertyName("wind_speed_10m")] public double WindSpeed10m { get; set; }
        [JsonPropertyName("weather_code")] public int WeatherCode { get; set; }
    }
}
