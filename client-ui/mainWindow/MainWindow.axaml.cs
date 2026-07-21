using Avalonia;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using client_ui.ViewModels;
using System.Security.Cryptography.X509Certificates;
using client_core.core;
using client_core.router.logic;
using format.core;

namespace client_ui;

public partial class MainWindow : Window
{
    public Connection? serverConnection { get; set; } = null;

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
            var success = await viewModel!.OnButtonPressed();

            if (success)
            {
                await Dispatcher.UIThread.InvokeAsync( () =>
                {
                    UserListCallBack callBack = new UserListCallBack();
                    serverConnection.RouterMap.AddRoute(MessageType.UserList,callBack.UserListCall,1,true);
                    var listWindowViewModel = new ListWindowViewModel();
                    var awaitingList = callBack._awaitingMessage.Task.Result;
                    var text = System.Text.Encoding.UTF8.GetString(awaitingList.Body);
                    var textList = text[1..^1].Replace("\"", "").Split(",");
                    listWindowViewModel.RefreshList(textList);
                    // listWindowViewModel.RefreshList(new[]
                    // {
                    //     "192.168.1.10:8000",
                    //     "10.0.0.5:9000",
                    //     "127.0.0.1:1234",
                    //     "192.168.1.10:8000",
                    //     "10.0.0.5:9000",
                    //     "127.0.0.1:1234",
                    //     "192.168.1.10:8000",
                    //     "10.0.0.5:9000",
                    //     "127.0.0.1:1234"
                    // });

                    var listWindow = new ListWindow();
                    listWindow.DataContext = listWindowViewModel;

                    listWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                    listWindow.Position = this.Position;

                    listWindow.Show();
                    this.Close();
                });
            }
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