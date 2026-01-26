using System.Text.Json;
using DaikinManagerV2.Models;

namespace DaikinManagerV2.Services;

/// <summary>
/// Settings service using file-based storage (for unpackaged apps).
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private Dictionary<string, object> _settings;
    
    private const string KeyDeviceIp = "DeviceIpAddress";
    private const string KeyTempUnit = "TemperatureUnit";
    private const string KeyRefreshInterval = "RefreshIntervalSeconds";
    private const string KeyHasCompletedSetup = "HasCompletedSetup";
    
    public SettingsService()
    {
        // Store settings in AppData/Local/DaikinManagerV2
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "DaikinManagerV2");
        Directory.CreateDirectory(appFolder);
        _settingsFilePath = Path.Combine(appFolder, "settings.json");
        
        _settings = LoadSettings();
    }
    
    public string DeviceIpAddress
    {
        get => GetValue(KeyDeviceIp, "192.168.183.26");
        set => SetValue(KeyDeviceIp, value);
    }
    
    public TemperatureUnit TemperatureUnit
    {
        get => (TemperatureUnit)GetValue(KeyTempUnit, (int)TemperatureUnit.Fahrenheit);
        set => SetValue(KeyTempUnit, (int)value);
    }
    
    public int RefreshIntervalSeconds
    {
        get => GetValue(KeyRefreshInterval, 30);
        set => SetValue(KeyRefreshInterval, Math.Clamp(value, 5, 300));
    }
    
    public bool HasCompletedSetup
    {
        get => GetValue(KeyHasCompletedSetup, false);
        set => SetValue(KeyHasCompletedSetup, value);
    }
    
    private T GetValue<T>(string key, T defaultValue)
    {
        if (_settings.TryGetValue(key, out var value))
        {
            try
            {
                if (value is JsonElement element)
                {
                    if (typeof(T) == typeof(string))
                        return (T)(object)element.GetString()!;
                    if (typeof(T) == typeof(int))
                        return (T)(object)element.GetInt32();
                    if (typeof(T) == typeof(bool))
                        return (T)(object)element.GetBoolean();
                }
                else if (value is T typedValue)
                {
                    return typedValue;
                }
            }
            catch { }
        }
        return defaultValue;
    }
    
    private void SetValue<T>(string key, T value)
    {
        _settings[key] = value!;
        SaveSettings();
    }
    
    private Dictionary<string, object> LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();
            }
        }
        catch { }
        return new Dictionary<string, object>();
    }
    
    private void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch { }
    }
}
