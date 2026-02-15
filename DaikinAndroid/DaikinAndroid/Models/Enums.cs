namespace DaikinAndroid.Models;

/// <summary>
/// Operating mode for the Daikin unit.
/// Values match API 'mode' parameter.
/// </summary>
public enum DaikinMode
{
    Auto = 0,   // Automatic mode selection
    Dry = 2,    // Dehumidify
    Cool = 3,   // Cooling
    Heat = 4,   // Heating
    Fan = 6     // Fan only (no compressor)
}

/// <summary>
/// Fan speed setting.
/// </summary>
public enum FanSpeed
{
    Auto,   // "A" - Automatic speed
    Quiet,  // "B" - Silent/low noise mode
    Low,    // "3"
    Medium, // "5"
    High    // "7"
}

/// <summary>
/// Swing/airflow direction mode.
/// Values match API 'f_dir' parameter.
/// </summary>
public enum SwingMode
{
    Off = 0,
    Vertical = 1,
    Horizontal = 2,
    Both = 3
}

/// <summary>
/// Application connection state.
/// </summary>
public enum ConnectionState
{
    Disconnected,   // No connection attempted
    Connecting,     // Request in flight
    Connected,      // Last request succeeded
    Error           // Last request failed (with message)
}

/// <summary>
/// Temperature unit for display.
/// </summary>
public enum TemperatureUnit
{
    Fahrenheit,
    Celsius
}
