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

    private void Connect_Click(object? sender, RoutedEventArgs e)
    {
        (DataContext as MainWindowViewModel)?.OnButtonPressed();
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