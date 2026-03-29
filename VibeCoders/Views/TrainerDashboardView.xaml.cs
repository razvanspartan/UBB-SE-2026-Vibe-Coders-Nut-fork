using Microsoft.UI.Xaml.Controls;
using System;
using VibeCoders.Core.Services;
using VibeCoders.Services;
using VibeCoders.ViewModels;

namespace VibeCoders.Views
{
    public sealed partial class TrainerDashboardView : Page
    {
        public TrainerDashboardViewModel ViewModel { get; }

        public static string FormatWorkoutDate(DateTime Date)
        {

            return Date.ToString("MMM dd, yyyy");
        }

        public TrainerDashboardView()
        {

            var db = new SqlDataStorage();
            var service = new TrainerService(db);

            this.ViewModel = new TrainerDashboardViewModel(service);
            this.InitializeComponent();
        }
    }
}