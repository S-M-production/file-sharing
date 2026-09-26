using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace client_ui;

/// <summary>
/// Represents the Avalonia application entry point for the client UI.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Loads the application XAML resources.
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Creates the main application window after the framework has finished initializing.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow.MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}