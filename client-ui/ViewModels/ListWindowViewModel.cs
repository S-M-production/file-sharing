using ReactiveUI;
using System;
using System.Reactive;
using System.Collections.ObjectModel;
using System.Linq;
using network_core.core;
using format.core;
using System.Threading.Tasks;
using client_core.logic;

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
            _activeConnection.AddTask(new ProtocolMessage(MessageType.Disconnect));
            _activeConnection.CompleteQueue();
            await _activeConnection.CompleteCallBack();
            _window.Exit();
        });
    }
    
    /// <summary>
    /// Checks for the row containing ip:port and removes it
    /// </summary>
    /// <param name="ip">the ip, ipv4 . . . .</param>
    /// <param name="port">port yk</param>
    /// <returns>returns a bool if it was found and removed, if it wasn't found or null it isnt removed</returns>
    public bool RemoveEntry(string ip, int port)
    {
        LookUp(ip,port,out var remotePeer);
        if (remotePeer == null) return false;
        RemotePeers.Remove(remotePeer);
        return true;
    }

    /// <summary>
    /// O(n) look up, should re-write using a hashset later....
    /// TODO: ReWrite using a hashset in the far distant future
    /// </summary>
    /// <param name="ip">the ip, ipv4 . . . .</param>
    /// <param name="port">port yk</param>
    /// <param name="row">returns the row that it could find</param>
    /// <returns>Tells you if it could find the row or not</returns>
    public bool LookUp(string ip, int port, out Row? row)
    {
        row = RemotePeers.FirstOrDefault(row => row.Ip == ip && row.Port == port);
        if (row == null) return false;
        return true;
    }
    
    /// <summary>
    /// Just a wrapper for RemotePeers.Add
    /// </summary>
    /// <param name="ip">the ip, ipv4 . . . .</param>
    /// <param name="port">port yk</param>
    public void AddEntry(String ip, int port)
    {
        RemotePeers.Add(new Row(ip, port, _activeConnection,this));
    }

    /// <summary>
    /// Sets the whole display list to something u got
    /// </summary>
    /// <param name="entries">THe string containing IP:PORT per element</param>
    public void SetList(string[] entries)
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

            RemotePeers.Add(new Row(parts[0], port, _activeConnection,this));
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
    /// <summary>
    /// The ip (IPv4 . . . .) that belongs to the user the row belongs to
    /// </summary>
    public string Ip { get; }

    /// <summary>
    /// The port that belongs to the user the row belongs to
    /// </summary>
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
            activeConnection.AddTask(new ProtocolMessage(MessageType.ConnectToUser, System.Text.Encoding.UTF8.GetBytes(temp)));
            _ = _parent.ConnectionRequest();
        });
    }
}