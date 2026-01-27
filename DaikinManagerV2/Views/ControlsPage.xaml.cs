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
    private Brush? _fanBarNeutralBrush;

    public ControlsPage()
    {
        this.InitializeComponent();
        this.Loaded += ControlsPage_Loaded;
    }
    
    private void ControlsPage_Loaded(object sender, RoutedEventArgs e)
    {
        // Re-sync UI after visual tree is fully loaded (needed for template-based shadows)
        // Defer slightly to ensure button templates are applied
        if (_viewModel != null)
        {
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                SyncUIFromViewModel();
            });
        }
    }

    public void Initialize(ControlsViewModel viewModel)
    {
        _viewModel = viewModel;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Capture neutral brush so we can restore after Auto mode uses a special color
        _fanBarNeutralBrush = FanBar0.Background;
        
        // Listen to temperature changes from dial
        TempDial.RegisterPropertyChangedCallback(TemperatureDial.TemperatureProperty, OnDialTemperatureChanged);
        
        // Set initial page state (will show loading until data arrives)
        UpdatePageState();
        
        // Initial UI sync (only matters once we switch to Ready state)
        SyncUIFromViewModel();
    }
    
    /// <summary>
    /// Helper to find a named element inside a templated control's visual tree.
    /// </summary>
    private static T? FindTemplateChild<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T element && element.Name == name)
            {
                return element;
            }
            
            var result = FindTemplateChild<T>(child, name);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Sets the InsetShadow opacity for a ToggleButton using the custom template.
    /// </summary>
    private static void SetInsetShadowOpacity(ToggleButton button, double opacity)
    {
        var shadow = FindTemplateChild<Border>(button, "InsetShadow");
        if (shadow != null)
        {
            shadow.Opacity = opacity;
        }
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
            
            // Labels: bold + primary for selected, muted for unselected
            var primaryBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
            var mutedBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];

            ModeCoolLabel.Foreground = mode == DaikinMode.Cool ? primaryBrush : mutedBrush;
            ModeHeatLabel.Foreground = mode == DaikinMode.Heat ? primaryBrush : mutedBrush;
            ModeAutoLabel.Foreground = mode == DaikinMode.Auto ? primaryBrush : mutedBrush;
            ModeDryLabel.Foreground = mode == DaikinMode.Dry ? primaryBrush : mutedBrush;
            ModeFanLabel.Foreground = mode == DaikinMode.Fan ? primaryBrush : mutedBrush;

            ModeCoolLabel.FontWeight = mode == DaikinMode.Cool ? FontWeights.SemiBold : FontWeights.Normal;
            ModeHeatLabel.FontWeight = mode == DaikinMode.Heat ? FontWeights.SemiBold : FontWeights.Normal;
            ModeAutoLabel.FontWeight = mode == DaikinMode.Auto ? FontWeights.SemiBold : FontWeights.Normal;
            ModeDryLabel.FontWeight = mode == DaikinMode.Dry ? FontWeights.SemiBold : FontWeights.Normal;
            ModeFanLabel.FontWeight = mode == DaikinMode.Fan ? FontWeights.SemiBold : FontWeights.Normal;

            // Icon colors: white when selected, grey when not
            var whiteBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
            var greyBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorDisabledBrush"];
            
            HeatIcon.Foreground = mode == DaikinMode.Heat ? whiteBrush : greyBrush;
            AutoIcon.Foreground = mode == DaikinMode.Auto ? whiteBrush : greyBrush;
            DryIcon.Foreground = mode == DaikinMode.Dry ? whiteBrush : greyBrush;

            // Image icons can't be tinted via Foreground; use opacity for selection cue.
            CoolIcon.Opacity = mode == DaikinMode.Cool ? 1.0 : 0.35;
            FanModeIcon.Opacity = mode == DaikinMode.Fan ? 1.0 : 0.35;
            
            // Inset shadow effect - show on selected button (pressed-in look)
            SetInsetShadowOpacity(ModeCool, mode == DaikinMode.Cool ? 1.0 : 0.0);
            SetInsetShadowOpacity(ModeHeat, mode == DaikinMode.Heat ? 1.0 : 0.0);
            SetInsetShadowOpacity(ModeAuto, mode == DaikinMode.Auto ? 1.0 : 0.0);
            SetInsetShadowOpacity(ModeDry, mode == DaikinMode.Dry ? 1.0 : 0.0);
            SetInsetShadowOpacity(ModeFan, mode == DaikinMode.Fan ? 1.0 : 0.0);
            
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
        var primaryBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
        var mutedBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
        var neutralBarBrush = _fanBarNeutralBrush ?? mutedBrush;
        
        int sliderPos = (int)FanSpeedSlider.Value;
        
        // Disable/fade controls when Auto is on
        FanSpeedSlider.IsEnabled = !isAuto;
        FanBarsGrid.Opacity = isAuto ? 0.5 : 1.0;
        FanLabelsGrid.Opacity = isAuto ? 0.5 : 1.0;
        
        // Summary label: one prominent readout
        FanSummaryText.Text = isAuto ? "Fan: Auto" : $"Fan: {SliderPosToLabel(sliderPos)}";

        // Keep the Auto label subtle (acts like an affordance, not the primary readout)
        AutoToggleLabel.Foreground = mutedBrush;
        AutoToggleLabel.FontWeight = FontWeights.Normal;
        
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
            // Manual mode - neutral bars (avoid competing with dial accent)
            FanBar0.Background = neutralBarBrush;
            FanBar1.Background = neutralBarBrush;
            FanBar2.Background = neutralBarBrush;
            FanBar3.Background = neutralBarBrush;
            
            // Progressive visibility: Quiet=1 bar, Low=2, Med=3, High=4 (all)
            FanBar0.Opacity = sliderPos >= 0 ? 1 : 0;
            FanBar1.Opacity = sliderPos >= 1 ? 1 : 0;
            FanBar2.Opacity = sliderPos >= 2 ? 1 : 0;
            FanBar3.Opacity = sliderPos >= 3 ? 1 : 0;
            
            // Update label styling - highlight selected label without accent color
            TextBlock[] labels = { QuietLabel, LowLabel, MediumLabel, HighLabel };
            for (int i = 0; i < labels.Length; i++)
            {
                if (i == sliderPos)
                {
                    labels[i].FontWeight = FontWeights.SemiBold;
                    labels[i].Foreground = primaryBrush;
                }
                else
                {
                    labels[i].FontWeight = FontWeights.Normal;
                    labels[i].Foreground = mutedBrush;
                }
            }
        }
    }

    private static string SliderPosToLabel(int sliderPos) => sliderPos switch
    {
        0 => "Quiet",
        1 => "Low",
        2 => "Medium",
        3 => "High",
        _ => "--"
    };
    
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
            
            // Labels: bold + primary for selected, muted for unselected
            var primaryBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
            var mutedBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];

            SwingStoppedLabel.Foreground = swing == SwingMode.Off ? primaryBrush : mutedBrush;
            SwingVerticalLabel.Foreground = swing == SwingMode.Vertical ? primaryBrush : mutedBrush;
            SwingHorizontalLabel.Foreground = swing == SwingMode.Horizontal ? primaryBrush : mutedBrush;
            SwingBothLabel.Foreground = swing == SwingMode.Both ? primaryBrush : mutedBrush;

            SwingStoppedLabel.FontWeight = swing == SwingMode.Off ? FontWeights.SemiBold : FontWeights.Normal;
            SwingVerticalLabel.FontWeight = swing == SwingMode.Vertical ? FontWeights.SemiBold : FontWeights.Normal;
            SwingHorizontalLabel.FontWeight = swing == SwingMode.Horizontal ? FontWeights.SemiBold : FontWeights.Normal;
            SwingBothLabel.FontWeight = swing == SwingMode.Both ? FontWeights.SemiBold : FontWeights.Normal;

            // Icon colors: white when selected, grey when not
            var whiteBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
            var greyBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorDisabledBrush"];
            
            SwingStoppedIcon.Foreground = swing == SwingMode.Off ? whiteBrush : greyBrush;
            SwingVerticalIcon1.Foreground = swing == SwingMode.Vertical ? whiteBrush : greyBrush;
            SwingVerticalIcon2.Foreground = swing == SwingMode.Vertical ? whiteBrush : greyBrush;
            SwingHorizontalIcon.Foreground = swing == SwingMode.Horizontal ? whiteBrush : greyBrush;
            SwingBothIcon.Foreground = swing == SwingMode.Both ? whiteBrush : greyBrush;
            
            // Inset shadow effect - show on selected button (pressed-in look)
            SetInsetShadowOpacity(SwingStopped, swing == SwingMode.Off ? 1.0 : 0.0);
            SetInsetShadowOpacity(SwingVertical, swing == SwingMode.Vertical ? 1.0 : 0.0);
            SetInsetShadowOpacity(SwingHorizontal, swing == SwingMode.Horizontal ? 1.0 : 0.0);
            SetInsetShadowOpacity(SwingBoth, swing == SwingMode.Both ? 1.0 : 0.0);
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

    private void ModeButton_Checked(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingUI) return;

        if (sender is ToggleButton button && button.Tag is string modeStr)
        {
            if (Enum.TryParse<DaikinMode>(modeStr, out var mode))
            {
                // Update the ViewModel - this triggers UpdateModeButtonsUI via PropertyChanged
                // which will properly uncheck the other buttons
                _viewModel.StagedMode = mode;
            }
        }
    }

    private void ModeButton_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingUI) return;

        if (sender is ToggleButton button && button.Tag is string modeStr && Enum.TryParse<DaikinMode>(modeStr, out var mode))
        {
            // Prevent "toggle off" for the active selection.
            if (_viewModel.StagedMode == mode)
            {
                _isUpdatingUI = true;
                try
                {
                    button.IsChecked = true;
                }
                finally
                {
                    _isUpdatingUI = false;
                }
            }
        }
    }

    private void SwingButton_Checked(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingUI) return;

        if (sender is ToggleButton btn && btn.Tag is string tagStr)
        {
            if (Enum.TryParse<SwingMode>(tagStr, out var swing))
            {
                // Update the ViewModel - this triggers UpdateSwingButtonsUI via PropertyChanged
                // which will properly uncheck the other buttons
                _viewModel.StagedSwingMode = swing;
            }
        }
    }

    private void SwingButton_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingUI) return;

        if (sender is ToggleButton btn && btn.Tag is string tagStr && Enum.TryParse<SwingMode>(tagStr, out var swing))
        {
            // Prevent "toggle off" for the active selection.
            if (_viewModel.StagedSwingMode == swing)
            {
                _isUpdatingUI = true;
                try
                {
                    btn.IsChecked = true;
                }
                finally
                {
                    _isUpdatingUI = false;
                }
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
