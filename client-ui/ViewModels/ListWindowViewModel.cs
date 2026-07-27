using Avalonia;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Collections.ObjectModel;
using client_ui;
using client_core.logic;
using Avalonia.Controls.Primitives;
using network_core.core;
using format.core;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;

namespace client_ui.ViewModels;

public class ListWindowViewModel : ReactiveObject
{   
    private readonly Connection? _activeConnection;
    public UserRequestCallBack _userRequest;
    private readonly listWindow.ListWindow _window;
    public ReactiveCommand<Unit, Unit> RequestLeave { get; }
    public ObservableCollection<Row> RemotePeers { get; } = new();

    private bool _isPopupOpen;

    public bool IsPopupOpen
    {
        get => _isPopupOpen;
        set => this.RaiseAndSetIfChanged(ref _isPopupOpen, value);
    }

    public ListWindowViewModel(Connection? activeConnection, listWindow.ListWindow window, UserRequestCallBack userRequest)
    {
        _activeConnection = activeConnection;
        _window = window;
        _userRequest = userRequest;

        RequestLeave = ReactiveCommand.CreateFromTask(async () =>
        {
            await _activeConnection.AddTask(new ProtocolMessage(MessageType.Disconnect));
            await _activeConnection.CompleteQueue();
            _window.Exit();
        });
    }

    public void RefreshList(IEnumerable<string> entries)
    {
        RemotePeers.Clear();
        foreach (var entry in entries)
        {
            var parts = entry.Split(":");
            if (parts.Length != 2)
            {
                continue;
            }
            if (!int.TryParse(parts[1], out int port))
            {
                continue;
            }

            RemotePeers.Add(new Row(parts[0], port, _activeConnection, this));
        }
    }

    public async Task<ProtocolMessage?> ConnectionRequest()
    {
        Console.WriteLine("before callback");

        ProtocolMessage msg = await _userRequest._awaitingMessage.Task;
        Console.WriteLine("after callback");
        Popup();

        return null;
    }

    private void Popup()
    {
        IsPopupOpen = true;
    } 
}

public class Row : ReactiveObject
{
    private readonly ListWindowViewModel _parent;
    private string _buttonText = "Request Connect";

    public string Ip { get; }
    public int Port { get; }

    public string ButtonText
    {
        get => _buttonText;
        private set => this.RaiseAndSetIfChanged(ref _buttonText, value);
    }


    public ReactiveCommand<Unit, Unit> RequestConnectCommand { get; }
    public Row(string ip, int port, Connection? activeConnection, ListWindowViewModel parent)
    {
        Ip = ip;
        Port = port;
        _parent = parent;

        RequestConnectCommand = ReactiveCommand.Create(() =>
        {
            ButtonText = "Waiting for responce";

            try
            {
                Console.WriteLine($"Requesting connection to {ip}:{port}");
                Console.WriteLine(activeConnection is null
                ? "no active connection aviable"
                : "active connection avaiable");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Row command exception" + ex);
                throw;
            }
            var temp = Ip + ":" + Port.ToString();
            Task con = activeConnection.AddTask(new ProtocolMessage(MessageType.ConnectToUser, System.Text.Encoding.UTF8.GetBytes(temp)));
            con.Wait();
            _ = _parent.ConnectionRequest();
        });
    }
}