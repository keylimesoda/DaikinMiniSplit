using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
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
                    UpdateControlsEnabledState(_viewModel.StagedPower);
                    break;
                case nameof(ControlsViewModel.StagedMode):
                    UpdateModeButtonsUI();
                    UpdateFanComboUI(); // Show/hide Auto based on mode
                    break;
                case nameof(ControlsViewModel.StagedTemperatureC):
                    UpdateTemperatureUI();
                    break;
                case nameof(ControlsViewModel.StagedFanSpeed):
                    UpdateFanComboUI();
                    break;
                case nameof(ControlsViewModel.StagedSwingMode):
                    UpdateSwingButtonsUI();
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
            UpdateControlsEnabledState(_viewModel.StagedPower);
            UpdateModeButtonsUI();
            UpdateTemperatureUI();
            UpdateFanComboUI();
            UpdateSwingButtonsUI();
            UpdateApplyButtonUI();
            UpdateActualTempUI();
        }
        finally
        {
            _isUpdatingUI = false;
        }
    }

    private void UpdateControlsEnabledState(bool isPowerOn)
    {
        // Mode buttons
        ModeCool.IsEnabled = isPowerOn;
        ModeHeat.IsEnabled = isPowerOn;
        ModeAuto.IsEnabled = isPowerOn;
        ModeDry.IsEnabled = isPowerOn;
        ModeFan.IsEnabled = isPowerOn;
        
        // Mode button visual dimming
        double modeOpacity = isPowerOn ? 1.0 : 0.4;
        ModeCool.Opacity = modeOpacity;
        ModeHeat.Opacity = modeOpacity;
        ModeAuto.Opacity = modeOpacity;
        ModeDry.Opacity = modeOpacity;
        ModeFan.Opacity = modeOpacity;
        ModeCoolLabel.Opacity = modeOpacity;
        ModeHeatLabel.Opacity = modeOpacity;
        ModeAutoLabel.Opacity = modeOpacity;
        ModeDryLabel.Opacity = modeOpacity;
        ModeFanLabel.Opacity = modeOpacity;
        
        // Temperature dial
        bool canSetTemp = isPowerOn && _viewModel.StagedMode != DaikinMode.Fan;
        TempDial.IsEnabled = canSetTemp;
        TempDial.Opacity = canSetTemp ? 1.0 : 0.4;
        
        // Fan speed slider
        FanSpeedSlider.IsEnabled = isPowerOn;
        FanSpeedPanel.Opacity = isPowerOn ? 1.0 : 0.4;
        
        // Swing
        SwingSection.Opacity = isPowerOn ? 1.0 : 0.4;
        SwingStopped.IsEnabled = isPowerOn;
        SwingVertical.IsEnabled = isPowerOn;
        SwingHorizontal.IsEnabled = isPowerOn;
        SwingBoth.IsEnabled = isPowerOn;
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

    // Mapping: Slider position -> FanSpeed enum (no Auto on slider anymore)
    // Slider: 0=Quiet, 1=Low, 2=Med, 3=High
    // Enum:   Auto=0, Quiet=1, Low=2, Medium=3, High=4
    private static readonly FanSpeed[] SliderToFanSpeed = { FanSpeed.Quiet, FanSpeed.Low, FanSpeed.Medium, FanSpeed.High };
    private static readonly int[] FanSpeedToSlider = { 2, 0, 1, 2, 3 }; // Auto->2(Medium default), Quiet->0, Low->1, Med->2, High->3
    private int _lastManualSliderPos = 2; // Remember last manual setting (default Medium)
    
    private void UpdateFanComboUI()
    {
        _isUpdatingUI = true;
        try
        {
            bool isAuto = _viewModel.StagedFanSpeed == FanSpeed.Auto;
            FanAutoToggle.IsOn = isAuto;
            
            // Hide Auto toggle in Fan-Only mode (Auto not supported)
            bool isFanOnlyMode = _viewModel.StagedMode == DaikinMode.Fan;
            FanAutoToggle.Visibility = isFanOnlyMode ? Visibility.Collapsed : Visibility.Visible;
            AutoToggleLabel.Visibility = isFanOnlyMode ? Visibility.Collapsed : Visibility.Visible;
            
            // If in Fan-Only and was on Auto, switch to last manual setting
            if (isFanOnlyMode && isAuto)
            {
                _viewModel.StagedFanSpeed = SliderToFanSpeed[_lastManualSliderPos];
                isAuto = false;
            }
            
            if (!isAuto)
            {
                int enumIndex = (int)_viewModel.StagedFanSpeed;
                int sliderPos = FanSpeedToSlider[enumIndex];
                _lastManualSliderPos = sliderPos;
                FanSpeedSlider.Value = sliderPos;
            }
            
            UpdateFanSpeedVisuals(isAuto);
        }
        finally
        {
            _isUpdatingUI = false;
        }
    }
    
    private void UpdateFanSpeedVisuals(bool isAuto)
    {
        // Auto uses teal color (same as Auto mode on temp dial)
        var autoBrush = new SolidColorBrush(Color.FromArgb(255, 60, 179, 113)); // Medium sea green
        var accentBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        var accentTextBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"];
        var mutedBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
        
        int sliderPos = (int)FanSpeedSlider.Value;
        
        // Disable/fade controls when Auto is on
        FanSpeedSlider.IsEnabled = !isAuto;
        FanBarsGrid.Opacity = isAuto ? 0.5 : 1.0;
        FanLabelsGrid.Opacity = isAuto ? 0.5 : 1.0;
        
        // Update Auto toggle label color
        AutoToggleLabel.Foreground = isAuto ? autoBrush : mutedBrush;
        AutoToggleLabel.FontWeight = isAuto ? FontWeights.Bold : FontWeights.Normal;
        
        if (isAuto)
        {
            // Auto - all bars visible with teal color
            FanBar0.Background = autoBrush;
            FanBar1.Background = autoBrush;
            FanBar2.Background = autoBrush;
            FanBar3.Background = autoBrush;
            FanBar0.Opacity = 1;
            FanBar1.Opacity = 1;
            FanBar2.Opacity = 1;
            FanBar3.Opacity = 1;
            
            // All labels muted when Auto
            QuietLabel.FontWeight = FontWeights.Normal;
            QuietLabel.Foreground = mutedBrush;
            LowLabel.FontWeight = FontWeights.Normal;
            LowLabel.Foreground = mutedBrush;
            MediumLabel.FontWeight = FontWeights.Normal;
            MediumLabel.Foreground = mutedBrush;
            HighLabel.FontWeight = FontWeights.Normal;
            HighLabel.Foreground = mutedBrush;
        }
        else
        {
            // Manual mode - accent color bars
            FanBar0.Background = accentBrush;
            FanBar1.Background = accentBrush;
            FanBar2.Background = accentBrush;
            FanBar3.Background = accentBrush;
            
            // Progressive visibility: Quiet=1 bar, Low=2, Med=3, High=4 (all)
            FanBar0.Opacity = sliderPos >= 0 ? 1 : 0;
            FanBar1.Opacity = sliderPos >= 1 ? 1 : 0;
            FanBar2.Opacity = sliderPos >= 2 ? 1 : 0;
            FanBar3.Opacity = sliderPos >= 3 ? 1 : 0;
            
            // Update label styling - highlight selected label
            TextBlock[] labels = { QuietLabel, LowLabel, MediumLabel, HighLabel };
            for (int i = 0; i < labels.Length; i++)
            {
                if (i == sliderPos)
                {
                    labels[i].FontWeight = FontWeights.Bold;
                    labels[i].Foreground = accentTextBrush;
                }
                else
                {
                    labels[i].FontWeight = FontWeights.Normal;
                    labels[i].Foreground = mutedBrush;
                }
            }
        }
    }
    
    private void FanAutoToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingUI) return;
        
        bool isAuto = FanAutoToggle.IsOn;
        if (isAuto)
        {
            _viewModel.StagedFanSpeed = FanSpeed.Auto;
        }
        else
        {
            // Restore last manual setting
            _viewModel.StagedFanSpeed = SliderToFanSpeed[_lastManualSliderPos];
        }
        UpdateFanSpeedVisuals(isAuto);
    }
    
    private void FanSpeedSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_isUpdatingUI) return;
        
        int sliderPos = (int)e.NewValue;
        _lastManualSliderPos = sliderPos;
        _viewModel.StagedFanSpeed = SliderToFanSpeed[sliderPos];
        UpdateFanSpeedVisuals(false); // Not auto when slider is changing
    }

    private void UpdateSwingButtonsUI()
    {
        var swing = _viewModel.StagedSwingMode;
        
        _isUpdatingUI = true;
        try
        {
            SwingStopped.IsChecked = swing == SwingMode.Off;
            SwingVertical.IsChecked = swing == SwingMode.Vertical;
            SwingHorizontal.IsChecked = swing == SwingMode.Horizontal;
            SwingBoth.IsChecked = swing == SwingMode.Both;
            
            // Update label colors - use accent color for selected (consistent with fan speed labels)
            var accentBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"];
            var mutedBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
            
            SwingStoppedLabel.Foreground = swing == SwingMode.Off ? accentBrush : mutedBrush;
            SwingVerticalLabel.Foreground = swing == SwingMode.Vertical ? accentBrush : mutedBrush;
            SwingHorizontalLabel.Foreground = swing == SwingMode.Horizontal ? accentBrush : mutedBrush;
            SwingBothLabel.Foreground = swing == SwingMode.Both ? accentBrush : mutedBrush;
            
            // Bold the selected label
            SwingStoppedLabel.FontWeight = swing == SwingMode.Off ? FontWeights.Bold : FontWeights.Normal;
            SwingVerticalLabel.FontWeight = swing == SwingMode.Vertical ? FontWeights.Bold : FontWeights.Normal;
            SwingHorizontalLabel.FontWeight = swing == SwingMode.Horizontal ? FontWeights.Bold : FontWeights.Normal;
            SwingBothLabel.FontWeight = swing == SwingMode.Both ? FontWeights.Bold : FontWeights.Normal;
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

    private void SwingButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingUI) return;
        
        if (sender is ToggleButton btn && btn.Tag is string tagStr)
        {
            if (Enum.TryParse<SwingMode>(tagStr, out var swing))
            {
                _viewModel.StagedSwingMode = swing;
                UpdateSwingButtonsUI();
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
