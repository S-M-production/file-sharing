using System;
using Microsoft.Extensions.Logging;

namespace client_ui;
public class LoggerSingleton
{
    private static ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
    public static ILogger _instance{ get; private set; } = factory.CreateLogger<Program>();
    
}