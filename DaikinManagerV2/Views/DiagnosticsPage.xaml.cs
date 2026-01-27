using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using DaikinManagerV2.ViewModels;

namespace DaikinManagerV2.Views;

public sealed partial class DiagnosticsPage : Page
{
    private DiagnosticsViewModel _viewModel = null!;

    public DiagnosticsPage()
    {
        this.InitializeComponent();
    }

    public void Initialize(DiagnosticsViewModel viewModel)
    {
        _viewModel = viewModel;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        
        // Initial UI sync
        UpdateAllUI();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(UpdateAllUI);
    }

    private void UpdateAllUI()
    {
        var state = _viewModel.DeviceState;
        
        // Outdoor & System section (with colors!)
        double outF = state.OutdoorTempC * 9.0 / 5.0 + 32.0;
        OutsideTempText.Text = $"Outside Temp: {outF:F1}°F ({state.OutdoorTempC:F1}°C)";
        
        if (state.CompressorFrequency == 0)
        {
            CompressorText.Text = "Compressor: Idle";
            CompressorText.Foreground = new SolidColorBrush(Colors.Gray);
        }
        else
        {
            CompressorText.Text = $"Compressor: ACTIVE ({state.CompressorFrequency} Hz)";
            CompressorText.Foreground = new SolidColorBrush(Colors.Green);
        }
        
        if (state.ErrorCode == 0)
        {
            SystemHealthText.Text = "System Health: OK";
            SystemHealthText.Foreground = new SolidColorBrush(Colors.Green);
        }
        else
        {
            SystemHealthText.Text = $"ERROR CODE: {state.ErrorCode}";
            SystemHealthText.Foreground = new SolidColorBrush(Colors.Red);
        }
        
        // System Status
        PowerStatus.Text = state.IsPoweredOn ? "ON" : "OFF";
        ModeStatus.Text = state.Mode.ToString();
        
        // Show both F and C for temperatures
        double setTempF = state.SetTemperatureC * 9.0 / 5.0 + 32.0;
        SetTempStatus.Text = $"{setTempF:F0}°F / {state.SetTemperatureC:F1}°C";
        
        double actualTempF = state.IndoorTempC * 9.0 / 5.0 + 32.0;
        ActualTempStatus.Text = $"{actualTempF:F0}°F / {state.IndoorTempC:F1}°C";
        
        // Compressor
        CompressorFrequency.Text = _viewModel.CompressorDisplay;
        CompressorProgress.Value = state.CompressorFrequency;
        CompressorStatus.Text = GetCompressorStatusText(state.CompressorFrequency);
        
        // Fan
        FanSpeedStatus.Text = state.FanSpeed.ToString();
        FanRpmStatus.Text = "N/A"; // Fan RPM not available in current API
        SwingStatus.Text = state.SwingMode.ToString();
        
        // Device Info
        IpAddressText.Text = _viewModel.IpAddress ?? "--";
        MacAddressText.Text = _viewModel.MacAddress ?? "--";
        FirmwareText.Text = _viewModel.FirmwareVersion ?? "--";
        ModelText.Text = "--"; // No model in current state
        
        // Raw Data
        RawSensorText.Text = FormatRawData(state);
        
        // Raw HTTP Responses with timestamp
        RawHttpTimestamp.Text = $"Last updated: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}";
        RawSensorHttpText.Text = state.RawSensorResponse ?? "--";
        RawControlHttpText.Text = state.RawControlResponse ?? "--";
        RawBasicHttpText.Text = state.RawBasicResponse ?? "--";
        
        // Connectivity spinner - show when connecting or polling
        ConnectingSpinner.Visibility = _viewModel.ConnectionState == Models.ConnectionState.Connecting 
            ? Microsoft.UI.Xaml.Visibility.Visible 
            : Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    private static string FormatRawData(Models.DaikinState state)
    {
        return $"""
            Indoor Temp: {state.IndoorTempC:F1}°C
            Outdoor Temp: {state.OutdoorTempC:F1}°C
            Set Temp: {state.SetTemperatureC:F1}°C
            Compressor: {state.CompressorFrequency} Hz
            Error Code: {state.ErrorCode}
            MAC: {state.MacAddress}
            Firmware: {state.FirmwareVersion}
            """;
    }

    private static string GetCompressorStatusText(double frequency)
    {
        return frequency switch
        {
            0 => "Idle",
            < 30 => "Low Load",
            < 60 => "Medium Load",
            < 90 => "High Load",
            _ => "Maximum"
        };
    }

    private void RefreshButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // The diagnostics view model is automatically updated by MainViewModel's polling
        // This button just triggers a UI refresh
        UpdateAllUI();
    }
}
