using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
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
    
    // Mode button tracking
    private readonly Dictionary<DaikinMode, ToggleButton> _modeButtons = new();
    private readonly Dictionary<DaikinMode, TextBlock> _modeLabels = new();
    private readonly Dictionary<DaikinMode, UIElement> _sunkenOverlays = new();

    public ControlsPage()
    {
        this.InitializeComponent();
        this.Loaded += ControlsPage_Loaded;
    }
    
    private void ControlsPage_Loaded(object sender, RoutedEventArgs e)
    {
        // Create mode buttons after visual tree is loaded
        CreateModeButtons();
        
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
    
    #region Mode Buttons
    
    private void CreateModeButtons()
    {
        // Use Segoe UI Symbol for monochrome emoji, FontIcon for MDL2 glyphs
        var modes = new (DaikinMode mode, string label, UIElement icon)[]
        {
            (DaikinMode.Cool, "Cool", new TextBlock { Text = "❄", FontSize = 24, FontFamily = new FontFamily("Segoe UI Symbol"), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, -2, 0, 0) }),
            (DaikinMode.Heat, "Heat", new FontIcon { Glyph = "\uE706", FontSize = 20 }),
            (DaikinMode.Auto, "Auto", new FontIcon { Glyph = "\uE895", FontSize = 20 }),
            (DaikinMode.Dry, "Dry", new FontIcon { Glyph = "\uEB42", FontSize = 20 }),
            (DaikinMode.Fan, "Fan", CreatePngIcon("ms-appx:///Assets/fan.png")),
        };
        
        for (int i = 0; i < modes.Length; i++)
        {
            var (mode, label, icon) = modes[i];
            
            // Create ToggleButton - default WinUI3 styling
            var button = new ToggleButton
            {
                Tag = mode,
                CornerRadius = new CornerRadius(4),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Height = 40,
                Content = icon,
            };
            ToolTipService.SetToolTip(button, label);
            button.Click += ModeButton_Click;
            
            // Wrap button in Grid with sunken overlay
            var wrapper = new Grid();
            wrapper.Children.Add(button);
            
            // Create sunken overlay (hidden by default)
            var sunken = CreateSunkenOverlay();
            sunken.Opacity = 0;
            sunken.IsHitTestVisible = false;
            wrapper.Children.Add(sunken);
            _sunkenOverlays[mode] = sunken;
            
            // Create label
            var labelBlock = new TextBlock
            {
                Text = label,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            
            // StackPanel: wrapper, label
            var stack = new StackPanel { Spacing = 4 };
            stack.Children.Add(wrapper);
            stack.Children.Add(labelBlock);
            
            Grid.SetColumn(stack, i);
            ModeButtonsGrid.Children.Add(stack);
            
            _modeButtons[mode] = button;
            _modeLabels[mode] = labelBlock;
        }
    }
    
    /// <summary>
    /// Creates a sunken border effect using 4 edge rectangles.
    /// Dark on top/left (shadow), light on bottom/right (highlight).
    /// Deeper recess = thicker/darker shadows, fainter highlights.
    /// </summary>
    private static UIElement CreateSunkenOverlay()
    {
        var grid = new Grid();
        
        // Top edge - dark shadow (thicker = deeper)
        var top = new Rectangle
        {
            Height = 4,
            Fill = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)),
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
        // Left edge - dark shadow (thicker = deeper)
        var left = new Rectangle
        {
            Width = 4,
            Fill = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)),
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        
        // Bottom edge - faint highlight (less light reaches deep floor)
        var bottom = new Rectangle
        {
            Height = 1,
            Fill = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)),
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
        // Right edge - faint highlight
        var right = new Rectangle
        {
            Width = 1,
            Fill = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)),
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        
        grid.Children.Add(top);
        grid.Children.Add(left);
        grid.Children.Add(bottom);
        grid.Children.Add(right);
        
        return grid;
    }
    
    private static Viewbox CreateSvgInViewbox(string uri)
    {
        var image = new Image
        {
            Source = new SvgImageSource(new Uri(uri))
        };
        var viewbox = new Viewbox
        {
            Width = 20,
            Height = 20,
            Child = image
        };
        return viewbox;
    }
    
    private static Image CreatePngIcon(string uri)
    {
        var image = new Image
        {
            Width = 20,
            Height = 20,
            Source = new BitmapImage(new Uri(uri))
        };
        return image;
    }
    
    private void ModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingUI) return;
        
        if (sender is ToggleButton button && button.Tag is DaikinMode mode)
        {
            // Always select this mode (radio behavior - can't uncheck)
            _viewModel.StagedMode = mode;
            
            // Force this button to stay checked (in case it was a toggle-off click)
            _isUpdatingUI = true;
            button.IsChecked = true;
            _isUpdatingUI = false;
        }
    }
    
    #endregion
    
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
        // Mode buttons - use dictionary
        double modeOpacity = isPowerOn ? 1.0 : 0.4;
        foreach (var mode in _modeButtons.Keys)
        {
            _modeButtons[mode].IsEnabled = isPowerOn;
            _modeButtons[mode].Opacity = modeOpacity;
            _modeLabels[mode].Opacity = modeOpacity;
        }
        
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
        var selectedMode = _viewModel.StagedMode;
        
        _isUpdatingUI = true;
        try
        {
            var primaryBrush = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
            var mutedBrush = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
            
            foreach (var mode in _modeButtons.Keys)
            {
                bool isSelected = mode == selectedMode;
                _modeButtons[mode].IsChecked = isSelected;
                _modeLabels[mode].Foreground = isSelected ? primaryBrush : mutedBrush;
                _modeLabels[mode].FontWeight = isSelected ? FontWeights.SemiBold : FontWeights.Normal;
                
                // Show/hide sunken overlay
                if (_sunkenOverlays.TryGetValue(mode, out var overlay))
                {
                    overlay.Opacity = isSelected ? 1.0 : 0.0;
                }
            }
            
            // Update temperature slider enabled state based on mode
            bool canSetTemp = _viewModel.StagedPower && selectedMode != DaikinMode.Fan;
            TempDial.IsEnabled = canSetTemp;
            TempDial.Opacity = canSetTemp ? 1.0 : 0.5;
            
            // Update dial mode for color
            TempDial.Mode = selectedMode switch
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
            
            // WinUI handles visual states (checked appearance) automatically
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
