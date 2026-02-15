namespace DaikinAndroid.Models;

/// <summary>
/// Immutable snapshot of the Daikin device state.
/// Thread-safe by design - no mutable shared state.
/// </summary>
public sealed record DaikinState(
    bool IsPoweredOn,
    DaikinMode Mode,
    double SetTemperatureC,
    FanSpeed FanSpeed,
    SwingMode SwingMode,
    double IndoorTempC,
    double OutdoorTempC,
    int CompressorFrequency,
    int ErrorCode,
    string MacAddress,
    string FirmwareVersion,
    DateTime LastUpdated,
    string RawSensorResponse = "",
    string RawControlResponse = "",
    string RawBasicResponse = ""
)
{
    /// <summary>
    /// Default state for when not connected.
    /// </summary>
    public static DaikinState Default => new(
        IsPoweredOn: false,
        Mode: DaikinMode.Cool,
        SetTemperatureC: 22.0,
        FanSpeed: FanSpeed.Auto,
        SwingMode: SwingMode.Off,
        IndoorTempC: 0,
        OutdoorTempC: 0,
        CompressorFrequency: 0,
        ErrorCode: 0,
        MacAddress: "--",
        FirmwareVersion: "--",
        LastUpdated: DateTime.MinValue,
        RawSensorResponse: "",
        RawControlResponse: "",
        RawBasicResponse: ""
    );
}
