using Microsoft.UI.Xaml.Controls;
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
        
        // System Status
        PowerStatus.Text = state.IsPoweredOn ? "ON" : "OFF";
        ModeStatus.Text = state.Mode.ToString();
        SetTempStatus.Text = $"{state.SetTemperatureC:F1}°C";
        ActualTempStatus.Text = _viewModel.IndoorTemperatureDisplay;
        
        // Compressor
        CompressorFrequency.Text = _viewModel.CompressorDisplay;
        CompressorProgress.Value = state.CompressorFrequency;
        CompressorStatus.Text = GetCompressorStatusText(state.CompressorFrequency);
        
        // Fan
        FanSpeedStatus.Text = state.FanSpeed.ToString();
        FanRpmStatus.Text = "N/A"; // Fan RPM not available in current API
        SwingStatus.Text = state.SwingMode.ToString();
        
        // Error
        if (_viewModel.HasError)
        {
            ErrorCard.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            ErrorText.Text = $"Error Code: {state.ErrorCode}";
        }
        else
        {
            ErrorCard.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }
        
        // Device Info
        IpAddressText.Text = _viewModel.IpAddress ?? "--";
        MacAddressText.Text = _viewModel.MacAddress ?? "--";
        FirmwareText.Text = _viewModel.FirmwareVersion ?? "--";
        ModelText.Text = "--"; // No model in current state
        
        // Raw Data
        RawDataText.Text = FormatRawData(state);
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
}
