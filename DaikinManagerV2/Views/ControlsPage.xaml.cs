using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using DaikinManagerV2.Models;
using DaikinManagerV2.ViewModels;
using DaikinManagerV2.Controls;
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
        
        // Listen to temperature changes from dial
        TempDial.RegisterPropertyChangedCallback(TemperatureDial.TemperatureProperty, OnDialTemperatureChanged);
        
        // Set initial page state (will show loading until data arrives)
        UpdatePageState();
        
        // Initial UI sync (only matters once we switch to Ready state)
        SyncUIFromViewModel();
    }
    
    #region Page State Management
    
    private void UpdatePageState()
    {
        var state = _viewModel.PageState;
        
        switch (state)
        {
            case Models.PageState.Loading:
                ShowLoading();
                break;
            case Models.PageState.Error:
                ShowError();
                break;
            case Models.PageState.Ready:
                ShowControls();
                break;
        }
    }
    
    private void ShowLoading()
    {
        LoadingPanel.Visibility = Visibility.Visible;
        ErrorPanel.Visibility = Visibility.Collapsed;
        ControlsPanel.Visibility = Visibility.Collapsed;
    }
    
    private void ShowError()
    {
        LoadingPanel.Visibility = Visibility.Collapsed;
        ErrorPanel.Visibility = Visibility.Visible;
        ControlsPanel.Visibility = Visibility.Collapsed;
        
        // Update error message
        ErrorMessageText.Text = string.IsNullOrEmpty(_viewModel.ErrorMessage) 
            ? "Unable to connect to the Daikin unit" 
            : _viewModel.ErrorMessage;
    }
    
    private void ShowControls()
    {
        LoadingPanel.Visibility = Visibility.Collapsed;
        ErrorPanel.Visibility = Visibility.Collapsed;
        ControlsPanel.Visibility = Visibility.Visible;
        
        // Make sure UI is synced when showing controls
        SyncUIFromViewModel();
    }
    
    private async void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        // Go back to loading state and retry connection
        await _viewModel.RetryConnectionAsync();
    }
    
    #endregion
    
    private void OnDialTemperatureChanged(DependencyObject sender, DependencyProperty dp)
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
                double tempF = TempDial.Temperature;
                double tempC = (tempF - 32.0) * 5.0 / 9.0;
                _viewModel.StagedTemperatureC = tempC;
            });
        };
        _debounceTimer.Start();
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
                case nameof(ControlsViewModel.PageState):
                    UpdatePageState();
                    break;
                case nameof(ControlsViewModel.ErrorMessage):
                    ErrorMessageText.Text = string.IsNullOrEmpty(_viewModel.ErrorMessage) 
                        ? "Unable to connect to the Daikin unit" 
                        : _viewModel.ErrorMessage;
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
            PowerButton.Background = new SolidColorBrush(Microsoft.UI.Colors.Green);
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
        
        TempDial.IsEnabled = isPowerOn && _viewModel.StagedMode != DaikinMode.Fan;
        FanComboBox.IsEnabled = isPowerOn;
        SwingComboBox.IsEnabled = isPowerOn;
        
        // Visual feedback for dial only (not the Actual temp text)
        TempDial.Opacity = isPowerOn && _viewModel.StagedMode != DaikinMode.Fan ? 1.0 : 0.5;
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
            
            // Update label colors - selected = white, unselected = gray
            var selectedBrush = new SolidColorBrush(Microsoft.UI.Colors.White);
            var unselectedBrush = new SolidColorBrush(Color.FromArgb(255, 140, 140, 140));
            ModeCoolLabel.Foreground = mode == DaikinMode.Cool ? selectedBrush : unselectedBrush;
            ModeHeatLabel.Foreground = mode == DaikinMode.Heat ? selectedBrush : unselectedBrush;
            ModeAutoLabel.Foreground = mode == DaikinMode.Auto ? selectedBrush : unselectedBrush;
            ModeDryLabel.Foreground = mode == DaikinMode.Dry ? selectedBrush : unselectedBrush;
            ModeFanLabel.Foreground = mode == DaikinMode.Fan ? selectedBrush : unselectedBrush;
            
            // Update temperature slider enabled state based on mode
            bool canSetTemp = _viewModel.StagedPower && mode != DaikinMode.Fan;
            TempDial.IsEnabled = canSetTemp;
            TempDial.Opacity = canSetTemp ? 1.0 : 0.5;
            
            // Update dial mode for color
            TempDial.Mode = mode switch
            {
                DaikinMode.Cool => DialMode.Cool,
                DaikinMode.Heat => DialMode.Heat,
                DaikinMode.Auto => DialMode.Auto,
                DaikinMode.Dry => DialMode.Dry,
                DaikinMode.Fan => DialMode.Fan,
                _ => DialMode.Cool
            };
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
            
            TempDial.Temperature = tempF;
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
                // Radio button behavior: if clicking the already-selected mode, keep it selected
                if (mode == _viewModel.StagedMode)
                {
                    // Re-check it to prevent deselection
                    button.IsChecked = true;
                }
                else
                {
                    _viewModel.StagedMode = mode;
                }
            }
        }
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
        ApplyButton.Content = "Applying...";
        
        try
        {
            await _viewModel.ApplySettingsAsync();
        }
        finally
        {
            ApplyButton.Content = "APPLY SETTINGS";
            UpdateApplyButtonUI();
        }
    }
}
