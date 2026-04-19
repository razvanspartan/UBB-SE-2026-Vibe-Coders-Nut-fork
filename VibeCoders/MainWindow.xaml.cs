namespace VibeCoders;

using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VibeCoders.Services;
using WinRT.Interop;

public sealed partial class MainWindow : Window
{
    private readonly NavigationService navigationService;
    private readonly IAchievementUnlockedBus achievementBus;

    public MainWindow(NavigationService navigationService, IAchievementUnlockedBus achievementBus)
    {
        this.InitializeComponent();
        this.navigationService = navigationService;
        this.achievementBus = achievementBus;
        this.achievementBus.AchievementUnlocked += this.OnAchievementUnlocked;
        this.navigationService.AttachFrame(this.ContentFrame);

        var placementDone = false;
        this.Activated += (_, _) =>
        {
            if (placementDone)
            {
                return;
            }

            placementDone = true;
            this.ApplyInitialPlacement();
        };
    }

    private void ApplyInitialPlacement()
    {
        var windowHandle = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
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

    private async void OnAchievementUnlocked(object? sender, AchievementUnlockedEventArgs eventArgs)
    {
        var achievement = eventArgs.Achievement;

        var body = new StackPanel { Spacing = 8 };
        body.Children.Add(new TextBlock
        {
            Text = achievement.Title,
            Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"],
        });
        body.Children.Add(new TextBlock
        {
            Text = achievement.Description,
            Style = (Style)Application.Current.Resources["BodyTextBlockStyle"],
            TextWrapping = TextWrapping.Wrap,
        });
        body.Children.Add(new TextBlock
        {
            Text = $"Criteria: {achievement.Criteria}",
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            TextWrapping = TextWrapping.Wrap,
        });

        var dialog = new ContentDialog
        {
            Title = "Achievement Unlocked",
            Content = body,
            CloseButtonText = "Awesome!",
            XamlRoot = this.Content.XamlRoot,
        };

        try
        {
            await dialog.ShowAsync();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Achievement dialog error: {exception.Message}");
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();

            if (tag == "Dashboard")
            {
                this.navigationService.NavigateToClientDashboard(requestRefresh: true);
            }
            else if (tag == "WorkoutLogs")
            {
                this.navigationService.NavigateToWorkoutLogs();
            }
            else if (tag == "Calendar")
            {
                this.navigationService.NavigateToCalendarIntegration();
            }
            else if (tag == "Rank")
            {
                this.navigationService.NavigateToRankShowcase();
            }
            else if (tag == "TrainerDashboard")
            {
                this.navigationService.NavigateToTrainerDashboard();
            }
        }
    }
}