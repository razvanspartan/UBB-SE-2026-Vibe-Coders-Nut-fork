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

    public MainWindow(NavigationService navigationService)
    {
        InitializeComponent();
        _navigationService = navigationService;
        _navigationService.AttachFrame(ContentFrame);

        var placementDone = false;
        Activated += (_, _) =>
        {
            if (placementDone)
            {
                return;
            }

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

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            if (tag == "Dashboard")
            {
                _navigationService.NavigateToClientDashboard(requestRefresh: true);
            }
            else if (tag == "Calendar")
            {
                _navigationService.NavigateToCalendarIntegration();
            }
            else if (tag == "Rank")
            {
                _navigationService.NavigateToRankShowcase();
            }
        }
    }
}
