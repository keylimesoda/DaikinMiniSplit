using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Text;
using DaikinAndroid.Models;
using DaikinAndroid.ViewModels;
using DaikinAndroid.Controls;
using Windows.UI;

namespace DaikinAndroid.Views;

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
        var modes = new (DaikinMode mode, string label, UIElement icon)[]
        {
            (DaikinMode.Cool, "Cool", new TextBlock { Text = "❄", FontSize = 24, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, -2, 0, 0) }),
            (DaikinMode.Heat, "Heat", new FontIcon { Glyph = "\uE706", FontSize = 20 }),
            (DaikinMode.Auto, "Auto", new FontIcon { Glyph = "\uE895", FontSize = 20 }),
            (DaikinMode.Dry, "Dry", new FontIcon { Glyph = "\uEB42", FontSize = 20 }),
            (DaikinMode.Fan, "Fan", CreatePngIcon("ms-appx:///Assets/fan.png")),
        };
        
        for (int i = 0; i < modes.Length; i++)
        {
            var (mode, label, icon) = modes[i];
            
            // Create ToggleButton
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
    /// </summary>
    private static UIElement CreateSunkenOverlay()
    {
        var grid = new Grid();
        
        var top = new Rectangle
        {
            Height = 4,
            Fill = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)),
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
        var left = new Rectangle
        {
            Width = 4,
            Fill = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)),
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        
        var bottom = new Rectangle
        {
            Height = 1,
            Fill = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)),
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
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
        
        ErrorMessageText.Text = string.IsNullOrEmpty(_viewModel.ErrorMessage) 
            ? "Unable to connect to the Daikin unit" 
            : _viewModel.ErrorMessage;
    }
    
    private void ShowControls()
    {
        LoadingPanel.Visibility = Visibility.Collapsed;
        ErrorPanel.Visibility = Visibility.Collapsed;
        ControlsPanel.Visibility = Visibility.Visible;
        
        SyncUIFromViewModel();
    }
    
    private async void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.RetryConnectionAsync();
    }
    
    #endregion
    
    private void OnDialTemperatureChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (_isUpdatingUI) return;
        
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        
        _debounceTimer = new System.Timers.Timer(200);
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
                    UpdateFanComboUI();
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
        double modeOpacity = isPowerOn ? 1.0 : 0.4;
        foreach (var mode in _modeButtons.Keys)
        {
            _modeButtons[mode].IsEnabled = isPowerOn;
            _modeButtons[mode].Opacity = modeOpacity;
            _modeLabels[mode].Opacity = modeOpacity;
        }
        
        bool canSetTemp = isPowerOn && _viewModel.StagedMode != DaikinMode.Fan;
        TempDial.IsEnabled = canSetTemp;
        TempDial.Opacity = canSetTemp ? 1.0 : 0.4;
        
        FanSpeedSlider.IsEnabled = isPowerOn;
        FanSpeedPanel.Opacity = isPowerOn ? 1.0 : 0.4;
        
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
                
                if (_sunkenOverlays.TryGetValue(mode, out var overlay))
                {
                    overlay.Opacity = isSelected ? 1.0 : 0.0;
                }
            }
            
            bool canSetTemp = _viewModel.StagedPower && selectedMode != DaikinMode.Fan;
            TempDial.IsEnabled = canSetTemp;
            TempDial.Opacity = canSetTemp ? 1.0 : 0.5;
            
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
    private static readonly FanSpeed[] SliderToFanSpeed = { FanSpeed.Quiet, FanSpeed.Low, FanSpeed.Medium, FanSpeed.High };
    private static readonly int[] FanSpeedToSlider = { 2, 0, 1, 2, 3 };
    private int _lastManualSliderPos = 2;
    
    private void UpdateFanComboUI()
    {
        _isUpdatingUI = true;
        try
        {
            bool isAuto = _viewModel.StagedFanSpeed == FanSpeed.Auto;
            FanAutoToggle.IsOn = isAuto;
            
            bool isFanOnlyMode = _viewModel.StagedMode == DaikinMode.Fan;
            FanAutoToggle.Visibility = isFanOnlyMode ? Visibility.Collapsed : Visibility.Visible;
            AutoToggleLabel.Visibility = isFanOnlyMode ? Visibility.Collapsed : Visibility.Visible;
            
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
        var autoBrush = new SolidColorBrush(Color.FromArgb(255, 60, 179, 113));
        var primaryBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
        var mutedBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
        var neutralBarBrush = _fanBarNeutralBrush ?? mutedBrush;
        
        int sliderPos = (int)FanSpeedSlider.Value;
        
        FanSpeedSlider.IsEnabled = !isAuto;
        FanBarsGrid.Opacity = isAuto ? 0.5 : 1.0;
        FanLabelsGrid.Opacity = isAuto ? 0.5 : 1.0;
        
        FanSummaryText.Text = isAuto ? "Fan: Auto" : $"Fan: {SliderPosToLabel(sliderPos)}";

        AutoToggleLabel.Foreground = mutedBrush;
        AutoToggleLabel.FontWeight = FontWeights.Normal;
        
        if (isAuto)
        {
            FanBar0.Background = autoBrush;
            FanBar1.Background = autoBrush;
            FanBar2.Background = autoBrush;
            FanBar3.Background = autoBrush;
            FanBar0.Opacity = 1;
            FanBar1.Opacity = 1;
            FanBar2.Opacity = 1;
            FanBar3.Opacity = 1;
            
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
            FanBar0.Background = neutralBarBrush;
            FanBar1.Background = neutralBarBrush;
            FanBar2.Background = neutralBarBrush;
            FanBar3.Background = neutralBarBrush;
            
            FanBar0.Opacity = sliderPos >= 0 ? 1 : 0;
            FanBar1.Opacity = sliderPos >= 1 ? 1 : 0;
            FanBar2.Opacity = sliderPos >= 2 ? 1 : 0;
            FanBar3.Opacity = sliderPos >= 3 ? 1 : 0;
            
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
        UpdateFanSpeedVisuals(false);
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

            var whiteBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
            var greyBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorDisabledBrush"];
            
            SwingStoppedIcon.Foreground = swing == SwingMode.Off ? whiteBrush : greyBrush;
            SwingVerticalIcon1.Foreground = swing == SwingMode.Vertical ? whiteBrush : greyBrush;
            SwingVerticalIcon2.Foreground = swing == SwingMode.Vertical ? whiteBrush : greyBrush;
            SwingHorizontalIcon.Foreground = swing == SwingMode.Horizontal ? whiteBrush : greyBrush;
            SwingBothIcon.Foreground = swing == SwingMode.Both ? whiteBrush : greyBrush;
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
                _viewModel.StagedSwingMode = swing;
            }
        }
    }

    private void SwingButton_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingUI) return;

        if (sender is ToggleButton btn && btn.Tag is string tagStr && Enum.TryParse<SwingMode>(tagStr, out var swing))
        {
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
