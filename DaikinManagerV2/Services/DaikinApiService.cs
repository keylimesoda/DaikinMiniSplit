using System.Net.Http;
using System.Web;
using DaikinManagerV2.Models;

namespace DaikinManagerV2.Services;

/// <summary>
/// HTTP implementation of Daikin API service.
/// Thread-safe: builds immutable results, no shared mutable state.
/// </summary>
public sealed class DaikinApiService : IDaikinApiService
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settings;
    private string _baseUrl;
    private bool _disposed;

    // Hysteresis compensation (1°C)
    private const double HysteresisOffset = 1.0;
    
    // Retry delays for GET requests (exponential backoff)
    private static readonly int[] RetryDelays = { 1000, 2000, 4000 };

    public DaikinApiService(ISettingsService settings)
    {
        _settings = settings;
        _baseUrl = $"http://{settings.DeviceIpAddress}";
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    public void SetDeviceAddress(string ipAddress)
    {
        _baseUrl = $"http://{ipAddress}";
    }

    /// <summary>
    /// Get current device state with retry and parallel fetching.
    /// </summary>
    public async Task<DaikinState> GetStateAsync(CancellationToken ct = default)
    {
        // Parallel fetch with retry - all calls start together
        var basicTask = WithRetryAsync(() => GetBasicInfoAsync(ct), ct);
        var controlTask = WithRetryAsync(() => GetControlInfoAsync(ct), ct);
        var sensorTask = WithRetryAsync(() => GetSensorInfoAsync(ct), ct);
        
        await Task.WhenAll(basicTask, controlTask, sensorTask);
        
        var basic = basicTask.Result;
        var control = controlTask.Result;
        var sensor = sensorTask.Result;
        
        // Compose immutable record from results (no shared mutable state)
        return new DaikinState(
            IsPoweredOn: control.Power,
            Mode: control.Mode,
            SetTemperatureC: control.Temperature,
            FanSpeed: control.FanSpeed,
            SwingMode: control.SwingMode,
            IndoorTempC: sensor.IndoorTemp,
            OutdoorTempC: sensor.OutdoorTemp,
            CompressorFrequency: sensor.CompressorFreq,
            ErrorCode: sensor.ErrorCode,
            MacAddress: basic.Mac,
            FirmwareVersion: basic.Version,
            LastUpdated: DateTime.Now
        );
    }

    /// <summary>
    /// Apply settings - NO RETRY (could double-apply).
    /// </summary>
    public async Task ApplySettingsAsync(
        bool power,
        DaikinMode mode,
        double temperatureCelsius,
        FanSpeed fanSpeed,
        SwingMode swingMode,
        CancellationToken ct = default)
    {
        // Apply hysteresis compensation
        var adjustedTemp = mode switch
        {
            DaikinMode.Heat => temperatureCelsius + HysteresisOffset,
            DaikinMode.Cool => temperatureCelsius - HysteresisOffset,
            _ => temperatureCelsius
        };
        adjustedTemp = Math.Clamp(adjustedTemp, 18.0, 30.0);
        
        var fanRateCode = fanSpeed switch
        {
            FanSpeed.Auto => "A",
            FanSpeed.Quiet => "B",
            FanSpeed.Low => "3",
            FanSpeed.Medium => "5",
            FanSpeed.High => "7",
            _ => "A"
        };
        
        var query = $"pow={(power ? 1 : 0)}" +
                    $"&mode={(int)mode}" +
                    $"&stemp={adjustedTemp:F1}" +
                    $"&f_rate={fanRateCode}" +
                    $"&f_dir={(int)swingMode}" +
                    $"&shum=0";
        
        await _httpClient.GetStringAsync($"{_baseUrl}/aircon/set_control_info?{query}", ct);
    }

    #region Private API Helpers

    private record BasicInfo(string Mac, string Version);
    private record ControlInfo(bool Power, DaikinMode Mode, double Temperature, FanSpeed FanSpeed, SwingMode SwingMode);
    private record SensorInfo(double IndoorTemp, double OutdoorTemp, int CompressorFreq, int ErrorCode);

    private async Task<BasicInfo> GetBasicInfoAsync(CancellationToken ct)
    {
        var response = await _httpClient.GetStringAsync($"{_baseUrl}/common/basic_info", ct);
        var data = ParseResponse(response);
        
        return new BasicInfo(
            Mac: data.GetValueOrDefault("mac", "--"),
            Version: data.GetValueOrDefault("ver", "--").Replace("_", ".")
        );
    }

    private async Task<ControlInfo> GetControlInfoAsync(CancellationToken ct)
    {
        var response = await _httpClient.GetStringAsync($"{_baseUrl}/aircon/get_control_info", ct);
        var data = ParseResponse(response);
        
        var power = data.GetValueOrDefault("pow", "0") == "1";
        var mode = ParseMode(data.GetValueOrDefault("mode", "3"));
        
        // Reverse hysteresis compensation for display
        var rawTempC = double.TryParse(data.GetValueOrDefault("stemp", "22"), out var t) ? t : 22.0;
        var displayTemp = mode switch
        {
            DaikinMode.Heat => rawTempC - HysteresisOffset,
            DaikinMode.Cool => rawTempC + HysteresisOffset,
            _ => rawTempC
        };
        
        return new ControlInfo(
            Power: power,
            Mode: mode,
            Temperature: displayTemp,
            FanSpeed: ParseFanSpeed(data.GetValueOrDefault("f_rate", "A")),
            SwingMode: ParseSwingMode(data.GetValueOrDefault("f_dir", "0"))
        );
    }

    private async Task<SensorInfo> GetSensorInfoAsync(CancellationToken ct)
    {
        var response = await _httpClient.GetStringAsync($"{_baseUrl}/aircon/get_sensor_info", ct);
        var data = ParseResponse(response);
        
        return new SensorInfo(
            IndoorTemp: double.TryParse(data.GetValueOrDefault("htemp", "0"), out var h) ? h : 0,
            OutdoorTemp: double.TryParse(data.GetValueOrDefault("otemp", "0"), out var o) ? o : 0,
            CompressorFreq: int.TryParse(data.GetValueOrDefault("cmpfreq", "0"), out var c) ? c : 0,
            ErrorCode: int.TryParse(data.GetValueOrDefault("err", "0"), out var e) ? e : 0
        );
    }

    private static Dictionary<string, string> ParseResponse(string response)
    {
        return response.Split(',')
            .Select(pair => pair.Split('='))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1]);
    }

    private static DaikinMode ParseMode(string value) => value switch
    {
        "0" => DaikinMode.Auto,
        "2" => DaikinMode.Dry,
        "3" => DaikinMode.Cool,
        "4" => DaikinMode.Heat,
        "6" => DaikinMode.Fan,
        _ => DaikinMode.Cool
    };

    private static FanSpeed ParseFanSpeed(string value) => value switch
    {
        "A" => FanSpeed.Auto,
        "B" => FanSpeed.Quiet,
        "3" => FanSpeed.Low,
        "5" => FanSpeed.Medium,
        "7" => FanSpeed.High,
        _ => FanSpeed.Auto
    };

    private static SwingMode ParseSwingMode(string value) => value switch
    {
        "0" => SwingMode.Off,
        "1" => SwingMode.Vertical,
        "2" => SwingMode.Horizontal,
        "3" => SwingMode.Both,
        _ => SwingMode.Off
    };

    #endregion

    #region Retry Logic

    private async Task<T> WithRetryAsync<T>(Func<Task<T>> operation, CancellationToken ct)
    {
        Exception? lastException = null;
        
        for (int attempt = 0; attempt <= RetryDelays.Length; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (HttpRequestException ex) when (attempt < RetryDelays.Length)
            {
                lastException = ex;
                await Task.Delay(RetryDelays[attempt], ct);
            }
            catch (TaskCanceledException ex) when (attempt < RetryDelays.Length && !ct.IsCancellationRequested)
            {
                // Timeout, retry
                lastException = ex;
                await Task.Delay(RetryDelays[attempt], ct);
            }
        }
        
        throw lastException ?? new HttpRequestException("Request failed after retries");
    }

    #endregion

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}
