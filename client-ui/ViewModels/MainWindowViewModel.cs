using System;
using Avalonia;
using ReactiveUI;

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

        Console.WriteLine($"Lifetime: {Application.Current?.ApplicationLifetime?.GetType().Name}");
    }
}
