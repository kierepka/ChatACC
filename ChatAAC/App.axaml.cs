using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ChatAAC.ViewModels;
using ChatAAC.Views;
using ReactiveUI;
using System.Globalization;

namespace ChatAAC;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        // Configure the ReactiveUI Scheduler
        RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Setting the culture to the current system culture
        Lang.Resources.Culture = CultureInfo.CurrentCulture;
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };

        base.OnFrameworkInitializationCompleted();
    }

    private void OpenConfigWindow()
    {
        var configWindow = new ConfigWindow
        {
            DataContext = ConfigViewModel.Instance
        };
        var mainWindow = (Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
            ?.MainWindow;
        if (mainWindow != null)
            configWindow.ShowDialog(mainWindow);
        else
            configWindow.Show();
    }

    private void OpenAboutWindow()
    {
        var aboutWindow = new AboutWindow
        {
            DataContext = new AboutViewModel()
        };
        var mainWindow = (Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
            ?.MainWindow;
        if (mainWindow != null)
            aboutWindow.ShowDialog(mainWindow);
        else
            aboutWindow.Show();
    }

    private void OnAboutClick(object? sender, EventArgs e)
    {
        OpenAboutWindow();
    }

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        OpenConfigWindow();
    }
}