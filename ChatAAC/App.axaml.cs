using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ChatAAC.Models.Obf;
using ChatAAC.Services;
using ChatAAC.ViewModels;
using ChatAAC.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace ChatAAC;

public class App : Application
{
    public override void Initialize()
    {
        // Load the XAML defined for this application
        AvaloniaXamlLoader.Load(this);

        // Set the default ReactiveUI Scheduler to Avalonia's scheduler
        RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Set the current culture to the system's culture
        Lang.Resources.Culture = CultureInfo.CurrentCulture;

        // Możesz użyć ServiceProvider do utworzenia MainViewModel
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        // Check if the application is running in Classic Desktop mode
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow
            {
                DataContext = serviceProvider.GetRequiredService<MainViewModel>()
            };

        base.OnFrameworkInitializationCompleted();

    }
    private void ConfigureServices(IServiceCollection services)
    {
        // Konfiguracja usług
        services.AddLogging(configure => 
        {
            configure.AddConsole();
            configure.AddDebug();
            configure.SetMinimumLevel(LogLevel.Information);
        });

        // Dodaj Logger Factory
        services.AddSingleton(LoggerFactory.Create(builder => 
        {
            builder
                .AddConsole()
                .AddDebug()
                .SetMinimumLevel(LogLevel.Information);
        }));

        // Dodaj logger dla MainViewModel
        services.AddSingleton(typeof(ILogger<MainViewModel>), 
            sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger<MainViewModel>());

        // Dodaj MainViewModel jako usługę
        services.AddTransient<MainViewModel>();
        services.AddTransient<BoardLoaderService>();
        services.AddSingleton<HistoryService>();
        services.AddSingleton<ObfLoader>();
       




        // Dodaj pozostałe wymagane usługi
        // np. services.AddSingleton<IDialogService, DialogService>();
    }

    /// <summary>
    ///     Opens the configuration window (ConfigWindow).
    ///     If MainWindow exists, displays it as a dialog; otherwise, as a standalone window.
    /// </summary>
    private static void OpenConfigWindow()
    {
        var configWindow = new ConfigWindow
        {
            DataContext = ConfigViewModel.Instance
        };
        ShowWindowAsDialogIfPossible(configWindow);
    }

    /// <summary>
    ///     Opens the "AboutWindow".
    ///     If MainWindow exists, displays it as a dialog; otherwise, as a standalone window.
    /// </summary>
    private static void OpenAboutWindow()
    {
        var aboutWindow = new AboutWindow
        {
            DataContext = new AboutViewModel()
        };
        ShowWindowAsDialogIfPossible(aboutWindow);
    }

    /// <summary>
    ///     Shows the provided window as a dialog if MainWindow exists; otherwise, as a normal window.
    /// </summary>
    /// <param name="window">The window instance to display.</param>
    private static void ShowWindowAsDialogIfPossible(Window window)
    {
        var mainWindow = (Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
            ?.MainWindow;

        if (mainWindow != null)
            window.ShowDialog(mainWindow);
        else
            window.Show();
    }

    /// <summary>
    ///     Event handler for the "About" menu/button click.
    /// </summary>
    private void OnAboutClick(object? sender, EventArgs e)
    {
        OpenAboutWindow();
    }

    /// <summary>
    ///     Event handler for the "Settings" menu/button click.
    /// </summary>
    private void OnSettingsClick(object? sender, EventArgs e)
    {
        OpenConfigWindow();
        RefreshMainWindow();
    }

    /// <summary>
    ///     Example method to demonstrate one approach to refresh MainWindow
    ///     after changing the Lang.Resources.Culture.
    /// </summary>
    /// <remarks>
    ///     This approach can be called after updating the culture
    ///     (e.g., from Polish to English) to reload or rebind localizable
    ///     text resources in the MainWindow.
    /// </remarks>
    private static void RefreshMainWindow()
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop)
            // Option A: Force the window to redraw itself
            desktop.MainWindow.InvalidateVisual();
        // Option B: If your window's text blocks or controls 
        // are bound to localizable properties, simply raising 
        // property changed notifications in the ViewModel 
        // might be enough.
        // Option C (more drastic): Recreate or reload the window's XAML.
        // However, this can cause the window to lose runtime state:
        // AvaloniaXamlLoader.Load(desktop.MainWindow);
    }
}