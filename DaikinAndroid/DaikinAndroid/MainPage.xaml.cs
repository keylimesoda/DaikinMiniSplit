using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using DaikinAndroid.Models;
using DaikinAndroid.ViewModels;
using DaikinAndroid.Views;
using Windows.UI;

namespace DaikinAndroid;

public sealed partial class MainPage : Page
{
    private MainViewModel? _viewModel;
    private ControlsViewModel? _controlsViewModel;
    private bool _initialized;

    public MainPage()
    {
        this.InitializeComponent();
        this.Loaded += MainPage_Loaded;
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized) return;
        _initialized = true;

#if WINDOWS
        SetupWindowsTitleBar();
#else
        AppTitleBar.Visibility = Visibility.Collapsed;
#endif

        try
        {
            _viewModel = App.Services.GetRequiredService<MainViewModel>();
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            _controlsViewModel = App.Services.GetRequiredService<ControlsViewModel>();
            _controlsViewModel.PropertyChanged += ControlsViewModel_PropertyChanged;
            UpdatePowerButtonUI();

            NavView.SelectedItem = NavView.MenuItems[0];

            _ = _viewModel.ConnectAsync();
        }
        catch (Exception ex)
        {
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

#if WINDOWS
    private void SetupWindowsTitleBar()
    {
        if (App.MainAppWindow is { } window)
        {
            window.SetTitleBar(AppTitleBar);
        }
    }
#endif

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
            StatusDot.Visibility = Visibility.Collapsed;
            ErrorIcon.Visibility = Visibility.Collapsed;

            switch (_viewModel.ConnectionState)
            {
                case ConnectionState.Disconnected:
                    StatusDot.Fill = new SolidColorBrush(Colors.Gray);
                    StatusDot.Visibility = Visibility.Visible;
                    break;
                case ConnectionState.Connecting:
                    break;
                case ConnectionState.Connected:
                    break;
                case ConnectionState.Error:
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
                    PowerButtonBackground.Background = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60));
                    PowerIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 76, 180, 80));
                }
                else
                {
                    PowerButtonBackground.Background = new SolidColorBrush(Colors.Transparent);
                    PowerIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 76, 180, 80));
                }
            }
            else
            {
                if (isHovering)
                {
                    var gradientBrush = new Microsoft.UI.Xaml.Media.RadialGradientBrush();
#if WINDOWS
                    gradientBrush.GradientOrigin = new Windows.Foundation.Point(0.5, 0.5);
#endif
                    gradientBrush.Center = new Windows.Foundation.Point(0.5, 0.5);
                    gradientBrush.RadiusX = 0.6;
                    gradientBrush.RadiusY = 0.6;
                    gradientBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(60, 76, 140, 80), Offset = 0 });
                    gradientBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(30, 76, 140, 80), Offset = 0.7 });
                    gradientBrush.GradientStops.Add(new GradientStop { Color = Colors.Transparent, Offset = 1.0 });
                    PowerButtonBackground.Background = gradientBrush;
                    PowerIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 140, 140, 140));
                }
                else
                {
                    PowerButtonBackground.Background = new SolidColorBrush(Colors.Transparent);
                    PowerIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 120, 120, 120));
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
