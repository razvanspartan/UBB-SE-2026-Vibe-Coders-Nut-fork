// <copyright file="FocusModeView.xaml.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Microsoft.UI.Xaml.Controls;
using VibeCoders.ViewModels;

namespace VibeCoders.Views;

public sealed partial class FocusModeView : Page
{
    private readonly ContentDialog hostDialog;

    public ActiveWorkoutViewModel ViewModel { get; }

    public FocusModeView(ActiveWorkoutViewModel viewModel, ContentDialog hostDialog)
    {
        this.InitializeComponent();
        this.ViewModel = viewModel;
        this.DataContext = viewModel;
        this.hostDialog = hostDialog;
    }

    private void ExitButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs eventArgs)
    {
        this.hostDialog.Hide();
    }
}
