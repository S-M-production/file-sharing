using Avalonia;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using client_ui.ViewModels;

namespace client_ui;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    private async void Connect_Click(object? sender, RoutedEventArgs e)
    {
        var viewModel = DataContext as MainWindowViewModel;
        var success = await viewModel!.OnButtonPressed();

        if (success)
        {
            var listWindow = new ListWindow();

            listWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            listWindow.Position = this.Position;

            listWindow.Show();
            this.Close();
        }
    }
    private void OnDragWindow(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }
    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}