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
        Console.WriteLine($"IP: {IpAddress}, Port: {PortNumber}");
        Console.WriteLine($"Lifetime: {Application.Current?.ApplicationLifetime?.GetType().Name}");
    }
}
