using System;
using System.Diagnostics;
using Avalonia;
using ReactiveUI;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using client_core.middleware;
using network_core.core;
using router_core.core;
using client_core.logic;

namespace client_ui.ViewModels;

public class MainWindowViewModel : ReactiveObject
{

    private string _ipAddress = "";
    private string _portNumber = "";
    private Connection? _connection;
    private Middleware _middleware;

    public RouterMap temp = new RouterMap();

    public UserRequestCallBack caller;

    public string IpAddress
    {
        get => _ipAddress;
        set => this.RaiseAndSetIfChanged(ref _ipAddress, value);
    }

    public string PortNumber
    {
        get => _portNumber;
        set => this.RaiseAndSetIfChanged(ref _portNumber, value);
    }

    public Connection? ActiveConnection
    {
        get => _connection;
        private set => this.RaiseAndSetIfChanged(ref _connection, value);
    }

    public async Task<bool> OnButtonPressed()
    {
        Console.WriteLine("Button Pressed!");
        int port;
        try
        {
            port = int.Parse(PortNumber);
            Console.WriteLine($"IP: {IpAddress}, Port: {port}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"IP: {IpAddress}, Port: invalid ({PortNumber})");
            return false;
        }


        try
        {
            caller = new UserRequestCallBack();
            temp.AddRoute(format.core.MessageType.ConnectToUser, caller.UserRequestCall);
            //TODO: Initializr a middleware inside the class and store it for more distribution and object lifecycle stuff
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var connection = await Connector.Connect(IpAddress, port, LoggerSingleton._instance, new Middleware(), temp);
            stopwatch.Stop();
            
            if (connection == null)
            {
                LoggerSingleton._instance.LogError("Failed to connect to server");
                return false;
            }
            
            LoggerSingleton._instance.LogInformation("Connection established in {Time} ms", stopwatch.ElapsedMilliseconds);
            connection!.Start();
            LoggerSingleton._instance.LogInformation("Connected to server!!!");
            ActiveConnection = connection;
        }
        catch (OperationCanceledException e)
        {
            LoggerSingleton._instance.LogError("Timed out of server connection {}", e.Message);
            return false;
        }
        catch (Exception e)
        {
            LoggerSingleton._instance.LogError("Connecting to invalid server {}",e.Message);
            LoggerSingleton._instance.LogCritical(e.StackTrace);
            return false;
        }
        LoggerSingleton._instance.LogInformation("Connected to server!!!");
        LoggerSingleton._instance.LogInformation("Lifetime: {Name}", Application.Current?.ApplicationLifetime?.GetType().Name);
        return true;
    }
}