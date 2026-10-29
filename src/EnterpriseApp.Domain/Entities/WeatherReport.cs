using EnterpriseApp.Domain.Common;

namespace EnterpriseApp.Domain.Entities;

public class WeatherReport : BaseEntity
{
    public string City { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public double TemperatureCelsius { get; private set; }
    public double FeelsLikeCelsius { get; private set; }
    public int HumidityPercent { get; private set; }
    public double WindSpeedKph { get; private set; }
    public string Condition { get; private set; } = string.Empty;
    public string Icon { get; private set; } = string.Empty;
    public DateTime ObservationTimeUtc { get; private set; }

    public double TemperatureFahrenheit => Math.Round((TemperatureCelsius * 9 / 5) + 32, 1);
    public double FeelsLikeFahrenheit => Math.Round((FeelsLikeCelsius * 9 / 5) + 32, 1);

    private WeatherReport() { }

    public WeatherReport(
        string city,
        string country,
        double temperatureCelsius,
        double feelsLikeCelsius,
        int humidityPercent,
        double windSpeedKph,
        string condition,
        string icon,
        DateTime observationTimeUtc)
    {
        City = city.Trim();
        Country = country.Trim();
        TemperatureCelsius = Math.Round(temperatureCelsius, 1);
        FeelsLikeCelsius = Math.Round(feelsLikeCelsius, 1);
        HumidityPercent = humidityPercent;
        WindSpeedKph = Math.Round(windSpeedKph, 1);
        Condition = condition.Trim();
        Icon = icon.Trim();
        ObservationTimeUtc = observationTimeUtc;
    }
}
