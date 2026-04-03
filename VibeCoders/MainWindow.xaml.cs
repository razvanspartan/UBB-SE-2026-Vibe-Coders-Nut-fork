using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VibeCoders.Services;
using WinRT.Interop;

namespace VibeCoders;

public sealed partial class MainWindow : Window
{
    private readonly NavigationService _navigationService;
    private readonly IAchievementUnlockedBus _achievementBus;

    public MainWindow(NavigationService navigationService, IAchievementUnlockedBus achievementBus)
    {
        InitializeComponent();
        _navigationService = navigationService;
        _achievementBus = achievementBus;
        _achievementBus.AchievementUnlocked += OnAchievementUnlocked;
        _navigationService.AttachFrame(ContentFrame);

        var placementDone = false;
        Activated += (_, _) =>
        {
            if (placementDone) return;
            placementDone = true;
            ApplyInitialPlacement();
        };
    }

    private void ApplyInitialPlacement()
    {
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        const int width = 1280;
        const int height = 800;
        appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = width, Height = height });

        var display = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        var work = display.WorkArea;
        int x = work.X + Math.Max(0, (work.Width - width) / 2);
        int y = work.Y + Math.Max(0, (work.Height - height) / 2);
        appWindow.Move(new Windows.Graphics.PointInt32 { X = x, Y = y });
    }

    private async void OnAchievementUnlocked(object? sender, AchievementUnlockedEventArgs e)
    {
        var achievement = e.Achievement;

        var body = new StackPanel { Spacing = 8 };
        body.Children.Add(new TextBlock
        {
            Text = achievement.Title,
            Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"]
        });
        body.Children.Add(new TextBlock
        {
            Text = achievement.Description,
            Style = (Style)Application.Current.Resources["BodyTextBlockStyle"],
            TextWrapping = TextWrapping.Wrap
        });
        body.Children.Add(new TextBlock
        {
            Text = $"Criteria: {achievement.Criteria}",
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            TextWrapping = TextWrapping.Wrap
        });

        var dialog = new ContentDialog
        {
            Title = "Achievement Unlocked",
            Content = body,
            CloseButtonText = "Awesome!",
            XamlRoot = Content.XamlRoot
        };

        try
        {
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Achievement dialog error: {ex.Message}");
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();

            if (tag == "Dashboard")
            {
                _navigationService.NavigateToClientDashboard(requestRefresh: true);
            }
            else if (tag == "WorkoutLogs")
            {
                _navigationService.NavigateToWorkoutLogs();
            }
            else if (tag == "Calendar")
            {
                _navigationService.NavigateToCalendarIntegration();
            }
            else if (tag == "Rank")
            {
                _navigationService.NavigateToRankShowcase();
            }

            else if (tag == "TrainerDashboard")
            {
                _navigationService.NavigateToTrainerDashboard();
            }

        }
    }
}