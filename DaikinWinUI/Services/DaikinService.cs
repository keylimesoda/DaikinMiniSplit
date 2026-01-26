using System.Net.Http;
using System.Web;

namespace DaikinManager.Services;

public class DaikinService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    
    // Current state
    public bool IsPoweredOn { get; private set; }
    public DaikinMode Mode { get; private set; }
    public double SetTemperatureC { get; private set; }
    public FanSpeed FanSpeed { get; private set; }
    public SwingMode SwingMode { get; private set; }
    public double IndoorTempC { get; private set; }
    public double OutdoorTempC { get; private set; }
    public int CompressorFrequency { get; private set; }
    public int ErrorCode { get; private set; }
    public string MacAddress { get; private set; } = "--";
    public string FirmwareVersion { get; private set; } = "--";

    // Hysteresis compensation (1°C)
    private const double HysteresisOffset = 1.0;

    public event EventHandler? DataRefreshed;

    public DaikinService(string ipAddress)
    {
        _baseUrl = $"http://{ipAddress}";
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    public async Task RefreshAllAsync()
    {
        await Task.WhenAll(
            RefreshBasicInfoAsync(),
            RefreshControlInfoAsync(),
            RefreshSensorInfoAsync()
        );
        DataRefreshed?.Invoke(this, EventArgs.Empty);
    }

    private async Task RefreshBasicInfoAsync()
    {
        var response = await _httpClient.GetStringAsync($"{_baseUrl}/common/basic_info");
        var data = ParseResponse(response);
        MacAddress = data.GetValueOrDefault("mac", "--");
        FirmwareVersion = data.GetValueOrDefault("ver", "--").Replace("_", ".");
    }

    private async Task RefreshControlInfoAsync()
    {
        var response = await _httpClient.GetStringAsync($"{_baseUrl}/aircon/get_control_info");
        Console.WriteLine($"[RefreshControlInfoAsync] RAW: {response}");
        var data = ParseResponse(response);

        IsPoweredOn = data.GetValueOrDefault("pow", "0") == "1";
        Mode = ParseMode(data.GetValueOrDefault("mode", "3"));
        
        // Reverse hysteresis compensation for display
        double rawTempC = double.Parse(data.GetValueOrDefault("stemp", "22"));
        SetTemperatureC = Mode switch
        {
            DaikinMode.Heat => rawTempC - HysteresisOffset,
            DaikinMode.Cool => rawTempC + HysteresisOffset,
            _ => rawTempC
        };

        FanSpeed = ParseFanSpeed(data.GetValueOrDefault("f_rate", "A"));
        SwingMode = ParseSwingMode(data.GetValueOrDefault("f_dir", "0"));
        
        Console.WriteLine($"[RefreshControlInfoAsync] PARSED: pow={IsPoweredOn}, mode={Mode}, tempC={SetTemperatureC}");
    }

    private async Task RefreshSensorInfoAsync()
    {
        var response = await _httpClient.GetStringAsync($"{_baseUrl}/aircon/get_sensor_info");
        var data = ParseResponse(response);

        if (double.TryParse(data.GetValueOrDefault("htemp", "-"), out var htemp))
            IndoorTempC = htemp;
        if (double.TryParse(data.GetValueOrDefault("otemp", "-"), out var otemp))
            OutdoorTempC = otemp;
        
        int.TryParse(data.GetValueOrDefault("cmpfreq", "0"), out var freq);
        CompressorFrequency = freq;
        
        int.TryParse(data.GetValueOrDefault("err", "0"), out var err);
        ErrorCode = err;
    }

    public async Task SetControlAsync(bool power, DaikinMode mode, double tempF, FanSpeed fan, SwingMode swing)
    {
        double tempC = FahrenheitToCelsius(tempF);
        
        // Apply hysteresis compensation
        tempC = mode switch
        {
            DaikinMode.Heat => tempC + HysteresisOffset,
            DaikinMode.Cool => tempC - HysteresisOffset,
            _ => tempC
        };
        
        // Round to nearest 0.5
        tempC = Math.Round(tempC * 2) / 2;

        var url = $"{_baseUrl}/aircon/set_control_info?" +
            $"pow={(power ? 1 : 0)}&" +
            $"mode={ModeToInt(mode)}&" +
            $"stemp={tempC}&" +
            $"f_rate={FanSpeedToString(fan)}&" +
            $"shum=0&" +
            $"f_dir={SwingModeToInt(swing)}";

        await _httpClient.GetAsync(url);
        await Task.Delay(200);
        await RefreshAllAsync();
    }

    public async Task TogglePowerAsync()
    {
        await SetControlAsync(!IsPoweredOn, Mode, CelsiusToFahrenheit(SetTemperatureC), FanSpeed, SwingMode);
    }

    // Temperature conversions
    public static double CelsiusToFahrenheit(double c) => Math.Round((c * 1.8) + 32, 1);
    public static double FahrenheitToCelsius(double f) => (f - 32) / 1.8;

    // Parsing helpers
    private static Dictionary<string, string> ParseResponse(string response)
    {
        var dict = new Dictionary<string, string>();
        foreach (var pair in response.Split(','))
        {
            var parts = pair.Split('=');
            if (parts.Length == 2)
                dict[parts[0]] = parts[1];
        }
        return dict;
    }

    private static DaikinMode ParseMode(string mode) => mode switch
    {
        "3" => DaikinMode.Cool,
        "4" => DaikinMode.Heat,
        "1" => DaikinMode.Auto,
        "2" => DaikinMode.Dry,
        "0" => DaikinMode.Fan,
        _ => DaikinMode.Cool
    };

    private static int ModeToInt(DaikinMode mode) => mode switch
    {
        DaikinMode.Cool => 3,
        DaikinMode.Heat => 4,
        DaikinMode.Auto => 1,
        DaikinMode.Dry => 2,
        DaikinMode.Fan => 0,
        _ => 3
    };

    private static FanSpeed ParseFanSpeed(string rate) => rate switch
    {
        "A" => FanSpeed.Auto,
        "B" => FanSpeed.Quiet,
        "3" => FanSpeed.Level3,
        "4" => FanSpeed.Level4,
        "5" => FanSpeed.Level5,
        "6" => FanSpeed.Level6,
        "7" => FanSpeed.Level7,
        _ => FanSpeed.Auto
    };

    private static string FanSpeedToString(FanSpeed speed) => speed switch
    {
        FanSpeed.Auto => "A",
        FanSpeed.Quiet => "B",
        FanSpeed.Level3 => "3",
        FanSpeed.Level4 => "4",
        FanSpeed.Level5 => "5",
        FanSpeed.Level6 => "6",
        FanSpeed.Level7 => "7",
        _ => "A"
    };

    private static SwingMode ParseSwingMode(string dir) => dir switch
    {
        "0" => SwingMode.Stopped,
        "1" => SwingMode.Vertical,
        "2" => SwingMode.Horizontal,
        "3" => SwingMode.Both,
        _ => SwingMode.Stopped
    };

    private static int SwingModeToInt(SwingMode mode) => mode switch
    {
        SwingMode.Stopped => 0,
        SwingMode.Vertical => 1,
        SwingMode.Horizontal => 2,
        SwingMode.Both => 3,
        _ => 0
    };
}

public enum DaikinMode { Cool, Heat, Auto, Dry, Fan }
public enum FanSpeed { Auto, Quiet, Level3, Level4, Level5, Level6, Level7 }
public enum SwingMode { Stopped, Vertical, Horizontal, Both }
