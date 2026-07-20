using System;
using Avalonia;
using ReactiveUI;
using Microsoft.Extensions.Logging;
using client_core.core;
using System.Threading.Tasks;

namespace client_ui.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    
    private string _ipAddress = "";
    private string _portNumber = "";
    
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
        
        Connection connection;
        try
        {
            connection = await Connector.Connect(IpAddress, port, LoggerSingleton._instance);
        }
        catch (TimeoutException e)
        {
            LoggerSingleton._instance.LogError("Timed out of server connection {}", e.Message);
            return false;
        }
        catch (Exception e)
        {
            LoggerSingleton._instance.LogError("Connecting to invalid server {}",e.Message);
            return false;
        }
        LoggerSingleton._instance.LogInformation("Connected to server!!!");
        LoggerSingleton._instance.LogInformation("Lifetime: {Name}", Application.Current?.ApplicationLifetime?.GetType().Name);
        return true;
    }
}
