using Avalonia.Controls;
using Avalonia.Input;
using client_ui.ViewModels;

namespace client_ui;

public partial class ListWindow : Window
{
    public ListWindow()
    {
        InitializeComponent();
    }

    private void OnDragWindow(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }
}