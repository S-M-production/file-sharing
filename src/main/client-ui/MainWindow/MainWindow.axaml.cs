using System;
using System.Text;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using client_core.logic;
using client_ui.ViewModels;
using format.core;
using router_core.core;

namespace client_ui.MainWindow;

/// <summary>
/// Represents the initial client window used to establish a connection to the server and open the peer list.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> window.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    /// <summary>
    /// Attempts to connect to the configured server and opens the list window when the user list is received.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private async void Connect_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var viewModel = DataContext as MainWindowViewModel; // var here!!!!

            if (viewModel == null)
                return;

            var success = await viewModel.OnButtonPressed();

            if (!success)
                return;

            var connection = viewModel.ActiveConnection!;

            // Create callback first
            UserListCallBack callBack = new UserListCallBack();

            // Register route before sending request
            connection.RouterMap.AddRoute(
                MessageType.UserList,
                callBack.UserListCall,
                1,
                true);

            // Send request
            connection.Writer.AddTask(
                new ProtocolMessage(MessageType.RequestUserList));

            // Wait for response
            var awaitingList = await callBack._awaitingMessage.Task;

            var text = Encoding.UTF8.GetString(awaitingList.Body);

            var textList = JsonSerializer.Deserialize<String[]>(text);

            var listWindow = new ListWindow.ListWindow
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                Position = this.Position
            };
            var listWindowViewModel = new ListWindowViewModel(connection, listWindow, viewModel.caller); //error here!!!
            listWindowViewModel.SetList(textList!);

            listWindow.DataContext = listWindowViewModel;
            listWindow.Show();
            Close();
            MessageHandler AddElement = (message) =>
            {
                var temp = System.Text.Encoding.UTF8.GetString(message.Body);
                var temp2 = temp.Split(":");
                Dispatcher.UIThread.Post(() =>
                {
                    listWindowViewModel.AddEntry(temp2[0], int.Parse(temp2[1]));
                });
                return null;
            };
            MessageHandler RemoveElement = (message) =>
            {
                var temp = System.Text.Encoding.UTF8.GetString(message.Body);
                var temp2 = temp.Split(":");
                Dispatcher.UIThread.Post(() =>
                {
                    listWindowViewModel.RemoveEntry(temp2[0], int.Parse(temp2[1]));
                });

                return null;
            };
            viewModel.ActiveConnection!.RouterMap.AddRoute(MessageType.AddUserToList, AddElement);
            viewModel.ActiveConnection.RouterMap.AddRoute(MessageType.RemoveUserFromList, RemoveElement);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Connect_Click exception: " + ex);
        }
    }

    /// <summary>
    /// Starts dragging the window when the user presses the title bar.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The pointer event data.</param>
    private void OnDragWindow(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    /// <summary>
    /// Closes the connection window when the close button is clicked.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}