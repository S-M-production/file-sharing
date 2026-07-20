using Avalonia;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using client_ui.ViewModels;
using System.Security.Cryptography.X509Certificates;

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
            var listWindowViewModel = new ListWindowViewModel();
            listWindowViewModel.RefreshList(new[]
            {
                "192.168.1.10:8000",
                "10.0.0.5:9000",
                "127.0.0.1:1234"
            });
            
            var listWindow = new ListWindow();
            listWindow.DataContext = listWindowViewModel;

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