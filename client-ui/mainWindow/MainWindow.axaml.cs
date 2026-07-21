using Avalonia;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
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
        try
        {
            var viewModel = DataContext as MainWindowViewModel;
            var success = await viewModel!.OnButtonPressed();

            if (success)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    UserListCallBack callBack = new UserListCallBack();
                    serverConnection.RouterMap.AddRoute(MessageType.UserList,callBack.UserListCall,1,true);
                    var listWindowViewModel = new ListWindowViewModel();
                    var awaitingList = callBack._awaitingMessage.Task.Result;
                    var text = System.Text.Encoding.UTF8.GetString(awaitingList.Body);
                    var textList = text[1..^1].Replace("\"", "").Split(",");
                    listWindowViewModel.RefreshList(textList);
 

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