using Avalonia;
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

    private void Button_Click(object? sender, RoutedEventArgs e)
    {
        (DataContext as MainWindowViewModel)?.OnButtonPressed();
    }
}