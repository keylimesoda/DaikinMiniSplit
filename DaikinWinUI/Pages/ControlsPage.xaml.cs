using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using DaikinManager.Services;

namespace DaikinManager.Pages;

public sealed partial class ControlsPage : Page
{
    private DaikinService? _service;
    private bool _dataLoaded = false;  // True ONLY after successful data fetch + UI update

    public ControlsPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        // Reset state - data is NOT loaded yet
        _dataLoaded = false;
        
        // IMMEDIATELY hide controls and show loading - before any async work
        ShowLoading();
        
        if (e.Parameter is DaikinService service)
        {
            // Unsubscribe from old service if any
            if (_service != null)
            {
                _service.DataRefreshed -= OnDataRefreshed;
            }
            _service = service;
            _service.DataRefreshed += OnDataRefreshed;
        }
        
        // Always fetch fresh data on navigation
        if (_service != null)
        {
            await ConnectAsync();
        }
    }

    private async Task ConnectAsync()
    {
        ShowLoading();
        
        try
        {
            if (App.Current is App appStart && appStart.m_window is MainWindow mainWindowStart)
            {
                mainWindowStart.ResetAutoRefresh();
            }

            await _service!.RefreshAllAsync();
            UpdateUI();
            _dataLoaded = true;
            ShowControls();
            
            if (App.Current is App app && app.m_window is MainWindow mainWindow)
            {
                mainWindow.SetConnected();
                mainWindow.StartAutoRefresh();
            }
        }
        catch (Exception ex)
        {
            _dataLoaded = false;
            ShowError(ex.Message);
            
            if (App.Current is App app && app.m_window is MainWindow mainWindow)
            {
                mainWindow.SetOffline();
            }
        }
    }

    private void ShowLoading()
    {
        LoadingPanel.Visibility = Visibility.Visible;
        ErrorPanel.Visibility = Visibility.Collapsed;
        ControlsPanel.Visibility = Visibility.Collapsed;
    }

    private void ShowError(string message)
    {
        LoadingPanel.Visibility = Visibility.Collapsed;
        ErrorPanel.Visibility = Visibility.Visible;
        ControlsPanel.Visibility = Visibility.Collapsed;
        ErrorMessage.Text = message;
    }

    private void ShowControls()
    {
        LoadingPanel.Visibility = Visibility.Collapsed;
        ErrorPanel.Visibility = Visibility.Collapsed;
        ControlsPanel.Visibility = Visibility.Visible;
        
        // Force UI update after controls are visible
        DispatcherQueue.TryEnqueue(() => UpdateUI());
    }

    private async void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        await ConnectAsync();
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
        DispatcherQueue.TryEnqueue(() =>
        {
            // Only update UI if we've completed initial load (data was already loaded)
            // This event fires from auto-refresh timer, so _dataLoaded should already be true
            if (_dataLoaded)
            {
                UpdateUI();
            }
        });
    }

    private void UpdateUI()
    {
        if (_service == null) return;

        Console.WriteLine($"[UpdateUI] Service state: pow={_service.IsPoweredOn}, mode={_service.Mode}, tempC={_service.SetTemperatureC}");
        
        // Power button
        PowerText.Text = _service.IsPoweredOn ? "POWER: ON" : "POWER: OFF";
        PowerIcon.Glyph = _service.IsPoweredOn ? "\uE7E8" : "\uE7E8";
        PowerButton.Background = new SolidColorBrush(
            _service.IsPoweredOn ? Colors.Green : Colors.Gray);

        // Mode - set the correct toggle button
        SetModeButton(_service.Mode);

        // Temperature
        double tempF = DaikinService.CelsiusToFahrenheit(_service.SetTemperatureC);
        Console.WriteLine($"[UpdateUI] Setting TempDial.Temperature to {tempF}°F (from {_service.SetTemperatureC}°C)");
        TempDial.Temperature = tempF;
        TempDial.Mode = _service.Mode;
        Console.WriteLine($"[UpdateUI] After set, TempDial.Temperature = {TempDial.Temperature}");
        
        double actualF = DaikinService.CelsiusToFahrenheit(_service.IndoorTempC);
        ActualTempText.Text = $"Actual: {actualF:F1}°F";

        // Fan
        FanComboBox.SelectedIndex = (int)_service.FanSpeed;

        // Swing
        SwingComboBox.SelectedIndex = (int)_service.SwingMode;
    }

    private DaikinMode _selectedMode = DaikinMode.Cool;

    private void SetModeButton(DaikinMode mode)
    {
        _selectedMode = mode;
        ModeCool.IsChecked = mode == DaikinMode.Cool;
        ModeHeat.IsChecked = mode == DaikinMode.Heat;
        ModeAuto.IsChecked = mode == DaikinMode.Auto;
        ModeDry.IsChecked = mode == DaikinMode.Dry;
        ModeFan.IsChecked = mode == DaikinMode.Fan;
        
        // Update label colors - selected = white, unselected = gray
        var selectedBrush = new SolidColorBrush(Colors.White);
        var unselectedBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 140, 140, 140));
        ModeCoolLabel.Foreground = mode == DaikinMode.Cool ? selectedBrush : unselectedBrush;
        ModeHeatLabel.Foreground = mode == DaikinMode.Heat ? selectedBrush : unselectedBrush;
        ModeAutoLabel.Foreground = mode == DaikinMode.Auto ? selectedBrush : unselectedBrush;
        ModeDryLabel.Foreground = mode == DaikinMode.Dry ? selectedBrush : unselectedBrush;
        ModeFanLabel.Foreground = mode == DaikinMode.Fan ? selectedBrush : unselectedBrush;
    }

    private void ModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton btn && btn.Tag is string tag)
        {
            var mode = Enum.Parse<DaikinMode>(tag);
            SetModeButton(mode);
            TempDial.Mode = mode;
        }
    }

    private async void PowerButton_Click(object sender, RoutedEventArgs e)
    {
        if (_service == null) return;
        
        try
        {
            await _service.TogglePowerAsync();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to toggle power: {ex.Message}");
        }
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_service == null) return;

        try
        {
            var mode = _selectedMode;
            var fan = (FanSpeed)FanComboBox.SelectedIndex;
            var swing = (SwingMode)SwingComboBox.SelectedIndex;

            await _service.SetControlAsync(
                _service.IsPoweredOn,
                mode,
                TempDial.Temperature,
                fan,
                swing
            );

            // Reset auto-refresh timer
            if (App.Current is App app && app.m_window is MainWindow mainWindow)
            {
                mainWindow.ResetAutoRefresh();
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to apply settings: {ex.Message}");
        }
    }

    private async Task ShowErrorAsync(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "Error",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }
}
