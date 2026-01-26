using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using DaikinManagerV2.Models;
using DaikinManagerV2.ViewModels;
using Windows.UI;

namespace DaikinManagerV2.Views;

public sealed partial class ControlsPage : Page
{
    private ControlsViewModel _viewModel = null!;
    private bool _isUpdatingUI;
    private System.Timers.Timer? _debounceTimer;

    public ControlsPage()
    {
        this.InitializeComponent();
    }

    public void Initialize(ControlsViewModel viewModel)
    {
        _viewModel = viewModel;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        
        // Initial UI sync
        SyncUIFromViewModel();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(ControlsViewModel.StagedPower):
                    UpdatePowerButtonUI();
                    break;
                case nameof(ControlsViewModel.StagedMode):
                    UpdateModeButtonsUI();
                    break;
                case nameof(ControlsViewModel.StagedTemperatureC):
                    UpdateTemperatureUI();
                    break;
                case nameof(ControlsViewModel.StagedFanSpeed):
                    UpdateFanComboUI();
                    break;
                case nameof(ControlsViewModel.StagedSwingMode):
                    UpdateSwingComboUI();
                    break;
                case nameof(ControlsViewModel.HasPendingChanges):
                    UpdateApplyButtonUI();
                    break;
                case nameof(ControlsViewModel.IndoorTemperatureDisplay):
                    UpdateActualTempUI();
                    break;
            }
        });
    }

    private void SyncUIFromViewModel()
    {
        _isUpdatingUI = true;
        try
        {
            UpdatePowerButtonUI();
            UpdateModeButtonsUI();
            UpdateTemperatureUI();
            UpdateFanComboUI();
            UpdateSwingComboUI();
            UpdateApplyButtonUI();
            UpdateActualTempUI();
        }
        finally
        {
            _isUpdatingUI = false;
        }
    }

    private void UpdatePowerButtonUI()
    {
        bool isPowerOn = _viewModel.StagedPower;
        
        PowerText.Text = isPowerOn ? "POWER: ON" : "POWER: OFF";
        
        // Update visual state
        if (isPowerOn)
        {
            PowerButton.Background = new SolidColorBrush(Color.FromArgb(255, 76, 175, 80)); // Green
            PowerIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
            PowerText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
        }
        else
        {
            PowerButton.Background = null; // Default
            PowerIcon.Foreground = null; // Inherit
            PowerText.Foreground = null; // Inherit
        }
        
        // Enable/disable mode and temperature controls based on power state
        UpdateControlsEnabledState(isPowerOn);
    }

    private void UpdateControlsEnabledState(bool isPowerOn)
    {
        ModeCool.IsEnabled = isPowerOn;
        ModeHeat.IsEnabled = isPowerOn;
        ModeAuto.IsEnabled = isPowerOn;
        ModeDry.IsEnabled = isPowerOn;
        ModeFan.IsEnabled = isPowerOn;
        
        TempSlider.IsEnabled = isPowerOn && _viewModel.StagedMode != DaikinMode.Fan;
        FanComboBox.IsEnabled = isPowerOn;
        SwingComboBox.IsEnabled = isPowerOn;
        
        // Visual feedback for temperature section
        TemperatureSection.Opacity = isPowerOn && _viewModel.StagedMode != DaikinMode.Fan ? 1.0 : 0.5;
    }

    private void UpdateModeButtonsUI()
    {
        var mode = _viewModel.StagedMode;
        
        _isUpdatingUI = true;
        try
        {
            ModeCool.IsChecked = mode == DaikinMode.Cool;
            ModeHeat.IsChecked = mode == DaikinMode.Heat;
            ModeAuto.IsChecked = mode == DaikinMode.Auto;
            ModeDry.IsChecked = mode == DaikinMode.Dry;
            ModeFan.IsChecked = mode == DaikinMode.Fan;
            
            // Update temperature slider enabled state based on mode
            bool canSetTemp = _viewModel.StagedPower && mode != DaikinMode.Fan;
            TempSlider.IsEnabled = canSetTemp;
            TemperatureSection.Opacity = canSetTemp ? 1.0 : 0.5;
            
            // Update slider range based on mode
            UpdateTemperatureSliderRange();
        }
        finally
        {
            _isUpdatingUI = false;
        }
    }

    private void UpdateTemperatureSliderRange()
    {
        // Mode-aware temperature validation from spec 10.1
        var (minC, maxC) = _viewModel.StagedMode switch
        {
            DaikinMode.Cool => (18.0, 32.0),
            DaikinMode.Heat => (10.0, 30.0),
            DaikinMode.Auto => (18.0, 30.0),
            _ => (18.0, 30.0)
        };
        
        // Convert to Fahrenheit for display (assuming F for now)
        double minF = minC * 9.0 / 5.0 + 32.0;
        double maxF = maxC * 9.0 / 5.0 + 32.0;
        
        _isUpdatingUI = true;
        try
        {
            TempSlider.Minimum = minF;
            TempSlider.Maximum = maxF;
        }
        finally
        {
            _isUpdatingUI = false;
        }
    }

    private void UpdateTemperatureUI()
    {
        if (_isUpdatingUI) return;
        
        _isUpdatingUI = true;
        try
        {
            double tempC = _viewModel.StagedTemperatureC;
            double tempF = tempC * 9.0 / 5.0 + 32.0;
            
            SetTempText.Text = $"{tempF:F0}°F";
            TempSlider.Value = tempF;
        }
        finally
        {
            _isUpdatingUI = false;
        }
    }

    private void UpdateActualTempUI()
    {
        ActualTempText.Text = $"Actual: {_viewModel.IndoorTemperatureDisplay}";
    }

    private void UpdateFanComboUI()
    {
        _isUpdatingUI = true;
        try
        {
            int index = (int)_viewModel.StagedFanSpeed;
            if (index >= 0 && index < FanComboBox.Items.Count)
            {
                FanComboBox.SelectedIndex = index;
            }
        }
        finally
        {
            _isUpdatingUI = false;
        }
    }

    private void UpdateSwingComboUI()
    {
        _isUpdatingUI = true;
        try
        {
            int index = (int)_viewModel.StagedSwingMode;
            if (index >= 0 && index < SwingComboBox.Items.Count)
            {
                SwingComboBox.SelectedIndex = index;
            }
        }
        finally
        {
            _isUpdatingUI = false;
        }
    }

    private void UpdateApplyButtonUI()
    {
        bool hasChanges = _viewModel.HasPendingChanges;
        ApplyButton.IsEnabled = hasChanges;
        ApplyButton.Opacity = hasChanges ? 1.0 : 0.5;
    }

    private void PowerButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.StagedPower = !_viewModel.StagedPower;
    }

    private void ModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingUI) return;
        
        if (sender is ToggleButton button && button.Tag is string modeStr)
        {
            if (Enum.TryParse<DaikinMode>(modeStr, out var mode))
            {
                _viewModel.StagedMode = mode;
            }
        }
    }

    private void TempSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_isUpdatingUI) return;
        
        // Debounce temperature changes (spec 3.11)
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        
        _debounceTimer = new System.Timers.Timer(200); // 200ms debounce
        _debounceTimer.AutoReset = false;
        _debounceTimer.Elapsed += (s, args) =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                double tempF = TempSlider.Value;
                double tempC = (tempF - 32.0) * 5.0 / 9.0;
                _viewModel.StagedTemperatureC = tempC;
                
                // Update display immediately
                SetTempText.Text = $"{tempF:F0}°F";
            });
        };
        _debounceTimer.Start();
        
        // Update display immediately for responsiveness
        SetTempText.Text = $"{e.NewValue:F0}°F";
    }

    private void FanComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUI) return;
        
        if (FanComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tagStr)
        {
            if (Enum.TryParse<FanSpeed>(tagStr, out var speed))
            {
                _viewModel.StagedFanSpeed = speed;
            }
        }
    }

    private void SwingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUI) return;
        
        if (SwingComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tagStr)
        {
            if (Enum.TryParse<SwingMode>(tagStr, out var swing))
            {
                _viewModel.StagedSwingMode = swing;
            }
        }
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyButton.IsEnabled = false;
        ApplySpinner.Visibility = Visibility.Visible;
        ApplyText.Text = "Applying...";
        
        try
        {
            await _viewModel.ApplySettingsAsync();
        }
        finally
        {
            ApplySpinner.Visibility = Visibility.Collapsed;
            ApplyText.Text = "Apply Settings";
            UpdateApplyButtonUI();
        }
    }
}
