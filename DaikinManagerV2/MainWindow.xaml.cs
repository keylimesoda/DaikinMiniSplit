using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using DaikinManagerV2.Models;
using DaikinManagerV2.ViewModels;
using DaikinManagerV2.Views;
using Windows.Graphics;
using Windows.UI;

namespace DaikinManagerV2;

public sealed partial class MainWindow : Window
{
    private MainViewModel? _viewModel;
    private ControlsViewModel? _controlsViewModel;

    public MainWindow()
    {
        this.InitializeComponent();
        
        // Set window title explicitly  
        this.Title = "Daikin Manager V2";
        
        // Hide the white Windows title bar by extending content into it
        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(AppTitleBar);
        
        // Defer heavy initialization to after the window is created
        this.Activated += MainWindow_Activated;
    }
    
    private bool _initialized = false;
    
    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_initialized) return;
        _initialized = true;
        
        try
        {
            // Set window size
            var appWindow = GetAppWindowForCurrentWindow();
            appWindow.Resize(new SizeInt32(450, 880));
            
            // Get MainViewModel from DI
            _viewModel = App.Services.GetRequiredService<MainViewModel>();
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            
            // Get ControlsViewModel for power button
            _controlsViewModel = App.Services.GetRequiredService<ControlsViewModel>();
            _controlsViewModel.PropertyChanged += ControlsViewModel_PropertyChanged;
            UpdatePowerButtonUI();
            
            // Select the first navigation item (Controls)
            NavView.SelectedItem = NavView.MenuItems[0];
            
            // Start connection
            _ = _viewModel.ConnectAsync();
        }
        catch (Exception ex)
        {
            // Show error in a simple TextBlock if initialization fails
            var errorText = new TextBlock
            {
                Text = $"Initialization Error:\n{ex.Message}\n\n{ex.StackTrace}",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20),
                IsTextSelectionEnabled = true
            };
            this.Content = new ScrollViewer { Content = errorText };
        }
    }
    
    private AppWindow GetAppWindowForCurrentWindow()
    {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            if (tag == "Controls")
            {
                NavigateToControls();
            }
            else if (tag == "Schedule")
            {
                NavigateToSchedule();
            }
            else if (tag == "Diagnostics")
            {
                NavigateToDiagnostics();
            }
        }
    }
    
    private void NavigateToControls()
    {
        ContentFrame.Navigate(typeof(ControlsPage), null);
        if (ContentFrame.Content is ControlsPage page)
        {
            page.Initialize(App.Services.GetRequiredService<ControlsViewModel>());
        }
    }
    
    private void NavigateToSchedule()
    {
        ContentFrame.Navigate(typeof(SchedulePage), null);
        if (ContentFrame.Content is SchedulePage page)
        {
            page.Initialize(App.Services.GetRequiredService<ScheduleViewModel>());
        }
    }
    
    private void NavigateToDiagnostics()
    {
        ContentFrame.Navigate(typeof(DiagnosticsPage), null);
        if (ContentFrame.Content is DiagnosticsPage page)
        {
            page.Initialize(App.Services.GetRequiredService<DiagnosticsViewModel>());
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.ConnectionState))
        {
            UpdateStatusBadge();
        }
    }

    private void UpdateStatusBadge()
    {
        if (_viewModel == null) return;
        
        DispatcherQueue.TryEnqueue(() =>
        {
            // Hide all status indicators first
            StatusDot.Visibility = Visibility.Collapsed;
            ErrorIcon.Visibility = Visibility.Collapsed;
            
            switch (_viewModel.ConnectionState)
            {
                case ConnectionState.Disconnected:
                    // Show muted gray dot
                    StatusDot.Fill = new SolidColorBrush(Colors.Gray);
                    StatusDot.Visibility = Visibility.Visible;
                    break;
                case ConnectionState.Connecting:
                    // Spinner is now shown in DiagnosticsPage
                    break;
                case ConnectionState.Connected:
                    // Hide dot entirely - the functional UI is proof of connection
                    // This avoids color collision with the green power button
                    break;
                case ConnectionState.Error:
                    // Show warning icon (error states get maximum visibility)
                    ErrorIcon.Visibility = Visibility.Visible;
                    break;
            }
        });
    }

    private void ControlsViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ControlsViewModel.StagedPower))
        {
            UpdatePowerButtonUI();
        }
    }

    private async void PowerButton_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_controlsViewModel != null && !_controlsViewModel.IsTogglingPower)
        {
            bool newPowerState = !_controlsViewModel.StagedPower;
            await _controlsViewModel.SetPowerAsync(newPowerState);
        }
    }

    private void UpdatePowerButtonUI(bool isHovering = false)
    {
        if (_controlsViewModel == null) return;

        DispatcherQueue.TryEnqueue(() =>
        {
            bool isPowerOn = _controlsViewModel.StagedPower;
            
            if (isPowerOn)
            {
                if (isHovering)
                {
                    // ON + Hover: Show button surface to indicate clickable area
                    PowerButtonBackground.Background = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)); // Dark surface
                    PowerIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 76, 180, 80)); // Keep green icon
                }
                else
                {
                    // ON, no hover: Transparent background, just green icon
                    PowerButtonBackground.Background = new SolidColorBrush(Colors.Transparent);
                    PowerIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 76, 180, 80)); // Bright green
                }
            }
            else
            {
                if (isHovering)
                {
                    // OFF + Hover: Very faint green radial gradient to hint at ON state
                    var gradientBrush = new Microsoft.UI.Xaml.Media.RadialGradientBrush();
                    gradientBrush.GradientOrigin = new Windows.Foundation.Point(0.5, 0.5);
                    gradientBrush.Center = new Windows.Foundation.Point(0.5, 0.5);
                    gradientBrush.RadiusX = 0.6;
                    gradientBrush.RadiusY = 0.6;
                    gradientBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(60, 76, 140, 80), Offset = 0 }); // Faint green center
                    gradientBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(30, 76, 140, 80), Offset = 0.7 }); // Fade out
                    gradientBrush.GradientStops.Add(new GradientStop { Color = Colors.Transparent, Offset = 1.0 }); // Transparent edge
                    PowerButtonBackground.Background = gradientBrush;
                    PowerIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 140, 140, 140)); // Slightly brighter gray
                }
                else
                {
                    // OFF, no hover: Transparent background, just gray icon
                    PowerButtonBackground.Background = new SolidColorBrush(Colors.Transparent);
                    PowerIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 120, 120, 120)); // Muted gray
                }
            }
        });
    }

    private void PowerButton_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        UpdatePowerButtonUI(isHovering: true);
    }

    private void PowerButton_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        UpdatePowerButtonUI(isHovering: false);
    }
}
