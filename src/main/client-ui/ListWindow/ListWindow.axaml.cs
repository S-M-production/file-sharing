using Avalonia.Controls;
using Avalonia.Input;

namespace client_ui.ListWindow;

/// <summary>
/// Displays the list of active peer connections available to the user.
/// </summary>
public partial class ListWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListWindow"/> class.
    /// </summary>
    public ListWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Begins dragging the window when the title bar is pressed.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The pointer event data.</param>
    private void OnDragWindow(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    /// <summary>
    /// Closes the peer list window.
    /// </summary>
    public void Exit()
    {
        Close();
    }
}