using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Shapes;
using DaikinAndroid.ViewModels;

namespace DaikinAndroid.Views;

public sealed partial class SchedulePage : Page
{
    private ScheduleViewModel? _viewModel;
    private readonly Dictionary<DayOfWeek, ToggleButton> _dayToggles = new();

    public SchedulePage()
    {
        this.InitializeComponent();
    }

    public void Initialize(ScheduleViewModel viewModel)
    {
        _viewModel = viewModel;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        
        // Initial load
        _ = _viewModel.LoadScheduleCommand.ExecuteAsync(null);
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(UpdateUI);
    }

    private void UpdateUI()
    {
        if (_viewModel == null) return;

        LoadingPanel.Visibility = _viewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
        ErrorPanel.Visibility = _viewModel.HasError ? Visibility.Visible : Visibility.Collapsed;
        ContentPanel.Visibility = _viewModel.IsReady ? Visibility.Visible : Visibility.Collapsed;

        if (_viewModel.HasError)
        {
            ErrorText.Text = _viewModel.ErrorMessage;
        }

        if (_viewModel.IsReady)
        {
            EnableToggle.Toggled -= EnableToggle_Toggled;
            EnableToggle.IsOn = _viewModel.IsSchedulerEnabled;
            EnableToggle.Toggled += EnableToggle_Toggled;

            if (_viewModel.HasNextEvent && _viewModel.NextEvent != null)
            {
                NextUpCard.Visibility = Visibility.Visible;
                NoNextEventCard.Visibility = Visibility.Collapsed;
                
                NextEventTime.Text = _viewModel.NextEvent.TimeText;
                NextEventDescription.Text = _viewModel.NextEvent.Description;
                NextEventCountdown.Text = _viewModel.CountdownText;
                NextEventIcon.Glyph = _viewModel.NextEvent.IconGlyph;
                
                if (_viewModel.NextEvent.Event.PowerOn)
                {
                    NextEventIcon.Foreground = _viewModel.NextEvent.Event.Mode switch
                    {
                        Models.DaikinMode.Cool => new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DeepSkyBlue),
                        Models.DaikinMode.Heat => new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.OrangeRed),
                        _ => new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
                    };
                }
                else
                {
                    NextEventIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
                }
            }
            else
            {
                NextUpCard.Visibility = Visibility.Collapsed;
                NoNextEventCard.Visibility = Visibility.Visible;
            }

            TodayHeader.Text = $"TODAY ({_viewModel.TodayName})";

            if (_viewModel.HasTodayEvents)
            {
                TodayEventsList.ItemsSource = _viewModel.TodayEvents;
                TodayEventsList.Visibility = Visibility.Visible;
                NoEventsToday.Visibility = Visibility.Collapsed;
            }
            else
            {
                TodayEventsList.Visibility = Visibility.Collapsed;
                NoEventsToday.Visibility = Visibility.Visible;
            }

            HomeViewPanel.Visibility = _viewModel.IsWeekViewVisible ? Visibility.Collapsed : Visibility.Visible;
            WeekViewPanel.Visibility = _viewModel.IsWeekViewVisible ? Visibility.Visible : Visibility.Collapsed;

            if (_viewModel.IsWeekViewVisible)
            {
                UpdateWeekView();
            }
        }
    }

    private void UpdateWeekView()
    {
        if (_viewModel == null) return;

        SelectedDayHeader.Text = _viewModel.SelectedDayName;

        if (_dayToggles.Count == 0)
        {
            CreateDayToggles();
        }

        UpdateDayToggleStates();

        if (_viewModel.HasSelectedDayEvents)
        {
            SelectedDayEventsList.ItemsSource = _viewModel.SelectedDayEvents;
            SelectedDayEventsList.Visibility = Visibility.Visible;
            NoEventsSelectedDay.Visibility = Visibility.Collapsed;
        }
        else
        {
            SelectedDayEventsList.Visibility = Visibility.Collapsed;
            NoEventsSelectedDay.Visibility = Visibility.Visible;
        }
    }

    private void CreateDayToggles()
    {
        if (_viewModel == null) return;

        int column = 0;
        foreach (var dayInfo in _viewModel.WeekDays)
        {
            var badge = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorAttentionBrush"],
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, -2, -2, 0),
                Visibility = dayInfo.HasEvents
            };

            var content = new Grid
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = dayInfo.ShortName,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    badge
                }
            };

            var toggle = new ToggleButton
            {
                Content = content,
                Tag = dayInfo,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(4),
                Margin = new Thickness(2),
                CornerRadius = new CornerRadius(4)
            };
            toggle.Click += DayToggle_Click;

            Grid.SetColumn(toggle, column);
            DaySelectorGrid.Children.Add(toggle);
            _dayToggles[dayInfo.Day] = toggle;
            column++;
        }
    }

    private void UpdateDayToggleStates()
    {
        if (_viewModel == null) return;

        foreach (var dayInfo in _viewModel.WeekDays)
        {
            if (_dayToggles.TryGetValue(dayInfo.Day, out var toggle))
            {
                toggle.IsChecked = dayInfo.Day == _viewModel.SelectedDay;
                
                if (toggle.Content is Grid grid && grid.Children.Count > 1 && grid.Children[1] is Ellipse badge)
                {
                    badge.Visibility = dayInfo.HasEvents;
                }
            }
        }
    }

    private void DayToggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton toggle && toggle.Tag is DayButtonInfo dayInfo && _viewModel != null)
        {
            toggle.IsChecked = true;
            
            _viewModel.SelectDayCommand.Execute(dayInfo.Day);
            
            UpdateDayToggleStates();
        }
    }

    private void ViewWeekButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.ShowWeekViewCommand.Execute(null);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.HideWeekViewCommand.Execute(null);
    }

    private void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _ = _viewModel.LoadScheduleCommand.ExecuteAsync(null);
        }
    }

    private void EnableToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null && EnableToggle.IsOn != _viewModel.IsSchedulerEnabled)
        {
            _ = _viewModel.ToggleSchedulerCommand.ExecuteAsync(null);
        }
    }
}
