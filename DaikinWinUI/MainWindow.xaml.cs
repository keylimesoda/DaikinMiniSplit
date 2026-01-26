using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using DaikinManager.Services;
using DaikinManager.Pages;

namespace DaikinManager;

public sealed partial class MainWindow : Window
{
    private readonly DaikinService _daikinService;
    private readonly DispatcherTimer _refreshTimer;

    public MainWindow()
    {
        this.InitializeComponent();
        
        // Set window size and customize title bar
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new Windows.Graphics.SizeInt32(450, 880));
        
        // Extend content into title bar (removes the white bar)
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        
        _daikinService = new DaikinService("192.168.183.26");
        
        // Setup refresh timer (30 seconds)
        _refreshTimer = new DispatcherTimer();
        _refreshTimer.Interval = TimeSpan.FromSeconds(30);
        _refreshTimer.Tick += async (s, e) => await RefreshDataAsync();
        
        // Navigate to Controls page by default
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            switch (tag)
            {
                case "Controls":
                    ContentFrame.Navigate(typeof(ControlsPage), _daikinService);
                    break;
                case "Diagnostics":
                    ContentFrame.Navigate(typeof(DiagnosticsPage), _daikinService);
                    break;
            }
        }
    }

    public async Task RefreshDataAsync()
    {
        try
        {
            await _daikinService.RefreshAllAsync();
            UpdateStatus(true);
        }
        catch
        {
            UpdateStatus(false);
        }
    }

    private void UpdateStatus(bool connected)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (connected)
            {
                StatusText.Text = "Connected";
                StatusBadge.Background = new SolidColorBrush(Microsoft.UI.Colors.Green);
            }
            else
            {
                StatusText.Text = "Offline";
                StatusBadge.Background = new SolidColorBrush(Microsoft.UI.Colors.Red);
            }
        });
    }

    public void StartAutoRefresh() => _refreshTimer.Start();
    public void StopAutoRefresh() => _refreshTimer.Stop();
    public void ResetAutoRefresh()
    {
        _refreshTimer.Stop();
        _refreshTimer.Start();
    }
    
    public void SetConnected() => UpdateStatus(true);
    public void SetOffline() => UpdateStatus(false);
}
