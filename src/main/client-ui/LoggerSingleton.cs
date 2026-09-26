using System;
using Microsoft.Extensions.Logging;

namespace client_ui;

/// <summary>
/// Provides a shared application logger instance for the client UI.
/// </summary>
public class LoggerSingleton
{
    private static ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());

    /// <summary>
    /// Gets the shared logger instance used across the client application.
    /// </summary>
    public static ILogger _instance { get; private set; } = factory.CreateLogger<Program>();
}