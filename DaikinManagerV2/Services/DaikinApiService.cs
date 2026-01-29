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
            LastUpdated: DateTime.Now,
            RawSensorResponse: sensor.RawResponse,
            RawControlResponse: control.RawResponse,
            RawBasicResponse: basic.RawResponse
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

    private record BasicInfo(string Mac, string Version, string RawResponse);
    private record ControlInfo(bool Power, DaikinMode Mode, double Temperature, FanSpeed FanSpeed, SwingMode SwingMode, string RawResponse);
    private record SensorInfo(double IndoorTemp, double OutdoorTemp, int CompressorFreq, int ErrorCode, string RawResponse);

    private async Task<BasicInfo> GetBasicInfoAsync(CancellationToken ct)
    {
        var response = await _httpClient.GetStringAsync($"{_baseUrl}/common/basic_info", ct);
        var data = ParseResponse(response);
        
        return new BasicInfo(
            Mac: data.GetValueOrDefault("mac", "--"),
            Version: data.GetValueOrDefault("ver", "--").Replace("_", "."),
            RawResponse: response
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
            SwingMode: ParseSwingMode(data.GetValueOrDefault("f_dir", "0")),
            RawResponse: response
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
            ErrorCode: int.TryParse(data.GetValueOrDefault("err", "0"), out var e) ? e : 0,
            RawResponse: response
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

    #region Schedule APIs

    // Day code mapping for schedule API
    private static readonly (string Code, DayOfWeek Day)[] DayCodes =
    {
        ("mo", DayOfWeek.Monday),
        ("tu", DayOfWeek.Tuesday),
        ("we", DayOfWeek.Wednesday),
        ("th", DayOfWeek.Thursday),
        ("fr", DayOfWeek.Friday),
        ("sa", DayOfWeek.Saturday),
        ("su", DayOfWeek.Sunday)
    };

    /// <summary>
    /// Get the weekly schedule from the device.
    /// </summary>
    public async Task<WeeklySchedule> GetScheduleAsync(CancellationToken ct = default)
    {
        var response = await WithRetryAsync(
            () => _httpClient.GetStringAsync($"{_baseUrl}/aircon/get_scdltimer", ct), ct);
        
        var data = ParseResponse(response);
        
        var days = new List<DaySchedule>();
        
        foreach (var (code, day) in DayCodes)
        {
            var events = ParseDayEvents(data, code);
            days.Add(new DaySchedule(day, events));
        }
        
        return new WeeklySchedule(days);
    }

    /// <summary>
    /// Get scheduler metadata.
    /// </summary>
    public async Task<SchedulerInfo> GetSchedulerInfoAsync(CancellationToken ct = default)
    {
        var response = await WithRetryAsync(
            () => _httpClient.GetStringAsync($"{_baseUrl}/aircon/get_scdltimer_info", ct), ct);
        
        var data = ParseResponse(response);
        
        return new SchedulerInfo(
            Enabled: data.GetValueOrDefault("en_scdltimer", "0") == "1",
            ActiveSlot: int.TryParse(data.GetValueOrDefault("active_no", "1"), out var slot) ? slot : 1,
            MaxSchedules: int.TryParse(data.GetValueOrDefault("scdl_num", "3"), out var max) ? max : 3,
            MaxEventsPerDay: int.TryParse(data.GetValueOrDefault("scdl_per_day", "6"), out var perDay) ? perDay : 6,
            Schedule1Name: data.GetValueOrDefault("scdl1_name", ""),
            Schedule2Name: data.GetValueOrDefault("scdl2_name", ""),
            Schedule3Name: data.GetValueOrDefault("scdl3_name", "")
        );
    }

    /// <summary>
    /// Enable or disable the scheduler.
    /// </summary>
    public async Task SetSchedulerEnabledAsync(bool enabled, CancellationToken ct = default)
    {
        var value = enabled ? "1" : "0";
        await _httpClient.GetStringAsync($"{_baseUrl}/aircon/set_scdltimer_info?en_scdltimer={value}", ct);
    }

    /// <summary>
    /// Parse events for a single day from the schedule response.
    /// </summary>
    private static List<ScheduleEvent> ParseDayEvents(Dictionary<string, string> data, string dayCode)
    {
        var events = new List<ScheduleEvent>();
        
        // Get event count for this day (e.g., "moc" for Monday)
        var countKey = $"{dayCode}c";
        var eventCount = int.TryParse(data.GetValueOrDefault(countKey, "0"), out var count) ? count : 0;
        
        // Parse each event (1-indexed: mo1_en, mo1_pow, etc.)
        for (int i = 1; i <= eventCount; i++)
        {
            var prefix = $"{dayCode}{i}_";
            
            var enabled = data.GetValueOrDefault($"{prefix}en", "0") == "1";
            var powerOn = data.GetValueOrDefault($"{prefix}pow", "0") == "1";
            var modeValue = data.GetValueOrDefault($"{prefix}mod", "1");
            var tempValue = data.GetValueOrDefault($"{prefix}tmp", "--");
            var timeMinutes = int.TryParse(data.GetValueOrDefault($"{prefix}time", "0"), out var mins) ? mins : 0;
            
            // Parse mode
            var mode = modeValue switch
            {
                "2" => DaikinMode.Dry,
                "3" => DaikinMode.Cool,
                "4" => DaikinMode.Heat,
                "6" => DaikinMode.Fan,
                _ => DaikinMode.Auto
            };
            
            // Parse temperature (may be "--" for power-off events)
            double? temperature = null;
            if (tempValue != "--" && double.TryParse(tempValue, out var temp))
            {
                temperature = temp;
            }
            
            // Convert minutes from midnight to TimeOnly
            var time = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(timeMinutes));
            
            events.Add(new ScheduleEvent(enabled, powerOn, mode, temperature, time));
        }
        
        return events;
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
