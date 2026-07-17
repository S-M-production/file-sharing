using System;
using Avalonia;
using ReactiveUI;
using Microsoft.Extensions.Logging;
using client_core.core;
using client_ui;
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

    public void OnButtonPressed()
    {

        Console.WriteLine("Button Pressed!");

        if (int.TryParse(PortNumber, out var port))
        {
            Console.WriteLine($"IP: {IpAddress}, Port: {port}");
        }
        else
        {
            Console.WriteLine($"IP: {IpAddress}, Port: invalid ({PortNumber})");
        }

        Connector connector;
        try
        {
            connector = new Connector(IpAddress, int.Parse(PortNumber), LoggerSingleton.Instance._instance);
        }
        catch (Exception e)
        {
            LoggerSingleton.Instance._instance.LogError(e, "Failed to connect to server");
            return;
        }

        Connection? connection = connector.Connect().Result;
        if(connection == null)
        {
            Console.WriteLine("Not connected");
            return;
        }
        
        Console.WriteLine($"Lifetime: {Application.Current?.ApplicationLifetime?.GetType().Name}");
    }
}
