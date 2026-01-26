using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using DaikinManager.Services;

namespace DaikinManager.Pages;

public sealed partial class DiagnosticsPage : Page
{
    private DaikinService? _service;

    public DiagnosticsPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (e.Parameter is DaikinService service)
        {
            _service = service;
            _service.DataRefreshed += OnDataRefreshed;
            
            await _service.RefreshAllAsync();
            UpdateUI();
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        if (_service != null)
        {
            _service.DataRefreshed -= OnDataRefreshed;
        }
    }

    private void OnDataRefreshed(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(UpdateUI);
    }

    private void UpdateUI()
    {
        if (_service == null) return;

        // Outside temp
        double outF = DaikinService.CelsiusToFahrenheit(_service.OutdoorTempC);
        OutsideTempText.Text = $"Outside Temp: {outF:F1}°F ({_service.OutdoorTempC}°C)";

        // Compressor
        if (_service.CompressorFrequency == 0)
        {
            CompressorText.Text = "Compressor: Idle";
            CompressorText.Foreground = new SolidColorBrush(Colors.Gray);
        }
        else
        {
            CompressorText.Text = $"Compressor: ACTIVE ({_service.CompressorFrequency} Hz)";
            CompressorText.Foreground = new SolidColorBrush(Colors.Green);
        }

        // Error
        if (_service.ErrorCode == 0)
        {
            ErrorText.Text = "System Health: OK";
            ErrorText.Foreground = new SolidColorBrush(Colors.Green);
        }
        else
        {
            ErrorText.Text = $"ERROR CODE: {_service.ErrorCode}";
            ErrorText.Foreground = new SolidColorBrush(Colors.Red);
        }

        // Hardware info
        MacText.Text = _service.MacAddress;
        FirmwareText.Text = _service.FirmwareVersion;
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (_service == null) return;

        try
        {
            RefreshButton.IsEnabled = false;
            
            // Reset auto-refresh timer
            if (App.Current is App app && app.m_window is MainWindow mainWindow)
            {
                mainWindow.ResetAutoRefresh();
            }

            await _service.RefreshAllAsync();
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = "Refresh Failed",
                Content = ex.Message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }
}
