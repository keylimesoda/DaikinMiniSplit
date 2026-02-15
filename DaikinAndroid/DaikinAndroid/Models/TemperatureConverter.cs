namespace DaikinAndroid.Models;

/// <summary>
/// Temperature conversion and validation utilities.
/// </summary>
public static class TemperatureConverter
{
    public static double CelsiusToFahrenheit(double c) => c * 9.0 / 5.0 + 32.0;
    public static double FahrenheitToCelsius(double f) => (f - 32.0) * 5.0 / 9.0;
    
    // Valid range: 18-30°C / 64-86°F
    public static double ClampCelsius(double c) => Math.Clamp(c, 18.0, 30.0);
    public static double ClampFahrenheit(double f) => Math.Clamp(f, 64.0, 86.0);
    
    /// <summary>
    /// Format temperature for display with unit.
    /// </summary>
    public static string Format(double celsius, TemperatureUnit unit, string format = "F1")
    {
        var value = unit == TemperatureUnit.Fahrenheit 
            ? CelsiusToFahrenheit(celsius) 
            : celsius;
        var symbol = unit == TemperatureUnit.Fahrenheit ? "°F" : "°C";
        return $"{value.ToString(format)}{symbol}";
    }
}

/// <summary>
/// Mode-aware validation for temperature control.
/// </summary>
public static class ModeValidation
{
    /// <summary>
    /// Returns whether temperature control is applicable for the given mode.
    /// </summary>
    public static bool SupportsTemperature(DaikinMode mode) => mode switch
    {
        DaikinMode.Cool => true,
        DaikinMode.Heat => true,
        DaikinMode.Auto => true,
        DaikinMode.Dry => false,  // Dehumidify - no temp control
        DaikinMode.Fan => false,  // Fan only - no temp control
        _ => false
    };
    
    /// <summary>
    /// Get the valid temperature range for a mode (in Celsius).
    /// Returns null if mode doesn't support temperature.
    /// </summary>
    public static (double Min, double Max)? GetTemperatureRange(DaikinMode mode) => mode switch
    {
        DaikinMode.Cool => (18.0, 30.0),
        DaikinMode.Heat => (18.0, 30.0),
        DaikinMode.Auto => (18.0, 30.0),
        _ => null
    };
}
