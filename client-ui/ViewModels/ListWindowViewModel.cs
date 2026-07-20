using Avalonia;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Collections.ObjectModel;

namespace client_ui.ViewModels;

public class ListWindowViewModel : ReactiveObject
{
    public ObservableCollection<Row> RemotePeers { get; } = new();

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

            RemotePeers.Add(new Row(parts[0], port));
        }
    }
}

public class Row
{
    public string Ip { get; }
    public int Port { get; }
    public ReactiveCommand<Unit, Unit> RequestConnectCommand { get; }
    public Row(string ip, int port)
    {
        Ip = ip;
        Port = port;
        RequestConnectCommand = ReactiveCommand.Create(() =>
        {
            try
            {
                Console.WriteLine($"Requesting connection to {ip}:{port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Row command exception" + ex);
                throw;
            }
        });
    }
}