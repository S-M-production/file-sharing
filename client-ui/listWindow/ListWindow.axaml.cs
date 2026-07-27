using Avalonia.Controls;
using Avalonia.Input;

namespace client_ui.listWindow;

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
    public void Exit()
    {
        this.Close();
    }
}