using Avalonia;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Text;
using client_ui.ViewModels;
using client_core.router.logic;
using format.core;

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
        try
        {
            var viewModel = DataContext as MainWindowViewModel;

            if (viewModel == null)
                return;

            var success = await viewModel.OnButtonPressed();

            if (!success)
                return;

            var connection = viewModel.ActiveConnection!;

            Console.WriteLine("Before all");

            // Create callback first
            UserListCallBack callBack = new UserListCallBack();

            // Register route before sending request
            connection.RouterMap.AddRoute(
                MessageType.UserList,
                callBack.UserListCall,
                1,
                true);

            // Send request
            await connection.AddTask(
                new ProtocolMessage(MessageType.RequestUserList));

            // Wait for response
            var awaitingList = await callBack._awaitingMessage.Task;

            Console.WriteLine("After all");

            var text = Encoding.UTF8.GetString(awaitingList.Body);

            // Assuming response is:
            // ["ip1","ip2"]
            var textList = text[1..^1]
                .Replace("\"", "")
                .Split(",");

            var listWindowViewModel = new ListWindowViewModel(connection);
            listWindowViewModel.RefreshList(textList);

            var listWindow = new ListWindow
            {
                DataContext = listWindowViewModel,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Position = this.Position
            };

            listWindow.Show();

            this.Close();
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