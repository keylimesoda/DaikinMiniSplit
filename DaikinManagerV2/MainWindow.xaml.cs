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

namespace DaikinManagerV2;

public sealed partial class MainWindow : Window
{
    private MainViewModel? _viewModel;

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
        else if (e.PropertyName == nameof(MainViewModel.IsRefreshing))
        {
            UpdateRefreshIndicator();
        }
        else if (e.PropertyName == nameof(MainViewModel.CountdownProgress))
        {
            UpdateRefreshIndicator();
        }
    }

    private void UpdateStatusBadge()
    {
        if (_viewModel == null) return;
        
        DispatcherQueue.TryEnqueue(() =>
        {
            switch (_viewModel.ConnectionState)
            {
                case ConnectionState.Disconnected:
                    StatusText.Text = "Not Connected";
                    StatusBadge.Background = new SolidColorBrush(Colors.Gray);
                    break;
                case ConnectionState.Connecting:
                    StatusText.Text = "Connecting...";
                    StatusBadge.Background = new SolidColorBrush(Colors.DodgerBlue);
                    break;
                case ConnectionState.Connected:
                    StatusText.Text = "Connected";
                    StatusBadge.Background = new SolidColorBrush(Colors.ForestGreen);
                    break;
                case ConnectionState.Error:
                    StatusText.Text = "Connection Lost";
                    StatusBadge.Background = new SolidColorBrush(Colors.Crimson);
                    break;
            }
        });
    }

    private void UpdateRefreshIndicator()
    {
        if (_viewModel == null) return;
        
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_viewModel.ConnectionState != ConnectionState.Connected)
            {
                RefreshIndicator.Visibility = Visibility.Collapsed;
                return;
            }
            
            RefreshIndicator.Visibility = Visibility.Visible;
            
            if (_viewModel.IsRefreshing)
            {
                RefreshIndicator.IsIndeterminate = true;
            }
            else if (_viewModel.RefreshFailed)
            {
                RefreshIndicator.IsIndeterminate = false;
                RefreshIndicator.Value = 0;
                // Could add error styling here
            }
            else
            {
                RefreshIndicator.IsIndeterminate = false;
                RefreshIndicator.Value = _viewModel.CountdownProgress * 100;
            }
        });
    }
}
