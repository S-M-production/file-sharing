using ReactiveUI;
using System;
using System.Reactive;
using System.Collections.ObjectModel;
using System.Linq;
using network_core.core;
using format.core;
using System.Threading.Tasks;
using Avalonia.Threading;
using client_core.logic;

namespace client_ui.ViewModels;

/// <summary>
/// Provides the peer list view model used by the client UI.
/// </summary>
public class ListWindowViewModel : ReactiveObject
{
    private readonly Connection? _activeConnection;

    /// <summary>
    /// Gets the callback used to observe incoming user requests.
    /// </summary>
    public UserRequestCallBack _userRequest;

    private readonly ListWindow.ListWindow _window;

    /// <summary>
    /// Gets the command used to leave the current session.
    /// </summary>
    public ReactiveCommand<Unit, Unit> RequestLeave { get; }

    /// <summary>
    /// Gets the collection of peers currently shown in the UI.
    /// </summary>
    public ObservableCollection<Row> RemotePeers { get; } = new();

    private bool _isPopupOpen;
    private string _incomingRequestSender = "";

    /// <summary>
    /// Gets or sets a value indicating whether the incoming request popup is visible.
    /// </summary>
    public bool IsPopupOpen
    {
        get => _isPopupOpen;
        set => this.RaiseAndSetIfChanged(ref _isPopupOpen, value);
    }

    /// <summary>
    /// Gets the sender address shown in the incoming request popup.
    /// </summary>
    public string IncomingRequestSender
    {
        get => _incomingRequestSender;
        private set => this.RaiseAndSetIfChanged(ref _incomingRequestSender, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListWindowViewModel"/> class.
    /// </summary>
    /// <param name="activeConnection">The active connection to the network.</param>
    /// <param name="window">The UI window associated with the view model.</param>
    /// <param name="userRequest">The callback used for inbound connection requests.</param>
    public ListWindowViewModel(Connection? activeConnection, ListWindow.ListWindow window, UserRequestCallBack userRequest)
    {
        _activeConnection = activeConnection;
        _window = window;
        _userRequest = userRequest;

        // Notify UI immediately when an incoming connect request arrives
        _userRequest.OnIncomingRequest = (msg) =>
        {
            // Ensure running on UI thread
            Dispatcher.UIThread.Post(() => popup(msg));
        };

        RequestLeave = ReactiveCommand.CreateFromTask(async () =>
        {
            _activeConnection.Writer.AddTask(new ProtocolMessage(MessageType.Disconnect));
            _activeConnection.Writer.CompleteQueue();
            await _activeConnection.Writer.CompleteCallBack();
            _window.Exit();
        });
    }

    /// <summary>
    /// Removes a peer entry matching the supplied IP and port.
    /// </summary>
    /// <param name="ip">The peer IP address.</param>
    /// <param name="port">The peer port number.</param>
    /// <returns><c>true</c> if the row was found and removed; otherwise, <c>false</c>.</returns>
    public bool RemoveEntry(string ip, int port)
    {
        LookUp(ip, port, out var remotePeer);
        if (remotePeer == null) return false;
        RemotePeers.Remove(remotePeer);
        return true;
    }

    /// <summary>
    /// Looks up a peer row by IP and port.
    /// </summary>
    /// <param name="ip">The peer IP address.</param>
    /// <param name="port">The peer port number.</param>
    /// <param name="row">The matching peer row if found.</param>
    /// <returns><c>true</c> if a matching peer row was found; otherwise, <c>false</c>.</returns>
    public bool LookUp(string ip, int port, out Row? row)
    {
        row = RemotePeers.FirstOrDefault(row => row.Ip == ip && row.Port == port);
        if (row == null) return false;
        return true;
    }

    /// <summary>
    /// Adds a new peer row to the remote peer collection.
    /// </summary>
    /// <param name="ip">The peer IP address.</param>
    /// <param name="port">The peer port number.</param>
    public void AddEntry(string ip, int port)
    {
        RemotePeers.Add(new Row(ip, port, _activeConnection, this));
    }

    /// <summary>
    /// Replaces the displayed peer list with a new set of serialized entries.
    /// </summary>
    /// <param name="entries">A list of IP:port strings.</param>
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

            RemotePeers.Add(new Row(parts[0], port, _activeConnection, this));
        }
    }

    /// <summary>
    /// Waits for an incoming connection request and displays the popup to the user.
    /// </summary>
    /// <returns>The completed protocol message.</returns>
    public async Task<ProtocolMessage?> ConnectionRequest()
    {
        Console.WriteLine("before callback");

        ProtocolMessage msg = await _userRequest._awaitingMessage.Task;
        Console.WriteLine("after callback");
        popup(msg);

        return null;
    }

    /// <summary>
    /// Displays an incoming peer request in the popup UI.
    /// </summary>
    /// <param name="message">The protocol message containing the requester details.</param>
    private void popup(ProtocolMessage message)
    {
        IncomingRequestSender = System.Text.Encoding.UTF8.GetString(message.Body);
        IsPopupOpen = true;
    }
}

/// <summary>
/// Represents a single remote peer row in the peer list UI.
/// </summary>
public class Row : ReactiveObject
{
    private readonly ListWindowViewModel _parent;
    private string _buttonText = "Request Connect";
    private bool _requestPending;

    /// <summary>
    /// Gets the peer IP address represented by this row.
    /// </summary>
    public string Ip { get; }

    /// <summary>
    /// Gets the peer port represented by this row.
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// Gets the display text for the connect button.
    /// </summary>
    public string ButtonText
    {
        get => _buttonText;
        private set => this.RaiseAndSetIfChanged(ref _buttonText, value);
    }

    /// <summary>
    /// Gets the command used to request a connection to the peer.
    /// </summary>
    public ReactiveCommand<Unit, Unit> RequestConnectCommand { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Row"/> class.
    /// </summary>
    /// <param name="ip">The peer IP address.</param>
    /// <param name="port">The peer port number.</param>
    /// <param name="activeConnection">The active client connection.</param>
    /// <param name="parent">The parent view model that owns the row.</param>
    public Row(string ip, int port, Connection? activeConnection, ListWindowViewModel parent)
    {
        Ip = ip;
        Port = port;
        _parent = parent;

        RequestConnectCommand = ReactiveCommand.Create(() =>
        {
            if (_requestPending)
                return;

            _requestPending = true;
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
            activeConnection.Writer.AddTask(new ProtocolMessage(MessageType.ConnectToUser, System.Text.Encoding.UTF8.GetBytes(temp)));
            _ = _parent.ConnectionRequest();
        });
    }
}