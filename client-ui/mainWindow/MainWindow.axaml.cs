using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using client_core.router.logic;
using client_ui.ViewModels;
using format.core;
using router_core.core;

namespace client_ui.mainWindow;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    private async void Connect_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var viewModel = DataContext as MainWindowViewModel;

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
            connection.AddTask(
                new ProtocolMessage(MessageType.RequestUserList));

            // Wait for response
            var awaitingList = await callBack._awaitingMessage.Task;

            var text = Encoding.UTF8.GetString(awaitingList.Body);
            
            var textList = JsonSerializer.Deserialize<String[]>(text);

            var listWindow = new listWindow.ListWindow
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                Position = this.Position
            };
            var listWindowViewModel = new ListWindowViewModel(connection, listWindow);
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
                listWindowViewModel.AddEntry(temp2[0],int.Parse(temp2[1]));
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

    private void OnDragWindow(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}