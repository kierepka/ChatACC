using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ChatAAC.Views;
using ReactiveUI;
using Avalonia.ReactiveUI;
using ChatAAC.ViewModels;

namespace ChatAAC;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        // Konfiguracja ReactiveUI Scheduler
        RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    private void OpenConfigWindow()
    {
        var configWindow = new ConfigWindow()
        {
            DataContext = new ConfigViewModel()
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
        var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
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