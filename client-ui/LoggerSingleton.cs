using Microsoft.Extensions.Logging;

namespace client_ui;
public class LoggerSingleton
{
    public ILogger _instance{ get; private set; }
    public static LoggerSingleton Instance { get; } = new LoggerSingleton();
    private LoggerSingleton()
    {
        ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        _instance = factory.CreateLogger<Program>();
    }

}